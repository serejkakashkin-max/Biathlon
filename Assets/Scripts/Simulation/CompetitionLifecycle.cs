using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum CompetitionPhase { Prestart = 0, Skiing = 1, Range = 2, Penalty = 3, Finished = 4, Classified = 5, DidNotStart = 6, DidNotFinish = 7, Invalid = 8 }
    public enum CompetitionAction { None = 0, Start = 1, ReachLapBoundary = 2, CompleteBout = 3, CompletePenaltyLoop = 4, Finish = 5, Classify = 6, MarkDidNotStart = 7, MarkDidNotFinish = 8, MarkInvalid = 9, EnterRange = 10 }
    public readonly struct CompetitionReceipt
    {
        public CompetitionAction Action { get; } public int ClosedMask { get; } public int CompletedShots { get; } public FinishTime Finish { get; }
        public CompetitionReceipt(CompetitionAction action, int closedMask = 0, int completedShots = 0, FinishTime finish = default)
        { Action = action; ClosedMask = closedMask; CompletedShots = completedShots; Finish = finish; }
    }
    public readonly struct CompetitionFrame
    {
        public SimulationTick Tick { get; } public CompetitionPhase Phase { get; }
        public int CompletedLaps { get; } public int CompletedBouts { get; } public int PendingPenaltyLoops { get; }
        public double ElapsedSeconds { get; }
        public CompetitionFrame(SimulationTick tick, CompetitionPhase phase, int laps, int bouts, int penalties, double elapsed)
        { Tick = tick; Phase = phase; CompletedLaps = laps; CompletedBouts = bouts; PendingPenaltyLoops = penalties; ElapsedSeconds = elapsed; }
    }
    /// <summary>Accepts trusted spatial/bout receipts. Range arrival precedes the lap endpoint; neither substitutes for the other.</summary>
    public sealed class CompetitionLifecycle
    {
        private readonly CompetitionProfile profile;
        private readonly AthleteId athlete;
        private readonly int frequency;
        private readonly IRaceEventSink events;
        public string CompetitionProfileId => profile.Id;
        public AthleteId AthleteId => athlete;
        public int TickFrequency => frequency;
        public string CompetitionProfileJson { get; }
        public SimulationTick Tick { get; private set; }
        public SimulationTick StartTick { get; private set; }
        public CompetitionPhase Phase { get; private set; }
        public int CompletedLaps { get; private set; }
        public int CompletedBouts { get; private set; }
        public int PendingPenaltyLoops { get; private set; }
        public FinishTime GlobalFinishTime { get; private set; }
        public FinishTime ElapsedFinishTime { get; private set; }
        public CompetitionFrame LastFrame { get; private set; }
        private bool started, finished, previewing;
        private readonly struct Snapshot
        {
            private readonly SimulationTick tick, start;
            private readonly CompetitionPhase phase;
            private readonly int laps, bouts, penalties;
            private readonly FinishTime global, elapsed;
            private readonly CompetitionFrame frame;
            private readonly bool started, finished;
            public Snapshot(CompetitionLifecycle s)
            { tick = s.Tick; start = s.StartTick; phase = s.Phase; laps = s.CompletedLaps; bouts = s.CompletedBouts; penalties = s.PendingPenaltyLoops;
              global = s.GlobalFinishTime; elapsed = s.ElapsedFinishTime; frame = s.LastFrame; started = s.started; finished = s.finished; }
            public void Restore(CompetitionLifecycle s)
            { s.Tick = tick; s.StartTick = start; s.Phase = phase; s.CompletedLaps = laps; s.CompletedBouts = bouts; s.PendingPenaltyLoops = penalties;
              s.GlobalFinishTime = global; s.ElapsedFinishTime = elapsed; s.LastFrame = frame; s.started = started; s.finished = finished; }
        }
        /// <summary>Ordered spatial receipts within one fixed tick; validates the complete batch before any event is emitted.</summary>
        public CompetitionFrame StepBatch(SimulationTick tick, ReadOnlySpan<CompetitionReceipt> receipts)
        {
            if (receipts.Length == 0) return Step(tick);
            var original = new Snapshot(this); var previousTick = Tick;
            previewing = true;
            try { ApplyBatch(tick, previousTick, receipts); }
            finally { original.Restore(this); previewing = false; }
            ApplyBatch(tick, previousTick, receipts); return LastFrame;
        }
        private void ApplyBatch(SimulationTick tick, SimulationTick previous, ReadOnlySpan<CompetitionReceipt> receipts)
        {
            for (int i = 0; i < receipts.Length; i++)
            {
                Tick = previous; var r = receipts[i]; Step(tick, r.Action, r.ClosedMask, r.CompletedShots, r.Finish);
            }
        }
        public CompetitionLifecycle(CompetitionProfile profile, AthleteId athlete, int frequency, IRaceEventSink events)
        {
            this.profile = profile ?? throw new ArgumentNullException(nameof(profile));
            CompetitionProfileJson = profile.ToJson();
            if (!athlete.IsValid || frequency <= 0) throw new ArgumentException("Invalid competition identity/clock.");
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.athlete = athlete; this.frequency = frequency;
        }
        public CompetitionFrame Step(SimulationTick tick, CompetitionAction action = CompetitionAction.None,
            int closedTargetMask = 0, int completedShots = 0, FinishTime finish = default)
        {
            Validate(tick, action, closedTargetMask, completedShots, finish);
            bool wasTerminal = Phase == CompetitionPhase.DidNotFinish || Phase == CompetitionPhase.Invalid;
            Tick = tick;
            switch (action)
            {
                case CompetitionAction.Start:
                    started = true; StartTick = tick; Change(CompetitionPhase.Skiing); Record(RaceEventType.CompetitionStarted, 0); break;
                case CompetitionAction.ReachLapBoundary:
                    CompletedLaps++; Record(RaceEventType.LapCompleted, CompletedLaps); break;
                case CompetitionAction.EnterRange: Change(CompetitionPhase.Range); break;
                case CompetitionAction.CompleteBout:
                    int hits = 0; for (int i = 0; i < TargetGeometryDefinition.TargetCount; i++) if ((closedTargetMask & (1 << i)) != 0) hits++;
                    CompletedBouts++; PendingPenaltyLoops = profile.ShotsPerBout - hits;
                    Record(RaceEventType.ShootingBoutCompleted, PendingPenaltyLoops);
                    Change(PendingPenaltyLoops > 0 ? CompetitionPhase.Penalty : CompetitionPhase.Skiing); break;
                case CompetitionAction.CompletePenaltyLoop:
                    PendingPenaltyLoops--; Record(RaceEventType.PenaltyLoopCompleted, PendingPenaltyLoops);
                    if (PendingPenaltyLoops == 0) Change(CompetitionPhase.Skiing); break;
                case CompetitionAction.Finish:
                    CompletedLaps++; finished = true; GlobalFinishTime = finish;
                    ElapsedFinishTime = new FinishTime(finish.WholeTicks - StartTick.Value, finish.FractionParts, frequency);
                    Record(RaceEventType.LapCompleted, CompletedLaps); Change(CompetitionPhase.Finished); Record(RaceEventType.RaceFinished, 0); break;
                case CompetitionAction.Classify: Change(CompetitionPhase.Classified); break;
                case CompetitionAction.MarkDidNotStart: Change(CompetitionPhase.DidNotStart); break;
                case CompetitionAction.MarkDidNotFinish: Change(CompetitionPhase.DidNotFinish); break;
                case CompetitionAction.MarkInvalid: Change(CompetitionPhase.Invalid); break;
            }
            double elapsed = finished ? ElapsedFinishTime.Seconds : !started ? 0 :
                wasTerminal ? LastFrame.ElapsedSeconds : (Tick.Value - StartTick.Value) / (double)frequency;
            LastFrame = new CompetitionFrame(tick, Phase, CompletedLaps, CompletedBouts, PendingPenaltyLoops, elapsed);
            return LastFrame;
        }
        private void Validate(SimulationTick tick, CompetitionAction action, int mask, int shots, FinishTime finish)
        {
            if (tick.Value != checked(Tick.Value + 1)) throw new ArgumentException("Competition ticks must be consecutive.");
            if (action < CompetitionAction.None || action > CompetitionAction.EnterRange) throw new ArgumentOutOfRangeException(nameof(action));
            if (action != CompetitionAction.CompleteBout && (mask != 0 || shots != 0)) throw new ArgumentException("Unexpected shooting outcome.");
            if (action != CompetitionAction.Finish && finish.TickFrequency != 0) throw new ArgumentException("Unexpected finish timestamp.");
            if (action == CompetitionAction.None) return;
            bool active = Phase == CompetitionPhase.Skiing || Phase == CompetitionPhase.Range || Phase == CompetitionPhase.Penalty;
            switch (action)
            {
                case CompetitionAction.Start: case CompetitionAction.MarkDidNotStart:
                    if (Phase != CompetitionPhase.Prestart) throw new InvalidOperationException("Athlete already left prestart."); break;
                case CompetitionAction.ReachLapBoundary:
                    if (Phase != CompetitionPhase.Skiing || CompletedLaps >= profile.LapCount - 1 || CompletedBouts != CompletedLaps + 1)
                        throw new InvalidOperationException("Cannot bypass a lap/bout/penalty."); break;
                case CompetitionAction.EnterRange:
                    if (Phase != CompetitionPhase.Skiing || CompletedBouts >= profile.BoutCount || CompletedBouts != CompletedLaps ||
                        profile.GetBout(CompletedBouts).AfterLap != CompletedLaps + 1)
                        throw new InvalidOperationException("Range arrival requires the next scheduled shooting lap."); break;
                case CompetitionAction.CompleteBout:
                    if (Phase != CompetitionPhase.Range || CompletedBouts != CompletedLaps || shots != profile.ShotsPerBout || mask < 0 || mask >= (1 << TargetGeometryDefinition.TargetCount))
                        throw new ArgumentException("Complete ordered bout and valid target mask required."); break;
                case CompetitionAction.CompletePenaltyLoop:
                    if (Phase != CompetitionPhase.Penalty || PendingPenaltyLoops <= 0) throw new InvalidOperationException("No penalty traversal due."); break;
                case CompetitionAction.Finish:
                    if (Phase != CompetitionPhase.Skiing || CompletedLaps != profile.LapCount - 1 || CompletedBouts != profile.BoutCount || PendingPenaltyLoops != 0)
                        throw new InvalidOperationException("Race obligations are incomplete.");
                    if (finish.TickFrequency != frequency || finish.WholeTicks < StartTick.Value ||
                        !(finish.WholeTicks == tick.Value - 1 || (finish.WholeTicks == tick.Value && finish.FractionParts == 0)))
                        throw new ArgumentException("Finish timestamp must lie in the current tick interval after start."); break;
                case CompetitionAction.Classify:
                    if (Phase != CompetitionPhase.Finished) throw new InvalidOperationException("Only a finisher can be classified."); break;
                case CompetitionAction.MarkDidNotFinish:
                    if (!active) throw new InvalidOperationException("DNF requires an active race."); break;
                case CompetitionAction.MarkInvalid:
                    if (Phase == CompetitionPhase.Invalid || Phase == CompetitionPhase.DidNotStart || Phase == CompetitionPhase.DidNotFinish)
                        throw new InvalidOperationException("Terminal result already recorded."); break;
            }
        }
        private void Change(CompetitionPhase phase) { Phase = phase; Record(RaceEventType.CompetitionPhaseChanged, (int)phase); }
        private void Record(RaceEventType type, int payload)
        { if (!previewing) events.Record(new RaceEvent(Tick, frequency, athlete, type, payload)); }
    }
}

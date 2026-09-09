using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ShootingPhase { Outside = 0, AwaitingLane = 1, Setup = 2, Aiming = 3, BoltCycle = 4, Exiting = 5, Complete = 6 }
    public enum ShootingAction { None = 0, EnterRange = 1, AssignLane = 2, Trigger = 3 }
    public readonly struct ShootingFrame
    {
        public SimulationTick Tick { get; }
        public ShootingPhase Phase { get; }
        public int ShotsFired { get; }
        public bool ShotAccepted { get; }
        public long ReadyTick { get; }
        public ShootingFrame(SimulationTick tick, ShootingPhase phase, int shots, bool accepted, long ready)
        { Tick = tick; Phase = phase; ShotsFired = shots; ShotAccepted = accepted; ReadyTick = ready; }
    }
    /// <summary>One bout. Lane exclusivity is guaranteed by the race coordinator, not object names or this athlete-local machine.</summary>
    public sealed class ShootingStateMachine
    {
        private readonly ShootingDefinition definition;
        private readonly AthleteId athlete;
        private readonly string rangeId;
        private readonly int frequency, setupTicks, boltTicks, exitTicks;
        private readonly IRaceEventSink events;
        private long tick, ready;
        public ShootingPhase Phase { get; private set; }
        public ShootingLaneAssignment Lane { get; private set; }
        public int ShotsFired { get; private set; }
        public ShootingFrame LastFrame { get; private set; }
        public ShootingStateMachine(ShootingDefinition definition, AthleteId athlete, string rangeId, int frequency,
            IRaceEventSink events, SimulationTick initialTick = default, ShootingStanceProfile stance = null, ShootingHandlingProfile? handling = null)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!athlete.IsValid || string.IsNullOrWhiteSpace(rangeId) || frequency <= 0 || frequency > 10000)
                throw new ArgumentException("Invalid shooting identity/clock.");
            this.athlete = athlete; this.rangeId = rangeId; this.frequency = frequency;
            this.events = events ?? throw new ArgumentNullException(nameof(events)); tick = initialTick.Value;
            if (handling.HasValue && !handling.Value.IsValid) throw new ArgumentException("Invalid handling profile.");
            double handlingTime = handling?.TimeMultiplier ?? 1;
            setupTicks = (int)Math.Ceiling(definition.SetupSeconds * frequency * (stance?.SetupMultiplier ?? 1) * handlingTime);
            boltTicks = (int)Math.Ceiling(definition.BoltSeconds * frequency * handlingTime);
            exitTicks = (int)Math.Ceiling(definition.ExitSeconds * frequency * (stance?.ExitMultiplier ?? 1));
        }
        public ShootingFrame Step(SimulationTick target, ShootingAction action = ShootingAction.None, ShootingLaneAssignment lane = default)
        {
            ValidateCommand(target, action, lane);
            long setupReady = checked(target.Value + setupTicks), boltReady = checked(target.Value + boltTicks), exitReady = checked(target.Value + exitTicks);
            tick = target.Value;
            if ((Phase == ShootingPhase.Setup || Phase == ShootingPhase.BoltCycle) && tick >= ready) Change(ShootingPhase.Aiming);
            if (Phase == ShootingPhase.Exiting && tick >= ready) Change(ShootingPhase.Complete);
            bool accepted = false;
            if (action == ShootingAction.EnterRange) Change(ShootingPhase.AwaitingLane);
            else if (action == ShootingAction.AssignLane)
            {
                Lane = lane; Record(RaceEventType.ShootingLaneAssigned, lane.Ordinal);
                ready = setupReady; Change(ShootingPhase.Setup);
            }
            else if (action == ShootingAction.Trigger && Phase == ShootingPhase.Aiming)
            {
                accepted = true; ShotsFired++; Record(RaceEventType.ShotAccepted, ShotsFired);
                ready = ShotsFired == definition.Rounds ? exitReady : boltReady;
                Change(ShotsFired == definition.Rounds ? ShootingPhase.Exiting : ShootingPhase.BoltCycle);
            }
            LastFrame = new ShootingFrame(target, Phase, ShotsFired, accepted, ready);
            return LastFrame;
        }
        internal void ValidateCommand(SimulationTick target, ShootingAction action, ShootingLaneAssignment lane)
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Shooting ticks must be consecutive.");
            if (action < ShootingAction.None || action > ShootingAction.Trigger) throw new ArgumentOutOfRangeException(nameof(action));
            if (action == ShootingAction.EnterRange && Phase != ShootingPhase.Outside) throw new InvalidOperationException("Bout already entered.");
            if (action == ShootingAction.AssignLane && (Phase != ShootingPhase.AwaitingLane || !lane.IsValid || lane.RangeId != rangeId))
                throw new ArgumentException("Lane must belong to this range and be assigned while waiting.");
            if (action != ShootingAction.AssignLane && lane.IsValid) throw new ArgumentException("Unexpected lane assignment.");
            // Validate every possible deadline before state/event mutation, including very late clocks.
            _ = checked(target.Value + setupTicks); _ = checked(target.Value + boltTicks); _ = checked(target.Value + exitTicks);
        }
        private void Change(ShootingPhase phase) { Phase = phase; Record(RaceEventType.ShootingPhaseChanged, (int)phase); }
        private void Record(RaceEventType type, int payload) => events.Record(new RaceEvent(new SimulationTick(tick), frequency, athlete, type, payload));
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum OvertakePhase { Idle, MovingOut, Passing, Returning }
    public readonly struct OvertakeOutput
    {
        public OvertakePhase Phase { get; }
        public AthleteId TargetId { get; }
        public double DesiredLateralMeters { get; }
        public int EffectiveEffortControl { get; }
        public double PropulsionMultiplier { get; }
        public bool Blocked { get; }
        internal OvertakeOutput(OvertakePhase phase, AthleteId target, double lateral, int effort, double propulsion, bool blocked)
        { Phase = phase; TargetId = target; DesiredLateralMeters = lateral; EffectiveEffortControl = effort; PropulsionMultiplier = propulsion; Blocked = blocked; }
    }
    public sealed class OvertakeController
    {
        private readonly OvertakeDefinition definition;
        private readonly OccupancySystem occupancy;
        private readonly IRaceEventSink events;
        private readonly AthleteId athleteId;
        private readonly int frequency;
        private readonly double maximumLateralStep;
        private readonly long timeoutTicks;
        private double homeLane, passingLane;
        private long elapsed;
        private bool aborted;
        public SimulationTick Tick { get; private set; }
        public OvertakePhase Phase { get; private set; }
        public AthleteId TargetId { get; private set; }
        public OvertakeController(OvertakeDefinition definition, OccupancySystem occupancy, AthleteId athleteId,
            int tickFrequency, IRaceEventSink events)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.occupancy = occupancy ?? throw new ArgumentNullException(nameof(occupancy));
            this.events = events ?? throw new ArgumentNullException(nameof(events));
            if (!athleteId.IsValid || tickFrequency <= 0) throw new ArgumentException("Valid athlete and tick frequency required.");
            this.athleteId = athleteId; frequency = tickFrequency;
            maximumLateralStep = definition.LateralSpeed / frequency;
            if (maximumLateralStep < PhysiologyNumbers.MinimumPositive) throw new ArgumentException("Lateral step is below coordinate precision.");
            timeoutTicks = checked((long)Math.Ceiling(definition.TimeoutSeconds * frequency));
        }

        public OvertakeOutput Step(SimulationInput input, SkiPlacement current, TrackSample track,
            ReadOnlySpan<SkiPlacement> roster, bool request, bool cancel = false)
        {
            if (!input.AthleteId.Equals(athleteId) || !input.Tick.Equals(Tick.Next()) || !current.IsValid || !current.AthleteId.Equals(athleteId))
                throw new ArgumentException("Overtake input must target this athlete and next tick.");
            if (string.IsNullOrEmpty(track.SegmentId) || !occupancy.FitsWidth(current.LateralMeters, track.WidthMeters))
                throw new ArgumentException("Current placement must fit a valid track sample.");
            for (int i = 0; i < roster.Length; i++) if (!roster[i].IsValid) throw new ArgumentException("Invalid roster.");
            var phase = Phase; var targetId = TargetId; double home = homeLane, lane = passingLane;
            long duration = elapsed; bool wasAborted = aborted;
            bool started = false, completed = false, abortNow = false, blocked = false;
            var target = default(SkiPlacement);
            for (int i = 0; i < roster.Length; i++)
                if (roster[i].AthleteId.Equals(targetId) && roster[i].CorridorId == current.CorridorId) target = roster[i];
            if (phase == OvertakePhase.Idle && request && !cancel && track.OvertakeAllowed)
            {
                var opportunity = OvertakeOpportunityQuery.Evaluate(definition, occupancy, current, track.WidthMeters, track.OvertakeAllowed, roster);
                target = opportunity.Target;
                if (opportunity.Available)
                {
                    home = current.LateralMeters; lane = opportunity.PassingLaneMeters; targetId = target.AthleteId;
                    phase = OvertakePhase.MovingOut; duration = 0; wasAborted = false; started = true;
                }
                blocked = opportunity.Blocked;
            }
            if (phase != OvertakePhase.Idle)
            {
                duration = checked(duration + 1);
                if (!wasAborted && (cancel || duration > timeoutTicks || !target.IsValid || !track.OvertakeAllowed || !occupancy.FitsWidth(lane, track.WidthMeters)))
                { phase = OvertakePhase.Returning; wasAborted = true; abortNow = true; }
                if (phase == OvertakePhase.MovingOut && Math.Abs(current.LateralMeters - lane) <= PhysiologyNumbers.MinimumPositive)
                    phase = OvertakePhase.Passing;
                if (phase == OvertakePhase.Passing &&
                    (current.Motion.DistanceNanometers - target.Motion.DistanceNanometers) / (double)SkiState.NanometersPerMeter >= occupancy.Definition.LongitudinalSeparation + definition.PassingClearance)
                    phase = OvertakePhase.Returning;
                if (phase == OvertakePhase.Returning)
                {
                    double edge = track.WidthMeters * 0.5 - occupancy.Definition.EdgeMargin;
                    home = Math.Max(-edge, Math.Min(edge, home));
                }
                if (phase == OvertakePhase.Returning && Math.Abs(current.LateralMeters - home) <= PhysiologyNumbers.MinimumPositive)
                { completed = !wasAborted; phase = OvertakePhase.Idle; targetId = default; duration = 0; }
            }
            double desired = current.LateralMeters;
            if (phase != OvertakePhase.Idle)
            {
                double destination = phase == OvertakePhase.Returning ? home : lane;
                double delta = Math.Max(-maximumLateralStep, Math.Min(maximumLateralStep, destination - current.LateralMeters));
                double candidate = PhysiologyNumbers.Quantize(current.LateralMeters + delta);
                if (occupancy.QueryMove(current, At(current, current.Motion.DistanceNanometers, candidate), track.WidthMeters, roster).Blocked) blocked = true;
                else desired = candidate;
            }
            double movement = Math.Min(1, Math.Abs(desired - current.LateralMeters) / maximumLateralStep);
            int effort = Math.Min(AthleteState.EffortUnits, input.RequestedEffort + (int)Math.Ceiling(definition.LateralEffortSurcharge * movement));
            var output = new OvertakeOutput(phase, targetId, desired, effort, 1 - movement * definition.LateralPropulsionLoss, blocked);
            Tick = input.Tick; Phase = phase; TargetId = targetId; homeLane = home; passingLane = lane; elapsed = duration; aborted = wasAborted;
            if (started) Record(RaceEventType.OvertakeStarted, lane < home ? -1 : 1);
            if (abortNow) Record(RaceEventType.OvertakeAborted, 0);
            if (completed) Record(RaceEventType.OvertakeCompleted, 0);
            return output;
        }
        private static SkiPlacement At(SkiPlacement source, long distance, double lateral) =>
            new SkiPlacement(source.AthleteId, source.CorridorId, new SkiState(distance, source.Motion.SpeedMetersPerSecond), lateral);
        private void Record(RaceEventType type, int payload) => events.Record(new RaceEvent(Tick, frequency, athleteId, type, payload));
    }
}


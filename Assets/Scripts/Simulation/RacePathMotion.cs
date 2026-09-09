using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct RaceMotionSegment
    {
        public long RouteBefore { get; } public long RouteAfter { get; }
        public long TravelBefore { get; } public long TravelAfter { get; }
        public double StartSeconds { get; } public double EndSeconds { get; }
        public bool OnPenaltyLoop { get; }
        internal RaceMotionSegment(long rb, long ra, long tb, long ta, double start, double end, bool penalty)
        { RouteBefore = rb; RouteAfter = ra; TravelBefore = tb; TravelAfter = ta; StartSeconds = start; EndSeconds = end; OnPenaltyLoop = penalty; }
    }
    public readonly struct RacePathMotionFrame
    {
        public SkiState Motion { get; } public long RouteNanometers { get; } public long RangeTravelNanometers { get; }
        public double ConsumedSeconds { get; } public double RemainingSeconds { get; } public int SegmentCount { get; }
        public bool AtFiringLine { get; } public bool AtRouteStop { get; }
        internal RacePathMotionFrame(SkiState motion, long route, long rangeTravel, double consumed, double remaining, int count, bool firing, bool stop)
        { Motion = motion; RouteNanometers = route; RangeTravelNanometers = rangeTravel; ConsumedSeconds = consumed;
          RemainingSeconds = remaining; SegmentCount = count; AtFiringLine = firing; AtRouteStop = stop; }
    }
    /// <summary>Pure preview of one tick's movement. Caller owns physiology, path/lifecycle commit and replay events.</summary>
    public sealed class RacePathMotion
    {
        private readonly TrackDefinition track;
        private readonly RaceRoute route;
        private readonly TimeTrialAthlete athlete;
        private readonly SkiTechniqueSystem technique;
        private readonly CorneringSystem cornering;
        private readonly SnowConditionSystem snow;
        private readonly SkiEquipmentSystem equipment;
        private readonly SkiBoundaryStepper stepper;
        private readonly TrackSampler penaltySampler;
        private readonly double dt, maximumPower;
        private readonly long routeEnd;
        public RacePathMotion(SkiModelDefinitions definitions, TimeTrialAthlete athlete, TrackDefinition track, RaceRoute route,
            RaceSnowConditions conditions, string skiId, int frequency)
        {
            if (definitions == null || athlete == null || track == null || route == null) throw new ArgumentNullException();
            if (frequency <= 0 || route.TrackId != track.Id) throw new ArgumentException("Invalid path clock/track.");
            this.track = track; this.route = route; this.athlete = athlete; dt = 1.0 / frequency;
            maximumPower = definitions.Physics.MaximumPowerWatts;
            routeEnd = checked(route.Length.Millimeters * SkiState.NanometersPerMillimeter);
            technique = new SkiTechniqueSystem(definitions.Technique); cornering = new CorneringSystem(definitions.Cornering, definitions.Physics);
            snow = new SnowConditionSystem(definitions.Snow, conditions, track);
            equipment = new SkiEquipmentSystem(definitions.Equipment.GetSki(skiId), definitions.Snow);
            stepper = new SkiBoundaryStepper(definitions.Physics, frequency); penaltySampler = new TrackSampler(track.GetLoop(track.Penalty.LoopId));
            // Validate cold cross-definition terrain/equipment bindings, including the mandatory penalty surface.
            for (int i = 0; i < track.LoopCount; i++)
            {
                var loop = track.GetLoop(i); var sampler = new TrackSampler(loop);
                for (int j = 0; j < loop.SegmentCount; j++)
                {
                    var sample = sampler.Sample(loop.GetSegment(j).StartDistance);
                    var gear = equipment.Evaluate(snow.Sample(sample).Id);
                    cornering.Evaluate(default, sample, athlete.Cornering, athlete.Downhill, 1, 0, gear.CorneringGripMultiplier);
                    new SkiForceSystem(definitions.Physics, frequency).Step(default, 0, sample, snowResistanceMultiplier: gear.SnowResistanceMultiplier);
                }
            }
        }
        public RacePathMotionFrame Preview(SkiState initial, long initialRouteNanometers, long stopRouteNanometers,
            RangePenaltyTraversal range, double deliveredPower, double fatigue, TimeTrialInput input,
            Span<RaceMotionSegment> segments, double dragAreaMultiplier = 1)
        {
            if (initialRouteNanometers < 0 || stopRouteNanometers < initialRouteNanometers || stopRouteNanometers > routeEnd)
                throw new ArgumentException("Ordered route position/stop required.");
            if (!input.Effort.AthleteId.IsValid || input.Effort.AthleteId.Value != athlete.Id)
                throw new ArgumentException("Path input must identify the configured athlete.");
            PhysiologyNumbers.RequireRange(deliveredPower, 0, maximumPower, nameof(deliveredPower));
            PhysiologyNumbers.RequireRange(fatigue, 0, 1, nameof(fatigue));
            PhysiologyNumbers.RequireRange(dragAreaMultiplier, 0, 1, nameof(dragAreaMultiplier));
            if (range != null && range.RouteNanometers != initialRouteNanometers &&
                !(range.Phase == RangeTraversalPhase.Approach && range.TravelledNanometers == 0 && initialRouteNanometers < range.RangeEntryNanometers))
                throw new ArgumentException("Range/main coordinate mismatch.");
            long main = initialRouteNanometers, rangeTravel = 0; var motion = initial;
            double remaining = dt; int count = 0; bool firing = false;
            while (remaining > 0 && main < stopRouteNanometers)
            {
                var point = range?.PreviewMotion(rangeTravel) ?? default;
                bool inRange = range != null && main >= range.RangeEntryNanometers && point.Phase != RangeTraversalPhase.Complete;
                if (inRange && point.Phase == RangeTraversalPhase.Shooting) { firing = true; break; }
                bool penalty = inRange && point.Phase == RangeTraversalPhase.PenaltyLoop;
                TrackSample sample; long distance;
                if (penalty)
                {
                    sample = penaltySampler.Sample(new TrackDistance(point.PenaltyNanometers / SkiState.NanometersPerMillimeter));
                    var segment = track.GetLoop(track.Penalty.LoopId).GetSegment(sample.SegmentIndex);
                    distance = checked((segment.StartDistance.Millimeters + segment.Length.Millimeters) * SkiState.NanometersPerMillimeter) - point.PenaltyNanometers;
                }
                else
                {
                    var sampleRoute = route.Sample(new TrackDistance(main / SkiState.NanometersPerMillimeter)); sample = sampleRoute.Track;
                    var section = route.GetSection(sampleRoute.SectionIndex); var segment = track.GetLoop(section.LoopId).GetSegment(sample.SegmentIndex);
                    distance = checked((section.Start.Millimeters + segment.StartDistance.Millimeters + segment.Length.Millimeters) * SkiState.NanometersPerMillimeter) - main;
                    distance = Math.Min(distance, stopRouteNanometers - main);
                    if (range != null && main < range.RangeEntryNanometers) distance = Math.Min(distance, range.RangeEntryNanometers - main);
                }
                if (inRange) distance = Math.Min(distance, point.DistanceToBoundaryNanometers);
                if (distance <= 0) throw new InvalidOperationException("Non-forward path boundary.");
                if (count >= segments.Length) throw new ArgumentException("Motion segment output capacity is insufficient.");
                var gear = equipment.Evaluate(snow.Sample(sample).Id);
                var form = technique.Evaluate(athlete.TechniqueStats, sample, fatigue, deliveredPower, input.TechniqueId);
                var turn = cornering.Evaluate(motion, sample, athlete.Cornering, athlete.Downhill, input.LineQuality, fatigue, gear.CorneringGripMultiplier);
                var step = stepper.Step(motion, form.PropulsivePowerWatts * input.MovementEfficiency, sample, remaining,
                    checked(motion.DistanceNanometers + distance), input.HeadwindMetersPerSecond, dragAreaMultiplier, turn.BrakingNewtons, gear.SnowResistanceMultiplier);
                long moved = step.Forces.State.DistanceNanometers - motion.DistanceNanometers;
                long nextMain = penalty ? main : main + moved;
                segments[count++] = new RaceMotionSegment(main, nextMain, motion.DistanceNanometers, step.Forces.State.DistanceNanometers,
                    dt - remaining, dt - step.RemainingSeconds, penalty);
                motion = step.Forces.State; main = nextMain; remaining = step.RemainingSeconds;
                if (inRange) rangeTravel += moved;
            }
            // Reaching the firing boundary exactly at tick end still marks the stop for the next command.
            if (range != null && range.PreviewMotion(rangeTravel).Phase == RangeTraversalPhase.Shooting) firing = true;
            return new RacePathMotionFrame(motion, main, rangeTravel, dt - remaining, remaining, count, firing, main == stopRouteNanometers);
        }
    }
}

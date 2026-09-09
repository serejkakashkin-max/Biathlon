using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum ShotOutcome { NoShot = 0, Miss = 1, TargetClosed = 2, AlreadyClosed = 3 }
    public readonly struct ShotResolutionFrame
    {
        public ShootingFrame Lifecycle { get; } public TargetIntersection Impact { get; }
        public ShootingWindFrame Wind { get; } public ShotOutcome Outcome { get; } public int ClosedMask { get; }
        public ShotResolutionFrame(ShootingFrame lifecycle, TargetIntersection impact, ShootingWindFrame wind, ShotOutcome outcome, int closedMask)
        { Lifecycle = lifecycle; Impact = impact; Wind = wind; Outcome = outcome; ClosedMask = closedMask; }
    }
    public sealed class ShotResolutionSystem
    {
        private readonly ShootingStateMachine lifecycle;
        private readonly TargetGeometrySystem geometry;
        private readonly TargetGeometryDefinition targetDefinition;
        private readonly ShootingWindSystem windSystem;
        private readonly ShootingStance stance;
        private readonly AthleteId athlete;
        private readonly int frequency;
        private readonly IRaceEventSink events;
        private TargetBankState targets;
        public ShotResolutionFrame LastFrame { get; private set; }
        public ShootingPhase Phase => lifecycle.Phase;
        public int ShotsFired => lifecycle.ShotsFired;
        public int ClosedMask => targets.ClosedMask;
        internal void ValidateCommand(SimulationTick target, ShootingAction action, ShootingLaneAssignment lane) => lifecycle.ValidateCommand(target, action, lane);
        public ShotResolutionSystem(ShootingDefinition definition, TargetGeometryDefinition targetDefinition, ShootingWindDefinition windDefinition,
            ShootingStanceProfile stance, AthleteId athlete, string rangeId, int frequency, IRaceEventSink events, SimulationTick initialTick = default,
            ShootingHandlingProfile? handling = null)
        {
            this.targetDefinition = targetDefinition ?? throw new ArgumentNullException(nameof(targetDefinition));
            if (stance == null) throw new ArgumentNullException(nameof(stance));
            geometry = new TargetGeometrySystem(targetDefinition); windSystem = new ShootingWindSystem(windDefinition);
            lifecycle = new ShootingStateMachine(definition, athlete, rangeId, frequency, events, initialTick, stance, handling);
            this.stance = stance.Stance; this.athlete = athlete; this.frequency = frequency; this.events = events;
        }
        public ShotResolutionFrame Step(SimulationTick target, ShootingAction action = ShootingAction.None,
            ShootingLaneAssignment lane = default, ShotRay ray = default, WindVector wind = default, double headingRadians = 0, double exposure = 1)
        {
            TargetIntersection impact = default;
            // Wind is validated on every command; all geometric work precedes any state/event mutation.
            var windFrame = windSystem.Evaluate(wind, headingRadians, exposure, 0);
            if (action == ShootingAction.Trigger)
            {
                var straight = geometry.Intersect(ray, stance);
                if (straight.ReachesPlane)
                {
                    double dx = straight.X - ray.OriginX, dy = straight.Y - ray.OriginY, dz = targetDefinition.DistanceMeters - ray.OriginZ;
                    double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                    windFrame = windSystem.Evaluate(wind, headingRadians, exposure, distance);
                    impact = geometry.IntersectPlanePoint(straight.X + windFrame.HorizontalDriftMeters, straight.Y, stance);
                }
            }
            var frame = lifecycle.Step(target, action, lane);
            var outcome = ShotOutcome.NoShot;
            if (frame.ShotAccepted)
            {
                outcome = !impact.Hit ? ShotOutcome.Miss : targets.Close(impact.TargetIndex) ? ShotOutcome.TargetClosed : ShotOutcome.AlreadyClosed;
                // Positive = newly closed target (1..5), negative = already closed, zero = geometric miss.
                int payload = outcome == ShotOutcome.TargetClosed ? impact.TargetIndex + 1 : outcome == ShotOutcome.AlreadyClosed ? -(impact.TargetIndex + 1) : 0;
                events.Record(new RaceEvent(target, frequency, athlete, RaceEventType.ShotResolved, payload));
            }
            LastFrame = new ShotResolutionFrame(frame, frame.ShotAccepted ? impact : default, windFrame, outcome, targets.ClosedMask);
            return LastFrame;
        }
    }
}

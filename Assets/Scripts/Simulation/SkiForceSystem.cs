using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SkiForceOutput
    {
        public SkiState State { get; }
        public double PropulsiveNewtons { get; }
        public double GravityNewtons { get; }
        public double SnowNewtons { get; }
        public double AirDragNewtons { get; }
        public double BrakingNewtons { get; }
        public double NetNewtons => PropulsiveNewtons - GravityNewtons - SnowNewtons - AirDragNewtons - BrakingNewtons;
        public double AppliedAcceleration { get; }
        internal SkiForceOutput(SkiState state, double propulsion, double gravity, double snow, double air, double braking, double acceleration)
        { State = state; PropulsiveNewtons = propulsion; GravityNewtons = gravity; SnowNewtons = snow; AirDragNewtons = air; BrakingNewtons = braking; AppliedAcceleration = acceleration; }
    }
    public sealed class SkiForceSystem
    {
        private readonly SkiPhysicsDefinition definition;
        private readonly int frequency;
        private readonly double dt;
        public SkiForceSystem(SkiPhysicsDefinition definition, int tickFrequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (tickFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(tickFrequency));
            frequency = tickFrequency; dt = 1.0 / tickFrequency;
        }
        public SkiForceOutput Step(SkiState state, double deliveredPowerWatts, TrackSample track, double headwindMetersPerSecond = 0, double dragAreaMultiplier = 1, double brakingNewtons = 0, double snowResistanceMultiplier = 1)
            => Integrate(state, deliveredPowerWatts, track, dt, frequency, headwindMetersPerSecond, dragAreaMultiplier, brakingNewtons, snowResistanceMultiplier);
        /// <summary>Deterministic substep inside the configured fixed tick, for authored surface/route boundaries.</summary>
        public SkiForceOutput StepDuration(SkiState state, double deliveredPowerWatts, TrackSample track, double durationSeconds,
            double headwindMetersPerSecond = 0, double dragAreaMultiplier = 1, double brakingNewtons = 0, double snowResistanceMultiplier = 1)
        {
            Require(durationSeconds, double.Epsilon, dt, nameof(durationSeconds));
            return Integrate(state, deliveredPowerWatts, track, durationSeconds, durationSeconds == dt ? frequency : 0,
                headwindMetersPerSecond, dragAreaMultiplier, brakingNewtons, snowResistanceMultiplier);
        }
        private SkiForceOutput Integrate(SkiState state, double deliveredPowerWatts, TrackSample track, double duration, double inverseDuration,
            double headwindMetersPerSecond, double dragAreaMultiplier, double brakingNewtons, double snowResistanceMultiplier)
        {
            Require(state.SpeedMetersPerSecond, 0, definition.MaximumSpeedMetersPerSecond, nameof(state));
            Require(deliveredPowerWatts, 0, definition.MaximumPowerWatts, nameof(deliveredPowerWatts));
            Require(dragAreaMultiplier, 0, 1, nameof(dragAreaMultiplier));
            Require(brakingNewtons, 0, PhysiologyNumbers.MaximumMagnitude, nameof(brakingNewtons));
            Require(snowResistanceMultiplier, 0.000000001, PhysiologyNumbers.MaximumMagnitude, nameof(snowResistanceMultiplier));
            Require(headwindMetersPerSecond, -definition.MaximumHeadwindMetersPerSecond, definition.MaximumHeadwindMetersPerSecond, nameof(headwindMetersPerSecond));
            if (string.IsNullOrEmpty(track.SegmentId)) throw new ArgumentException("A valid track sample is required.", nameof(track));
            double speed = state.SpeedMetersPerSecond;
            double cosine = 1 / Math.Sqrt(1 + track.Grade * track.Grade);
            double weight = definition.MassKilograms * definition.GravityMetersPerSecondSquared;
            double gravity = weight * track.Grade * cosine;
            double snow = definition.SnowFrictionCoefficient * weight * cosine * snowResistanceMultiplier;
            Require(snow, 0, PhysiologyNumbers.MaximumMagnitude, nameof(snowResistanceMultiplier));
            double flow = speed + headwindMetersPerSecond * track.WindExposure;
            double air = 0.5 * definition.AirDensity * definition.DragAreaSquareMeters * dragAreaMultiplier * flow * Math.Abs(flow);
            double propulsion = Math.Min(definition.MaximumPropulsiveForceNewtons, deliveredPowerWatts / Math.Max(speed, definition.PowerForceMinimumSpeed));
            double acceleration = (propulsion - gravity - snow - air - brakingNewtons) / definition.MassKilograms;
            double proposedSpeed = speed + acceleration * duration;
            double nextSpeed = Math.Min(definition.MaximumSpeedMetersPerSecond, Math.Max(0, proposedSpeed));
            double moved;
            if (proposedSpeed < 0)
            {
                double stoppingTime = speed == 0 ? 0 : -speed / acceleration;
                moved = 0.5 * speed * stoppingTime;
            }
            else if (proposedSpeed > definition.MaximumSpeedMetersPerSecond)
            {
                double reachingTime = (definition.MaximumSpeedMetersPerSecond - speed) / acceleration;
                moved = (speed + definition.MaximumSpeedMetersPerSecond) * 0.5 * reachingTime + definition.MaximumSpeedMetersPerSecond * (duration - reachingTime);
            }
            else moved = (speed + nextSpeed) * 0.5 * duration;
            long increment = checked((long)Math.Round(moved * SkiState.NanometersPerMeter, MidpointRounding.ToEven));
            var next = new SkiState(checked(state.DistanceNanometers + increment), nextSpeed);
            double applied = inverseDuration > 0 ? (next.SpeedMetersPerSecond - speed) * inverseDuration : (next.SpeedMetersPerSecond - speed) / duration;
            return new SkiForceOutput(next, propulsion, gravity, snow, air, brakingNewtons, applied);
        }
        private static void Require(double value, double minimum, double maximum, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}

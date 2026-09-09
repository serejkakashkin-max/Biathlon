using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Independent horizontal/vertical sight angles relative to the lane target plane.</summary>
    public readonly struct WeaponAimState
    {
        public double YawRadians { get; }
        public double PitchRadians { get; }
        public WeaponAimState(double yaw, double pitch)
        {
            YawRadians = PhysiologyNumbers.RequireRange(yaw, -0.5, 0.5, nameof(yaw));
            PitchRadians = PhysiologyNumbers.RequireRange(pitch, -0.5, 0.5, nameof(pitch));
        }
        public ShotRay Ray => new ShotRay(0, 0, 0, Math.Tan(YawRadians), Math.Tan(PitchRadians), 1);
    }
    public sealed class WeaponAimSystem
    {
        private readonly WeaponAimDefinition definition;
        private readonly AthleteId athlete;
        private readonly int frequency;
        private readonly double stanceRate;
        private long tick;
        public WeaponAimState State { get; private set; }
        public WeaponAimSystem(WeaponAimDefinition definition, AthleteId athlete, int frequency,
            WeaponAimState initial = default, SimulationTick initialTick = default, ShootingStanceProfile stance = null)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!athlete.IsValid || frequency <= 0) throw new ArgumentException("Invalid aim identity/clock.");
            if (Math.Abs(initial.YawRadians) > definition.MaxYawRadians || Math.Abs(initial.PitchRadians) > definition.MaxPitchRadians)
                throw new ArgumentException("Initial aim exceeds configured bounds.");
            this.athlete = athlete; this.frequency = frequency; State = initial; tick = initialTick.Value; stanceRate = stance?.AimRateMultiplier ?? 1;
        }
        public WeaponAimState Step(WeaponAimCommand command)
        {
            if (!command.AthleteId.Equals(athlete) || command.Tick.Value != checked(tick + 1))
                throw new ArgumentException("Aim command requires matching athlete and consecutive tick.");
            double x = command.Horizontal / 1000.0, y = command.Vertical / 1000.0;
            double magnitude = Math.Sqrt(x * x + y * y);
            if (magnitude > 1) { x /= magnitude; y /= magnitude; }
            double step = definition.RateRadiansPerSecond / frequency * stanceRate * (command.FineControl ? definition.FineMultiplier : 1);
            var next = new WeaponAimState(
                PhysiologyNumbers.Quantize(Math.Max(-definition.MaxYawRadians, Math.Min(definition.MaxYawRadians, State.YawRadians + x * step))),
                PhysiologyNumbers.Quantize(Math.Max(-definition.MaxPitchRadians, Math.Min(definition.MaxPitchRadians, State.PitchRadians + y * step))));
            State = next; tick = command.Tick.Value; return next;
        }
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ShootingHandlingFrame
    {
        public double HorizontalRadians { get; } public double VerticalRadians { get; }
        public ShootingHandlingFrame(double horizontal, double vertical) { HorizontalRadians = horizontal; VerticalRadians = vertical; }
    }
    public sealed class ShootingHandlingSystem
    {
        private readonly ShootingHandlingDefinition definition;
        private readonly double decay;
        private long tick;
        public ShootingHandlingProfile Profile { get; }
        public ShootingHandlingFrame LastFrame { get; private set; }
        public ShootingHandlingSystem(ShootingHandlingDefinition definition, ShootingEquipmentDefinition equipment, int shootingSpeed,
            int frequency, SimulationTick initialTick = default)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            Profile = new ShootingHandlingProfile(definition, equipment, shootingSpeed);
            decay = Math.Exp(-1.0 / (frequency * Profile.SettleTauSeconds)); tick = initialTick.Value;
        }
        /// <summary>Advance AFTER resolution; new recoil affects subsequent shots, never the shot that created it.</summary>
        public ShootingHandlingFrame Step(SimulationTick target, bool shotAccepted)
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Handling ticks must be consecutive.");
            LastFrame = new ShootingHandlingFrame(
                PhysiologyNumbers.Quantize(Math.Min(definition.MaximumRecoil, LastFrame.HorizontalRadians * decay + (shotAccepted ? Profile.RecoilX : 0))),
                PhysiologyNumbers.Quantize(Math.Min(definition.MaximumRecoil, LastFrame.VerticalRadians * decay + (shotAccepted ? Profile.RecoilY : 0))));
            tick = target.Value; return LastFrame;
        }
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ShootingPressureFrame
    {
        public double Level { get; } public double Target { get; }
        public double BreathingMultiplier { get; } public double HeartbeatMultiplier { get; }
        public double MuscularMultiplier { get; } public double DecisionDelayMultiplier { get; }
        internal ShootingPressureFrame(double level, double target, ShootingPressureDefinition definition)
        {
            Level = level; Target = target;
            BreathingMultiplier = PhysiologyNumbers.Quantize(1 + level * definition.BreathingGain);
            HeartbeatMultiplier = PhysiologyNumbers.Quantize(1 + level * definition.HeartbeatGain);
            MuscularMultiplier = PhysiologyNumbers.Quantize(1 + level * definition.MuscularGain);
            DecisionDelayMultiplier = PhysiologyNumbers.Quantize(1 + level * definition.DecisionDelayGain);
        }
    }
    public sealed class ShootingPressureSystem
    {
        private readonly ShootingPressureDefinition definition;
        private readonly double riseAlpha, recoveryAlpha, stanceSensitivity;
        private long tick;
        public ShootingPressureFrame LastFrame { get; private set; }
        public ShootingPressureSystem(ShootingPressureDefinition definition, int frequency, ShootingStanceProfile stance = null,
            SimulationTick initialTick = default)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            riseAlpha = 1 - Math.Exp(-1.0 / (frequency * definition.RiseTau));
            recoveryAlpha = 1 - Math.Exp(-1.0 / (frequency * definition.RecoveryTau));
            stanceSensitivity = stance?.PressureSensitivity ?? 1; tick = initialTick.Value;
            LastFrame = new ShootingPressureFrame(0, 0, definition);
        }
        public ShootingPressureFrame Step(SimulationTick targetTick, ShootingPressureContext context, int composure)
        {
            if (targetTick.Value != checked(tick + 1)) throw new ArgumentException("Pressure ticks must be consecutive.");
            if (composure < 0 || composure > 100) throw new ArgumentOutOfRangeException(nameof(composure));
            double sensitivity = (1 + (definition.ExpertSensitivity - 1) * composure / 100.0) * stanceSensitivity;
            double target = Math.Min(1, context.RaceImportance * context.PositionImportance * context.RivalProximity * sensitivity *
                (1 + definition.LowConfidenceGain * (1 - context.Confidence)) *
                (context.FinalStanding ? 1 + definition.FinalStandingGain : 1) * (context.FirstPodium ? 1 + definition.FirstPodiumGain : 1));
            double alpha = target > LastFrame.Level ? riseAlpha : recoveryAlpha;
            double level = PhysiologyNumbers.Quantize(LastFrame.Level + (target - LastFrame.Level) * alpha);
            LastFrame = new ShootingPressureFrame(level, PhysiologyNumbers.Quantize(target), definition);
            tick = targetTick.Value; return LastFrame;
        }
    }
}

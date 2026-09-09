using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class HeartRateSystem
    {
        private readonly PhysiologyDefinition definition;
        private readonly double riseAlpha;
        private readonly double recoveryAlpha;
        public HeartRateSystem(PhysiologyDefinition definition, int tickFrequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (tickFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(tickFrequency));
            double dt = 1.0 / tickFrequency;
            riseAlpha = 1 - Math.Exp(-dt / definition.HrRiseTauSeconds);
            recoveryAlpha = 1 - Math.Exp(-dt * definition.RecoveryModifier / definition.HrRecoveryTauSeconds);
        }
        public HeartRateOutput Step(double currentBpm, double deliveredRelativeEffort, double muscularFatigue, double driftBpm = 0)
        {
            PhysiologyNumbers.RequireRange(currentBpm, definition.RestingHrBpm, definition.HrSafetyCeilingBpm, nameof(currentBpm));
            PhysiologyNumbers.RequireRange(deliveredRelativeEffort, 0, definition.MaximumRelativeEffort, nameof(deliveredRelativeEffort));
            PhysiologyNumbers.RequireRange(muscularFatigue, 0, 1, nameof(muscularFatigue));
            PhysiologyNumbers.RequireRange(driftBpm, 0, definition.DriftStrengthBpm, nameof(driftBpm));
            double load = Math.Min(definition.HrMaximumLoad, deliveredRelativeEffort * (1 + muscularFatigue * definition.FatigueHrModifier));
            double range = definition.MaxHrBpm - definition.RestingHrBpm;
            double target = definition.RestingHrBpm + range * Math.Pow(Math.Min(load, 1), definition.HrLoadExponent);
            if (load > 1) target += range * definition.HrOverloadScale * (load - 1);
            target = Math.Min(definition.HrSafetyCeilingBpm, target + driftBpm);
            double alpha = target > currentBpm ? riseAlpha : recoveryAlpha;
            return new HeartRateOutput(currentBpm + (target - currentBpm) * alpha, target, load);
        }
    }
}

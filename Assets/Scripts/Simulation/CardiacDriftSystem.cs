using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct CardiacDriftOutput
    {
        public double HighLoadSeconds { get; }
        public double DriftBpm { get; }
        internal CardiacDriftOutput(double seconds, double bpm)
        {
            HighLoadSeconds = seconds;
            DriftBpm = bpm;
        }
    }

    public sealed class CardiacDriftSystem
    {
        private readonly PhysiologyDefinition definition;
        private readonly double dt;
        public CardiacDriftSystem(PhysiologyDefinition definition, int tickFrequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (tickFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(tickFrequency));
            dt = 1.0 / tickFrequency;
        }
        public CardiacDriftOutput Step(double highLoadSeconds, double deliveredRelativeEffort, double muscularFatigue)
        {
            PhysiologyNumbers.RequireRange(highLoadSeconds, 0, definition.DriftSaturationSeconds, nameof(highLoadSeconds));
            PhysiologyNumbers.RequireRange(deliveredRelativeEffort, 0, definition.MaximumRelativeEffort, nameof(deliveredRelativeEffort));
            PhysiologyNumbers.RequireRange(muscularFatigue, 0, 1, nameof(muscularFatigue));
            double change = deliveredRelativeEffort > definition.DriftThreshold ? dt : -dt * definition.RecoveryModifier;
            double exposure = PhysiologyNumbers.Quantize(Math.Min(definition.DriftSaturationSeconds, Math.Max(0, highLoadSeconds + change)));
            double drift = muscularFatigue * definition.DriftStrengthBpm * (exposure / definition.DriftSaturationSeconds);
            return new CardiacDriftOutput(exposure, drift);
        }
    }
}

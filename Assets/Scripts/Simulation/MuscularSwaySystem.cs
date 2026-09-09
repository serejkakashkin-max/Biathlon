using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct MuscularSwayFrame
    {
        public double AmplitudeRadians { get; } public double HorizontalRadians { get; } public double VerticalRadians { get; }
        public MuscularSwayFrame(double amplitude, double horizontal, double vertical)
        { AmplitudeRadians = amplitude; HorizontalRadians = horizontal; VerticalRadians = vertical; }
    }
    public sealed class MuscularSwaySystem
    {
        private readonly MuscularSwayDefinition definition;
        private readonly double[] horizontalPhases, verticalPhases;
        private readonly int frequency;
        private long tick;
        public MuscularSwayFrame LastFrame { get; private set; }
        public MuscularSwaySystem(MuscularSwayDefinition definition, int frequency, IRandomSource random, SimulationTick initialTick = default)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (random == null) throw new ArgumentNullException(nameof(random));
            for (int i = 0; i < definition.ToneCount; i++)
                if (frequency <= 2 * definition.Frequency(i)) throw new ArgumentOutOfRangeException(nameof(frequency));
            this.frequency = frequency; tick = initialTick.Value;
            horizontalPhases = new double[definition.ToneCount]; verticalPhases = new double[definition.ToneCount];
            for (int i = 0; i < definition.ToneCount; i++)
            {
                horizontalPhases[i] = (random.NextUInt64() >> 11) / 9007199254740992.0;
                verticalPhases[i] = (random.NextUInt64() >> 11) / 9007199254740992.0;
            }
        }
        public MuscularSwayFrame Step(SimulationTick target, double fatigue, double armFatigue, int stabilitySkill)
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Muscular sway ticks must be consecutive.");
            PhysiologyNumbers.RequireRange(fatigue, 0, 1, nameof(fatigue)); PhysiologyNumbers.RequireRange(armFatigue, 0, 1, nameof(armFatigue));
            if (stabilitySkill < 0 || stabilitySkill > 100) throw new ArgumentOutOfRangeException(nameof(stabilitySkill));
            double amplitude = definition.Amplitude * (1 + definition.FatigueGain * fatigue + definition.ArmFatigueGain * armFatigue) *
                (1 + (definition.ExpertMultiplier - 1) * stabilitySkill / 100.0);
            double horizontal = 0, vertical = 0;
            for (int i = 0; i < definition.ToneCount; i++)
            {
                double step = definition.Frequency(i) / frequency;
                horizontalPhases[i] = PhysiologyNumbers.Quantize((horizontalPhases[i] + step) % 1) % 1;
                verticalPhases[i] = PhysiologyNumbers.Quantize((verticalPhases[i] + step) % 1) % 1;
                horizontal += definition.Weight(i) * Math.Sin(horizontalPhases[i] * 2 * Math.PI);
                vertical += definition.Weight(i) * Math.Sin(verticalPhases[i] * 2 * Math.PI);
            }
            LastFrame = new MuscularSwayFrame(PhysiologyNumbers.Quantize(amplitude), PhysiologyNumbers.Quantize(amplitude * horizontal),
                PhysiologyNumbers.Quantize(amplitude * vertical * definition.VerticalRatio));
            tick = target.Value; return LastFrame;
        }
    }
}

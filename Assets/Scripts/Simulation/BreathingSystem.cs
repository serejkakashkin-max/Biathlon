using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct BreathingFrame
    {
        public double PhaseCycles { get; }
        public double FrequencyHz { get; }
        public double AmplitudeRadians { get; }
        public double VerticalRadians { get; }
        public BreathingFrame(double phase, double frequency, double amplitude, double vertical)
        { PhaseCycles = phase; FrequencyHz = frequency; AmplitudeRadians = amplitude; VerticalRadians = vertical; }
    }
    public sealed class BreathingSystem
    {
        private readonly BreathingDefinition definition;
        private readonly PhysiologyDefinition physiology;
        private readonly int frequency;
        private long tick;
        private double phase;
        public BreathingFrame LastFrame { get; private set; }
        public BreathingSystem(BreathingDefinition definition, PhysiologyDefinition physiology, int frequency,
            double initialPhaseCycles = 0, SimulationTick initialTick = default)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.physiology = physiology ?? throw new ArgumentNullException(nameof(physiology));
            if (frequency <= 2 * definition.HighHrFrequency) throw new ArgumentOutOfRangeException(nameof(frequency), "Oscillator requires sampling above Nyquist frequency.");
            this.frequency = frequency; phase = PhysiologyNumbers.RequireRange(initialPhaseCycles, 0, 1, nameof(initialPhaseCycles)) % 1;
            tick = initialTick.Value;
        }
        public BreathingFrame Step(SimulationTick target, double heartRateBpm, double fatigue, int breathControl)
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Breathing ticks must be consecutive.");
            PhysiologyNumbers.RequireRange(heartRateBpm, physiology.RestingHrBpm, physiology.HrSafetyCeilingBpm, nameof(heartRateBpm));
            PhysiologyNumbers.RequireRange(fatigue, 0, 1, nameof(fatigue));
            if (breathControl < 0 || breathControl > 100) throw new ArgumentOutOfRangeException(nameof(breathControl));
            double load = Math.Min(1, (heartRateBpm - physiology.RestingHrBpm) / (physiology.MaxHrBpm - physiology.RestingHrBpm));
            double hz = definition.RestFrequency + (definition.HighHrFrequency - definition.RestFrequency) * load;
            double amplitude = definition.BaseAmplitude * (1 + (definition.HighHrMultiplier - 1) * load) *
                (1 + definition.FatigueGain * fatigue) * (1 + (definition.ExpertMultiplier - 1) * breathControl / 100.0);
            double nextPhase = PhysiologyNumbers.Quantize((phase + hz / frequency) % 1) % 1;
            var next = new BreathingFrame(nextPhase, PhysiologyNumbers.Quantize(hz), PhysiologyNumbers.Quantize(amplitude),
                PhysiologyNumbers.Quantize(amplitude * Math.Sin(nextPhase * 2 * Math.PI)));
            tick = target.Value; phase = nextPhase; LastFrame = next; return next;
        }
    }
}

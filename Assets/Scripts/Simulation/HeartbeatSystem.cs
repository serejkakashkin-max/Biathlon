using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct HeartbeatFrame
    {
        public double PhaseCycles { get; } public long CompletedBeats { get; }
        public bool BeatStarted { get; } public double Envelope { get; }
        public double AmplitudeRadians { get; } public double VerticalRadians { get; }
        public HeartbeatFrame(double phase, long beats, bool started, double envelope, double amplitude, double vertical)
        { PhaseCycles = phase; CompletedBeats = beats; BeatStarted = started; Envelope = envelope; AmplitudeRadians = amplitude; VerticalRadians = vertical; }
    }
    public sealed class HeartbeatSystem
    {
        private readonly HeartbeatDefinition definition;
        private readonly PhysiologyDefinition physiology;
        private readonly long periodUnits;
        private long phaseUnits, beats, tick;
        public HeartbeatFrame LastFrame { get; private set; }
        public HeartbeatSystem(HeartbeatDefinition definition, PhysiologyDefinition physiology, int frequency,
            SimulationTick initialTick = default)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.physiology = physiology ?? throw new ArgumentNullException(nameof(physiology));
            if (frequency <= 2 * physiology.HrSafetyCeilingBpm / 60 || frequency > 1000000)
                throw new ArgumentOutOfRangeException(nameof(frequency));
            periodUnits = 60L * frequency * 1000000000L; tick = initialTick.Value;
        }
        public HeartbeatFrame Step(SimulationTick target, double heartRateBpm)
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Heartbeat ticks must be consecutive.");
            PhysiologyNumbers.RequireRange(heartRateBpm, physiology.RestingHrBpm, physiology.HrSafetyCeilingBpm, nameof(heartRateBpm));
            long total = checked(phaseUnits + (long)Math.Round(heartRateBpm * 1e9, MidpointRounding.ToEven));
            long nextBeats = checked(beats + total / periodUnits), nextPhase = total % periodUnits;
            double phase = nextPhase / (double)periodUnits;
            double envelope = 0, progress = phase / definition.PulseWidth;
            if (progress < definition.AttackFraction) envelope = progress / definition.AttackFraction;
            else if (progress < 1)
            {
                double decay = (progress - definition.AttackFraction) / (1 - definition.AttackFraction);
                envelope = 1 - decay * decay * (3 - 2 * decay);
            }
            double load = Math.Min(1, (heartRateBpm - physiology.RestingHrBpm) / (physiology.MaxHrBpm - physiology.RestingHrBpm));
            double amplitude = definition.Amplitude * (1 + (definition.HighHrMultiplier - 1) * load);
            var frame = new HeartbeatFrame(PhysiologyNumbers.Quantize(phase), nextBeats, nextBeats != beats,
                PhysiologyNumbers.Quantize(envelope), PhysiologyNumbers.Quantize(amplitude), PhysiologyNumbers.Quantize(amplitude * envelope));
            tick = target.Value; phaseUnits = nextPhase; beats = nextBeats; LastFrame = frame; return frame;
        }
    }
}

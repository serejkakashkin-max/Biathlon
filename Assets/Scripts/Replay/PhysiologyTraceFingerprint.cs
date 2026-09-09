using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    /// <summary>Versioned streaming hash of every input/state/telemetry sample, quantized to ADR-0002 resolution.</summary>
    public sealed class PhysiologyTraceFingerprint : IDisposable
    {
        private readonly SHA256 sha = SHA256.Create();
        private readonly MemoryStream buffer = new MemoryStream(256);
        private readonly BinaryWriter writer;
        private readonly AthleteId athlete;
        private long tick;
        private bool sealedHash;
        public PhysiologyTraceFingerprint(PhysiologySession initial)
        {
            if (initial == null || initial.Clock.Tick.Value != 0) throw new ArgumentException("Trace requires an initial session.");
            athlete = initial.AthleteId;
            writer = new BinaryWriter(buffer, Encoding.UTF8, true);
            writer.Write(1); // Trace encoding version.
            writer.Write(PhysiologyFingerprint.Compute(initial, new RaceEventLog()));
            FlushBlock();
        }
        public void Append(PhysiologyInput input, PhysiologyFrame frame)
        {
            if (sealedHash) throw new InvalidOperationException("Trace is sealed.");
            if (frame.Tick.Value != checked(tick + 1) || !input.Effort.Tick.Equals(frame.Tick) ||
                !frame.AthleteId.Equals(athlete) || !input.Effort.AthleteId.Equals(athlete))
                throw new ArgumentException("Trace samples must be consecutive and match input/athlete.");
            writer.Write(frame.Tick.Value); writer.Write(input.Effort.RequestedEffort);
            Write(input.Form); Write(input.TerrainMuscleFactor);
            Write(frame.State.HeartRateBpm); Write(frame.State.AnaerobicReserve);
            Write(frame.State.MuscularFatigue); Write(frame.State.HighLoadSeconds);
            Write(frame.Effort.RequestedPowerWatts); Write(frame.Effort.DeliveredPowerWatts);
            Write(frame.Effort.SustainablePowerWatts); Write(frame.Effort.RelativeEffort);
            Write(frame.Effort.DeliveredRelativeEffort); Write(frame.Effort.PowerEfficiency);
            Write(frame.HeartRate.TargetBpm); Write(frame.HeartRate.RelativeLoad);
            Write(frame.DriftBpm); Write(frame.ReserveChangePerSecond); Write(frame.FatigueChangePerSecond);
            FlushBlock();
            tick = frame.Tick.Value;
        }
        private void Write(double value) => writer.Write(checked((long)Math.Round(PhysiologyNumbers.Quantize(value) * 1e9, MidpointRounding.ToEven)));
        private void FlushBlock()
        {
            writer.Flush();
            byte[] bytes = buffer.GetBuffer();
            sha.TransformBlock(bytes, 0, checked((int)buffer.Length), bytes, 0);
            buffer.SetLength(0);
            buffer.Position = 0;
        }
        public string Complete()
        {
            if (sealedHash) throw new InvalidOperationException("Trace is sealed.");
            sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            sealedHash = true;
            return BitConverter.ToString(sha.Hash).Replace("-", "").ToLowerInvariant();
        }
        public void Dispose() { writer.Dispose(); buffer.Dispose(); sha.Dispose(); }
    }
}

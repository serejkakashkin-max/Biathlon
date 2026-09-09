using System;
using System.Globalization;
using System.IO;
using Biathlon.Domain;

namespace Biathlon.Replay
{
    /// <summary>Explicit export path. Formatting allocations are not part of simulation Step.</summary>
    public static class PhysiologyCsv
    {
        public const string Header = "tick,time_s,control,form,terrain_muscle_factor,requested_watts,delivered_watts,sustainable_watts,relative_effort,delivered_relative_effort,hr_bpm,hr_target_bpm,anaerobic,fatigue,muscle_freshness,high_load_s,drift_bpm,reserve_change_per_s,fatigue_change_per_s";
        public static void WriteFrame(TextWriter writer, PhysiologyInput input, PhysiologyFrame frame, int frequency)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (frequency <= 0 || !input.Effort.Tick.Equals(frame.Tick) || !input.Effort.AthleteId.Equals(frame.AthleteId))
                throw new ArgumentException("Trace input/frame tick and athlete must match.");
            writer.Write(frame.Tick.Value.ToString(CultureInfo.InvariantCulture));
            Write(writer, frame.Tick.Value / (double)frequency); Write(writer, input.Effort.RequestedEffort);
            Write(writer, input.Form); Write(writer, input.TerrainMuscleFactor);
            Write(writer, frame.Effort.RequestedPowerWatts); Write(writer, frame.Effort.DeliveredPowerWatts);
            Write(writer, frame.Effort.SustainablePowerWatts); Write(writer, frame.Effort.RelativeEffort);
            Write(writer, frame.Effort.DeliveredRelativeEffort); Write(writer, frame.State.HeartRateBpm);
            Write(writer, frame.HeartRate.TargetBpm); Write(writer, frame.State.AnaerobicReserve);
            Write(writer, frame.State.MuscularFatigue); Write(writer, 1 - frame.State.MuscularFatigue);
            Write(writer, frame.State.HighLoadSeconds); Write(writer, frame.DriftBpm);
            Write(writer, frame.ReserveChangePerSecond); Write(writer, frame.FatigueChangePerSecond);
            writer.WriteLine();
        }
        private static void Write(TextWriter writer, double value)
        {
            writer.Write(','); writer.Write(value.ToString("R", CultureInfo.InvariantCulture));
        }
    }
}

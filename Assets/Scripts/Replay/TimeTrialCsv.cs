using System;
using System.Globalization;
using System.IO;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    /// <summary>Explicit allocating export boundary; never called by the authoritative tick.</summary>
    public static class TimeTrialCsv
    {
        public const string Header = "tick,athlete_id,requested_control,form,line_quality,headwind_mps,tuck_requested,sprint_requested,requested_technique,lap_index,section_index,loop_id,segment_id,grade,curvature_per_m,distance_nm,speed_mps,delivered_watts,propulsive_watts,hr_bpm,hr_target_bpm,reserve,muscular_fatigue,high_load_seconds,drift_bpm,reserve_rate,fatigue_rate,technique_id,technique_efficiency,tuck_active,tuck_rejection,effective_control,drag_area_multiplier,steering_multiplier,safe_speed_mps,excess_speed_mps,braking_n,ski_id,snow_profile_id,snow_resistance_multiplier,cornering_grip_multiplier,sprint_active,remaining_m,propulsion_n,gravity_n,snow_n,air_drag_n,net_force_n,acceleration_mps2,movement_efficiency";
        public static void WriteFrame(TextWriter writer, TimeTrialFrame f)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            object[] values = {
                f.Input.Effort.Tick.Value, f.Input.Effort.AthleteId.Value, f.Input.Effort.RequestedEffort, f.Input.Form,
                f.Input.LineQuality, f.Input.HeadwindMetersPerSecond, f.Input.TuckRequested, f.Input.SprintRequested, f.Input.TechniqueId ?? "",
                f.RouteSample.LapIndex, f.RouteSample.SectionIndex, f.RouteSample.Track.LoopId, f.RouteSample.Track.SegmentId,
                f.RouteSample.Track.Grade, f.RouteSample.Track.CurvaturePerMeter, f.Forces.State.DistanceNanometers, f.Forces.State.SpeedMetersPerSecond,
                f.Physiology.Effort.DeliveredPowerWatts, f.Technique.PropulsivePowerWatts, f.Physiology.State.HeartRateBpm, f.Physiology.HeartRate.TargetBpm,
                f.Physiology.State.AnaerobicReserve, f.Physiology.State.MuscularFatigue, f.Physiology.State.HighLoadSeconds, f.Physiology.DriftBpm,
                f.Physiology.ReserveChangePerSecond, f.Physiology.FatigueChangePerSecond, f.Technique.TechniqueId, f.Technique.Efficiency,
                f.Tuck.Active, f.Tuck.Rejection.ToString(), f.Tuck.EffectiveEffortControl, f.Tuck.DragAreaMultiplier, f.Tuck.SteeringResponseMultiplier,
                f.Cornering.SafeSpeedMetersPerSecond, f.Cornering.ExcessSpeedMetersPerSecond, f.Forces.BrakingNewtons,
                f.Equipment.SkiId, f.Equipment.SnowProfileId, f.Equipment.SnowResistanceMultiplier, f.Equipment.CorneringGripMultiplier,
                f.Sprint.Active, f.Sprint.RemainingMeters, f.Forces.PropulsiveNewtons, f.Forces.GravityNewtons, f.Forces.SnowNewtons,
                f.Forces.AirDragNewtons, f.Forces.NetNewtons, f.Forces.AppliedAcceleration, f.Input.MovementEfficiency
            };
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) writer.Write(',');
                string text = values[i] is double number ? FormatNumber(number) :
                    values[i] is bool flag ? flag ? "1" : "0" : Convert.ToString(values[i], CultureInfo.InvariantCulture);
                if (text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0) writer.Write("\"" + text.Replace("\"", "\"\"") + "\"");
                else writer.Write(text);
            }
            writer.Write('\n');
        }
        private static string FormatNumber(double value)
        {
            double rounded = Math.Round(value, 9, MidpointRounding.ToEven);
            // .NET and Mono format the IEEE negative-zero sign differently; canonical telemetry has one zero.
            return rounded == 0 ? "0" : rounded.ToString("0.#########", CultureInfo.InvariantCulture);
        }
    }
}

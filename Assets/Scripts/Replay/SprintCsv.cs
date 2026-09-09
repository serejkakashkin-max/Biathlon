using System.Globalization;
using System.IO;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    public static class SprintCsv
    {
        public const string Header = "tick,phase,route_nm,travel_nm,speed_mps,hr_bpm,reserve,fatigue,high_load_s,laps,bouts,penalty_remaining,penalty_total,shooting_active,shooting_phase,shots,closed_mask,aim_yaw,aim_pitch,sight_yaw,sight_pitch,hold_debt,pressure,impact_x,impact_y,shot_outcome";
        public static void WriteFrame(TextWriter writer, SprintAthleteFrame frame)
        {
            var c = frame.Competition; var p = frame.Physiology.State; var shot = frame.Shooting;
            object[] fields = { frame.Tick.Value, (int)c.Phase, frame.RouteNanometers, frame.Motion.DistanceNanometers, frame.Motion.SpeedMetersPerSecond,
                p.HeartRateBpm, p.AnaerobicReserve, p.MuscularFatigue, p.HighLoadSeconds, c.CompletedLaps, c.CompletedBouts, c.PendingPenaltyLoops,
                frame.TotalPenaltyLoops, frame.ShootingActive ? 1 : 0, (int)shot.Shot.Lifecycle.Phase, shot.Shot.Lifecycle.ShotsFired, shot.Shot.ClosedMask,
                shot.Aim.YawRadians, shot.Aim.PitchRadians, shot.ShotAim.YawRadians, shot.ShotAim.PitchRadians, shot.BreathControl.Debt,
                shot.Pressure.Level, shot.Shot.Impact.X, shot.Shot.Impact.Y, (int)shot.Shot.Outcome };
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0) writer.Write(',');
                if (fields[i] is double value)
                { value = System.Math.Round(value, 9, System.MidpointRounding.ToEven); writer.Write(value == 0 ? "0" : value.ToString("0.#########", CultureInfo.InvariantCulture)); }
                else writer.Write(((System.IFormattable)fields[i]).ToString(null, CultureInfo.InvariantCulture));
            }
            writer.Write('\n');
        }
    }
}

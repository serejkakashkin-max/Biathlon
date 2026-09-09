using System;
using System.Globalization;
using System.IO;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    public static class ShootingCsv
    {
        public const string Header = "tick,phase,shots,closed_mask,aim_x_rad,aim_y_rad,breathing_rad,heartbeat_rad,muscular_x_rad,muscular_y_rad,hold,debt,pressure,recoil_x_rad,recoil_y_rad,shot_x_rad,shot_y_rad,outcome,target_index,impact_x_m,impact_y_m,wind_drift_m";
        public static void WriteFrame(TextWriter writer, ShootingSessionFrame f)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            object[] values = { f.Input.Aim.Tick.Value, (int)f.Shot.Lifecycle.Phase, f.Shot.Lifecycle.ShotsFired, f.Shot.ClosedMask,
                f.Aim.YawRadians, f.Aim.PitchRadians, f.Breathing.VerticalRadians, f.Heartbeat.VerticalRadians,
                f.Muscular.HorizontalRadians, f.Muscular.VerticalRadians, f.BreathControl.Holding ? 1 : 0, f.BreathControl.Debt,
                f.Pressure.Level, f.Handling.HorizontalRadians, f.Handling.VerticalRadians, f.ShotAim.YawRadians, f.ShotAim.PitchRadians,
                (int)f.Shot.Outcome, f.Shot.Lifecycle.ShotAccepted ? f.Shot.Impact.TargetIndex : -1, f.Shot.Impact.X, f.Shot.Impact.Y, f.Shot.Wind.HorizontalDriftMeters };
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0) writer.Write(',');
                if (values[i] is double value)
                {
                    double rounded = Math.Round(value, 9, MidpointRounding.ToEven);
                    writer.Write(rounded == 0 ? "0" : rounded.ToString("0.#########", CultureInfo.InvariantCulture));
                }
                else writer.Write(Convert.ToString(values[i], CultureInfo.InvariantCulture));
            }
            writer.Write('\n');
        }
    }
}

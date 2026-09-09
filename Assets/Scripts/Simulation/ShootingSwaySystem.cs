using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct ShootingSwayFrame
    {
        public double HorizontalRadians { get; } public double VerticalRadians { get; }
        public double BreathingRadians { get; } public double HeartbeatRadians { get; }
        public ShootingSwayFrame(double x, double y, double breathing, double heartbeat)
        { HorizontalRadians = x; VerticalRadians = y; BreathingRadians = breathing; HeartbeatRadians = heartbeat; }
    }
    public static class ShootingSwaySystem
    {
        public static ShootingSwayFrame Compose(ShootingStanceProfile stance, BreathingFrame breathing,
            HeartbeatFrame heartbeat, MuscularSwayFrame muscular, double breathingMultiplier = 1, ShootingPressureFrame? pressure = null)
        {
            if (stance == null) throw new ArgumentNullException(nameof(stance));
            PhysiologyNumbers.RequireRange(breathingMultiplier, 0.01, 5, nameof(breathingMultiplier));
            double pb = pressure?.BreathingMultiplier ?? 1, ph = pressure?.HeartbeatMultiplier ?? 1, pm = pressure?.MuscularMultiplier ?? 1;
            PhysiologyNumbers.RequireRange(pb, 1, 3, nameof(pressure)); PhysiologyNumbers.RequireRange(ph, 1, 3, nameof(pressure)); PhysiologyNumbers.RequireRange(pm, 1, 3, nameof(pressure));
            double b = breathing.VerticalRadians * stance.BreathingMultiplier * breathingMultiplier * pb;
            double h = heartbeat.VerticalRadians * stance.HeartbeatMultiplier * ph;
            return new ShootingSwayFrame(PhysiologyNumbers.Quantize(muscular.HorizontalRadians * stance.MuscularMultiplier * pm),
                PhysiologyNumbers.Quantize(b + h + muscular.VerticalRadians * stance.MuscularMultiplier * pm),
                PhysiologyNumbers.Quantize(b), PhysiologyNumbers.Quantize(h));
        }
    }
}

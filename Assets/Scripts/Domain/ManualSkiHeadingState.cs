using System;

namespace Biathlon.Domain
{
    /// <summary>World pose on the course plane; yaw zero is +Z, positive yaw turns toward +X.</summary>
    public readonly struct ManualSkiHeadingState
    {
        // Encoding policy, not athlete tuning. An integer full turn makes wrapping canonical.
        public const long NanoradiansPerRadian = 1000000000;
        public const long FullTurnNanoradians = 6283185307;
        public bool IsValid { get; }
        public double X { get; }
        public double Z { get; }
        public long HeadingNanoradians { get; }
        public double HeadingRadians => HeadingNanoradians / (double)NanoradiansPerRadian;
        public double YawRateRadiansPerSecond { get; }

        public ManualSkiHeadingState(double x, double z, double headingRadians, double yawRateRadiansPerSecond = 0)
        {
            X = PhysiologyNumbers.Quantize(x);
            Z = PhysiologyNumbers.Quantize(z);
            PhysiologyNumbers.RequireRange(headingRadians, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(headingRadians));
            long heading = checked((long)Math.Round(headingRadians * NanoradiansPerRadian, MidpointRounding.ToEven));
            heading %= FullTurnNanoradians;
            if (heading < 0) heading += FullTurnNanoradians;
            HeadingNanoradians = heading;
            YawRateRadiansPerSecond = PhysiologyNumbers.Quantize(yawRateRadiansPerSecond);
            IsValid = true;
        }
    }
}

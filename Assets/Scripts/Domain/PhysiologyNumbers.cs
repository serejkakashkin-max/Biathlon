using System;

namespace Biathlon.Domain
{
    /// <summary>Numeric encoding policy, not gameplay tuning. See ADR-0002.</summary>
    public static class PhysiologyNumbers
    {
        public const double MaximumMagnitude = 1000000;
        public const double MinimumPositive = 0.000000001;
        public static double RequireRange(double value, double min, double max, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(name, value, "Finite value outside the supported range.");
            return value;
        }
        public static double Quantize(double value)
        {
            RequireRange(value, -MaximumMagnitude, MaximumMagnitude, nameof(value));
            return Math.Round(value, 9, MidpointRounding.ToEven);
        }
        public static double Clamp(double value, double min, double max)
        {
            RequireRange(min, -MaximumMagnitude, MaximumMagnitude, nameof(min));
            RequireRange(max, min, MaximumMagnitude, nameof(max));
            RequireRange(value, -MaximumMagnitude, MaximumMagnitude, nameof(value));
            return Math.Min(max, Math.Max(min, value));
        }
    }
}

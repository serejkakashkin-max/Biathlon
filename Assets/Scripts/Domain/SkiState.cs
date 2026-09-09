using System;

namespace Biathlon.Domain
{
    public readonly struct SkiState
    {
        public const long NanometersPerMillimeter = 1000000;
        public const long NanometersPerMeter = 1000000000;
        public long DistanceNanometers { get; }
        public TrackDistance Distance => new TrackDistance(DistanceNanometers / NanometersPerMillimeter);
        public double DistanceMeters => DistanceNanometers / (double)NanometersPerMeter;
        public double SpeedMetersPerSecond { get; }
        public SkiState(long distanceNanometers, double speedMetersPerSecond)
        {
            if (distanceNanometers < 0) throw new ArgumentOutOfRangeException(nameof(distanceNanometers));
            TrackValidation.Finite(speedMetersPerSecond, 0, 1000000, nameof(speedMetersPerSecond));
            DistanceNanometers = distanceNanometers;
            SpeedMetersPerSecond = Math.Round(speedMetersPerSecond, 9, MidpointRounding.ToEven);
        }
    }
}

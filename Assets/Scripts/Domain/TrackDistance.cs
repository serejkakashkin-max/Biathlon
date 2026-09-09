using System;

namespace Biathlon.Domain
{
    /// <summary>Exact route coordinate in integer millimetres. Authoring conversion is a cold path.</summary>
    public readonly struct TrackDistance : IEquatable<TrackDistance>, IComparable<TrackDistance>
    {
        public long Millimeters { get; }
        public double Meters => Millimeters / 1000.0;
        public TrackDistance(long millimeters)
        {
            if (millimeters < 0) throw new ArgumentOutOfRangeException(nameof(millimeters));
            Millimeters = millimeters;
        }
        public static TrackDistance FromMeters(double meters)
        {
            TrackValidation.Finite(meters, 0, 1000000, nameof(meters));
            double scaled = meters * 1000;
            long mm = checked((long)Math.Round(scaled, MidpointRounding.ToEven));
            if (Math.Abs(scaled - mm) > 0.000001) throw new ArgumentException("Track coordinates require whole millimetres.", nameof(meters));
            return new TrackDistance(mm);
        }
        public bool Equals(TrackDistance other) => Millimeters == other.Millimeters;
        public override bool Equals(object obj) => obj is TrackDistance other && Equals(other);
        public override int GetHashCode() => Millimeters.GetHashCode();
        public int CompareTo(TrackDistance other) => Millimeters.CompareTo(other.Millimeters);
    }
    internal static class TrackValidation
    {
        internal static void Id(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable track identifier is required.");
        }
        internal static void Finite(double value, double min, double max, string field)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(field);
        }
    }
}

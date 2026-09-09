using System;
namespace Biathlon.Domain
{
    public readonly struct AthleteId : IEquatable<AthleteId>
    {
        public string Value { get; }
        public bool IsValid => !string.IsNullOrWhiteSpace(Value);
        public AthleteId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable ID is required.", nameof(value));
            Value = value;
        }
        public bool Equals(AthleteId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is AthleteId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
    }
}

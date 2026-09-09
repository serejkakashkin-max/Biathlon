using System;
namespace Biathlon.Domain
{
    /// <summary>Exact count of completed simulation steps. Zero is the initial state.</summary>
    public readonly struct SimulationTick : IEquatable<SimulationTick>
    {
        public long Value { get; }
        public SimulationTick(long value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            Value = value;
        }
        public SimulationTick Next() => new SimulationTick(checked(Value + 1));
        public bool Equals(SimulationTick other) => Value == other.Value;
        public override bool Equals(object obj) => obj is SimulationTick other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
    }
}

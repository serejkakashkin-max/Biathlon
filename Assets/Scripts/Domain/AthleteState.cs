using System;
namespace Biathlon.Domain
{
    /// <summary>Placeholder: records requested input only; no physiological or movement model.</summary>
    public readonly struct AthleteState
    {
        // Encoding resolution, not a gameplay tuning value.
        public const int EffortUnits = 1000;
        public AthleteId Id { get; }
        public int RequestedEffort { get; }
        public AthleteState(AthleteId id, int requestedEffort = 0)
        {
            if (!id.IsValid) throw new ArgumentException("Invalid athlete ID.", nameof(id));
            ValidateEffort(requestedEffort);
            Id = id;
            RequestedEffort = requestedEffort;
        }
        public static void ValidateEffort(int value)
        {
            if (value < 0 || value > EffortUnits) throw new ArgumentOutOfRangeException(nameof(value));
        }
    }
}

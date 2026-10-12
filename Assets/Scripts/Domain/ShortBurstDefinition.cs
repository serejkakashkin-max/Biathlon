using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Optional effort curve extension. Absent from legacy definitions and replays.</summary>
    [DataContract]
    public sealed class ShortBurstDefinition
    {
        [DataMember(Name = "schema_version", IsRequired = true, Order = 0)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true, Order = 1)] public string Id { get; private set; }
        [DataMember(Name = "activation_control", IsRequired = true, Order = 2)] public int ActivationControl { get; private set; }
        [DataMember(Name = "normal_maximum_relative_effort", IsRequired = true, Order = 3)] public double NormalMaximumRelativeEffort { get; private set; }
        [DataMember(Name = "full_power_reserve_fraction", IsRequired = true, Order = 4)] public double FullPowerReserveFraction { get; private set; }

        internal void Validate(double aerobicThreshold, double maximumRelativeEffort)
        {
            if (SchemaVersion != 1 || string.IsNullOrWhiteSpace(Id))
                throw new SerializationException("A versioned short-burst definition is required.");
            if (ActivationControl <= 0 || ActivationControl >= AthleteState.EffortUnits)
                throw new ArgumentOutOfRangeException(nameof(ActivationControl));
            PhysiologyNumbers.RequireRange(NormalMaximumRelativeEffort, aerobicThreshold,
                maximumRelativeEffort, nameof(NormalMaximumRelativeEffort));
            PhysiologyNumbers.RequireRange(FullPowerReserveFraction, PhysiologyNumbers.MinimumPositive,
                1, nameof(FullPowerReserveFraction));
            if (NormalMaximumRelativeEffort >= maximumRelativeEffort)
                throw new ArgumentException("Short burst must extend the normal effort range.");
        }
    }
}

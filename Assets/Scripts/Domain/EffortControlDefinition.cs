using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class EffortControlDefinition
    {
        private EffortControlDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "analog_deadzone", IsRequired = true)] public int AnalogDeadzone { get; private set; }
        [DataMember(Name = "analog_saturation", IsRequired = true)] public int AnalogSaturation { get; private set; }
        [DataMember(Name = "recovery_control", IsRequired = true)] public int RecoveryControl { get; private set; }
        [DataMember(Name = "race_control", IsRequired = true)] public int RaceControl { get; private set; }
        [DataMember(Name = "attack_control", IsRequired = true)] public int AttackControl { get; private set; }
        [DataMember(Name = "assisted_rise_units_per_s", IsRequired = true)] public double AssistedRiseRate { get; private set; }
        [DataMember(Name = "assisted_fall_units_per_s", IsRequired = true)] public double AssistedFallRate { get; private set; }
        public static EffortControlDefinition FromJson(string json)
        {
            var v = RouteJson.Read<EffortControlDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid effort control schema.");
            TrackValidation.Id(v.Id);
            AthleteState.ValidateEffort(v.AnalogDeadzone); AthleteState.ValidateEffort(v.AnalogSaturation);
            AthleteState.ValidateEffort(v.RecoveryControl); AthleteState.ValidateEffort(v.RaceControl); AthleteState.ValidateEffort(v.AttackControl);
            if (v.AnalogDeadzone >= v.AnalogSaturation || v.RecoveryControl > v.RaceControl || v.RaceControl > v.AttackControl)
                throw new ArgumentException("Effort control intervals/presets must be ordered.");
            TrackValidation.Finite(v.AssistedRiseRate, 0.000000001, 1000000, nameof(AssistedRiseRate));
            TrackValidation.Finite(v.AssistedFallRate, 0.000000001, 1000000, nameof(AssistedFallRate));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

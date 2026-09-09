using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Biathlon.Domain
{
    /// <summary>Immutable, required, versioned tuning. Only the JSON file supplies defaults.</summary>
    [DataContract]
    public sealed class PhysiologyDefinition
    {
        [DataMember(Name = "schema_version", IsRequired = true, Order = 0)]
        public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true, Order = 1)]
        public string Id { get; private set; }
        [DataMember(Name = "resting_hr_bpm", IsRequired = true, Order = 2)]
        public double RestingHrBpm { get; private set; }
        [DataMember(Name = "max_hr_bpm", IsRequired = true, Order = 3)]
        public double MaxHrBpm { get; private set; }
        [DataMember(Name = "hr_safety_ceiling_bpm", IsRequired = true, Order = 4)]
        public double HrSafetyCeilingBpm { get; private set; }
        [DataMember(Name = "sustainable_power_watts", IsRequired = true, Order = 5)]
        public double SustainablePowerWatts { get; private set; }
        [DataMember(Name = "maximum_relative_effort", IsRequired = true, Order = 6)]
        public double MaximumRelativeEffort { get; private set; }
        [DataMember(Name = "hr_rise_tau_s", IsRequired = true, Order = 7)]
        public double HrRiseTauSeconds { get; private set; }
        [DataMember(Name = "hr_recovery_tau_s", IsRequired = true, Order = 8)]
        public double HrRecoveryTauSeconds { get; private set; }
        [DataMember(Name = "hr_load_exponent", IsRequired = true, Order = 9)]
        public double HrLoadExponent { get; private set; }
        [DataMember(Name = "hr_maximum_load", IsRequired = true, Order = 10)]
        public double HrMaximumLoad { get; private set; }
        [DataMember(Name = "hr_overload_scale", IsRequired = true, Order = 11)]
        public double HrOverloadScale { get; private set; }
        [DataMember(Name = "fatigue_hr_modifier", IsRequired = true, Order = 12)]
        public double FatigueHrModifier { get; private set; }
        [DataMember(Name = "drift_strength_bpm", IsRequired = true, Order = 13)]
        public double DriftStrengthBpm { get; private set; }
        [DataMember(Name = "drift_threshold", IsRequired = true, Order = 14)]
        public double DriftThreshold { get; private set; }
        [DataMember(Name = "drift_saturation_s", IsRequired = true, Order = 15)]
        public double DriftSaturationSeconds { get; private set; }
        [DataMember(Name = "anaerobic_capacity_joules", IsRequired = true, Order = 16)]
        public double AnaerobicCapacityJoules { get; private set; }
        [DataMember(Name = "aerobic_threshold", IsRequired = true, Order = 17)]
        public double AerobicThreshold { get; private set; }
        [DataMember(Name = "anaerobic_drain_exponent", IsRequired = true, Order = 18)]
        public double AnaerobicDrainExponent { get; private set; }
        [DataMember(Name = "anaerobic_drain_watts", IsRequired = true, Order = 19)]
        public double AnaerobicDrainWatts { get; private set; }
        [DataMember(Name = "anaerobic_recovery_watts", IsRequired = true, Order = 20)]
        public double AnaerobicRecoveryWatts { get; private set; }
        [DataMember(Name = "recovery_modifier", IsRequired = true, Order = 21)]
        public double RecoveryModifier { get; private set; }
        [DataMember(Name = "fatigue_exponent", IsRequired = true, Order = 22)]
        public double FatigueExponent { get; private set; }
        [DataMember(Name = "fatigue_gain_per_s", IsRequired = true, Order = 23)]
        public double FatigueGainPerSecond { get; private set; }
        [DataMember(Name = "fatigue_recovery_per_s", IsRequired = true, Order = 24)]
        public double FatigueRecoveryPerSecond { get; private set; }
        [DataMember(Name = "minimum_power_efficiency", IsRequired = true, Order = 25)]
        public double MinimumPowerEfficiency { get; private set; }
        [DataMember(Name = "fatigue_recovery_penalty", IsRequired = true, Order = 26)]
        public double FatigueRecoveryPenalty { get; private set; }
        [DataMember(Name = "maximum_terrain_muscle_factor", IsRequired = true, Order = 27)]
        public double MaximumTerrainMuscleFactor { get; private set; }
        private PhysiologyDefinition() { }
        internal PhysiologyDefinition WithStatModifiers(string id, double power, double capacity, double recovery, double fatigue)
        {
            var result = (PhysiologyDefinition)MemberwiseClone();
            result.Id = id;
            result.SustainablePowerWatts *= power;
            result.AnaerobicCapacityJoules *= capacity;
            result.RecoveryModifier *= recovery;
            result.FatigueGainPerSecond *= fatigue;
            result.Validate();
            return result;
        }
        public static PhysiologyDefinition FromJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json)))
            {
                var result = (PhysiologyDefinition)new DataContractJsonSerializer(typeof(PhysiologyDefinition)).ReadObject(stream);
                if (result == null) throw new SerializationException("Definition cannot be null.");
                result.Validate();
                return result;
            }
        }
        public string ToJson()
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(PhysiologyDefinition)).WriteObject(stream, this);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        private void Validate()
        {
            if (SchemaVersion != 1) throw new ArgumentOutOfRangeException(nameof(SchemaVersion), "Unsupported physiology schema.");
            if (string.IsNullOrWhiteSpace(Id)) throw new ArgumentException("A stable definition ID is required.", nameof(Id));
            PhysiologyNumbers.RequireRange(RestingHrBpm, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(RestingHrBpm));
            PhysiologyNumbers.RequireRange(MaxHrBpm, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(MaxHrBpm));
            PhysiologyNumbers.RequireRange(HrSafetyCeilingBpm, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(HrSafetyCeilingBpm));
            PhysiologyNumbers.RequireRange(SustainablePowerWatts, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(SustainablePowerWatts));
            PhysiologyNumbers.RequireRange(MaximumRelativeEffort, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(MaximumRelativeEffort));
            PhysiologyNumbers.RequireRange(HrRiseTauSeconds, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(HrRiseTauSeconds));
            PhysiologyNumbers.RequireRange(HrRecoveryTauSeconds, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(HrRecoveryTauSeconds));
            PhysiologyNumbers.RequireRange(HrLoadExponent, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(HrLoadExponent));
            PhysiologyNumbers.RequireRange(HrMaximumLoad, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(HrMaximumLoad));
            PhysiologyNumbers.RequireRange(HrOverloadScale, 0, PhysiologyNumbers.MaximumMagnitude, nameof(HrOverloadScale));
            PhysiologyNumbers.RequireRange(FatigueHrModifier, 0, PhysiologyNumbers.MaximumMagnitude, nameof(FatigueHrModifier));
            PhysiologyNumbers.RequireRange(DriftStrengthBpm, 0, PhysiologyNumbers.MaximumMagnitude, nameof(DriftStrengthBpm));
            PhysiologyNumbers.RequireRange(DriftThreshold, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(DriftThreshold));
            PhysiologyNumbers.RequireRange(DriftSaturationSeconds, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(DriftSaturationSeconds));
            PhysiologyNumbers.RequireRange(AnaerobicCapacityJoules, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(AnaerobicCapacityJoules));
            PhysiologyNumbers.RequireRange(AerobicThreshold, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(AerobicThreshold));
            PhysiologyNumbers.RequireRange(AnaerobicDrainExponent, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(AnaerobicDrainExponent));
            PhysiologyNumbers.RequireRange(AnaerobicDrainWatts, 0, PhysiologyNumbers.MaximumMagnitude, nameof(AnaerobicDrainWatts));
            PhysiologyNumbers.RequireRange(AnaerobicRecoveryWatts, 0, PhysiologyNumbers.MaximumMagnitude, nameof(AnaerobicRecoveryWatts));
            PhysiologyNumbers.RequireRange(RecoveryModifier, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(RecoveryModifier));
            PhysiologyNumbers.RequireRange(FatigueExponent, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(FatigueExponent));
            PhysiologyNumbers.RequireRange(FatigueGainPerSecond, 0, PhysiologyNumbers.MaximumMagnitude, nameof(FatigueGainPerSecond));
            PhysiologyNumbers.RequireRange(FatigueRecoveryPerSecond, 0, PhysiologyNumbers.MaximumMagnitude, nameof(FatigueRecoveryPerSecond));
            PhysiologyNumbers.RequireRange(MinimumPowerEfficiency, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(MinimumPowerEfficiency));
            PhysiologyNumbers.RequireRange(FatigueRecoveryPenalty, 0, PhysiologyNumbers.MaximumMagnitude, nameof(FatigueRecoveryPenalty));
            PhysiologyNumbers.RequireRange(MaximumTerrainMuscleFactor, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(MaximumTerrainMuscleFactor));
            if (RestingHrBpm >= MaxHrBpm || MaxHrBpm > HrSafetyCeilingBpm)
                throw new ArgumentException("Require resting HR < max HR <= safety ceiling.");
            if (AerobicThreshold >= MaximumRelativeEffort || DriftThreshold > MaximumRelativeEffort || HrMaximumLoad < 1)
                throw new ArgumentException("Effort thresholds must fit the configured effort domain; HR load must allow 1.");
            PhysiologyNumbers.RequireRange(MinimumPowerEfficiency, PhysiologyNumbers.MinimumPositive, 1, nameof(MinimumPowerEfficiency));
            PhysiologyNumbers.RequireRange(FatigueRecoveryPenalty, 0, 1, nameof(FatigueRecoveryPenalty));
            // Runtime HR is quantized: bounds must be representable without rounding outside them.
            if (PhysiologyNumbers.Quantize(RestingHrBpm) != RestingHrBpm || PhysiologyNumbers.Quantize(HrSafetyCeilingBpm) != HrSafetyCeilingBpm)
                throw new ArgumentException("HR bounds require at most nine decimal places.");
            if (PhysiologyNumbers.Quantize(DriftSaturationSeconds) != DriftSaturationSeconds)
                throw new ArgumentException("Exposure bound requires at most nine decimal places.");
        }
    }
}

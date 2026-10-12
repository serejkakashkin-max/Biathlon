using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SkiConditionAffinity
    {
        private SkiConditionAffinity() { }
        [DataMember(Name = "snow_profile_id", IsRequired = true)] public string SnowProfileId { get; private set; }
        [DataMember(Name = "glide_multiplier", IsRequired = true)] public double GlideMultiplier { get; private set; }
    }
    [DataContract]
    public sealed class SkiEquipmentDefinition
    {
        private SkiEquipmentDefinition() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "glide_multiplier", IsRequired = true)] public double GlideMultiplier { get; private set; }
        [DataMember(Name = "grip_multiplier", IsRequired = true)] public double GripMultiplier { get; private set; }
        [DataMember(Name = "stability_multiplier", IsRequired = true)] public double StabilityMultiplier { get; private set; }
        [DataMember(Name = "condition_affinities", IsRequired = true)] private SkiConditionAffinity[] Affinities { get; set; }
        public int AffinityCount => Affinities.Length;
        public SkiConditionAffinity GetAffinity(int index) => Affinities[index];
        public double AffinityFor(string snowId)
        {
            if (snowId == null) throw new ArgumentNullException(nameof(snowId));
            for (int i = 0; i < Affinities.Length; i++)
                if (string.Equals(Affinities[i].SnowProfileId, snowId, StringComparison.Ordinal)) return Affinities[i].GlideMultiplier;
            return 1;
        }
        internal void Validate(double maximumDeviation, double maximumAffinityDeviation)
        {
            TrackValidation.Id(Id);
            TrackValidation.Finite(GlideMultiplier, 1 - maximumDeviation, 1 + maximumDeviation, nameof(GlideMultiplier));
            TrackValidation.Finite(GripMultiplier, 1 - maximumDeviation, 1 + maximumDeviation, nameof(GripMultiplier));
            TrackValidation.Finite(StabilityMultiplier, 1 - maximumDeviation, 1 + maximumDeviation, nameof(StabilityMultiplier));
            TrackValidation.Finite(GripMultiplier * StabilityMultiplier, 1 - maximumDeviation, 1 + maximumDeviation, "combined cornering effect");
            if (Affinities == null) throw new ArgumentException("Missing ski condition affinities.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var affinity in Affinities)
            {
                if (affinity == null) throw new ArgumentException("Null ski affinity.");
                TrackValidation.Id(affinity.SnowProfileId);
                if (!ids.Add(affinity.SnowProfileId)) throw new ArgumentException("Duplicate ski affinity.");
                TrackValidation.Finite(affinity.GlideMultiplier, 1 - maximumAffinityDeviation, 1 + maximumAffinityDeviation, "condition affinity");
                TrackValidation.Finite(GlideMultiplier * affinity.GlideMultiplier, 1 - maximumDeviation, 1 + maximumDeviation, "combined glide effect");
            }
        }
    }
    [DataContract]
    public sealed class SkiEquipmentCatalog
    {
        private SkiEquipmentCatalog() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "maximum_modifier_deviation", IsRequired = true)] public double MaximumModifierDeviation { get; private set; }
        [DataMember(Name = "maximum_affinity_deviation", IsRequired = true)] public double MaximumAffinityDeviation { get; private set; }
        [DataMember(Name = "skis", IsRequired = true)] private SkiEquipmentDefinition[] Skis { get; set; }
        private Dictionary<string, SkiEquipmentDefinition> byId;
        public int Count=>Skis.Length;
        public SkiEquipmentDefinition Get(int index)=>Skis[index];
        public SkiEquipmentDefinition GetSki(string id) => byId.TryGetValue(id, out var value) ? value : throw new ArgumentException("Unknown ski equipment.", nameof(id));
        public static SkiEquipmentCatalog FromJson(string json)
        {
            var v = RouteJson.Read<SkiEquipmentCatalog>(json);
            if (v == null || v.SchemaVersion != 1 || v.Skis == null || v.Skis.Length == 0) throw new SerializationException("Invalid ski catalog.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.MaximumModifierDeviation, 0, 1 - PhysiologyNumbers.MinimumPositive, nameof(MaximumModifierDeviation));
            TrackValidation.Finite(v.MaximumAffinityDeviation, 0, v.MaximumModifierDeviation, nameof(MaximumAffinityDeviation));
            v.byId = new Dictionary<string, SkiEquipmentDefinition>(StringComparer.Ordinal);
            foreach (var ski in v.Skis)
            {
                if (ski == null) throw new ArgumentException("Null ski definition.");
                ski.Validate(v.MaximumModifierDeviation, v.MaximumAffinityDeviation);
                if (v.byId.ContainsKey(ski.Id)) throw new ArgumentException("Duplicate ski ID.");
                v.byId.Add(ski.Id, ski);
            }
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

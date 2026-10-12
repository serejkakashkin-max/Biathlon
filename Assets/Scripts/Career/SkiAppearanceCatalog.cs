using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class SkiAppearanceDefinition
    {
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "base_color", IsRequired = true)] public string BaseColorHex { get; private set; }
        [DataMember(Name = "accent_color", IsRequired = true)] public string AccentColorHex { get; private set; }
        internal void Validate() { CareerUnits.Id(Id); ValidateColor(BaseColorHex); ValidateColor(AccentColorHex); }
        internal static void ValidateColor(string value)
        {
            if (value == null || value.Length != 7 || value[0] != '#') throw new SerializationException("Expected an opaque RGB ski color.");
            for (int i = 1; i < value.Length; i++) if (!Uri.IsHexDigit(value[i])) throw new SerializationException("Invalid ski color.");
        }
    }
    /// <summary>The same stable equipment ID selects its shop illustration and its actual ski prop. Visual only.</summary>
    [DataContract] public sealed class SkiAppearanceCatalog
    {
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "skis", IsRequired = true)] private SkiAppearanceDefinition[] skis { get; set; }
        public int Count => skis.Length;
        public SkiAppearanceDefinition GetAt(int index) => skis[index];
        public SkiAppearanceDefinition Get(string equipmentId)
        {
            if (equipmentId == null) throw new ArgumentNullException(nameof(equipmentId));
            foreach (var row in skis) if (row.Id == equipmentId) return row;
            throw new ArgumentException("No appearance is registered for selected skis: " + equipmentId, nameof(equipmentId));
        }
        public static SkiAppearanceCatalog FromJson(string json)
        {
            var result = CareerJson.Read<SkiAppearanceCatalog>(json);
            if (result == null || result.SchemaVersion != 1 || result.skis == null || result.skis.Length < 1 || result.skis.Length > 100) throw new SerializationException("Invalid ski appearance catalog.");
            CareerUnits.Id(result.Id); var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in result.skis) { if (row == null) throw new SerializationException("Missing ski appearance."); row.Validate(); if (!ids.Add(row.Id)) throw new SerializationException("Duplicate ski appearance ID."); }
            return result;
        }
    }
}

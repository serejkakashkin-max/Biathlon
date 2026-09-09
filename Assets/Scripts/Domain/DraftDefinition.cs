using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class DraftDefinition
    {
        private DraftDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "minimum_gap_m", IsRequired = true)] public double MinimumGap { get; private set; }
        [DataMember(Name = "maximum_gap_m", IsRequired = true)] public double MaximumGap { get; private set; }
        [DataMember(Name = "wake_half_width_m", IsRequired = true)] public double WakeHalfWidth { get; private set; }
        [DataMember(Name = "wake_expansion_per_m", IsRequired = true)] public double WakeExpansion { get; private set; }
        [DataMember(Name = "reference_speed_mps", IsRequired = true)] public double ReferenceSpeed { get; private set; }
        [DataMember(Name = "minimum_flow_speed_mps", IsRequired = true)] public double MinimumFlowSpeed { get; private set; }
        [DataMember(Name = "maximum_drag_reduction", IsRequired = true)] public double MaximumDragReduction { get; private set; }
        [DataMember(Name = "maximum_wind_mps", IsRequired = true)] public double MaximumWind { get; private set; }
        [DataMember(Name = "headwind_sensitivity", IsRequired = true)] public double HeadwindSensitivity { get; private set; }
        [DataMember(Name = "minimum_wind_factor", IsRequired = true)] public double MinimumWindFactor { get; private set; }
        [DataMember(Name = "maximum_wind_factor", IsRequired = true)] public double MaximumWindFactor { get; private set; }
        [DataMember(Name = "crosswind_displacement_factor", IsRequired = true)] public double CrosswindDisplacementFactor { get; private set; }
        public static DraftDefinition FromJson(string json)
        {
            var v = RouteJson.Read<DraftDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid draft schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.MinimumGap, 0.000000001, 1000000, nameof(MinimumGap));
            TrackValidation.Finite(v.MaximumGap, v.MinimumGap, 1000000, nameof(MaximumGap));
            if (v.MaximumGap <= v.MinimumGap) throw new ArgumentException("Draft gap interval must have positive length.");
            TrackValidation.Finite(v.WakeHalfWidth, 0.000000001, 1000000, nameof(WakeHalfWidth));
            TrackValidation.Finite(v.WakeExpansion, 0, 1, nameof(WakeExpansion));
            TrackValidation.Finite(v.ReferenceSpeed, 0.000000001, 1000000, nameof(ReferenceSpeed));
            TrackValidation.Finite(v.MinimumFlowSpeed, 0.000000001, 1000000, nameof(MinimumFlowSpeed));
            TrackValidation.Finite(v.MaximumDragReduction, 0, 1, nameof(MaximumDragReduction));
            TrackValidation.Finite(v.MaximumWind, 0, 1000000, nameof(MaximumWind));
            TrackValidation.Finite(v.HeadwindSensitivity, 0, 1, nameof(HeadwindSensitivity));
            TrackValidation.Finite(v.MinimumWindFactor, 0, 1, nameof(MinimumWindFactor));
            TrackValidation.Finite(v.MaximumWindFactor, 1, 1000000, nameof(MaximumWindFactor));
            TrackValidation.Finite(v.CrosswindDisplacementFactor, 0, 1, nameof(CrosswindDisplacementFactor));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

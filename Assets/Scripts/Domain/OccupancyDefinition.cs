using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class OccupancyDefinition
    {
        private OccupancyDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "body_length_m", IsRequired = true)] public double BodyLength { get; private set; }
        [DataMember(Name = "body_width_m", IsRequired = true)] public double BodyWidth { get; private set; }
        [DataMember(Name = "personal_clearance_m", IsRequired = true)] public double PersonalClearance { get; private set; }
        [DataMember(Name = "edge_clearance_m", IsRequired = true)] public double EdgeClearance { get; private set; }
        public double LongitudinalSeparation => BodyLength + PersonalClearance;
        public double LateralSeparation => BodyWidth + PersonalClearance;
        public double EdgeMargin => BodyWidth * 0.5 + EdgeClearance;
        public static OccupancyDefinition FromJson(string json)
        {
            var v = RouteJson.Read<OccupancyDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Invalid occupancy schema.");
            TrackValidation.Id(v.Id);
            TrackValidation.Finite(v.BodyLength, 0.000000001, 1000000, nameof(BodyLength));
            TrackValidation.Finite(v.BodyWidth, 0.000000001, 1000000, nameof(BodyWidth));
            TrackValidation.Finite(v.PersonalClearance, 0, 1000000, nameof(PersonalClearance));
            TrackValidation.Finite(v.EdgeClearance, 0, 1000000, nameof(EdgeClearance));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

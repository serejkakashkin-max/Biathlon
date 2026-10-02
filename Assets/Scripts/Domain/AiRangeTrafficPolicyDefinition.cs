using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    /// <summary>Optional decision policy for athletes using authored physical range paths.</summary>
    [DataContract] public sealed class AiRangeTrafficPolicyDefinition
    {
        private AiRangeTrafficPolicyDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="align_physical_range_paths",IsRequired=true)] public bool AlignPhysicalPaths { get; private set; }
        [DataMember(Name="alignment_lead_m",IsRequired=true)] public double AlignmentLeadMeters { get; private set; }
        public static AiRangeTrafficPolicyDefinition FromJson(string json)
        {
            var value=RouteJson.Read<AiRangeTrafficPolicyDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Unsupported AI range traffic policy.");
            TrackValidation.Id(value.Id);TrackValidation.Finite(value.AlignmentLeadMeters,.001,1000,nameof(AlignmentLeadMeters));return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

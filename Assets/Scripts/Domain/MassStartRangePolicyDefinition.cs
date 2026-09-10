using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract]
    public sealed class MassStartRangePolicyDefinition
    {
        private MassStartRangePolicyDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="first_bout",IsRequired=true)] public string FirstBout { get; private set; }
        [DataMember(Name="subsequent_bouts",IsRequired=true)] public string SubsequentBouts { get; private set; }
        public static MassStartRangePolicyDefinition FromJson(string json)
        {
            var v=RouteJson.Read<MassStartRangePolicyDefinition>(json);
            if(v==null || v.SchemaVersion!=1 || v.FirstBout!="start_number" || v.SubsequentBouts!="arrival_order")throw new SerializationException("Unsupported Mass Start lane policy.");
            TrackValidation.Id(v.Id);return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

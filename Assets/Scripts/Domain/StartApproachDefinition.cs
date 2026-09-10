using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract]
    public sealed class StartApproachDefinition
    {
        private StartApproachDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="track_id",IsRequired=true)] public string TrackId { get; private set; }
        [DataMember(Name="loop_id",IsRequired=true)] public string LoopId { get; private set; }
        [DataMember(Name="route_entry_loop_id",IsRequired=true)] public string RouteEntryLoopId { get; private set; }
        public static StartApproachDefinition FromJson(string json)
        {
            var d=RouteJson.Read<StartApproachDefinition>(json);
            if(d==null || d.SchemaVersion!=1)throw new SerializationException("Unsupported start approach schema.");
            TrackValidation.Id(d.Id);TrackValidation.Id(d.TrackId);TrackValidation.Id(d.LoopId);TrackValidation.Id(d.RouteEntryLoopId);return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

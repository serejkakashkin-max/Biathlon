using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class PhysicalJunctionPort
    {
        private PhysicalJunctionPort() { }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="anchor",IsRequired=true)] public TrackAnchor Anchor { get; private set; }
        [DataMember(Name="guard_before_m",IsRequired=true)] private double BeforeMeters { get; set; }
        [DataMember(Name="guard_after_m",IsRequired=true)] private double AfterMeters { get; set; }
        public TrackDistance GuardBefore { get; private set; }
        public TrackDistance GuardAfter { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            TrackValidation.Id(Id);if(Anchor==null) throw new ArgumentException("Junction anchor required.");
            var loop=Anchor.Validate(track);GuardBefore=TrackDistance.FromMeters(BeforeMeters);GuardAfter=TrackDistance.FromMeters(AfterMeters);
            long total=checked(GuardBefore.Millimeters+GuardAfter.Millimeters);
            if(total<=0 || total>=loop.Length.Millimeters) throw new ArgumentException("Junction guard must be nonempty and smaller than its loop.");
            if(!loop.Closed && (Anchor.Distance.Millimeters<GuardBefore.Millimeters || loop.Length.Millimeters-Anchor.Distance.Millimeters<GuardAfter.Millimeters))
                throw new ArgumentException("Open-loop junction guard exceeds track.");
        }
    }
    [DataContract] public sealed class PhysicalJunction
    {
        private PhysicalJunction() { }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="ports",IsRequired=true)] private PhysicalJunctionPort[] Ports { get; set; }
        public int PortCount=>Ports.Length;
        public PhysicalJunctionPort GetPort(int index)=>Ports[index];
        internal void Validate(TrackDefinition track,HashSet<string> portIds)
        {
            TrackValidation.Id(Id);if(Ports==null || Ports.Length<2) throw new ArgumentException("At least two explicit junction ports required.");
            foreach(var port in Ports)
            { if(port==null) throw new ArgumentException("Null junction port.");port.Validate(track);if(!portIds.Add(port.Id)) throw new ArgumentException("Duplicate junction port ID."); }
        }
    }
    [DataContract] public sealed class PhysicalJunctionDefinition
    {
        private PhysicalJunctionDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="track_id",IsRequired=true)] public string TrackId { get; private set; }
        [DataMember(Name="junctions",IsRequired=true)] private PhysicalJunction[] Junctions { get; set; }
        public string TrackSnapshotJson { get; private set; }
        public int Count=>Junctions.Length;
        public PhysicalJunction Get(int index)=>Junctions[index];
        public static PhysicalJunctionDefinition FromJson(string json,TrackDefinition track)
        {
            if(track==null) throw new ArgumentNullException(nameof(track));var v=RouteJson.Read<PhysicalJunctionDefinition>(json);
            if(v==null || v.SchemaVersion!=1) throw new SerializationException("Unsupported physical junction schema.");
            TrackValidation.Id(v.Id);if(v.TrackId!=track.Id || v.Junctions==null) throw new ArgumentException("Matching track and explicit junction list required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);var ports=new HashSet<string>(StringComparer.Ordinal);
            foreach(var junction in v.Junctions)
            { if(junction==null) throw new ArgumentException("Null junction.");junction.Validate(track,ports);if(!ids.Add(junction.Id)) throw new ArgumentException("Duplicate junction ID."); }
            v.TrackSnapshotJson=track.ToJson();return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

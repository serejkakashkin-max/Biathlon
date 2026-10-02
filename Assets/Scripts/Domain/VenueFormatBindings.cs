using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class VenueFormatBinding
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="profile_resource",IsRequired=true)] public string ProfileResource {get;private set;}
        [DataMember(Name="route_resource",IsRequired=true)] public string RouteResource {get;private set;}
        [DataMember(Name="directed_passages_resource",EmitDefaultValue=false)] public string DirectedPassagesResource {get;private set;}
        public CompetitionProfile Profile {get;private set;}
        public RaceRoutePlan Route {get;private set;}
        public PhysicalDirectedPassageDefinition DirectedPassages {get;private set;}
        internal void Bind(TrackDefinition track,PhysicalJunctionDefinition junctions,PhysicalConnectionDefinition connections,Func<string,string> read,OccupancyDefinition body)
        {
            if(Id!="sprint"&&Id!="pursuit"&&Id!="mass_start")throw new ArgumentException("Unknown venue competition format.");
            VenueManifest.ResourceId(ProfileResource);VenueManifest.ResourceId(RouteResource);
            Profile=CompetitionProfile.FromJson(read(ProfileResource));Route=RaceRoutePlan.FromJson(read(RouteResource));
            var expected=Id=="sprint"?CompetitionFormat.Sprint:Id=="pursuit"?CompetitionFormat.Pursuit:CompetitionFormat.MassStart;
            if(Profile.Format!=expected)throw new ArgumentException("Format binding and competition profile disagree.");
            PhysicalRoutePlanValidation.Validate(track,Route,Profile.ToRouteRequirements(),junctions,connections);
            if(Profile.PenaltyLoopDistance.Millimeters!=track.GetLoop(track.Penalty.LoopId).Length.Millimeters)
                throw new ArgumentException("Format penalty distance differs from the authored physical circuit.");
            // Legacy whole-loop routes cannot certify an open production branch.
            if(Route.SchemaVersion==1)for(int lap=0;lap<Route.LapCount;lap++)for(int i=0;i<Route.GetLap(lap).LoopCount;i++)
                if(!track.GetLoop(Route.GetLap(lap).GetLoopId(i)).Closed)throw new ArgumentException("Open competition branches require explicit directed slices.");
            if(DirectedPassagesResource!=null)
            {
                if(Profile.Format!=CompetitionFormat.Pursuit)throw new ArgumentException("The initial directed-passage policy binds Pursuit only.");
                VenueManifest.ResourceId(DirectedPassagesResource);
                DirectedPassages=PhysicalDirectedPassageDefinition.FromJson(read(DirectedPassagesResource),track,Route,junctions,connections,
                    body??throw new ArgumentException("Directed passages require the exact venue occupancy definition."),read);
            }
        }
    }
    [DataContract] public sealed class VenueFormatBindings
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="venue_id",IsRequired=true)] public string VenueId {get;private set;}
        [DataMember(Name="track_id",IsRequired=true)] public string TrackId {get;private set;}
        [DataMember(Name="formats",IsRequired=true)] private VenueFormatBinding[] Formats {get;set;}
        public int Count=>Formats.Length;
        public VenueFormatBinding Get(int index)=>Formats[index];
        public static VenueFormatBindings FromJson(string json,VenueManifest manifest,TrackDefinition track,
            PhysicalJunctionDefinition junctions,PhysicalConnectionDefinition connections,Func<string,string> read,OccupancyDefinition body=null)
        {
            if(manifest==null||track==null||read==null)throw new ArgumentNullException();var value=RouteJson.Read<VenueFormatBindings>(json);
            if(value==null||value.SchemaVersion!=1||value.Formats==null||value.Count<1||value.Count>3)throw new SerializationException("Invalid venue format catalog schema/count.");
            TrackValidation.Id(value.Id);
            if(value.VenueId!=manifest.Id||value.TrackId!=track.Id)throw new ArgumentException("Format catalog requires the exact venue and track IDs.");
            var ids=new HashSet<string>(StringComparer.Ordinal);var profiles=new HashSet<string>(StringComparer.Ordinal);var routes=new HashSet<string>(StringComparer.Ordinal);
            foreach(var item in value.Formats)
            {
                if(item==null||!ids.Add(item.Id))throw new ArgumentException("Null or duplicate format binding.");item.Bind(track,junctions,connections,read,body);
                if(!profiles.Add(item.Profile.Id)||!routes.Add(item.Route.Id))throw new ArgumentException("Format profile/route IDs must be distinct.");
            }
            return value;
        }
        public bool UsesLoop(string id)
        {foreach(var item in Formats)for(int lap=0;lap<item.Route.LapCount;lap++)for(int i=0;i<item.Route.GetLap(lap).LoopCount;i++)if(item.Route.GetLap(lap).GetLoopId(i)==id)return true;return false;}
        public string ToJson()=>RouteJson.Write(this);
    }
}

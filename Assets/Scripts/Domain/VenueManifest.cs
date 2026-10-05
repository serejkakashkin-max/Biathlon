using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class VenueVector
    {
        [DataMember(Name="x",IsRequired=true)] public double X { get; private set; }
        [DataMember(Name="y",IsRequired=true)] public double Y { get; private set; }
        [DataMember(Name="z",IsRequired=true)] public double Z { get; private set; }
        internal void Validate(){TrackValidation.Finite(X,-1000000,1000000,"x");TrackValidation.Finite(Y,-1000000,1000000,"y");TrackValidation.Finite(Z,-1000000,1000000,"z");}
    }
    [DataContract] public sealed class VenueCameraMarker
    {
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="anchor",IsRequired=true)] public TrackAnchor Anchor { get; private set; }
        [DataMember(Name="offset",IsRequired=true)] public VenueVector Offset { get; private set; }
        [DataMember(Name="look_offset",IsRequired=true)] public VenueVector LookOffset { get; private set; }
        [DataMember(Name="fov_deg",IsRequired=true)] public double FieldOfView { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            TrackValidation.Id(Id);if(Anchor==null||Offset==null||LookOffset==null)throw new ArgumentException("Camera anchor and offsets required.");
            Anchor.Validate(track);Offset.Validate();LookOffset.Validate();TrackValidation.Finite(FieldOfView,1,150,"camera fov");
            if(Offset.X==LookOffset.X&&Offset.Y==LookOffset.Y&&Offset.Z==LookOffset.Z)throw new ArgumentException("Camera must look away from its position.");
        }
    }
    [DataContract] public sealed class VenueAmbienceHook
    {
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="anchor",IsRequired=true)] public TrackAnchor Anchor { get; private set; }
        [DataMember(Name="event_id",IsRequired=true)] public string EventId { get; private set; }
        [DataMember(Name="radius_m",IsRequired=true)] public double Radius { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            TrackValidation.Id(Id);TrackValidation.Id(EventId);if(Anchor==null)throw new ArgumentException("Ambience anchor required.");
            Anchor.Validate(track);TrackValidation.Finite(Radius,.1,10000,"ambience radius");
        }
    }
    [DataContract] public sealed class VenueAssetPlacement
    {
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="resource",IsRequired=true)] public string Resource { get; private set; }
        [DataMember(Name="anchor",IsRequired=true)] public TrackAnchor Anchor { get; private set; }
        [DataMember(Name="offset",IsRequired=true)] public VenueVector Offset { get; private set; }
        [DataMember(Name="yaw_deg",IsRequired=true)] public double Yaw { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            TrackValidation.Id(Id);VenueManifest.ResourceId(Resource);if(Anchor==null||Offset==null)throw new ArgumentException("Asset placement required.");
            Anchor.Validate(track);Offset.Validate();TrackValidation.Finite(Yaw,-360,360,"asset yaw");
        }
    }
    [DataContract] public sealed class VenueRangeLayout
    {
        [DataMember(Name="approach",IsRequired=true)] public string Approach { get; private set; }
        [DataMember(Name="entry",IsRequired=true)] public TrackAnchor Entry { get; private set; }
        [DataMember(Name="exit",IsRequired=true)] public TrackAnchor Exit { get; private set; }
        [DataMember(Name="firing_origin",IsRequired=true)] public VenueVector Origin { get; private set; }
        [DataMember(Name="forward",IsRequired=true)] public VenueVector Forward { get; private set; }
        [DataMember(Name="lane_pitch_m",IsRequired=true)] public double LanePitch { get; private set; }
        [DataMember(Name="apron_depth_m",IsRequired=true)] public double ApronDepth { get; private set; }
        [DataMember(Name="mat_definition",EmitDefaultValue=false)] public VenueRangeMatDefinition MatDefinition { get; private set; }
        internal void Validate(TrackDefinition track)
        {
            if(Approach!="side_entry"||Entry==null||Exit==null||Origin==null||Forward==null)throw new ArgumentException("Explicit side-entry range layout required.");
            Entry.Validate(track);Exit.Validate(track);Origin.Validate();Forward.Validate();
            if(Math.Abs(Forward.Y)>1e-9||Math.Abs(Forward.X*Forward.X+Forward.Z*Forward.Z-1)>1e-9)throw new ArgumentException("Horizontal unit range direction required.");
            TrackValidation.Finite(LanePitch,1,10,"lane pitch");TrackValidation.Finite(ApronDepth,1,100,"range apron depth");
            if(MatDefinition!=null)MatDefinition.Validate();
        }
    }
    /// <summary>Versioned production authoring sidecar. Core coordinates remain in TrackDefinition.
    /// Parsing proves reference/schema validity, not geometric production readiness.</summary>
    [DataContract] public sealed class VenueManifest
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="track_resource",IsRequired=true)] public string TrackResource { get; private set; }
        [DataMember(Name="visual_resource",IsRequired=true)] public string VisualResource { get; private set; }
        [DataMember(Name="lanes_resource",EmitDefaultValue=false)] public string LanesResource { get; private set; }
        [DataMember(Name="paths_resource",EmitDefaultValue=false)] public string PathsResource { get; private set; }
        [DataMember(Name="occupancy_resource",EmitDefaultValue=false)] public string OccupancyResource { get; private set; }
        [DataMember(Name="start_approach_resource",EmitDefaultValue=false)] public string StartApproachResource { get; private set; }
        [DataMember(Name="start_continuation_resource",EmitDefaultValue=false)] public string StartContinuationResource { get; private set; }
        [DataMember(Name="format_bindings_resource",EmitDefaultValue=false)] public string FormatBindingsResource { get; private set; }
        [DataMember(Name="shared_apron_resource",EmitDefaultValue=false)] public string SharedApronResource { get; private set; }
        [DataMember(Name="ai_range_traffic_resource",EmitDefaultValue=false)] public string AiRangeTrafficResource { get; private set; }
        [DataMember(Name="junctions_resource",IsRequired=true)] public string JunctionsResource { get; private set; }
        [DataMember(Name="connections_resource",IsRequired=true)] public string ConnectionsResource { get; private set; }
        [DataMember(Name="range_layout",IsRequired=true)] public VenueRangeLayout Range { get; private set; }
        [DataMember(Name="cameras",IsRequired=true)] private VenueCameraMarker[] cameras { get; set; }
        [DataMember(Name="ambience",IsRequired=true)] private VenueAmbienceHook[] ambience { get; set; }
        [DataMember(Name="assets",IsRequired=true)] private VenueAssetPlacement[] assets { get; set; }
        public int CameraCount=>cameras.Length;public VenueCameraMarker Camera(int i)=>cameras[i];
        public int AmbienceCount=>ambience.Length;public VenueAmbienceHook Ambience(int i)=>ambience[i];
        public int AssetCount=>assets.Length;public VenueAssetPlacement Asset(int i)=>assets[i];
        public static VenueManifest FromJson(string json)
        {
            var v=RouteJson.Read<VenueManifest>(json);
            if(v==null||(v.SchemaVersion!=1&&v.SchemaVersion!=2)||v.Range==null||v.cameras==null||v.ambience==null||v.assets==null)throw new SerializationException("Invalid venue manifest schema.");
            if(v.SchemaVersion==1)
            {ResourceId(v.LanesResource);if(v.PathsResource!=null||v.OccupancyResource!=null||v.StartApproachResource!=null||v.StartContinuationResource!=null||v.FormatBindingsResource!=null||v.SharedApronResource!=null||v.Range.MatDefinition!=null)throw new SerializationException("Version 1 venues bind lanes only.");}
            else
            {ResourceId(v.PathsResource);ResourceId(v.OccupancyResource);if(v.StartApproachResource!=null)ResourceId(v.StartApproachResource);if(v.StartContinuationResource!=null){ResourceId(v.StartContinuationResource);if(v.StartApproachResource==null)throw new SerializationException("Start continuation requires its explicit approach binding.");}if(v.LanesResource!=null)throw new SerializationException("Version 2 venues require an explicit physical path and occupancy binding, without legacy lanes.");}
            if(v.FormatBindingsResource!=null)ResourceId(v.FormatBindingsResource);
            if(v.SharedApronResource!=null)ResourceId(v.SharedApronResource);
            if(v.AiRangeTrafficResource!=null)
            {if(v.SchemaVersion!=2)throw new SerializationException("Venue traffic requires physical range paths.");ResourceId(v.AiRangeTrafficResource);}
            ResourceId(v.Id);
            if(v.Id.IndexOf('/')>=0)throw new SerializationException("Venue ID must be a single path component.");
            if(v.Id=="range_entry"||v.Id=="range_exit"||v.Id.StartsWith("firing_lane_",StringComparison.Ordinal))throw new SerializationException("Venue ID is reserved by generated markers.");
            foreach(string resource in new[]{v.TrackResource,v.VisualResource,v.JunctionsResource,v.ConnectionsResource})ResourceId(resource);
            if(v.CameraCount<1||v.CameraCount>128||v.AmbienceCount>1024||v.AssetCount>10000)throw new SerializationException("Venue collection outside authoring bounds.");
            return v;
        }
        internal void Validate(TrackDefinition track)
        {
            Range.Validate(track);var ids=new HashSet<string>(StringComparer.Ordinal){Id,"range_entry","range_exit"};
            for(int i=0;i<track.LoopCount;i++){var loop=track.GetLoop(i);for(int j=0;j<loop.SegmentCount;j++)if(!ids.Add(loop.Id+"/"+loop.GetSegment(j).Id))throw new ArgumentException("Duplicate derived semantic marker ID.");}
            foreach(var c in cameras){if(c==null)throw new ArgumentException("Null camera.");c.Validate(track);Unique(c.Id);}
            foreach(var a in ambience){if(a==null)throw new ArgumentException("Null ambience.");a.Validate(track);Unique(a.Id);}
            foreach(var a in assets){if(a==null)throw new ArgumentException("Null asset.");a.Validate(track);Unique(a.Id);}
            void Unique(string id){if(id.StartsWith("firing_lane_",StringComparison.Ordinal)||!ids.Add(id))throw new ArgumentException("Duplicate or reserved venue marker ID: "+id);}
        }
        public static void ResourceId(string resource)
        {
            if(string.IsNullOrEmpty(resource)||resource.Length>256)throw new ArgumentException("Invalid resource ID.");
            foreach(string part in resource.Split('/'))
            {if(part.Length==0)throw new ArgumentException("Empty resource segment.");foreach(char c in part)if(!(c>='a'&&c<='z'||c>='A'&&c<='Z'||c>='0'&&c<='9'||c=='_'||c=='-'))throw new ArgumentException("Resource IDs must be relative extensionless paths.");}
        }
        public string ToJson()=>RouteJson.Write(this);
    }
    public readonly struct VenueSemanticZone
    {
        public string LoopId {get;} public string SegmentId {get;} public TrackSemanticZone Kind {get;}
        public double Start {get;} public double End {get;}
        internal VenueSemanticZone(string loop,TrackSegmentDefinition segment){LoopId=loop;SegmentId=segment.Id;Kind=segment.SemanticZone;Start=segment.StartDistance.Meters;End=Start+segment.Length.Meters;}
    }
    public sealed class VenueAuthoringPlan
    {
        public VenueManifest Manifest {get;} public TrackDefinition Track {get;} public TrackVisualDefinition Visual {get;}
        public PhysicalRangeLaneDefinition Lanes {get;} public PhysicalJunctionDefinition Junctions {get;} public PhysicalConnectionDefinition Connections {get;}
        public PhysicalRangePathDefinition Paths {get;} public OccupancyDefinition Occupancy {get;}
        public StartApproachDefinition StartApproach {get;}
        public PhysicalStartContinuationDefinition StartContinuation {get;}
        public VenueFormatBindings Formats {get;}
        public PhysicalSharedApronDefinition SharedApron {get;}
        public AiRangeTrafficPolicyDefinition AiRangeTraffic {get;}
        public bool UsesPhysicalPaths=>Paths!=null;
        public int LaneCount=>UsesPhysicalPaths?Paths.Count:Lanes.Count;
        public string LaneId(int index)=>UsesPhysicalPaths?Paths.GetLane(index).Id:Lanes.GetLane(index);
        public TrackVisualPoint MatPoint(int index)=>UsesPhysicalPaths?At(Paths.GetLane(index).MatAnchor):Visual.Get(Lanes.GetLane(index)).Sample(Track.Range.FiringLine.Distance.Meters-Lanes.Entry.Meters);
        public double FiringDatumDepth=>Manifest.Range.MatDefinition?.AthleteStopDepth??0;
        private readonly VenueSemanticZone[] zones;
        public int ZoneCount=>zones.Length; public VenueSemanticZone Zone(int i)=>zones[i];
        public TrackVisualPoint At(TrackAnchor anchor)=>Visual.Get(anchor.LoopId).Sample(anchor.Distance.Meters);
        public VenueAuthoringPlan(VenueManifest manifest,Func<string,string> readResource)
        {
            Manifest=manifest??throw new ArgumentNullException(nameof(manifest));if(readResource==null)throw new ArgumentNullException(nameof(readResource));
            Track=TrackDefinition.FromJson(readResource(manifest.TrackResource));Visual=TrackVisualDefinition.FromJson(readResource(manifest.VisualResource),Track);
            Junctions=PhysicalJunctionDefinition.FromJson(readResource(manifest.JunctionsResource),Track);Connections=PhysicalConnectionDefinition.FromJson(readResource(manifest.ConnectionsResource),Junctions);
            if(manifest.SchemaVersion==1)Lanes=PhysicalRangeLaneDefinition.FromJson(readResource(manifest.LanesResource),Track);
            else
            {
                Paths=PhysicalRangePathDefinition.FromJson(readResource(manifest.PathsResource),Track,Junctions,Connections);Occupancy=OccupancyDefinition.FromJson(readResource(manifest.OccupancyResource));
                if(manifest.AiRangeTrafficResource!=null)
                    AiRangeTraffic=AiRangeTrafficPolicyDefinition.FromJson(readResource(manifest.AiRangeTrafficResource));
                if(manifest.Range.MatDefinition!=null)manifest.Range.MatDefinition.Validate(Occupancy);
                if(manifest.SharedApronResource!=null)
                    SharedApron=PhysicalSharedApronDefinition.FromJson(readResource(manifest.SharedApronResource),Track,Visual,Paths,Junctions,Connections,Occupancy,readResource);
                if(manifest.StartApproachResource!=null)
                {
                    StartApproach=StartApproachDefinition.FromJson(readResource(manifest.StartApproachResource));
                    if(StartApproach.TrackId!=Track.Id||StartApproach.RouteEntryLoopId!=Paths.MainLoopId||StartApproach.LoopId==Paths.MainLoopId)
                        throw new ArgumentException("Start approach must bind this exact track and the physical paths' main route entry.");
                    Track.GetLoop(StartApproach.LoopId);
                }
                if(manifest.StartContinuationResource!=null)
                    StartContinuation=PhysicalStartContinuationDefinition.FromJson(readResource(manifest.StartContinuationResource),Track,
                        StartApproach,Junctions,Connections,Occupancy,readResource);
                if(manifest.FormatBindingsResource!=null)
                    Formats=VenueFormatBindings.FromJson(readResource(manifest.FormatBindingsResource),manifest,Track,Junctions,Connections,readResource,Occupancy);
                if(Formats!=null)for(int i=0;i<Formats.Count;i++)
                    if(Formats.Get(i).DirectedPassages!=null&&!Formats.Get(i).DirectedPassages.MatchesPresentation(Visual))
                        throw new ArgumentException("Directed passage rendering must match its exact authoritative world corridors.");
            }
            manifest.Validate(Track);var list=new List<VenueSemanticZone>();
            for(int i=0;i<Track.LoopCount;i++){var loop=Track.GetLoop(i);Visual.Get(loop.Id);for(int j=0;j<loop.SegmentCount;j++)list.Add(new VenueSemanticZone(loop.Id,loop.GetSegment(j)));}
            zones=list.ToArray();
        }
    }
}

using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>Maps pre-start travel either onto the legacy terminal main overlay or an
    /// explicitly selected authored open corridor with a directed end-to-main seam.</summary>
    public sealed class PhysicalStartApproachMap
    {
        public PhysicalTrackMap PhysicalMap { get; }
        public StartApproachMap Approach { get; }
        public string MainLoopId { get; }
        public string ContextJson { get; }
        public bool UsesAuthoredBranch { get; }
        private readonly long length;
        public PhysicalStartApproachMap(PhysicalTrackMap map, TrackDefinition track, RaceRoute route, StartApproachMap approach,
            bool authoredBranch=false,PhysicalJunctionDefinition junctions=null,PhysicalConnectionDefinition connections=null)
        {
            if(map==null || track==null || route==null || approach==null)throw new ArgumentNullException();
            if(map.TrackSnapshotJson!=track.ToJson() || approach.TrackJson!=map.TrackSnapshotJson ||
                approach.RoutePlanJson!=map.RoutePlanSnapshotJson || approach.RouteRequirementsJson!=map.RouteRequirementsSnapshotJson ||
                route.PlanSnapshotJson!=map.RoutePlanSnapshotJson || route.RequirementsSnapshotJson!=map.RouteRequirementsSnapshotJson)
                throw new ArgumentException("Exact start grid physical context required.");
            var main=track.GetLoop(route.GetSection(0).LoopId);var source=track.GetLoop(approach.LoopId);
            if(authoredBranch)
            {
                if(!map.IsBoundTo(track,route)||source.Closed||source.Kind!=TrackLoopKind.Race||source.Id==main.Id)
                    throw new ArgumentException("Exact open authored approach context required.");
                var seams=new PhysicalConnectionMap(map,junctions,connections);
                if(seams.FindTransition(map.MapLoop(source.Id,approach.LengthNanometers),map.MapRoute(0))<0)
                    throw new ArgumentException("Authored start approach needs a directed endpoint-to-main-start seam.");
                PhysicalMap=map;Approach=approach;MainLoopId=main.Id;UsesAuthoredBranch=true;
                length=approach.LengthNanometers;
                ContextJson=ShootingModelDefinitions.EncodeContext(new[]{approach.ContextJson,junctions.ToJson(),connections.ToJson(),"authored_open_start_branch_v1"});
                return;
            }
            var last=main.GetSegment(main.SegmentCount-1);var s=source.GetSegment(0);
            if(!main.Closed || source.SegmentCount!=1 || source.Length.Millimeters>=main.Length.Millimeters ||
                source.Length.Millimeters>last.Length.Millimeters || last.StartGrade!=last.EndGrade ||
                s.StartGrade!=last.StartGrade || s.EndGrade!=last.EndGrade || s.CurvaturePerMeter!=last.CurvaturePerMeter ||
                s.WidthMeters!=last.WidthMeters || s.SnowProfileId!=last.SnowProfileId || s.WindExposure!=last.WindExposure ||
                s.TerrainMuscleFactor!=last.TerrainMuscleFactor || s.DraftAllowed!=last.DraftAllowed ||
                s.OvertakeAllowed!=last.OvertakeAllowed || s.RiskLevel!=last.RiskLevel || s.SemanticZone!=last.SemanticZone)
                throw new ArgumentException("Grid approach must exactly match a constant terminal main-loop section.");
            PhysicalMap=map;Approach=approach;MainLoopId=main.Id;
            length=checked(main.Length.Millimeters*SkiState.NanometersPerMillimeter);
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{approach.ContextJson,"pre_start_main_loop_overlay_v1"});
        }
        public PhysicalTrackPoint MapRemaining(long remaining,PhysicalBoundarySide side=PhysicalBoundarySide.Outgoing)
        {
            if(remaining<0 || remaining>Approach.LengthNanometers||(side!=PhysicalBoundarySide.Outgoing&&side!=PhysicalBoundarySide.Incoming))throw new ArgumentOutOfRangeException(nameof(remaining));
            if(UsesAuthoredBranch&&remaining==0&&side==PhysicalBoundarySide.Incoming)return PhysicalMap.MapLoop(Approach.LoopId,length);
            return PhysicalMap.MapLoop(UsesAuthoredBranch&&remaining>0?Approach.LoopId:MainLoopId,remaining==0?0:length-remaining);
        }
    }
}

using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>Explicit venue policy: the grid occupies the end of the main circuit before
    /// the start line. This is one physical corridor, not a branch merge or route credit.</summary>
    public sealed class PhysicalStartApproachMap
    {
        public PhysicalTrackMap PhysicalMap { get; }
        public StartApproachMap Approach { get; }
        public string MainLoopId { get; }
        public string ContextJson { get; }
        private readonly long length;
        public PhysicalStartApproachMap(PhysicalTrackMap map, TrackDefinition track, RaceRoute route, StartApproachMap approach)
        {
            if(map==null || track==null || route==null || approach==null)throw new ArgumentNullException();
            if(map.TrackSnapshotJson!=track.ToJson() || approach.TrackJson!=map.TrackSnapshotJson ||
                approach.RoutePlanJson!=map.RoutePlanSnapshotJson || approach.RouteRequirementsJson!=map.RouteRequirementsSnapshotJson ||
                route.PlanSnapshotJson!=map.RoutePlanSnapshotJson || route.RequirementsSnapshotJson!=map.RouteRequirementsSnapshotJson)
                throw new ArgumentException("Exact start grid physical context required.");
            var main=track.GetLoop(route.GetSection(0).LoopId);var source=track.GetLoop(approach.LoopId);
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
        public PhysicalTrackPoint MapRemaining(long remaining)
        {
            if(remaining<0 || remaining>Approach.LengthNanometers)throw new ArgumentOutOfRangeException(nameof(remaining));
            return PhysicalMap.MapLoop(MainLoopId,remaining==0?0:length-remaining);
        }
    }
}

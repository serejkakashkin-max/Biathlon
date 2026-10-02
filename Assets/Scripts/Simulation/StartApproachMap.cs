using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>Authored open approach ending at the start of the base route. Local approach
    /// travel is distinct from route progress; physical connection admission is a field concern.</summary>
    public sealed class StartApproachMap
    {
        private readonly TrackLoopDefinition loop;
        private readonly TrackSampler sampler;
        public long LengthNanometers { get; }
        public string LoopId=>loop.Id;
        public string TrackJson { get; }
        public string RoutePlanJson { get; }
        public string RouteRequirementsJson { get; }
        public string ContextJson { get; }
        internal string DefinitionSnapshotJson { get; }
        public StartApproachMap(TrackDefinition track,RaceRoute route,StartApproachDefinition definition)
        {
            if(track==null || route==null || definition==null)throw new ArgumentNullException();
            TrackJson=track.ToJson();RoutePlanJson=route.PlanSnapshotJson;RouteRequirementsJson=route.RequirementsSnapshotJson;
            DefinitionSnapshotJson=definition.ToJson();
            if(definition.TrackId!=track.Id || route.TrackSnapshotJson!=TrackJson || route.GetSection(0).LoopId!=definition.RouteEntryLoopId)
                throw new ArgumentException("Exact approach track and route entry required.");
            loop=track.GetLoop(definition.LoopId);
            if(loop.Closed || loop.Kind!=TrackLoopKind.Race)throw new ArgumentException("Start approach must be an open race corridor.");
            for(int i=0;i<route.SectionCount;i++)if(route.GetSection(i).LoopId==loop.Id)throw new ArgumentException("Approach cannot also be a counted lap section.");
            sampler=new TrackSampler(loop);LengthNanometers=checked(loop.Length.Millimeters*SkiState.NanometersPerMillimeter);
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{definition.ToJson(),TrackJson,RoutePlanJson,RouteRequirementsJson,"approach_travel_not_route_progress_v1"});
        }
        public TrackSample Sample(long remainingNanometers)
        {
            if(remainingNanometers<=0 || remainingNanometers>LengthNanometers)throw new ArgumentOutOfRangeException(nameof(remainingNanometers));
            return sampler.Sample(new TrackDistance((LengthNanometers-remainingNanometers)/SkiState.NanometersPerMillimeter));
        }
        public long DistanceToBoundary(long remainingNanometers)
        {
            var sample=Sample(remainingNanometers);var segment=loop.GetSegment(sample.SegmentIndex);
            return checked((segment.StartDistance.Millimeters+segment.Length.Millimeters)*SkiState.NanometersPerMillimeter)-(LengthNanometers-remainingNanometers);
        }
    }
}

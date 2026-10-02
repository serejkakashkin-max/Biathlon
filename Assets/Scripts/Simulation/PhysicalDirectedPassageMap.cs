using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Exact direction/route-occurrence binding plus conservative world body queries.
    /// Contains/FindOccurrence are geometry/provenance primitives, never an admission grant.</summary>
    public sealed class PhysicalDirectedPassageMap
    {
        private readonly PhysicalTrackMap map;
        private readonly RaceRoute route;
        private readonly PhysicalDirectedPassageDefinition definition;
        private readonly int[][] occurrences;
        private readonly PhysicalPassageWorldProjection[] projections;
        public int Count=>definition.Count;
        public string ContextJson {get;}
        public int GetJunctionIndex(int passage)=>definition.Get(passage).JunctionIndex;
        public int GetConnectionIndex(int passage)=>definition.Get(passage).ConnectionIndex;
        public string SourceLoopId(int passage)=>definition.Get(passage).Source.LoopId;
        public string TargetLoopId(int passage)=>definition.Get(passage).Target.LoopId;
        public double ProjectionMarginMeters(int passage)=>projections[passage].ProjectionMarginMeters;
        public PhysicalDirectedPassageMap(PhysicalTrackMap map,TrackDefinition track,RaceRoute route,
            PhysicalJunctionDefinition junctions,PhysicalConnectionDefinition connections,OccupancyDefinition body,PhysicalDirectedPassageDefinition definition)
        {
            this.map=map??throw new ArgumentNullException(nameof(map));this.route=route??throw new ArgumentNullException(nameof(route));this.definition=definition??throw new ArgumentNullException(nameof(definition));
            if(track==null||junctions==null||connections==null||body==null)throw new ArgumentNullException();
            if(!map.IsBoundTo(track,route)||route.SchemaVersion!=2||definition.TrackSnapshotJson!=map.TrackSnapshotJson||definition.RoutePlanSnapshotJson!=route.PlanSnapshotJson||
                definition.JunctionSnapshotJson!=junctions.ToJson()||definition.ConnectionSnapshotJson!=connections.ToJson()||definition.OccupancySnapshotJson!=body.ToJson())
                throw new ArgumentException("Exact directed passage authority and route snapshots required.");
            occurrences=new int[Count][];projections=new PhysicalPassageWorldProjection[Count];
            for(int p=0;p<Count;p++)
            {
                var passage=definition.Get(p);var list=new List<int>();
                for(int i=0;i<route.SectionCount-1;i++)
                {
                    var source=route.GetSection(i);var target=route.GetSection(i+1);
                    if(target.EnterConnectionId!=passage.ConnectionId)continue;
                    if(source.LoopId!=passage.Source.LoopId||target.LoopId!=passage.Target.LoopId||source.LoopEnd.Meters!=passage.Source.End||target.LoopStart.Meters!=passage.Target.Start||
                        source.LoopStart.Meters>passage.Source.Start||target.LoopEnd.Meters<passage.Target.End)
                        throw new ArgumentException("A named occurrence does not follow the captured passage.");
                    list.Add(i);
                }
                if(list.Count==0)throw new ArgumentException("No exact directed passage occurrence in this route.");
                occurrences[p]=list.ToArray();projections[p]=new PhysicalPassageWorldProjection(passage.Source,passage.Target,body,definition.ValidationPolicy);
            }
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{definition.ToJson(),definition.ValidationPolicy.ToJson(),definition.TrackSnapshotJson,
                definition.RoutePlanSnapshotJson,definition.JunctionSnapshotJson,definition.ConnectionSnapshotJson,definition.OccupancySnapshotJson,route.RequirementsSnapshotJson,
                "directed_format_passages_v1;exact_two_ports_connections_route_occurrences;bounded_native_world_windows;whole_interval_rotating_rectangles;all_receipts_endpoints_and_provenanced_tails;unknown_and_other_regions_exclusive;outward_scaled_nm_and_ieee_reserve_v1"});
        }
        public int FindPassageForJunction(int junction)
        {for(int p=0;p<Count;p++)if(GetJunctionIndex(p)==junction)return p;return -1;}
        public bool MatchesOccurrence(int passage,int sourceSection)
        {foreach(int i in occurrences[passage])if(i==sourceSection)return true;return false;}
        public int FindOccurrence(int passage,long routeNanometers,PhysicalTrackPoint point)
        {foreach(int i in occurrences[passage])if(Contains(passage,i,routeNanometers,point))return i;return -1;}
        public bool Contains(int passage,PhysicalTrackPoint point)
        {
            map.Validate(point);var p=definition.Get(passage);
            return In(p.Source)||In(p.Target);
            bool In(PhysicalPassageCorridor c)=>point.LoopId==c.LoopId&&point.LocalNanometers>=Nanometers(c.Start)&&point.LocalNanometers<=Nanometers(c.End);
        }
        public bool Contains(int passage,PhysicalOccupancySweep sweep)
        {
            if(!Contains(passage,sweep.Start))return false;
            long end=checked(sweep.Start.LocalNanometers+sweep.TravelNanometers);
            if(sweep.Start.Closed&&end>=sweep.Start.LoopLengthNanometers)return false;
            var p=definition.Get(passage);var c=sweep.Start.LoopId==p.Source.LoopId?p.Source:p.Target;
            return end<=Nanometers(c.End);
        }
        public bool Contains(int passage,int sourceSection,long routeNanometers,PhysicalTrackPoint point)
        {
            if(!MatchesOccurrence(passage,sourceSection)||!Contains(passage,point))return false;
            var p=definition.Get(passage);var section=route.GetSection(point.LoopId==p.Source.LoopId?sourceSection:sourceSection+1);
            if(point.LocalNanometers<Nanometers(section.LoopStart.Meters)||point.LocalNanometers>Nanometers(section.LoopEnd.Meters))return false;
            long expected=checked(section.Start.Millimeters*SkiState.NanometersPerMillimeter+point.LocalNanometers-section.LoopStart.Millimeters*SkiState.NanometersPerMillimeter);
            return routeNanometers==expected;
        }
        public bool Conflicts(int passage,PhysicalOccupancySweep a,PhysicalOccupancySweep b)
        {
            if(!Contains(passage,a)||!Contains(passage,b))throw new ArgumentException("Only complete bounded passage sweeps may use its world proof.");
            if(a.AthleteId.Equals(b.AthleteId))throw new ArgumentException("Distinct directed passage bodies required.");
            return projections[passage].Conflicts(a,b);
        }
        private static long Nanometers(double wholeMillimetreMeters)=>checked(TrackDistance.FromMeters(wholeMillimetreMeters).Millimeters*SkiState.NanometersPerMillimeter);
    }
}

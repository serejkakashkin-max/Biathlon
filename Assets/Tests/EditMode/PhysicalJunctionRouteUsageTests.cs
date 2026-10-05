using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalJunctionRouteUsageTests
    {
        [Test] public void PursuitSkippedMainCrossingCannotReserveAnUnreachablePort()
        {
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);
            var format=plan.Formats.Get(2);var route=new RaceRoute(plan.Track,format.Route,format.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            var map=new PhysicalTrackMap(plan.Track,route);var layout=new PhysicalRangeFieldLayout(map,plan.Track,route,plan.Paths);
            var coarse=new PhysicalJunctionMap(map,plan.Junctions,PhysicalJunctionMap.CaptureUsage(plan.Track,route,layout));
            var bound=new PhysicalJunctionMap(map,plan.Junctions,plan.Track,route,layout,plan.Occupancy);
            int crossing=-1;for(int j=0;j<plan.Junctions.Count;j++)if(plan.Junctions.Get(j).Id=="apron_body_main_pursuit_shortcut")crossing=j;
            Assert.That(crossing,Is.GreaterThanOrEqualTo(0));
            Assert.That(coarse.IsRegionActive(crossing),Is.True,"The former loop-only snapshot reproduces the false admission lease.");
            Assert.That(bound.IsRegionActive(crossing),Is.False,"The Main interval covered by this crossing is skipped by every Pursuit lap.");
            Assert.That(bound.UsageContextJson,Does.Contain("exact_slice_usage_v2"));
            Assert.Throws<ArgumentException>(()=>bound.Contains(crossing,map.MapLoop("main",500*SkiState.NanometersPerMeter)));
            Assert.Throws<ArgumentException>(()=>bound.TouchesForwardSweep(crossing,map.MapLoop("main",200*SkiState.NanometersPerMeter),2*SkiState.NanometersPerMeter,750000000));
        }
        [TestCase(0,true)] [TestCase(.001,false)]
        public void ExactSliceReachabilityIncludesFullBodyAtGuardBoundary(double extraGap,bool reachable)
        {
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);
            var format=plan.Formats.Get(2);var route=new RaceRoute(plan.Track,format.Route,format.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            var map=new PhysicalTrackMap(plan.Track,route);var layout=new PhysicalRangeFieldLayout(map,plan.Track,route,plan.Paths);
            double end=route.GetSection(0).LoopEnd.Meters;
            string json="{\"schema_version\":1,\"id\":\"slice_boundary\",\"track_id\":\""+plan.Track.Id+"\",\"junctions\":[{\"id\":\"cross\",\"ports\":["+
                VenuePathAuthoringTests.Port("main_port","main",end+plan.Occupancy.LongitudinalSeparation/2+.001+extraGap,.001,.001)+","+
                VenuePathAuthoringTests.Port("branch_port","pursuit_shortcut",100,.001,.001)+"]}]}";
            var definition=PhysicalJunctionDefinition.FromJson(json,plan.Track);
            var bound=new PhysicalJunctionMap(map,definition,plan.Track,route,layout,plan.Occupancy);
            Assert.That(bound.IsRegionActive(0),Is.EqualTo(reachable));
            Assert.That(bound.TouchesForwardSweep(0,map.MapLoop("main",(long)Math.Round(end*SkiState.NanometersPerMeter)),0,
                (long)Math.Ceiling(plan.Occupancy.LongitudinalSeparation*SkiState.NanometersPerMeter/2)),Is.EqualTo(reachable));
        }
        [Test] public void ExactUsageHotQueriesRemainAllocationFreeAndPreserveReachableSelfCrossings()
        {
            Fixture(out var track,out var route,out var map,out var definition,out var layout);
            var bound=new PhysicalJunctionMap(map,definition,track,route,layout,OccupancyTests.Definition());
            Assert.That(bound.ActiveRegionCount,Is.EqualTo(definition.Count));
            var point=map.MapLoop("main",10*SkiState.NanometersPerMeter);
            for(int i=0;i<1000;i++)bound.TouchesForwardSweep(0,point,0,750000000);
            long before=GC.GetAllocatedBytesForCurrentThread();bool seen=false;
            for(int i=0;i<1000;i++)seen|=bound.TouchesForwardSweep(0,point,0,750000000);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(seen,Is.True);Assert.That(allocated,Is.Zero);
            string json="{\"schema_version\":1,\"id\":\"exact_self\",\"track_id\":\""+track.Id+"\",\"junctions\":[{\"id\":\"cross\",\"ports\":["+
                VenuePathAuthoringTests.Port("first","main",10,1,1)+","+VenuePathAuthoringTests.Port("second","main",200,1,1)+"]}]}";
            var self=new PhysicalJunctionMap(map,PhysicalJunctionDefinition.FromJson(json,track),track,route,layout,OccupancyTests.Definition());
            Assert.That(self.SharesJunction(map.MapLoop("main",10*SkiState.NanometersPerMeter),map.MapLoop("main",200*SkiState.NanometersPerMeter)),Is.True);
        }
        private static void Fixture(out TrackDefinition track,out RaceRoute route,out PhysicalTrackMap map,out PhysicalJunctionDefinition junctions,out PhysicalRangeFieldLayout layout)
        {
            var paths=PhysicalRangePathDefinitionTests.Create(out track,out junctions,out _,null);
            var requirements=CompetitionRouteRequirements.FromJson("{\"schema_version\":1,\"id\":\"usage_profile\",\"lap_count\":3,\"distance_m\":900,\"range_required_on_laps\":[1,2]}");
            route=new RaceRoute(track,RaceRoutePlan.TimeTrial("usage_route",track.Id,"main",requirements),requirements);map=new PhysicalTrackMap(track,route);layout=new PhysicalRangeFieldLayout(map,track,route,paths);
        }
        [Test] public void UnreachableBranchForkDoesNotSerializeTrafficButInactivePointsCannotEnterIt()
        {
            Fixture(out _,out _,out var map,out var definition,out _);var legacy=new PhysicalJunctionMap(map,definition);var usage=new[]{"main"};var bound=new PhysicalJunctionMap(map,definition,usage);usage[0]="mat_1";
            var point=map.MapLoop("main",10*SkiState.NanometersPerMeter);
            Assert.That(legacy.Contains(0,point),Is.True);Assert.That(bound.Contains(0,point),Is.False);Assert.That(bound.ActiveRegionCount,Is.Zero);
            Assert.Throws<ArgumentException>(()=>bound.Contains(0,map.MapLoop("mat_1",0)));
            Assert.Throws<ArgumentException>(()=>bound.TouchesForwardSweep(0,map.MapLoop("mat_1",0),0,0));
            Assert.That(bound.UsageContextJson,Does.Contain("main"));
        }
        [Test] public void ActualRoutePathsPenaltyAndStartUsageRetainEveryReachableLocalGuard()
        {
            Fixture(out var track,out var route,out var map,out var definition,out var layout);
            var captured=PhysicalJunctionMap.CaptureUsage(track,route,layout);var bound=new PhysicalJunctionMap(map,definition,captured);
            Assert.That(bound.ActiveRegionCount,Is.EqualTo(definition.Count));
            for(int j=0;j<definition.Count;j++)for(int p=0;p<definition.Get(j).PortCount;p++)
            {var anchor=definition.Get(j).GetPort(p).Anchor;Assert.That(bound.Contains(j,map.MapLoop(anchor.LoopId,anchor.Distance.Millimeters*SkiState.NanometersPerMillimeter)),Is.True);}
            var withStart=PhysicalJunctionMap.CaptureUsage(track,route,layout,"main");Assert.That(withStart,Is.EqualTo(captured));
            Assert.Throws<ArgumentException>(()=>new PhysicalJunctionMap(map,definition,new[]{"main","main"}));
            Assert.Throws<ArgumentException>(()=>new PhysicalJunctionMap(map,definition,new[]{"missing"}));
        }
        [Test] public void SameLoopSelfCrossingStillHasTwoReachablePortsAndRemainsExclusive()
        {
            Fixture(out var track,out _,out var map,out _,out _);
            string json="{\"schema_version\":1,\"id\":\"self_usage\",\"track_id\":\""+track.Id+"\",\"junctions\":[{\"id\":\"cross\",\"ports\":["+
                VenuePathAuthoringTests.Port("first","main",10,1,1)+","+VenuePathAuthoringTests.Port("second","main",200,1,1)+"]}]}";
            var definition=PhysicalJunctionDefinition.FromJson(json,track);var bound=new PhysicalJunctionMap(map,definition,new[]{"main"});
            Assert.That(bound.ActiveRegionCount,Is.EqualTo(1));Assert.That(bound.SharesJunction(map.MapLoop("main",10*SkiState.NanometersPerMeter),map.MapLoop("main",200*SkiState.NanometersPerMeter)),Is.True);
        }
        [Test] public void ProductionMassUsageExcludesFormatOnlyForksWhileKeepingRangePenaltyAndStartRegions()
        {
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);VenueFormatBinding mass=null,sprint=null;
            for(int i=0;i<plan.Formats.Count;i++){if(plan.Formats.Get(i).Id=="mass_start")mass=plan.Formats.Get(i);if(plan.Formats.Get(i).Id=="sprint")sprint=plan.Formats.Get(i);}
            PhysicalJunctionMap Bound(VenueFormatBinding format,bool start)
            {var route=new RaceRoute(plan.Track,format.Route,format.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);var map=new PhysicalTrackMap(plan.Track,route);var layout=new PhysicalRangeFieldLayout(map,plan.Track,route,plan.Paths);return new PhysicalJunctionMap(map,plan.Junctions,PhysicalJunctionMap.CaptureUsage(plan.Track,route,layout,start?plan.StartApproach.LoopId:null));}
            var massMap=Bound(mass,true);var sprintMap=Bound(sprint,false);int unused=0;
            for(int j=0;j<plan.Junctions.Count;j++)
            {
                bool branch=false,sprintBranch=false;var region=plan.Junctions.Get(j);
                for(int p=0;p<region.PortCount;p++){string id=region.GetPort(p).Anchor.LoopId;branch|=id=="sprint_extension"||id=="pursuit_shortcut";sprintBranch|=id=="sprint_extension";}
                if(branch){unused++;Assert.That(massMap.IsRegionActive(j),Is.False);if(sprintBranch)Assert.That(sprintMap.IsRegionActive(j),Is.True);}
                else Assert.That(massMap.IsRegionActive(j),Is.True);
            }
            Assert.That(unused,Is.EqualTo(5),"The compressed course also includes its measured Main/shortcut body-clearance region.");
        }
    }
}

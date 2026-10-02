using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalJunctionRouteUsageTests
    {
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
            Assert.That(unused,Is.EqualTo(4));
        }
    }
}

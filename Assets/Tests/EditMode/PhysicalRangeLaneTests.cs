using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangeLaneTests
    {
        private const long M = SkiState.NanometersPerMeter;
        public static TrackDefinition Track() => SyntheticTrackTests.Generated("synthetic_stadium_v1");
        public static string Json() => PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_stadium_lanes_v1.json");
        public static RaceRoute Route(TrackDefinition track)
        {
            var requirements = CompetitionProfileTests.TestProfile().ToRouteRequirements();
            return new RaceRoute(track, RaceRoutePlan.TimeTrial("stadium_route", track.Id, "main_long", requirements), requirements);
        }
        [Test] public void MappingSeparatesAssignedBodiesAndPreservesBothMouthsAndFinalLapBypass()
        {
            var track = Track(); var route = Route(track); var map = new PhysicalTrackMap(track, route);
            var definition = PhysicalRangeLaneDefinition.FromJson(Json(), track);
            Assert.That(definition.Count, Is.EqualTo(30));
            Assert.That(PhysicalRangeLaneDefinition.FromJson(definition.ToJson(), track).ToJson(), Is.EqualTo(definition.ToJson()));
            var a = new PhysicalRangeLaneMap(map, route, definition, 0); var b = new PhysicalRangeLaneMap(map, route, definition, 1);
            Assert.That(a.MapRoute(800*M, PhysicalBoundarySide.Incoming).LoopId, Is.EqualTo("main_long"));
            Assert.That(a.MapRoute(800*M).LoopId, Is.EqualTo("range_lane_1"));
            Assert.That(a.MapRoute(850*M).LocalNanometers, Is.EqualTo(50*M));
            Assert.That(map.SameLoop(a.MapRoute(850*M), b.MapRoute(850*M)), Is.False);
            Assert.That(a.MapRoute(900*M, PhysicalBoundarySide.Incoming).LocalNanometers, Is.EqualTo(100*M));
            Assert.That(a.MapRoute(900*M).LoopId, Is.EqualTo("main_long"));
            Assert.That(a.MapRoute(1850*M).LoopId, Is.EqualTo("range_lane_1"));
            Assert.That(a.MapRoute(2850*M).LoopId, Is.EqualTo("main_long"));
            var junctions = PhysicalJunctionDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_stadium_junctions_v1.json"), track);
            var connections = PhysicalConnectionDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_stadium_connections_v1.json"), junctions);
            var seams = new PhysicalConnectionMap(map, junctions, connections);
            var regions = new PhysicalJunctionMap(map,junctions);
            Assert.That(regions.Contains(0,map.MapLoop("main_long",900*M)),Is.True);
            Assert.That(regions.Contains(0,map.MapLoop("main_long",910*M)),Is.False,"Logical rejoin travel is not a second crossing.");
            Assert.That(seams.Transition("range_lane_1_enter", map.MapLoop("main_long",800*M),800*M).Outgoing, Is.EqualTo(a.MapRoute(800*M)));
            Assert.That(seams.Transition("range_lane_1_exit", a.MapRoute(900*M,PhysicalBoundarySide.Incoming),900*M).Outgoing, Is.EqualTo(map.MapLoop("main_long",900*M)));
        }
        [Test] public void InvalidLaneDefinitionsCannotHideDifferentPhysicsOrAmbiguousIdentity()
        {
            var track = Track(); string json = PhysicalRangeLaneDefinition.FromJson(Json(),track).ToJson();
            foreach (var pair in new[]{("schema_version","2"),("entry_distance_m","801"),("exit_distance_m","899")})
                Assert.Catch(()=>PhysicalRangeLaneDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,pair.Item1,pair.Item2),track));
            Assert.Catch(()=>PhysicalRangeLaneDefinition.FromJson(json.Replace("\"range_lane_2\"","\"range_lane_1\""),track));
            // Changing one lane's metadata cannot be hidden behind unchanged base-route skiing.
            string authored=track.ToJson(); int lane=authored.IndexOf("\"range_lane_1\"",StringComparison.Ordinal);
            string changed=authored.Substring(0,lane)+PhysiologyContractTests.ReplaceNumber(authored.Substring(lane),"terrain_muscle_factor","1.1");
            Assert.Catch(()=>PhysicalRangeLaneDefinition.FromJson(json,TrackDefinition.FromJson(changed)));
            var route=Route(track);var map=new PhysicalTrackMap(track,route);var definition=PhysicalRangeLaneDefinition.FromJson(json,track);
            Assert.Catch(()=>new PhysicalRangeLaneMap(map,route,definition,30));
            Assert.Catch(()=>new PhysicalMotionProjection(new PhysicalTrackMap(track,route),.05,new PhysicalRangeLaneMap(map,route,definition,0)));
        }
        [Test] public void ReceiptProjectionConservesTravelAndRequiresExplicitBranchBoundarySplits()
        {
            var track=Track();var route=Route(track);var map=new PhysicalTrackMap(track,route);
            var lane=new PhysicalRangeLaneMap(map,route,PhysicalRangeLaneDefinition.FromJson(Json(),track),0);
            var projection=new PhysicalMotionProjection(map,.05,lane);
            RaceMotionSegment Segment(long from,long to)=>(RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment),
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,null,new object[]{from,to,0L,to-from,0.0,.05,false},null);
            var receipt=projection.Project(Segment(840*M,850*M),lane.MapRoute(840*M));
            Assert.That(receipt.TravelNanometers,Is.EqualTo(10*M));Assert.That(receipt.End.LocalNanometers,Is.EqualTo(50*M));
            var slice=projection.Slice(receipt,new AthleteId("a"),0,1000000000,0,0);
            Assert.That(slice.TravelNanometers,Is.EqualTo(10*M));
            Assert.Catch(()=>projection.Project(Segment(799*M,801*M),lane.MapRoute(799*M)));
            Assert.Catch(()=>projection.Project(Segment(899*M,901*M),lane.MapRoute(899*M)));
        }
    }
}

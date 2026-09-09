using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class PhysicalTopologyTests
    {
        private static RaceRoute Route(TrackDefinition track)
        { var requirements=CompetitionProfileTests.TestProfile().ToRouteRequirements();return new RaceRoute(track,RaceRoutePlan.TimeTrial("physical",track.Id,"main_long",requirements),requirements); }
        private static PhysicalJunctionDefinition Junctions(TrackDefinition track)=>PhysicalJunctionDefinition.FromJson(
            PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_flat_junctions_v1.json"),track);
        [Test] public void LapsSharePhysicalCoordinatesAndClosedSeamPreservesNanometres()
        {
            var track=SyntheticTrackTests.Generated("synthetic_flat_v1");var map=new PhysicalTrackMap(track,Route(track));
            var first=map.MapRoute(123000000001);var lapped=map.MapRoute(1123000000001);
            Assert.That(lapped,Is.EqualTo(first));Assert.That(first.LocalNanometers,Is.EqualTo(123000000001));
            var before=map.MapRoute(999999999999);var after=map.MapRoute(1000000000001);
            Assert.That(map.ForwardDistance(before,after),Is.EqualTo(2));Assert.That(map.SignedShortestGap(before,after),Is.EqualTo(2));
            Assert.That(map.SignedShortestGap(after,before),Is.EqualTo(-2));
            Assert.That(map.MapRoute(1000000000000).LocalNanometers,Is.Zero);
            Assert.That(map.MapRoute(1000000000000,PhysicalBoundarySide.Incoming).LocalNanometers,Is.Zero);
            Assert.That(map.MapRoute(3000000000000).LocalNanometers,Is.Zero);
            var penalty=map.MapPenalty(150000000000);Assert.That(penalty.LocalNanometers,Is.Zero);
            Assert.That(map.SameLoop(penalty,map.MapRoute(0)),Is.False);
            Assert.Catch(()=>map.SignedShortestGap(penalty,first));
            Assert.Catch(()=>map.MapRoute(3000000000001));Assert.Catch(()=>map.MapPenalty(-1));
            Assert.Catch(()=>map.SameLoop(first,new PhysicalTrackMap(track,Route(track)).MapRoute(123000000001)));
        }
        [Test] public void OpenLoopKeepsIncomingEndAndRejectsWrapAndChangedTrackBinding()
        {
            var track=SyntheticTrackTests.Generated("synthetic_flat_v1");
            var open=TrackDefinition.FromJson(new Regex("\"closed\":true").Replace(track.ToJson(),"\"closed\":false",2));
            var map=new PhysicalTrackMap(open,Route(open));var incoming=map.MapRoute(1000000000000,PhysicalBoundarySide.Incoming);var outgoing=map.MapRoute(1000000000000);
            Assert.That(incoming.Closed,Is.False);Assert.That(incoming.LocalNanometers,Is.EqualTo(1000000000000));Assert.That(outgoing.LocalNanometers,Is.Zero);
            Assert.Catch(()=>map.ForwardDistance(incoming,outgoing));
            Assert.Catch(()=>new PhysicalTrackMap(open,Route(track)));
            var half=map.MapLoop("main_long",500000000000);Assert.That(map.SignedShortestGap(outgoing,half),Is.EqualTo(500000000000));
        }
        [Test] public void WholeBodySweepsFindCrossingsWithBothEndpointsOutsideAndFullCircuits()
        {
            var track=SyntheticTrackTests.Generated("synthetic_flat_v1");var map=new PhysicalTrackMap(track,Route(track));var junctions=new PhysicalJunctionMap(map,Junctions(track));
            var start=map.MapRoute(890000000000);var end=map.MapRoute(930000000000);
            Assert.That(junctions.Contains(0,start)||junctions.Contains(0,end),Is.False);
            Assert.That(junctions.TouchesForwardSweep(0,start,40000000000,0),Is.True);
            Assert.That(junctions.TouchesForwardSweep(0,start,7000000000,0),Is.False);
            Assert.That(junctions.TouchesForwardSweep(0,start,7000000000,1000000000),Is.True);
            Assert.That(junctions.TouchesForwardSweep(0,map.MapPenalty(147000000000),6000000000,0),Is.True);
            Assert.That(junctions.TouchesForwardSweep(0,map.MapPenalty(75000000000),150000000000,0),Is.True);
            Assert.That(junctions.TouchesForwardSweep(0,map.MapPenalty(75000000000),1000000000,0),Is.False);
            Assert.That(junctions.TouchesForwardSweep(0,map.MapRoute(922500000000),0,500000000),Is.True);
            Assert.Catch(()=>junctions.TouchesForwardSweep(0,start,-1,0));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++) junctions.TouchesForwardSweep(0,start,40000000000,500000000);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void JunctionGuardSharesBranchesWrapsSeamAndDoesNotDisappearAtRejoin()
        {
            var track=SyntheticTrackTests.Generated("synthetic_flat_v1");var map=new PhysicalTrackMap(track,Route(track));var definition=Junctions(track);var junctions=new PhysicalJunctionMap(map,definition);
            var entry=map.MapRoute(900000000000);var rejoin=map.MapRoute(920000000000);var tail=map.MapPenalty(149999999999);
            Assert.That(junctions.SharesJunction(entry,tail),Is.True);Assert.That(junctions.SharesJunction(rejoin,tail),Is.True);
            Assert.That(junctions.Contains(0,map.MapRoute(910000000000)),Is.True);
            Assert.That(junctions.Contains(0,map.MapRoute(922000000000)),Is.True);Assert.That(junctions.Contains(0,map.MapRoute(922000000001)),Is.False);
            Assert.That(junctions.Contains(0,map.MapPenalty(2000000000)),Is.True);Assert.That(junctions.Contains(0,map.MapPenalty(2000000001)),Is.False);
            Assert.That(junctions.SharesJunction(entry,map.MapPenalty(75000000000)),Is.False);
            Assert.That(PhysicalJunctionDefinition.FromJson(definition.ToJson(),track).ToJson(),Is.EqualTo(definition.ToJson()));
            Assert.Catch(()=>PhysicalJunctionDefinition.FromJson(definition.ToJson().Replace("\"id\":\"main_rejoin\"","\"id\":\"main_entry\""),track));
            Assert.Catch(()=>PhysicalJunctionDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(definition.ToJson(),"guard_before_m","1001"),track));
            Assert.Catch(()=>junctions.Contains(0,default));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){map.MapRoute(1123000000001);map.SignedShortestGap(entry,rejoin);junctions.SharesJunction(entry,tail);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}



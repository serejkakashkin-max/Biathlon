using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathMapTests
    {
        private const long M=SkiState.NanometersPerMeter;
        public static PhysicalRangePathDefinition Setup(out TrackDefinition track,out RaceRoute route,out PhysicalTrackMap map,
            out PhysicalRangePathMap a,out PhysicalRangePathMap b)
        {
            var definition=PhysicalRangePathDefinitionTests.Create(out track,out _,out _);
            var requirements=CompetitionRouteRequirements.FromJson("{\"schema_version\":1,\"id\":\"path_map_profile\",\"lap_count\":3,\"distance_m\":"+
                (track.GetLoop("main").Length.Meters*3).ToString(CultureInfo.InvariantCulture)+",\"range_required_on_laps\":[1,2]}");
            route=new RaceRoute(track,RaceRoutePlan.TimeTrial("path_map_route",track.Id,"main",requirements),requirements);
            map=new PhysicalTrackMap(track,route);a=new PhysicalRangePathMap(map,track,route,definition,0);b=new PhysicalRangePathMap(map,track,route,definition,1);return definition;
        }
        [Test] public void DistributedForksHaveExactIncomingOutgoingOwnershipAndIndividualMats()
        {
            Setup(out _,out var route,out _,out var a,out var b);
            Assert.That(a.MapRoute(10*M,PhysicalBoundarySide.Incoming).LoopId,Is.EqualTo("main"));
            Assert.That(a.MapRoute(10*M).LoopId,Is.EqualTo("mat_1"));Assert.That(a.MapRoute(10*M).LocalNanometers,Is.Zero);
            Assert.That(b.MapRoute(10*M).LoopId,Is.EqualTo("main"));
            Assert.That(a.MapRoute(50*M,PhysicalBoundarySide.Incoming).LocalNanometers,Is.EqualTo(40*M));
            Assert.That(a.MapRoute(50*M).LoopId,Is.EqualTo("exit_aisle"));Assert.That(a.MapRoute(50*M).LocalNanometers,Is.EqualTo(10*M));
            Assert.That(b.MapRoute(70*M).LocalNanometers,Is.EqualTo(30*M));
            Assert.That(a.MapRoute(100*M,PhysicalBoundarySide.Incoming).LoopId,Is.EqualTo("exit_aisle"));
            Assert.That(a.MapRoute(100*M).LoopId,Is.EqualTo("main"));
            Assert.That(a.FiringRouteNanometers(0),Is.EqualTo(28*M));Assert.That(b.FiringRouteNanometers(0),Is.EqualTo(48*M));
            Assert.That(a.LaneForkRouteNanometers(0),Is.EqualTo(10*M));
            Assert.That(b.LaneForkRouteNanometers(0),Is.EqualTo(30*M));
            Assert.That(b.LaneForkRouteNanometers(1),Is.EqualTo(route.GetLapStart(1).Millimeters*SkiState.NanometersPerMillimeter+30*M));
            Assert.That(a.MapRoute(a.FiringRouteNanometers(0)).LocalNanometers,Is.EqualTo(a.MatAnchor.Distance.Millimeters*SkiState.NanometersPerMillimeter));
            Assert.That(a.FiringRouteNanometers(1),Is.EqualTo(route.GetLapStart(1).Millimeters*SkiState.NanometersPerMillimeter+28*M));
        }
        [Test] public void SharedAislesKeepSharedOccupancyWhileMatPathsStayDistinct()
        {
            Setup(out _,out _,out var map,out var a,out var b);
            Assert.That(map.SameLoop(a.MapRoute(5*M),b.MapRoute(5*M)),Is.True);
            Assert.That(map.SameLoop(a.MapRoute(28*M),b.MapRoute(48*M)),Is.False);
            Assert.That(map.SameLoop(a.MapRoute(80*M),b.MapRoute(80*M)),Is.True);
            Assert.That(a.MapRoute(80*M).LocalNanometers,Is.EqualTo(b.MapRoute(80*M).LocalNanometers));
            var occupancy=new PhysicalOccupancySystem(map,OccupancyTests.Definition());
            var left=new PhysicalOccupancySweep(new AthleteId("a"),a.MapRoute(80*M),0,0,0);
            var right=new PhysicalOccupancySweep(new AthleteId("b"),b.MapRoute(80*M),0,0,0);
            Assert.That(occupancy.QueryRelativeMove(left,right).Blocked,Is.True,"A shared exit is never duplicated into invisible private occupancy.");
        }
        [Test] public void ProductionForkUsesAbsoluteMainLoopCoordinateAfterNonzeroRangeEntry()
        {
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);
            VenueFormatBinding format=null;
            for(int i=0;i<venue.Formats.Count;i++)if(venue.Formats.Get(i).Id=="mass_start")format=venue.Formats.Get(i);
            Assert.That(format,Is.Not.Null);
            var route=new RaceRoute(venue.Track,format.Route,format.Profile.ToRouteRequirements());
            var map=new PhysicalTrackMap(venue.Track,route);
            var lane=venue.Paths.GetLane(5);
            var physical=new PhysicalRangePathMap(map,venue.Track,route,venue.Paths,5);
            Assert.That(venue.Paths.Entry.Millimeters,Is.GreaterThan(0));
            long expected=long.MinValue;
            for(int i=0;i<route.SectionCount;i++)
            {
                var section=route.GetSection(i);
                if(section.LapIndex==0&&section.HasRange&&section.LoopId==venue.Paths.MainLoopId)
                    expected=(section.Start.Millimeters-section.LoopStart.Millimeters+lane.Fork.Millimeters)*SkiState.NanometersPerMillimeter;
            }
            Assert.That(expected,Is.GreaterThan(0));
            Assert.That(physical.LaneForkRouteNanometers(0),Is.EqualTo(expected));
            Assert.That(physical.MapRoute(expected,PhysicalBoundarySide.Incoming).LoopId,Is.EqualTo(venue.Paths.MainLoopId));
            Assert.That(physical.MapRoute(expected).LoopId,Is.EqualTo(lane.GetSlice(1).LoopId));
        }
        [Test] public void ActualSamplesExposePrivateCurvatureAndWidthAndBypassFinalLap()
        {
            Setup(out var track,out var route,out _,out var a,out var b);
            var sample=a.SampleRoute(20*M);Assert.That(sample.LoopId,Is.EqualTo("mat_1"));
            Assert.That(sample.CurvaturePerMeter,Is.EqualTo(.08));Assert.That(sample.WidthMeters,Is.EqualTo(2));
            Assert.That(b.SampleRoute(20*M).LoopId,Is.EqualTo("main"));Assert.That(b.SampleRoute(20*M).WidthMeters,Is.EqualTo(6));
            long final=route.GetLapStart(2).Millimeters*SkiState.NanometersPerMillimeter+20*M;
            Assert.That(a.SampleRoute(final).LoopId,Is.EqualTo("main"));Assert.That(a.MapRoute(final).LoopId,Is.EqualTo("main"));
            Assert.That(a.SampleRoute(route.Length.Millimeters*SkiState.NanometersPerMillimeter).SegmentFraction,Is.EqualTo(1));
            Assert.That(a.ContextJson,Does.Contain("actual_path_sample"));Assert.That(a.ContextJson,Is.Not.EqualTo(b.ContextJson));
        }
        [Test] public void EveryPhysicalSliceBoundaryIsExposedWithoutChangingTravel()
        {
            Setup(out _,out _,out _,out var a,out var b);
            Assert.That(a.DistanceToNextBoundary(5*M),Is.EqualTo(5*M));
            Assert.That(a.DistanceToNextBoundary(10*M),Is.EqualTo(20*M),"Private terrain boundary must split the motion before its physical slice ends.");
            Assert.That(a.DistanceToNextBoundary(50*M),Is.EqualTo(50*M));
            foreach(var path in new[]{a,b})
            {
                long distance=0;int steps=0;
                while(distance<100*M){long next=path.DistanceToNextBoundary(distance);Assert.That(next,Is.GreaterThan(0));distance+=next;Assert.That(++steps,Is.LessThan(20));}
                Assert.That(distance,Is.EqualTo(100*M));
            }
        }
        [Test] public void ForeignSnapshotsInvalidCoordinatesAndNonShootingLapFail()
        {
            var definition=Setup(out var track,out var route,out var map,out var a,out _);
            Assert.Catch(()=>new PhysicalRangePathMap(map,track,route,definition,2));
            var requirements=CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson);
            var other=new RaceRoute(track,RaceRoutePlan.TimeTrial("other_plan",track.Id,"main",requirements),requirements);
            Assert.Catch(()=>new PhysicalRangePathMap(map,track,other,definition,0));
            Assert.Catch(()=>a.MapRoute(-1));Assert.Catch(()=>a.SampleRoute(-1));Assert.Catch(()=>a.DistanceToNextBoundary(-1));
            Assert.Catch(()=>a.MapRoute(0,(PhysicalBoundarySide)20));Assert.Catch(()=>a.FiringRouteNanometers(2));
            Assert.Throws<ArgumentException>(()=>a.LaneForkRouteNanometers(2));
            Assert.Throws<ArgumentOutOfRangeException>(()=>a.LaneForkRouteNanometers(-1));
            Assert.That(a.DistanceToNextBoundary(route.Length.Millimeters*SkiState.NanometersPerMillimeter),Is.Zero);
        }
        [Test] public void WarmMappingSamplingAndBoundaryQueriesAllocateNothing()
        {
            Setup(out _,out _,out _,out var a,out _);long sum=0;
            for(int i=0;i<1000;i++){sum+=a.MapRoute(20*M).LocalNanometers;sum+=a.SampleRoute(20*M).SegmentIndex;sum+=a.DistanceToNextBoundary(20*M);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<10000;i++){sum+=a.MapRoute(20*M).LocalNanometers;sum+=a.SampleRoute(20*M).SegmentIndex;sum+=a.DistanceToNextBoundary(20*M);}
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);Assert.That(sum,Is.GreaterThan(0));
        }
        [Test] public void PhysicalMatCannotMoveBeyondLogicalRangeExit()
        {
            string changed=Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("paths"),"\"mat_anchor\"\\s*:\\s*\\{[^}]*\\}",
                m=>m.Value.Contains("\"mat_2\"")?"\"mat_anchor\":{\"loop_id\":\"exit_aisle\",\"distance_m\":59}":m.Value);
            Assert.Throws<ArgumentException>(()=>PhysicalRangePathDefinitionTests.Create(out _,out _,out _,changed));
        }
    }
}

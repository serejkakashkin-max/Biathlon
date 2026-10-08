using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualRacePathCatalogTests
    {
        private const long N=SkiState.NanometersPerMeter;
        private const string Root="Biathlon/Venues/woodland_basin_v1/";
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static long Nano(double metres)=>(long)Math.Round(metres*N,MidpointRounding.ToEven);
        internal sealed class Fixture
        {
            internal readonly VenueAuthoringPlan Venue;
            internal readonly CompetitionProfile Profile;
            internal readonly RaceRoute Route;
            internal readonly PhysicalRangePathMap Path;
            internal readonly StartApproachMap Start;
            internal readonly ManualRacePathCatalog Catalog;
            internal Fixture(string format="mass_start",int lane=0)
            {
                Venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Root+"manifest")),Read);
                Profile=CompetitionProfile.FromJson(Read(Root+format+"_profile"));
                Route=new RaceRoute(Venue.Track,RaceRoutePlan.FromJson(Read(Root+format+"_route")),
                    Profile.ToRouteRequirements(),Venue.Junctions,Venue.Connections);
                Path=new PhysicalRangePathMap(new PhysicalTrackMap(Venue.Track,Route),Venue.Track,Route,Venue.Paths,lane);
                Start=format=="mass_start"?new StartApproachMap(Venue.Track,Route,Venue.StartApproach):null;
                Catalog=new ManualRacePathCatalog(Venue.Track,Route,Profile,Venue.Visual,Path,Start);
            }
            internal ManualSkiHeadingState At(int index,long arc,double side=0,double? heading=null)
            {
                var row=Catalog.Get(index);var point=Venue.Visual.Get(row.LoopId).Sample((row.LocalStartNanometers+arc)/(double)N);
                return new ManualSkiHeadingState(point.X+point.ForwardZ*side,point.Z-point.ForwardX*side,
                    heading??Math.Atan2(point.ForwardX,point.ForwardZ));
            }
        }
        [TestCase("mass_start",0)][TestCase("mass_start",29)]
        [TestCase("sprint",0)][TestCase("sprint",29)]
        [TestCase("pursuit",0)][TestCase("pursuit",29)]
        public void CountedFlowCoversEverySelectedSectionAndSplitsExactFiringAndPenaltyAnchors(string format,int lane)
        {
            var f=new Fixture(format,lane);var c=f.Catalog;long counted=0,physical=0;int firing=0;
            for(int i=0;i<c.BaseFlowCount;i++)
            {
                var row=c.Get(i);Assert.That(row.Index,Is.EqualTo(i));Assert.That(row.Id,Is.Not.Empty);
                Assert.That(row.LengthNanometers,Is.GreaterThan(0));Assert.That(row.RouteStartNanometers,Is.EqualTo(counted));
                physical+=row.LengthNanometers;
                if(row.CountsTowardsRoute)counted=row.RouteEndNanometers;
                else Assert.That(row.Kind,Is.EqualTo(ManualRacePathKind.StartApproach));
                if(row.End==ManualRacePathEnd.FiringLine)
                {
                    firing++;Assert.That(row.Kind,Is.EqualTo(ManualRacePathKind.RangeApproach));
                    Assert.That(row.RouteEndNanometers,Is.EqualTo(f.Path.FiringRouteNanometers(row.LapIndex)));
                    Assert.That(row.LoopId,Is.EqualTo(f.Path.MatAnchor.LoopId));
                    Assert.That(row.LocalEndNanometers,Is.EqualTo(f.Path.MatAnchor.Distance.Millimeters*SkiState.NanometersPerMillimeter));
                    var next=c.Get(c.GetBaseFlowSuccessor(i));Assert.That(next.Kind,Is.EqualTo(ManualRacePathKind.RangeExit));
                    Assert.That(next.LocalStartNanometers,Is.EqualTo(row.LocalEndNanometers));Assert.That(next.LoopId,Is.EqualTo(row.LoopId));
                }
                Assert.That(c.GetBaseFlowSuccessor(i),Is.EqualTo(i+1<c.BaseFlowCount?i+1:-1));
            }
            Assert.That(counted,Is.EqualTo(f.Route.Length.Millimeters*SkiState.NanometersPerMillimeter));
            Assert.That(physical-counted,Is.EqualTo(f.Start?.LengthNanometers??0));Assert.That(firing,Is.EqualTo(f.Profile.BoutCount));
            Assert.That(c.Get(c.BaseFlowCount-1).End,Is.EqualTo(ManualRacePathEnd.Finish));
            for(int b=0;b<f.Profile.BoutCount;b++)
            {
                var entry=c.Get(c.GetPenaltyEntryOccurrence(b));var rejoin=c.Get(c.GetPenaltyReturnOccurrence(b));
                Assert.That(entry.End,Is.EqualTo(ManualRacePathEnd.PenaltyDecision));
                Assert.That(rejoin.RouteStartNanometers,Is.EqualTo(entry.RouteEndNanometers));
                Assert.That(rejoin.LocalStartNanometers,Is.EqualTo(f.Venue.Track.Penalty.Entry.Distance.Millimeters*SkiState.NanometersPerMillimeter));
                Assert.That(c.Get(c.GetFiringOccurrence(b)).BoutIndex,Is.EqualTo(b));
                for(int p=1;p<=f.Profile.ShotsPerBout;p++)
                {
                    var circuit=c.Get(c.GetPenaltyOccurrence(b,p));Assert.That(circuit.PenaltyOrdinal,Is.EqualTo(p));
                    Assert.That(circuit.RouteStartNanometers,Is.EqualTo(entry.RouteEndNanometers));
                    Assert.That(circuit.RouteEndNanometers,Is.EqualTo(entry.RouteEndNanometers));
                    Assert.That(circuit.LengthNanometers,Is.EqualTo(f.Profile.PenaltyLoopDistance.Millimeters*SkiState.NanometersPerMillimeter));
                    Assert.That(c.GetBaseFlowSuccessor(circuit.Index),Is.EqualTo(-1));
                }
            }
        }
        [TestCase("mass_start",0)][TestCase("mass_start",29)][TestCase("sprint",0)][TestCase("pursuit",29)]
        public void AllPhysicalOccurrencesObserveActualClippedGeometryWithoutChangingPoseOrSelectingAnotherLap(string format,int lane)
        {
            var f=new Fixture(format,lane);int points=0;
            for(int i=0;i<f.Catalog.Count;i++)
            {
                var row=f.Catalog.Get(i);var loop=f.Venue.Visual.Get(row.LoopId);
                for(int k=1;k<loop.Count;k++)
                {
                    double from=Math.Max(row.LocalStartNanometers/(double)N,loop.Get(k-1).Distance);
                    double to=Math.Min(row.LocalEndNanometers/(double)N,loop.Get(k).Distance);if(to<=from)continue;
                    double local=(from+to)*.5;long arc=Nano(local)-row.LocalStartNanometers;
                    var pose=f.At(i,arc,.2,.7);var unchanged=pose;
                    Assert.That(f.Catalog.TryProject(pose,i,arc,.001,out var observation),Is.True,$"{row.Id} knot {k}");
                    Assert.That(observation.OccurrenceIndex,Is.EqualTo(i));Assert.That(observation.LoopId,Is.EqualTo(row.LoopId));
                    Assert.That(observation.PhysicalArcNanometers,Is.EqualTo(arc).Within(20));
                    Assert.That(observation.MappedRouteNanometers,Is.EqualTo(row.RouteStartNanometers+(row.CountsTowardsRoute?arc:0)).Within(20));
                    Assert.That(observation.LocalMeters,Is.EqualTo(local).Within(2e-8));Assert.That(pose,Is.EqualTo(unchanged));
                    Assert.That(f.Catalog.TrySample(i,arc,out var target),Is.True);
                    var expected=loop.Sample(local);Assert.That(target.X,Is.EqualTo(expected.X).Within(2e-8));
                    Assert.That(target.Y,Is.EqualTo(expected.Y).Within(2e-8));Assert.That(target.Z,Is.EqualTo(expected.Z).Within(2e-8));
                    var surface=f.Catalog.SampleSurface(observation);
                    Assert.That(surface.LoopId,Is.EqualTo(row.LoopId));Assert.That(surface.WidthMeters,Is.GreaterThan(0));points++;
                }
            }
            Assert.That(points,Is.GreaterThan(100));
        }
        [Test] public void RepeatedPenaltyCoordinatesRetainExplicitBoutAndOrdinalAndNeverEarnBaseRouteDistance()
        {
            var f=new Fixture();var c=f.Catalog;long arc=40*N;
            var first=c.GetPenaltyOccurrence(0,1);var pose=f.At(first,arc);
            for(int b=0;b<f.Profile.BoutCount;b++)for(int p=1;p<=f.Profile.ShotsPerBout;p++)
            {
                int index=c.GetPenaltyOccurrence(b,p);var row=c.Get(index);
                Assert.That(c.TryProject(pose,index,arc,.001,out var observed),Is.True);
                Assert.That(observed.OccurrenceIndex,Is.EqualTo(index));Assert.That(observed.PhysicalArcNanometers,Is.EqualTo(arc).Within(20));
                Assert.That(observed.MappedRouteNanometers,Is.EqualTo(row.RouteStartNanometers));
                Assert.That(c.TryProject(f.At(index,arc-N),index,arc,1.01,out var backward),Is.True);
                Assert.That(backward.PhysicalArcNanometers,Is.LessThan(arc));
                Assert.That(backward.MappedRouteNanometers,Is.EqualTo(observed.MappedRouteNanometers));
            }
            int start=c.StartApproachIndex;long mid=c.Get(start).LengthNanometers/2;
            Assert.That(c.TryProject(f.At(start,mid),start,mid,.01,out var approach),Is.True);
            Assert.That(approach.PhysicalArcNanometers,Is.GreaterThan(0));Assert.That(approach.MappedRouteNanometers,Is.Zero);
        }
        [Test] public void PrivateMatsStayDistinctAndSharedExitKeepsOnePhysicalIdentity()
        {
            var a=new Fixture(lane:0);var b=new Fixture(lane:29);
            var ma=a.Catalog.Get(a.Catalog.GetFiringOccurrence(0));var mb=b.Catalog.Get(b.Catalog.GetFiringOccurrence(0));
            Assert.That(ma.LoopId,Is.Not.EqualTo(mb.LoopId));
            var ea=a.Catalog.Get(a.Catalog.GetPenaltyEntryOccurrence(0));var eb=b.Catalog.Get(b.Catalog.GetPenaltyEntryOccurrence(0));
            Assert.That(ea.LoopId,Is.EqualTo(eb.LoopId));Assert.That(ea.LocalEndNanometers,Is.EqualTo(eb.LocalEndNanometers));
            Assert.That(a.Catalog.Id,Is.Not.EqualTo(b.Catalog.Id));Assert.That(a.Catalog.ContextJson,Is.Not.EqualTo(b.Catalog.ContextJson));
            Assert.That(a.Catalog.ContextJson,Does.Contain("physical_arc_distinct_from_counted_route"));
        }
        [Test] public void EveryScopedEndpointCanBeObservedInItsExactZeroWidthStationWindow()
        {
            var f=new Fixture();int observed=0;
            for(int i=0;i<f.Catalog.Count;i++)
            {
                var row=f.Catalog.Get(i);
                foreach(long arc in new[]{0L,row.LengthNanometers})
                {
                    Assert.That(f.Catalog.TryProject(f.At(i,arc),i,arc,0,out var point),Is.True,
                        $"{row.Id} arc={arc}");
                    Assert.That(point.PhysicalArcNanometers,Is.EqualTo(arc));observed++;
                    Assert.That(f.Catalog.TrySample(i,arc,out var target),Is.True,$"sample {row.Id} arc={arc}");
                    Assert.That(target.PhysicalArcNanometers,Is.EqualTo(arc));
                }
            }
            Assert.That(observed,Is.EqualTo(f.Catalog.Count*2));
        }
        [Test] public void DirectionalSlopeAndSurfaceFollowActualStartRangeAndBaseObservations()
        {
            var f=new Fixture();int seen=0;
            for(int i=0;i<f.Catalog.Count;i++)
            {
                var row=f.Catalog.Get(i);long arc=row.LengthNanometers/2;var forward=f.At(i,arc);
                var backward=f.At(i,arc,heading:forward.HeadingRadians+Math.PI);
                Assert.That(f.Catalog.TryProject(backward,i,arc,.01,out var observation),Is.True);
                Assert.That(observation.GradeAlong(backward),Is.EqualTo(-observation.GradeAlong(forward)).Within(2e-8));
                var actual=new TrackSampler(f.Venue.Track.GetLoop(row.LoopId)).Sample(
                    new TrackDistance((row.LocalStartNanometers+observation.PhysicalArcNanometers)/SkiState.NanometersPerMillimeter));
                var surface=f.Catalog.SampleSurface(observation);
                Assert.That(surface.SegmentId,Is.EqualTo(actual.SegmentId));Assert.That(surface.TerrainMuscleFactor,Is.EqualTo(actual.TerrainMuscleFactor));seen++;
            }
            Assert.That(seen,Is.EqualTo(f.Catalog.Count));
        }
        [Test] public void ForeignContextAndInvalidArcsFailAndScopedGeometryCannotBecomeALegacyBaseLedger()
        {
            var f=new Fixture();var foreign=new RaceRoute(f.Venue.Track,RaceRoutePlan.FromJson(Read(Root+"mass_start_route")),
                f.Profile.ToRouteRequirements(),f.Venue.Junctions,f.Venue.Connections);
            Assert.Throws<ArgumentException>(()=>new ManualRacePathCatalog(f.Venue.Track,foreign,f.Profile,f.Venue.Visual,f.Path,f.Start));
            Assert.Throws<ArgumentException>(()=>new ManualRacePathCatalog(f.Venue.Track,f.Route,
                CompetitionProfile.FromJson(Read(Root+"sprint_profile")),f.Venue.Visual,f.Path));
            var c=f.Catalog;Assert.Throws<ArgumentException>(()=>c.TryProject(default,0,0,1,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>c.TrySample(-1,0,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>c.TrySample(0,c.Get(0).LengthNanometers+1,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>c.TryProject(f.At(0,0),0,-1,1,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>c.GetPenaltyOccurrence(0,0));
            Assert.Throws<ArgumentException>(()=>c.SampleSurface(default));
            var another=new ManualRacePathCatalog(f.Venue.Track,f.Route,f.Profile,f.Venue.Visual,f.Path,f.Start);
            Assert.That(c.TryProject(f.At(0,10*N),0,10*N,.01,out var observation),Is.True);
            Assert.Throws<ArgumentException>(()=>another.SampleSurface(observation));
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"catalog_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"catalog_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var progress=new ManualRouteProgression(f.Route);var id=new AthleteId("catalog_actor");
            var sweep=new ManualRouteJointSweep(c.ProjectionGeometry,f.Venue.Occupancy,contact,work);
            Assert.Throws<ArgumentException>(()=>new ManualWorldRosterLedger(f.Route,c.ProjectionGeometry,sweep,progress,
                new[]{progress.Reconstitute(id,0,0,0)},new[]{f.At(0,0)},new[]{new SkiPlanarVelocity(0,0)},20));
        }
        [Test] public void WarmScopedProjectionSamplingAndSurfaceQueriesAllocateNothing()
        {
            var f=new Fixture();int index=f.Catalog.GetFiringOccurrence(0);long arc=f.Catalog.Get(index).LengthNanometers/2;
            var pose=f.At(index,arc);
            for(int i=0;i<128;i++)
            {
                f.Catalog.TryProject(pose,index,arc,.001,out var observation);
                f.Catalog.TrySample(index,arc,out _);f.Catalog.SampleSurface(observation);
            }
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
            {
                f.Catalog.TryProject(pose,index,arc,.001,out var observation);
                f.Catalog.TrySample(index,arc,out _);f.Catalog.SampleSurface(observation);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}

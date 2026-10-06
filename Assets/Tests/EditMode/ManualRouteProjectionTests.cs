using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualRouteProjectionTests
    {
        private static (RaceRoute route,TrackVisualDefinition geometry,ManualRouteProjectionMap map) Create(string format="mass_start")
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+format+"_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+format+"_route")),profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            return (route,venue.Visual,new ManualRouteProjectionMap(route,venue.Visual));
        }
        private static long Nano(double metres)=>(long)Math.Round(metres*SkiState.NanometersPerMeter);
        private static ManualSkiHeadingState At(TrackVisualLoop loop,double local,double lateral=0,double? heading=null)
        {
            var p=loop.Sample(local);
            return new ManualSkiHeadingState(p.X+p.ForwardZ*lateral,p.Z-p.ForwardX*lateral,
                heading??Math.Atan2(p.ForwardX,p.ForwardZ));
        }
        [TestCase("mass_start")][TestCase("sprint")][TestCase("pursuit")]
        public void EverySelectedOccurrenceProjectsClippedGeometryWithoutAwardingOdometerOrChangingPose(string format)
        {
            var f=Create(format);int samples=0;
            for(int s=0;s<f.route.SectionCount;s++)
            {
                var section=f.route.GetSection(s);var loop=f.geometry.Get(section.LoopId);
                for(int k=1;k<loop.Count;k++)
                {
                    double from=Math.Max(section.LoopStart.Meters,loop.Get(k-1).Distance);
                    double to=Math.Min(section.LoopEnd.Meters,loop.Get(k).Distance);if(to<=from)continue;
                    double local=(from+to)*.5,station=section.Start.Meters+local-section.LoopStart.Meters;
                    var pose=At(loop,local,.4,.71);var before=pose;
                    Assert.That(f.map.TryProject(pose,s,Nano(station),.05,false,out var projection),Is.True);
                    Assert.That(projection.SectionIndex,Is.EqualTo(s));Assert.That(projection.LoopId,Is.EqualTo(section.LoopId));
                    Assert.That(projection.LocalMeters,Is.EqualTo(local).Within(2e-8));
                    Assert.That(projection.RouteNanometers,Is.EqualTo(Nano(station)).Within(20));
                    Assert.That(projection.LateralMeters,Is.EqualTo(.4).Within(2e-8));
                    Assert.That(projection.DistanceFromCentreMeters,Is.EqualTo(.4).Within(2e-8));
                    Assert.That(projection.StationMetersPerPlanarMeter,Is.GreaterThan(0));
                    Assert.That(pose,Is.EqualTo(before));samples++;
                }
            }
            Assert.That(samples,Is.GreaterThan(100));
        }
        [Test] public void RepeatedWorldCoordinatesDoNotAdvanceToAnotherLapOrUnselectedShortcut()
        {
            var f=Create();var first=f.route.GetSection(0);var loop=f.geometry.Get(first.LoopId);
            var pose=At(loop,first.LoopStart.Meters+10);
            Assert.That(f.map.TryProject(pose,0,Nano(10),1,true,out var result),Is.True);
            Assert.That(result.SectionIndex,Is.Zero);Assert.That(result.RouteNanometers,Is.EqualTo(Nano(10)).Within(20));
            int repeated=-1;
            for(int s=1;s<f.route.SectionCount;s++)if(f.route.GetSection(s).LoopId==first.LoopId){repeated=s;break;}
            Assert.That(repeated,Is.GreaterThan(0));
            var later=f.route.GetSection(repeated);long station=Nano(later.Start.Meters+10);
            Assert.That(f.map.TryProject(pose,repeated,station,1,false,out result),Is.True);
            Assert.That(result.SectionIndex,Is.EqualTo(repeated));Assert.That(result.RouteNanometers,Is.EqualTo(station).Within(20));
            Assert.That(f.map.TryProject(pose,0,Nano(50),.1,false,out _),Is.False,"A spatially visible point outside the local station window cannot become paid progress.");
        }
        [Test] public void BranchRequiresExplicitNextOccurrenceAndCannotSkipAnOccurrence()
        {
            var f=Create("sprint");int s=0;
            while(s+1<f.route.SectionCount&&f.route.GetSection(s).LoopId==f.route.GetSection(s+1).LoopId)s++;
            Assert.That(s+1,Is.LessThan(f.route.SectionCount));
            var current=f.route.GetSection(s);var next=f.route.GetSection(s+1);
            long before=Nano(current.Start.Meters+current.Length.Meters-.05);
            var pose=At(f.geometry.Get(next.LoopId),next.LoopStart.Meters+.1);
            bool found=f.map.TryProject(pose,s,before,.3,false,out var held);
            if(found)Assert.That(held.SectionIndex,Is.EqualTo(s),"No implicit route-branch selection.");
            Assert.That(f.map.TryProject(pose,s,before,.3,true,out var entered),Is.True);
            Assert.That(entered.SectionIndex,Is.EqualTo(s+1));
            Assert.That(entered.RouteNanometers,Is.EqualTo(Nano(next.Start.Meters+.1)).Within(20));
            Assert.That(entered.LocalMeters,Is.EqualTo(next.LoopStart.Meters+.1).Within(2e-8));
        }
        [Test] public void BackwardObservationIsNotClampedIntoForwardProgressAndGradeUsesWorldHeading()
        {
            var f=Create();var section=f.route.GetSection(0);var loop=f.geometry.Get(section.LoopId);
            int k=1;while(k<loop.Count&&Math.Abs(loop.Get(k).Y-loop.Get(k-1).Y)<.01)k++;
            Assert.That(k,Is.LessThan(loop.Count));
            double local=(loop.Get(k).Distance+loop.Get(k-1).Distance)*.5;
            var forward=At(loop,local);var reverse=At(loop,local,heading:forward.HeadingRadians+Math.PI);
            long previous=Nano(section.Start.Meters+local-section.LoopStart.Meters+.1);
            Assert.That(f.map.TryProject(reverse,0,previous,.2,false,out var result),Is.True);
            Assert.That(result.RouteNanometers,Is.LessThan(previous));
            Assert.That(result.GradeAlong(forward),Is.EqualTo(result.GeometricGrade).Within(1e-8));
            Assert.That(result.GradeAlong(reverse),Is.EqualTo(-result.GeometricGrade).Within(1e-8));
            var cross=At(loop,local,heading:forward.HeadingRadians+Math.PI/2);
            Assert.That(result.GradeAlong(cross),Is.EqualTo(0).Within(1e-8));
        }
        [Test] public void InvalidCursorAndDefaultPosesFailAndWarmProjectionAllocatesNothing()
        {
            var f=Create();var section=f.route.GetSection(0);var pose=At(f.geometry.Get(section.LoopId),10);
            Assert.Throws<ArgumentException>(()=>f.map.TryProject(default,0,0,1,false,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.map.TryProject(pose,-1,0,1,false,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.map.TryProject(pose,0,-1,1,false,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.map.TryProject(pose,0,0,double.NaN,false,out _));
            Assert.Catch(()=>default(ManualRouteProjection).GradeAlong(pose));
            Assert.That(f.map.ContextJson,Does.Contain("manual_base_route_local_projection_v1"));
            for(int i=0;i<100;i++)f.map.TryProject(pose,0,Nano(10),1,false,out _);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)f.map.TryProject(pose,0,Nano(10),1,false,out _);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void QuantizedWorldPosesAtAuthoredKnotsRemainProjectable()
        {
            var f=Create("sprint");int checkedKnots=0;
            for(int s=0;s<f.route.SectionCount;s++)
            {
                var section=f.route.GetSection(s);var loop=f.geometry.Get(section.LoopId);
                for(int k=0;k<loop.Count;k++)
                {
                    var knot=loop.Get(k);if(knot.Distance<section.LoopStart.Meters||knot.Distance>section.LoopEnd.Meters)continue;
                    long station=Nano(section.Start.Meters+knot.Distance-section.LoopStart.Meters);
                    var pose=new ManualSkiHeadingState(knot.X,knot.Z,.3);
                    Assert.That(f.map.TryProject(pose,s,station,.001,false,out var result),Is.True,$"section={s} knot={k}");
                    Assert.That(result.RouteNanometers,Is.EqualTo(station).Within(20));checkedKnots++;
                }
            }
            Assert.That(checkedKnots,Is.GreaterThan(100));
        }
    }
}

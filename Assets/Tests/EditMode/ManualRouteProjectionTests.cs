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
        [Test] public void LocalCornerWedgeProjectsToKnotWithoutRemoteRouteSnap()
        {
            var f=Create();
            var pose=new ManualSkiHeadingState(225.348460838,-254.140580187,3.350784975);
            long previous=Nano(322.788637835);
            Assert.That(f.map.TryProject(pose,0,previous,2,false,out var observed),Is.True);
            Assert.That(observed.SectionIndex,Is.Zero);
            Assert.That(Math.Abs(observed.RouteNanometers-previous),Is.LessThan(Nano(2)));
            Assert.That(observed.DistanceFromCentreMeters,Is.LessThan(2));
            Assert.That(f.map.TryProject(pose,1,Nano(f.route.GetSection(1).Start.Meters+50),.1,false,out _),Is.False);
            Assert.That(f.map.ContextJson,Does.Contain("local_clamped_knot_observation_v2"));
        }
        [TestCase("mass_start")][TestCase("sprint")][TestCase("pursuit")]
        public void ForwardTargetSamplesOnlyTheSelectedOccurrenceWithoutMovingTheAthlete(string format)
        {
            var f=Create(format);int checkedSegments=0;
            for(int s=0;s<f.route.SectionCount;s++)
            {
                var section=f.route.GetSection(s);var loop=f.geometry.Get(section.LoopId);
                for(int k=1;k<loop.Count;k++)
                {
                    double from=Math.Max(section.LoopStart.Meters,loop.Get(k-1).Distance);
                    double to=Math.Min(section.LoopEnd.Meters,loop.Get(k).Distance);if(to<=from)continue;
                    double local=(from+to)*.5;
                    long station=Nano(section.Start.Meters+local-section.LoopStart.Meters);
                    var pose=At(loop,local,.3);var unchanged=pose;
                    Assert.That(f.map.TrySampleAhead(s,station,0,false,out var target),Is.True,$"section={s} edge={k}");
                    var visual=loop.Sample(local);
                    Assert.That(target.IsValid,Is.True);
                    Assert.That(target.SectionIndex,Is.EqualTo(s));
                    Assert.That(target.LoopId,Is.EqualTo(section.LoopId));
                    Assert.That(target.RouteNanometers,Is.EqualTo(station));
                    Assert.That(target.X,Is.EqualTo(visual.X).Within(2e-8));
                    Assert.That(target.Z,Is.EqualTo(visual.Z).Within(2e-8));
                    Assert.That(target.Y,Is.EqualTo(visual.Y).Within(2e-8));
                    Assert.That(target.ForwardX*target.ForwardX+target.ForwardZ*target.ForwardZ,Is.EqualTo(1).Within(1e-12));
                    Assert.That(pose,Is.EqualTo(unchanged));checkedSegments++;
                }
            }
            Assert.That(checkedSegments,Is.GreaterThan(100));
        }
        [Test] public void ForwardTargetRequiresExplicitSuccessorAndCannotSkipItOrSelectARepeatedLap()
        {
            var f=Create("sprint");int section=0;
            while(section+1<f.route.SectionCount&&f.route.GetSection(section).LoopId==f.route.GetSection(section+1).LoopId)section++;
            Assert.That(section+1,Is.LessThan(f.route.SectionCount));
            var current=f.route.GetSection(section);var next=f.route.GetSection(section+1);
            long before=Nano(current.Start.Meters+current.Length.Meters-.05);
            Assert.That(f.map.TrySampleAhead(section,before,.15,false,out var withheld),Is.False);
            Assert.That(withheld.IsValid,Is.False);
            Assert.That(f.map.TrySampleAhead(section,before,.15,true,out var admitted),Is.True);
            Assert.That(admitted.SectionIndex,Is.EqualTo(section+1));
            Assert.That(admitted.LoopId,Is.EqualTo(next.LoopId));
            Assert.That(admitted.RouteNanometers,Is.EqualTo(Nano(next.Start.Meters+.1)));
            Assert.That(f.map.TrySampleAhead(section,before,current.Length.Meters+next.Length.Meters+1,true,out _),Is.False);
            var first=f.route.GetSection(0);
            Assert.That(f.map.TrySampleAhead(0,Nano(first.Start.Meters+10),1,false,out var firstTarget),Is.True);
            Assert.That(firstTarget.SectionIndex,Is.Zero,"World coordinates repeated on later laps must not change occurrence.");
        }
        [Test] public void ForwardTargetValidatesCursorAndHasNoWarmPerQueryAllocation()
        {
            var f=Create();var section=f.route.GetSection(0);long station=Nano(section.Start.Meters+10);
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.map.TrySampleAhead(-1,station,1,false,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.map.TrySampleAhead(0,-1,1,false,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.map.TrySampleAhead(0,Nano(section.Length.Meters+1),1,false,out _));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.map.TrySampleAhead(0,station,double.NaN,false,out _));
            Assert.That(f.map.TrySampleAhead(0,station,1,false,out _),Is.True);
            for(int i=0;i<100;i++)f.map.TrySampleAhead(0,station,1,false,out _);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)f.map.TrySampleAhead(0,station,1,false,out _);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void ActualWoodlandRouteYieldsConservativeInteriorFourPlaneCellsOnly()
        {
            var f=Create();var body=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
            var track=TrackDefinition.FromJson(f.route.TrackSnapshotJson);
            int sampled=0,admitted=0,rejectedKnots=0;
            for(int s=0;s<f.route.SectionCount;s++)
            {
                var section=f.route.GetSection(s);var loop=f.geometry.Get(section.LoopId);
                for(int k=1;k<loop.Count;k++)
                {
                    double from=Math.Max(section.LoopStart.Meters,loop.Get(k-1).Distance);
                    double to=Math.Min(section.LoopEnd.Meters,loop.Get(k).Distance);if(to<=from)continue;
                    double local=(from+to)*.5,station=section.Start.Meters+local-section.LoopStart.Meters;
                    var pose=At(loop,local);
                    Assert.That(f.map.TryProject(pose,s,Nano(station),.01,false,out var observation),Is.True);
                    sampled++;
                    if(!f.map.TryGetInteriorCorridor(observation,body,out var cell))continue;
                    admitted++;
                    Assert.That(cell.IsValid,Is.True);
                    Assert.That(cell.SectionIndex,Is.EqualTo(s));
                    Assert.That(cell.MinimumWidthMeters,Is.GreaterThan(body.LateralSeparation+2*body.EdgeClearance));
                    Assert.That(new[]{cell.Left.Id,cell.Right.Id,cell.Entry.Id,cell.Exit.Id},Is.Unique);
                    foreach(var edge in new[]{cell.Left,cell.Right,cell.Entry,cell.Exit})
                    {
                        double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians);
                        double support=body.LongitudinalSeparation*.5*Math.Abs(fx*edge.InwardX+fz*edge.InwardZ)+
                            body.LateralSeparation*.5*Math.Abs(fz*edge.InwardX-fx*edge.InwardZ);
                        double gap=(pose.X-edge.X)*edge.InwardX+(pose.Z-edge.Z)*edge.InwardZ-support-body.EdgeClearance;
                        Assert.That(gap,Is.GreaterThan(0),$"section={s} edge={k} plane={edge.Id}");
                    }
                    var physical=track.GetLoop(section.LoopId);
                    double minimum=double.PositiveInfinity;
                    for(int p=0;p<physical.SegmentCount;p++)
                    {
                        var segment=physical.GetSegment(p);
                        if(segment.StartDistance.Meters<to&&segment.StartDistance.Meters+segment.Length.Meters>from)
                            minimum=Math.Min(minimum,segment.WidthMeters);
                    }
                    Assert.That(cell.MinimumWidthMeters,Is.EqualTo(minimum));
                }
                for(int k=0;k<loop.Count;k++)
                {
                    var knot=loop.Get(k);if(knot.Distance<section.LoopStart.Meters||knot.Distance>section.LoopEnd.Meters)continue;
                    long station=Nano(section.Start.Meters+knot.Distance-section.LoopStart.Meters);
                    if(f.map.TryProject(new ManualSkiHeadingState(knot.X,knot.Z,0),s,station,.001,false,out var observation))
                    {
                        Assert.That(f.map.TryGetInteriorCorridor(observation,body,out _),Is.False,
                            "A body straddling a cell seam needs explicit topology admission.");
                        rejectedKnots++;
                    }
                }
            }
            Assert.That(sampled,Is.GreaterThan(100));Assert.That(admitted,Is.GreaterThan(20));
            Assert.That(rejectedKnots,Is.GreaterThan(100));
            Assert.That(f.map.CorridorContextJson,Does.Contain("manual_selected_route_convex_cell_v1"));
        }
        [Test] public void InteriorCorridorLookupHasNoWarmAllocationAndRejectsForeignOccurrence()
        {
            var f=Create();var section=f.route.GetSection(0);var loop=f.geometry.Get(section.LoopId);
            var body=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
            ManualRouteProjection interior=default;
            for(int k=1;k<loop.Count;k++)
            {
                double local=(loop.Get(k-1).Distance+loop.Get(k).Distance)*.5;
                if(!f.map.TryProject(At(loop,local),0,Nano(local-section.LoopStart.Meters),.01,false,out var p))continue;
                if(f.map.TryGetInteriorCorridor(p,body,out _)){interior=p;break;}
            }
            Assert.That(interior.IsValid,Is.True);
            Assert.Throws<ArgumentException>(()=>f.map.TryGetInteriorCorridor(default,body,out _));
            Assert.Throws<ArgumentException>(()=>f.map.TryGetInteriorCorridor(interior,null,out _));
            for(int i=0;i<100;i++)f.map.TryGetInteriorCorridor(interior,body,out _);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)f.map.TryGetInteriorCorridor(interior,body,out _);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void ActualWoodlandCorridorRightEdgeStopsAnOutwardPhysicalTrajectory()
        {
            var f=Create();var section=f.route.GetSection(0);var loop=f.geometry.Get(section.LoopId);
            var body=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
            ManualSkiHeadingState pose=default;ManualRouteCorridorCell cell=default;
            for(int k=1;k<loop.Count;k++)
            {
                double local=(loop.Get(k-1).Distance+loop.Get(k).Distance)*.5;
                var candidate=At(loop,local);
                if(!f.map.TryProject(candidate,0,Nano(local-section.LoopStart.Meters),.01,false,out var observation))continue;
                if(!f.map.TryGetInteriorCorridor(observation,body,out cell))continue;
                pose=candidate;break;
            }
            Assert.That(pose.IsValid&&cell.IsValid,Is.True);
            double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians);
            var outward=new SkiPlanarVelocity(12*fz,-12*fx);
            var athlete=new ManualSkiContactBody(new AthleteId("corridor_probe"),pose,outward,75);
            var frame=new ManualSkiSnowContactSystem(body,1).PreviewTrajectory(athlete,
                new SkiContactMotion(outward,0),0,0,.5);
            var trajectory=new ManualSkiWorldTrajectory(frame);
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"corridor_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var checks=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"corridor_sweep_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var joint=new ManualSkiJointSweep(body,contact,checks);
            var id=athlete.Athlete;
            var actors=new[]{new SkiWorldMotionCandidate(id,trajectory)};
            var boundaries=new[]{new SkiActorCourseEdge(id,cell.Left),new SkiActorCourseEdge(id,cell.Right),
                new SkiActorCourseEdge(id,cell.Entry),new SkiActorCourseEdge(id,cell.Exit)};
            var result=joint.Preview(actors,1,Array.Empty<SkiReachedContactPair>(),0,boundaries,boundaries.Length);
            Assert.That(result.Status,Is.EqualTo(SkiJointSweepStatus.EdgeInterval));
            Assert.That(result.EdgeId,Is.EqualTo(cell.Right.Id));
            Assert.That(result.ClearUntilSeconds,Is.GreaterThan(0).And.LessThan(.5));
        }
    }
}

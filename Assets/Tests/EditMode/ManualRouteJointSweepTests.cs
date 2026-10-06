using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualRouteJointSweepTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static (RaceRoute route,TrackVisualDefinition visual,ManualRouteJointSweep solver,OccupancyDefinition body) Create()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var body=OccupancyDefinition.FromJson(Read("Biathlon/occupancy_provisional_v1"));
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"route_joint_test_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"route_joint_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            return (route,venue.Visual,new ManualRouteJointSweep(new ManualRouteProjectionMap(route,venue.Visual),body,contact,work),body);
        }
        private static long Nano(double metres)=>(long)Math.Round(metres*SkiState.NanometersPerMeter);
        private static SkiWorldMotionCandidate Actor(string id,OccupancyDefinition body,TrackVisualPoint at,
            double velocityX,double velocityZ,double seconds=.5)
        {
            var athlete=new AthleteId(id);
            var pose=new ManualSkiHeadingState(at.X,at.Z,Math.Atan2(at.ForwardX,at.ForwardZ));
            var velocity=new SkiPlanarVelocity(velocityX,velocityZ);
            var contact=new ManualSkiContactBody(athlete,pose,velocity,75);
            var trajectory=new ManualSkiSnowContactSystem(body,1).PreviewTrajectory(contact,
                new SkiContactMotion(velocity,0),0,0,seconds);
            return new SkiWorldMotionCandidate(athlete,new ManualSkiWorldTrajectory(trajectory));
        }
        private static ManualRouteActorWindow Scope(string id,double station,double span=8)
            =>new ManualRouteActorWindow(new AthleteId(id),0,Nano(station),span,false);
        private static ManualRouteJointPreview Preview(ManualRouteJointSweep solver,SkiWorldMotionCandidate[] actors,
            ManualRouteActorWindow[] windows)
            =>solver.Preview(actors,actors.Length,Array.Empty<SkiReachedContactPair>(),0,windows,
                Array.Empty<SkiActorCourseEdge>(),0);

        [Test] public void CommonPrefixStopsAtTheCourseBeforeAnOutwardActorLeavesTheTrack()
        {
            var f=Create();var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var actor=Actor("outward",f.body,p,10*p.ForwardZ,-10*p.ForwardX);
            var result=Preview(f.solver,new[]{actor},new[]{Scope("outward",10)});
            Assert.That(result.Status,Is.EqualTo(ManualRouteJointStatus.RouteTube));
            Assert.That(result.Athlete.Value,Is.EqualTo("outward"));
            Assert.That(result.ClearUntilSeconds,Is.GreaterThan(0).And.LessThan(.5));
            Assert.That(result.Tube.Status,Is.EqualTo(ManualRouteTubeStatus.PotentialBoundary));
        }
        [Test] public void ActualBodyConflictLimitsTheSameRouteSweepBeforeTheCourseBoundary()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var a=loop.Sample(10);var b=loop.Sample(12);
            var forward=Actor("a",f.body,a,6*a.ForwardX,6*a.ForwardZ);
            var stopped=Actor("b",f.body,b,0,0);
            var windows=new[]{Scope("a",10),Scope("b",12)};
            var first=Preview(f.solver,new[]{forward,stopped},windows);
            var reordered=Preview(f.solver,new[]{stopped,forward},new[]{windows[1],windows[0]});
            Assert.That(first.Status,Is.EqualTo(ManualRouteJointStatus.BodyOrEdge));
            Assert.That(first.BodyOrEdge.Status,Is.EqualTo(SkiJointSweepStatus.PairInterval));
            Assert.That(first.ClearUntilSeconds,Is.GreaterThan(0).And.LessThan(.5));
            Assert.That(reordered.Status,Is.EqualTo(first.Status));
            Assert.That(reordered.ClearUntilSeconds,Is.EqualTo(first.ClearUntilSeconds));
            Assert.That(reordered.BodyOrEdge.First,Is.EqualTo(first.BodyOrEdge.First));
            Assert.That(reordered.BodyOrEdge.Second,Is.EqualTo(first.BodyOrEdge.Second));
        }
        [Test] public void FullRosterAndSelectedRouteWindowsAreRequiredWithoutSilentOmission()
        {
            var f=Create();var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var actor=Actor("a",f.body,p,0,0);
            Assert.Throws<ArgumentException>(()=>Preview(f.solver,new[]{actor},new[]{Scope("foreign",10)}));
            Assert.Throws<ArgumentException>(()=>Preview(f.solver,new[]{actor},Array.Empty<ManualRouteActorWindow>()));
            Assert.Throws<ArgumentException>(()=>Preview(f.solver,new[]{actor,actor},new[]{Scope("a",10),Scope("a",10)}));
            Assert.That(f.solver.ContextJson,Does.Contain("manual_route_body_tube_common_prefix_v1"));
        }
        [Test] public void ClearCommonPrefixIsDeterministicWithoutWarmAllocation()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var p=loop.Sample(10);var actor=Actor("a",f.body,p,3*p.ForwardX,3*p.ForwardZ,.05);
            var actors=new[]{actor};var windows=new[]{Scope("a",10,2)};
            var expected=Preview(f.solver,actors,windows);
            Assert.That(expected.Status,Is.EqualTo(ManualRouteJointStatus.Clear));
            Assert.That(expected.ClearUntilSeconds,Is.EqualTo(.05));
            for(int i=0;i<100;i++)Preview(f.solver,actors,windows);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
            {
                var result=Preview(f.solver,actors,windows);
                if(result.Status!=expected.Status||result.ClearUntilSeconds!=expected.ClearUntilSeconds)
                    Assert.Fail("Common world-route preview changed for the same actors.");
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void CourseProofWorkLimitWinsTheCommonPrefixWithoutGhosting()
        {
            var f=Create();var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var actor=Actor("a",f.body,p,10*p.ForwardZ,-10*p.ForwardX);
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"route_joint_limit_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"route_joint_limit_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":1,\"time_tolerance_s\":0.0000001}");
            var limited=new ManualRouteJointSweep(new ManualRouteProjectionMap(f.route,f.visual),f.body,contact,work);
            var result=Preview(limited,new[]{actor},new[]{Scope("a",10)});
            Assert.That(result.Status,Is.EqualTo(ManualRouteJointStatus.RouteTube));
            Assert.That(result.Tube.Status,Is.EqualTo(ManualRouteTubeStatus.WorkLimit));
            Assert.That(result.ClearUntilSeconds,Is.Zero);
        }
        [Test] public void CommonPrefixProjectsWorldPoseWithoutTurningOrAwardingDistance()
        {
            var f=Create();var p=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var actor=Actor("a",f.body,p,3*p.ForwardX,3*p.ForwardZ,.05);
            var actors=new[]{actor};var windows=new[]{Scope("a",10,2)};
            var projections=new ManualRouteProjection[1];
            bool ok=f.solver.TryProjectCommonPrefix(actors,1,Array.Empty<SkiReachedContactPair>(),0,
                windows,Array.Empty<SkiActorCourseEdge>(),0,projections,out var joint);
            Assert.That(ok,Is.True);
            Assert.That(joint.Status,Is.EqualTo(ManualRouteJointStatus.Clear));
            Assert.That(projections[0].SectionIndex,Is.Zero);
            Assert.That(projections[0].RouteNanometers,Is.GreaterThan(Nano(10)));
            Assert.That(actor.Trajectory.Sample(1).X,Is.EqualTo(p.X+3*p.ForwardX*.05).Within(1e-7));
            for(int i=0;i<100;i++)f.solver.TryProjectCommonPrefix(actors,1,Array.Empty<SkiReachedContactPair>(),0,
                windows,Array.Empty<SkiActorCourseEdge>(),0,projections,out _);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)f.solver.TryProjectCommonPrefix(actors,1,Array.Empty<SkiReachedContactPair>(),0,
                windows,Array.Empty<SkiActorCourseEdge>(),0,projections,out _);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void FailedSecondProjectionLeavesEveryOutputSlotUntouched()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var a=loop.Sample(10);var b=loop.Sample(30);
            var actors=new[]{Actor("a",f.body,a,0,0),Actor("b",f.body,b,0,0)};
            var windows=new[]{Scope("a",10),Scope("b",200,.01)};
            var projections=new ManualRouteProjection[2];
            bool ok=f.solver.TryProjectCommonPrefix(actors,2,Array.Empty<SkiReachedContactPair>(),0,
                windows,Array.Empty<SkiActorCourseEdge>(),0,projections,out var joint);
            Assert.That(ok,Is.False);
            Assert.That(joint.Status,Is.EqualTo(ManualRouteJointStatus.RouteTube));
            Assert.That(projections[0].IsValid,Is.False);
            Assert.That(projections[1].IsValid,Is.False);
        }
        [Test] public void CommonSafeTimeBindsWholeRosterWorldAndRouteFrames()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var a=loop.Sample(10);var b=loop.Sample(12);
            var actors=new[]{Actor("a",f.body,a,6*a.ForwardX,6*a.ForwardZ),Actor("b",f.body,b,0,0)};
            var windows=new[]{Scope("a",10),Scope("b",12)};
            var frames=new ManualRouteActorFrame[2];
            bool ok=f.solver.TryPreviewCommonPrefixFrames(actors,2,Array.Empty<SkiReachedContactPair>(),0,
                windows,Array.Empty<SkiActorCourseEdge>(),0,frames,out var joint);
            Assert.That(ok,Is.True);
            Assert.That(joint.Status,Is.EqualTo(ManualRouteJointStatus.BodyOrEdge));
            Assert.That(joint.ClearUntilSeconds,Is.GreaterThan(0).And.LessThan(.5));
            for(int i=0;i<2;i++)
            {
                double fraction=joint.ClearUntilSeconds/actors[i].Trajectory.DurationSeconds;
                Assert.That(frames[i].Athlete,Is.EqualTo(actors[i].Athlete));
                Assert.That(frames[i].Pose,Is.EqualTo(actors[i].Trajectory.Sample(fraction)));
                Assert.That(frames[i].Velocity,Is.EqualTo(actors[i].Trajectory.VelocityAt(fraction)));
                Assert.That(frames[i].Projection.IsValid,Is.True);
            }
            Assert.That(frames[0].Projection.RouteNanometers,Is.GreaterThan(Nano(10)));
            for(int i=0;i<100;i++)f.solver.TryPreviewCommonPrefixFrames(actors,2,
                Array.Empty<SkiReachedContactPair>(),0,windows,Array.Empty<SkiActorCourseEdge>(),0,frames,out _);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)f.solver.TryPreviewCommonPrefixFrames(actors,2,
                Array.Empty<SkiReachedContactPair>(),0,windows,Array.Empty<SkiActorCourseEdge>(),0,frames,out _);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void FailedRosterFrameProjectionCannotPublishPartialWorldState()
        {
            var f=Create();var loop=f.visual.Get(f.route.GetSection(0).LoopId);
            var a=loop.Sample(10);var b=loop.Sample(30);
            var actors=new[]{Actor("a",f.body,a,0,0),Actor("b",f.body,b,0,0)};
            var frames=new ManualRouteActorFrame[2];
            bool ok=f.solver.TryPreviewCommonPrefixFrames(actors,2,Array.Empty<SkiReachedContactPair>(),0,
                new[]{Scope("a",10),Scope("b",200,.01)},Array.Empty<SkiActorCourseEdge>(),0,frames,out _);
            Assert.That(ok,Is.False);
            Assert.That(frames[0].IsValid,Is.False);
            Assert.That(frames[1].IsValid,Is.False);
        }
    }
}

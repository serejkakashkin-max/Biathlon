using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiSpatialContactTests
    {
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static SkiContactDefinition Contact()=>SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"spatial_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
        private static SkiContactSweepDefinition SweepDefinition(int checks=4096)=>SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"spatial_sweep_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":"+checks+",\"time_tolerance_s\":0.0000001}");
        private static ManualSkiContactBody Actor(string id,double x,double z,double vx,double vz,double heading=0,double spin=0)
            =>new ManualSkiContactBody(new AthleteId(id),new ManualSkiHeadingState(x,z,heading,spin),new SkiPlanarVelocity(vx,vz),75);
        private static SkiContactMotion Motion(ManualSkiContactBody a)=>new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond);
        private static SkiContactTrajectory Frame(ManualSkiContactBody body,double grip=0,double seconds=.05)
            =>new ManualSkiSnowContactSystem(Body(),1).PreviewTrajectory(body,Motion(body),grip,0,seconds);
        private static ManualSkiWorldTrajectory World(ManualSkiContactBody a,double grip=0,double seconds=.05)=>new ManualSkiWorldTrajectory(Frame(a,grip,seconds));

        [TestCase(.0000005,true)][TestCase(-.0000005,false)]
        public void UnregisteredPositiveResponseSkinNeverAdmitsZeroOrNegativePhysicalGap(double finalGap,bool clear)
        {
            var a=Actor("a",0,0,1,0);
            var b=Actor("b",Body().LateralSeparation+.05+finalGap,0,0,0);
            var actors=new[]{new SkiWorldMotionCandidate(a.Athlete,World(a)),new SkiWorldMotionCandidate(b.Athlete,World(b))};
            var none=Array.Empty<SkiReachedContactPair>();var edges=Array.Empty<SkiActorCourseEdge>();
            var joint=new ManualSkiJointSweep(Body(),Contact(),SweepDefinition(),separatedResponseBand:true);
            var result=joint.Preview(actors,2,none,0,edges,0);
            Assert.That(result.Status,Is.EqualTo(clear?SkiJointSweepStatus.Clear:SkiJointSweepStatus.PairInterval));
            Assert.That(joint.Preview(new[]{actors[1],actors[0]},2,none,0,edges,0),Is.EqualTo(result));
            if(clear)Assert.That(new ManualSkiJointSweep(Body(),Contact(),SweepDefinition()).Preview(actors,2,none,0,edges,0).Status,
                Is.EqualTo(SkiJointSweepStatus.PairInterval),"Legacy unregistered proximity policy remains explicit.");
            else Assert.That(result.ClearUntilSeconds,Is.LessThan(.05));
        }

        [Test]
        public void SpatialContinuationRetainsSideVelocityAndSpinInsteadOfTurningItIntoForwardSpeed()
        {
            var body=Actor("a",0,0,2,6,spin:.8);var frame=Frame(body);
            Assert.That(frame.State.X,Is.EqualTo(.1));Assert.That(frame.State.Z,Is.EqualTo(.3));
            Assert.That(frame.State.HeadingRadians,Is.EqualTo(.04).Within(1e-9));
            Assert.That(frame.MotionAt(1),Is.EqualTo(Motion(body)));
            Assert.That(frame.Sample(.5).X,Is.EqualTo(.05));Assert.That(frame.Sample(.5).Z,Is.EqualTo(.15));
            Assert.That(frame.VelocityHeadingMismatch(),Is.GreaterThan(.2));
        }

        [Test]
        public void PassiveImpulseIntegratesAsOneFrozenRampWithConsistentPrefixes()
        {
            var body=Actor("a",0,0,.8,6);var frame=Frame(body,4);
            Assert.That(frame.MotionAt(1).Velocity.X,Is.EqualTo(.6).Within(1e-12));
            Assert.That(frame.State.X,Is.EqualTo(.035).Within(1e-9));
            Assert.That(frame.Sample(.5).X,Is.EqualTo(.01875).Within(1e-9));
            Assert.That(frame.State.Z,Is.EqualTo(.3));
            for(int i=0;i<=20;i++)
            {
                double fraction=i/20.0,t=.05*fraction;
                Assert.That(frame.Sample(fraction).X,Is.EqualTo(.8*t-2*t*t).Within(1e-9));
            }
        }

        [Test]
        public void RawPostImpactSpinMayExceedCommandedYawWithoutBeingClamped()
        {
            var body=Actor("a",0,0,1,6);var incoming=new SkiContactMotion(body.Velocity,3.123456789123);
            var frame=new ManualSkiSnowContactSystem(Body(),20).PreviewTrajectory(body,incoming,0,0,.05);
            Assert.That(frame.InitialMotion.YawRate,Is.EqualTo(incoming.YawRate));
            Assert.That(frame.MotionAt(1).YawRate,Is.EqualTo(incoming.YawRate));
            Assert.That(frame.State.HeadingRadians,Is.EqualTo(incoming.YawRate*.05).Within(1e-9));
            Assert.That(frame.Sample(0).YawRateRadiansPerSecond,Is.EqualTo(Math.Round(incoming.YawRate,9)));
        }

        [Test]
        public void ContactContinuationAndOrdinaryMotionUseTheSameContinuousPairCheck()
        {
            var a=Actor("a",-2,0,20,0);var b=Actor("b",0,0,0,0);
            var flat=new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
            var travel=new SkiForceSystem(SkiForceTests.Definition(),1).PreviewTrajectory(default,0,flat,.1);
            var ordinary=new ManualSkiSteeringSystem(ManualSkiForceTrajectoryTests.HeadingDefinition(),1).Preview(b.Pose,0,travel,1,4);
            var sweep=new ManualSkiContactSweep(Body(),Contact(),SweepDefinition());
            var result=sweep.Preview(a.Athlete,World(a,seconds:.1),b.Athlete,new ManualSkiWorldTrajectory(ordinary));
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.PotentialContact));
            Assert.That(result.ClearUntilSeconds,Is.EqualTo((2-.7-1e-6)/20).Within(2e-7));
        }

        [Test]
        public void GroupResponseCanContinueSpatiallyWithoutStoppingEveryAthlete()
        {
            var bodies=new[]{Actor("a",0,0,0,8),Actor("b",0,1.5,0,0),Actor("c",0,3,0,0)};
            var output=new SkiContactMotion[3];
            var definition=SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"spatial_group_v1\",\"maximum_passes\":128,\"closing_speed_tolerance_mps\":0.000001}");
            var resolved=new ManualSkiContactGroup(Body(),Contact(),definition).Preview(bodies,3,output,new int[3]);
            Assert.That(resolved.Status,Is.EqualTo(SkiContactGroupStatus.Resolved));
            var snow=new ManualSkiSnowContactSystem(Body(),20);var frames=new ManualSkiWorldTrajectory[3];
            for(int i=0;i<3;i++)
            {
                frames[i]=new ManualSkiWorldTrajectory(snow.PreviewTrajectory(bodies[i],output[i],4,0,.05));
                Assert.That(frames[i].Sample(1).Z-bodies[i].Pose.Z,Is.GreaterThan(.13));
            }
            var sweep=new ManualSkiContactSweep(Body(),Contact(),SweepDefinition());
            for(int i=0;i<2;i++)Assert.That(sweep.PreviewExistingContact(bodies[i].Athlete,frames[i],bodies[i+1].Athlete,frames[i+1]).Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(sweep.Preview(bodies[0].Athlete,frames[0],bodies[2].Athlete,frames[2]).Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            var candidates=new SkiWorldMotionCandidate[3];var edges=new SkiActorCourseEdge[6];
            for(int i=0;i<3;i++)
            {
                candidates[i]=new SkiWorldMotionCandidate(bodies[i].Athlete,frames[i]);
                edges[i*2]=new SkiActorCourseEdge(bodies[i].Athlete,new SkiCourseEdgePlane("left",-2,0,1,0));
                edges[i*2+1]=new SkiActorCourseEdge(bodies[i].Athlete,new SkiCourseEdgePlane("right",2,0,-1,0));
            }
            var reached=new[]{new SkiReachedContactPair(bodies[0].Athlete,bodies[1].Athlete),new SkiReachedContactPair(bodies[1].Athlete,bodies[2].Athlete)};
            var joint=new ManualSkiJointSweep(Body(),Contact(),SweepDefinition()).Preview(candidates,3,reached,2,edges,6);
            Assert.That(joint.Status,Is.EqualTo(SkiJointSweepStatus.Clear));Assert.That(joint.ClearUntilSeconds,Is.EqualTo(.05));
        }

        [Test]
        public void EdgeCheckStopsAtSkiFootprintBeforeTheCentreLeavesTheCourse()
        {
            var body=Actor("a",0,0,20,6);var frame=World(body,seconds:.1);
            var edge=new SkiCourseEdgePlane("right",1,0,-1,0);
            var result=new ManualSkiEdgeSweep(Body(),Contact(),SweepDefinition()).Preview(frame,edge);
            Assert.That(result.Status,Is.EqualTo(SkiEdgeSweepStatus.PotentialBoundary));
            Assert.That(result.ClearUntilSeconds,Is.EqualTo((1-.35-.1+1e-6)/20).Within(2e-7));
            Assert.That(frame.Sample(result.ClearUntilSeconds/.1).X,Is.LessThan(1));
        }

        [Test]
        public void RotatingRearCanCrossAnEdgeWhileItsCentreStaysInside()
        {
            var a=Actor("a",0,0,0,0,spin:4);
            var frame=World(a,seconds:.2);var edge=new SkiCourseEdgePlane("right",.7,0,-1,0);
            var result=new ManualSkiEdgeSweep(Body(),Contact(),SweepDefinition()).Preview(frame,edge);
            Assert.That(result.Status,Is.EqualTo(SkiEdgeSweepStatus.PotentialBoundary));
            Assert.That(result.ClearUntilSeconds,Is.InRange(.08,.12));
            Assert.That(frame.Sample(1).X,Is.Zero);
        }

        [Test]
        public void TangentialMotionAlongAnEdgeDoesNotAutoSteerOrStop()
        {
            var body=Actor("a",.55,0,0,6);var frame=World(body);
            var result=new ManualSkiEdgeSweep(Body(),Contact(),SweepDefinition()).Preview(frame,new SkiCourseEdgePlane("right",1,0,-1,0));
            Assert.That(result.Status,Is.EqualTo(SkiEdgeSweepStatus.Clear));Assert.That(result.ClearUntilSeconds,Is.EqualTo(.05));
            Assert.That(frame.Sample(1).X,Is.EqualTo(.55));Assert.That(frame.Sample(1).HeadingRadians,Is.Zero);
        }

        [Test]
        public void EdgeWorkExhaustionAndOutsideStartNeverAuthorizeMotion()
        {
            var sweep=new ManualSkiEdgeSweep(Body(),Contact(),SweepDefinition(1));var edge=new SkiCourseEdgePlane("right",1,0,-1,0);
            Assert.That(sweep.Preview(World(Actor("a",0,0,20,0),seconds:.1),edge).Status,Is.EqualTo(SkiEdgeSweepStatus.WorkLimit));
            Assert.That(sweep.Preview(World(Actor("a",.6,0,0,6)),edge).Status,Is.EqualTo(SkiEdgeSweepStatus.UnresolvedStart));
        }

        [Test]
        public void JointSweepChoosesEarliestEdgeOrPairAcrossTheWholeGroup()
        {
            var a=Actor("a",0,0,10,0);var b=Actor("b",3,0,0,0);var c=Actor("c",0,10,10,0);
            var actors=new[]{new SkiWorldMotionCandidate(a.Athlete,World(a,seconds:.5)),new SkiWorldMotionCandidate(b.Athlete,World(b,seconds:.5)),new SkiWorldMotionCandidate(c.Athlete,World(c,seconds:.5))};
            var joint=new ManualSkiJointSweep(Body(),Contact(),SweepDefinition());
            var noContacts=Array.Empty<SkiReachedContactPair>();var noEdges=Array.Empty<SkiActorCourseEdge>();
            var collision=joint.Preview(actors,3,noContacts,0,noEdges,0);
            Assert.That(collision.Status,Is.EqualTo(SkiJointSweepStatus.PairInterval));
            Assert.That(collision.First,Is.EqualTo(a.Athlete));Assert.That(collision.Second,Is.EqualTo(b.Athlete));
            var boundaries=new[]{new SkiActorCourseEdge(c.Athlete,new SkiCourseEdgePlane("right",1,0,-1,0))};
            var withEdge=joint.Preview(actors,3,noContacts,0,boundaries,1);
            Assert.That(withEdge.Status,Is.EqualTo(SkiJointSweepStatus.EdgeInterval));Assert.That(withEdge.First,Is.EqualTo(c.Athlete));
            Assert.That(withEdge.ClearUntilSeconds,Is.LessThan(collision.ClearUntilSeconds));
            var reverse=new[]{actors[2],actors[1],actors[0]};
            Assert.That(joint.Preview(reverse,3,noContacts,0,boundaries,1),Is.EqualTo(withEdge));
        }

        [Test]
        public void ReachedContactIsExplicitAndUnknownOrDuplicateBindingsAreRejected()
        {
            var a=Actor("a",0,0,0,6);var b=Actor("b",.7,0,0,6);
            var actors=new[]{new SkiWorldMotionCandidate(a.Athlete,World(a)),new SkiWorldMotionCandidate(b.Athlete,World(b))};
            var joint=new ManualSkiJointSweep(Body(),Contact(),SweepDefinition());
            var reached=new[]{new SkiReachedContactPair(a.Athlete,b.Athlete)};var edges=Array.Empty<SkiActorCourseEdge>();
            Assert.That(joint.Preview(actors,2,reached,0,edges,0).ClearUntilSeconds,Is.Zero);
            Assert.That(joint.Preview(actors,2,reached,1,edges,0).Status,Is.EqualTo(SkiJointSweepStatus.Clear));
            Assert.Catch(()=>joint.Preview(actors,2,new[]{new SkiReachedContactPair(a.Athlete,new AthleteId("foreign"))},1,edges,0));
            Assert.Catch(()=>joint.Preview(actors,2,new[]{reached[0],reached[0]},2,edges,0));
            var edge=new SkiActorCourseEdge(a.Athlete,new SkiCourseEdgePlane("right",2,0,-1,0));
            Assert.Catch(()=>joint.Preview(actors,2,reached,1,new[]{edge,edge},2));
            Assert.Catch(()=>joint.Preview(new[]{actors[0],actors[0]},2,reached,0,edges,0));
        }

        [Test]
        public void JointQueryTiesAreStableAndQueriesDoNotAllocate()
        {
            var a=Actor("a",0,0,10,0);var b=Actor("b",0,10,10,0);
            var actors=new[]{new SkiWorldMotionCandidate(b.Athlete,World(b,seconds:.1)),new SkiWorldMotionCandidate(a.Athlete,World(a,seconds:.1))};
            var boundaries=new[]{new SkiActorCourseEdge(b.Athlete,new SkiCourseEdgePlane("right",1,0,-1,0)),new SkiActorCourseEdge(a.Athlete,new SkiCourseEdgePlane("right",1,0,-1,0))};
            var noContacts=Array.Empty<SkiReachedContactPair>();var joint=new ManualSkiJointSweep(Body(),Contact(),SweepDefinition());
            var expected=joint.Preview(actors,2,noContacts,0,boundaries,2);Assert.That(expected.First,Is.EqualTo(a.Athlete));
            SkiJointSweepPreview actual=default;long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<100;i++)actual=joint.Preview(actors,2,noContacts,0,boundaries,2);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(bytes,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
            Assert.That(joint.Preview(new[]{actors[1],actors[0]},2,noContacts,0,new[]{boundaries[1],boundaries[0]},2),Is.EqualTo(expected));
            Assert.That(default(SkiJointSweepPreview).IsValid,Is.False);
        }

        [Test]
        public void SpatialQueriesAreRepeatableAllocationFreeAndRejectInvalidFrames()
        {
            var body=Actor("a",0,0,.8,6,spin:.4);var snow=new ManualSkiSnowContactSystem(Body(),20);
            var edge=new SkiCourseEdgePlane("right",1,0,-1,0);var sweep=new ManualSkiEdgeSweep(Body(),Contact(),SweepDefinition());
            var expected=snow.PreviewTrajectory(body,Motion(body),4,0,.05);sweep.Preview(new ManualSkiWorldTrajectory(expected),edge);
            long before=GC.GetAllocatedBytesForCurrentThread();SkiContactTrajectory actual=default;
            for(int i=0;i<1000;i++){actual=snow.PreviewTrajectory(body,Motion(body),4,0,.05);sweep.Preview(new ManualSkiWorldTrajectory(actual),edge);actual.Sample(.33);}
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(bytes,Is.Zero);Assert.That(actual.State,Is.EqualTo(expected.State));
            Assert.Catch(()=>default(SkiContactTrajectory).Sample(0));Assert.Catch(()=>expected.Sample(double.NaN));
            Assert.Catch(()=>new ManualSkiWorldTrajectory(default(SkiContactTrajectory)));
            Assert.Catch(()=>new SkiCourseEdgePlane("invalid",0,0,0,0));Assert.Catch(()=>sweep.Preview(default,edge));
            Assert.Catch(()=>sweep.Preview(new ManualSkiWorldTrajectory(expected),default));
            Assert.Catch(()=>Frame(Actor("outside",999999.9,0,20,0)));
            Assert.That(default(SkiEdgeSweepPreview).IsValid,Is.False);
        }
    }
    internal static class SpatialContactTestExtensions
    {
        internal static double VelocityHeadingMismatch(this SkiContactTrajectory frame)
        {
            var v=frame.MotionAt(1).Velocity;
            return Math.Abs(Math.Atan2(v.X,v.Z)-frame.State.HeadingRadians);
        }
    }
}

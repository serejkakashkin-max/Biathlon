using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiFirstContactTests
    {
        private static readonly AthleteId A=new AthleteId("arrival_a"),B=new AthleteId("arrival_b");
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static SkiContactDefinition Contact()=>SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"first_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
        private static SkiContactSweepDefinition Work(int checks=4096)=>SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"first_contact_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":"+checks+",\"time_tolerance_s\":0.0000001}");
        private static ManualSkiWorldTrajectory Linear(AthleteId id,double x,double z,double vx=0,double vz=0,double heading=0,double yaw=0,double seconds=1)
        {
            var pose=new ManualSkiHeadingState(x,z,heading,yaw);var velocity=new SkiPlanarVelocity(vx,vz);
            return new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(Body(),1).PreviewTrajectory(
                new ManualSkiContactBody(id,pose,velocity,75),new SkiContactMotion(velocity,yaw),0,0,seconds));
        }
        private static ManualSkiWorldTrajectory Force(double z,double speed,double power)
        {
            var sample=new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
            var force=new SkiForceSystem(SkiForceTests.Definition(),1).PreviewTrajectory(new SkiState(0,speed),power,sample,1);
            return new ManualSkiWorldTrajectory(new ManualSkiSteeringSystem(ManualSkiForceTrajectoryTests.HeadingDefinition(),1)
                .Preview(new ManualSkiHeadingState(0,z,0),0,force,1,100));
        }
        private static double Gap(ManualSkiWorldTrajectory a,ManualSkiWorldTrajectory b,double fraction)
            =>new ManualSkiContactSystem(Body(),Contact()).Preview(new ManualSkiContactBody(A,a.Sample(fraction),default,75),
                new ManualSkiContactBody(B,b.Sample(fraction),default,75)).GapMeters;
        private static void CheckReached(ManualSkiWorldTrajectory a,ManualSkiWorldTrajectory b,SkiFirstContactPreview hit)
        {
            Assert.That(hit.Status,Is.EqualTo(SkiFirstContactStatus.Reached),$"{hit.Status} checks={hit.IntervalChecks} gap={hit.GapMeters:R}");
            Assert.That(hit.Admission.IsContactArrival,Is.True);Assert.That(hit.Admission.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(Math.Abs(hit.GapMeters),Is.LessThanOrEqualTo(Contact().ContactTolerance));
            Assert.That(new ManualSkiContactSweep(Body(),Contact(),Work()).PreviewArrivalContact(A,a.Slice(0,hit.ContactSeconds),B,b.Slice(0,hit.ContactSeconds)).Status,
                Is.EqualTo(SkiContactSweepStatus.Clear));
            for(int i=0;i<=128;i++)Assert.That(Gap(a,b,hit.ContactSeconds/a.DurationSeconds*i/128),Is.GreaterThan(-Contact().ContactTolerance));
        }
        [Test] public void FastPassThroughFindsActualProximityDespiteClearWholeEndpoints()
        {
            var a=Linear(A,-3,0,20,0,Math.PI/2);var b=Linear(B,0,0);
            Assert.That(Gap(a,b,0),Is.GreaterThan(0));Assert.That(Gap(a,b,1),Is.GreaterThan(0));
            var hit=new ManualSkiContactSweep(Body(),Contact(),Work()).PreviewFirstContact(A,a,B,b);CheckReached(a,b,hit);
            double exact=(3-(Body().LongitudinalSeparation+Body().LateralSeparation)*.5)/20;
            Assert.That(hit.ContactSeconds,Is.EqualTo(exact).Within(Contact().ContactTolerance/20));
        }
        [Test] public void AcceleratingRearContactUsesTheFrozenSharedForceTrajectory()
        {
            var a=Force(0,0,350);var b=Linear(B,0,2);
            var hit=new ManualSkiContactSweep(Body(),Contact(),Work()).PreviewFirstContact(A,a,B,b);CheckReached(a,b,hit);
            double low=0,high=1;
            for(int i=0;i<50;i++){double t=(low+high)*.5;if(Gap(a,b,t)>0)low=t;else high=t;}
            Assert.That(hit.ContactSeconds,Is.EqualTo(high).Within(2e-6));
            Assert.That(hit.ContactSeconds,Is.LessThan(high));
        }
        [Test] public void RotatingSkisFindCornerContactWithStationaryCentres()
        {
            var a=Linear(A,0,0,yaw:1.2);var b=Linear(B,1.1,0);
            var hit=new ManualSkiContactSweep(Body(),Contact(),Work()).PreviewFirstContact(A,a,B,b);CheckReached(a,b,hit);
            Assert.That(a.Sample(hit.ContactSeconds).X,Is.Zero);Assert.That(a.Sample(hit.ContactSeconds).Z,Is.Zero);
            Assert.That(hit.ContactSeconds,Is.InRange(.1,.9));
        }
        [Test] public void ParallelNearPassAndDistantRotationRequireAWholeClearProof()
        {
            var sweep=new ManualSkiContactSweep(Body(),Contact(),Work());
            foreach(var pair in new[]{(Linear(A,0,0,vz:20),Linear(B,Body().LateralSeparation+.001,0,vz:20)),
                (Linear(A,0,0,yaw:1.2),Linear(B,3,2,yaw:-1.2))})
            {
                var hit=sweep.PreviewFirstContact(A,pair.Item1,B,pair.Item2);
                Assert.That(hit.Status,Is.EqualTo(SkiFirstContactStatus.Clear));Assert.That(hit.Admission.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
                Assert.That(hit.ContactSeconds,Is.EqualTo(1));
            }
        }
        [Test] public void InitialProximityAndWorkExhaustionCannotBecomeMovingContactAdmission()
        {
            var still=Linear(A,0,0);var near=Linear(B,Body().LateralSeparation,0);
            var initial=new ManualSkiContactSweep(Body(),Contact(),Work()).PreviewFirstContact(A,still,B,near);
            Assert.That(initial.Status,Is.EqualTo(SkiFirstContactStatus.InitialProximity));Assert.That(initial.ContactSeconds,Is.Zero);
            Assert.That(initial.Admission.IsValid,Is.False);
            var a=Linear(A,-3,0,20,0,Math.PI/2);var b=Linear(B,0,0);
            var limited=new ManualSkiContactSweep(Body(),Contact(),Work(1)).PreviewFirstContact(A,a,B,b);
            Assert.That(limited.Status,Is.EqualTo(SkiFirstContactStatus.WorkLimit));Assert.That(limited.IntervalChecks,Is.EqualTo(1));
            Assert.That(limited.Admission.IsValid,Is.False);
        }
        [Test] public void ACoarseMovingTurnReserveCannotBeMistakenForActualContact()
        {
            var sample=new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
            var force=new SkiForceSystem(SkiForceTests.Definition(),1).PreviewTrajectory(new SkiState(0,20),700,sample,1);
            var definition=ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(
                ManualSkiForceTrajectoryTests.HeadingDefinition().ToJson(),"path_integration_substeps","1"));
            var a=new ManualSkiWorldTrajectory(new ManualSkiSteeringSystem(definition,1)
                .Preview(new ManualSkiHeadingState(0,0,0,1.2),1000,force,1,100));
            var b=Linear(B,0,Body().LongitudinalSeparation+.02);
            Assert.That(a.PositionErrorBoundMeters,Is.GreaterThan(Contact().ContactTolerance));
            var hit=new ManualSkiContactSweep(Body(),Contact(),Work()).PreviewFirstContact(A,a,B,b);
            Assert.That(hit.Status,Is.EqualTo(SkiFirstContactStatus.WorkLimit));Assert.That(hit.Admission.IsValid,Is.False);
        }
        [Test] public void IdentityDurationAndDefaultReceiptStayExplicit()
        {
            var sweep=new ManualSkiContactSweep(Body(),Contact(),Work());var a=Linear(A,0,0);var b=Linear(B,2,0,seconds:.5);
            Assert.Catch(()=>sweep.PreviewFirstContact(A,a,A,a));Assert.Catch(()=>sweep.PreviewFirstContact(A,a,B,b));
            Assert.Catch(()=>sweep.PreviewFirstContact(A,a,B,default));Assert.That(default(SkiFirstContactPreview).IsValid,Is.False);
            Assert.That(sweep.FirstContactContextJson,Does.Contain("existing_tolerance_half_band"));
        }
        [Test] public void SwappedAndWarmContactQueriesAreRepeatableAndAllocateNothing()
        {
            var sweep=new ManualSkiContactSweep(Body(),Contact(),Work());var a=Linear(A,0,0,yaw:1.2);var b=Linear(B,1.1,0);
            var expected=sweep.PreviewFirstContact(A,a,B,b);CheckReached(a,b,expected);
            Assert.That(sweep.PreviewFirstContact(B,b,A,a),Is.EqualTo(expected));
            for(int i=0;i<128;i++)sweep.PreviewFirstContact(A,a,B,b);
            long before=GC.GetAllocatedBytesForCurrentThread();SkiFirstContactPreview actual=default;
            for(int i=0;i<1000;i++)actual=sweep.PreviewFirstContact(A,a,B,b);
            long used=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(used,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
        }
        [Test] public void ActualWoodlandArrivalResponseAndSuffixPayOneFullClock()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var world=f.World;
            var candidates=f.Candidates();var windows=f.Windows();var initial=world.GetActor(0);
            var hit=new ManualSkiContactSweep(f.Source.Venue.Occupancy,Contact(),Work())
                .PreviewFirstContact(candidates[0].Athlete,candidates[0].Trajectory,candidates[1].Athlete,candidates[1].Trajectory);
            Assert.That(hit.Status,Is.EqualTo(SkiFirstContactStatus.Reached));var tick=world.BeginTick();
            for(int i=0;i<2;i++)candidates[i]=new SkiWorldMotionCandidate(candidates[i].Athlete,candidates[i].Trajectory.Slice(0,hit.ContactSeconds));
            var pairs=new[]{new SkiReachedContactPair(candidates[0].Athlete,candidates[1].Athlete)};
            Assert.That(world.TryAppendArrivalInterval(tick,candidates,windows,Array.Empty<SkiReachedContactPair>(),0,
                Array.Empty<SkiActorCourseEdge>(),0,new[]{0.0,0.0},pairs,1),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(world.TryResolveArrivals(tick),Is.True);var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);
            for(int i=0;i<2;i++)
            {
                var a=world.GetWorkingActor(tick,i);var c=world.GetWorkingCursor(tick,i);
                candidates[i]=new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(
                    new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),0,0,world.GetRemainingSeconds(tick))));
                windows[i]=new ManualScopedWorldWindow(a.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
            }
            Assert.That(world.TryAppendInterval(tick,candidates,windows,pairs,1,Array.Empty<SkiActorCourseEdge>(),0,new[]{0.0,0.0}),Is.True);
            Assert.That(world.TryFinalizeTick(tick,out var full),Is.True);world.Commit(full);
            Assert.That(world.Tick.Value,Is.EqualTo(1));Assert.That(world.GetPhysicalMotion(0).SpeedMetersPerSecond,Is.GreaterThan(0));
            Assert.That(world.GetActor(0).Pose,Is.Not.EqualTo(initial.Pose));
        }
    }
}

using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiContactSchedulerTests
    {
        private static readonly SkiReachedContactPair[] NoPairs=Array.Empty<SkiReachedContactPair>();
        private static readonly SkiActorCourseEdge[] NoEdges=Array.Empty<SkiActorCourseEdge>();
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static SkiContactDefinition Contact()=>SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"scheduler_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
        private static SkiContactSweepDefinition Work(int count=4096)=>SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"scheduler_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":"+count+",\"time_tolerance_s\":0.0000001}");
        private static SkiWorldMotionCandidate Actor(string id,double x,double z,double speed=0)
        {
            var key=new AthleteId(id);var pose=new ManualSkiHeadingState(x,z,0);var velocity=new SkiPlanarVelocity(0,speed);
            return new SkiWorldMotionCandidate(key,new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(Body(),1).PreviewTrajectory(
                new ManualSkiContactBody(key,pose,velocity,75),new SkiContactMotion(velocity,0),0,0,.05)));
        }
        private static ManualSkiContactScheduler Scheduler(int count=4,int checks=4096)=>new ManualSkiContactScheduler(Body(),Contact(),Work(checks),count);
        [Test] public void ArrivalGroupSamplesEachImmutableEndpointOnceAcrossAllPairs()
        {
            double gap=Body().LongitudinalSeparation+.05;
            var actors=new[]{Actor("b",0,gap),Actor("d",3,gap),Actor("c",3,0,2),Actor("a",0,0,2)};
            var scheduler=Scheduler();var result=scheduler.Preview(actors,4,NoPairs,0,NoEdges,0);
            Assert.That(result.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(result.ArrivingPairs,Is.EqualTo(2));
            Assert.That(scheduler.EndpointPoseSamples,Is.EqualTo(actors.Length),"The same immutable prefix endpoint is shared by every pair in this query.");
            actors[0]=Actor("b",0,gap+.02);
            var changed=scheduler.Preview(actors,4,NoPairs,0,NoEdges,0);
            Assert.That(changed.ArrivingPairs,Is.EqualTo(1));Assert.That(scheduler.GetArrival(0).First.Value,Is.EqualTo("c"));
            Assert.That(scheduler.EndpointPoseSamples,Is.EqualTo(actors.Length),"A later query must rebuild endpoints from its new prefix.");
        }
        [Test] public void TwoSimultaneousSeparatedContactsHaveOneStableActualEndpointGroup()
        {
            double gap=Body().LongitudinalSeparation+.05;
            var actors=new[]{Actor("b",0,gap),Actor("d",3,gap),Actor("c",3,0,2),Actor("a",0,0,2)};
            var s=Scheduler();var result=s.Preview(actors,4,NoPairs,0,NoEdges,0);
            Assert.That(result.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(result.ArrivingPairs,Is.EqualTo(2));
            Assert.That(result.Seconds,Is.EqualTo(.025).Within(5e-7));
            Assert.That(s.GetArrival(0).First.Value,Is.EqualTo("a"));Assert.That(s.GetArrival(0).Second.Value,Is.EqualTo("b"));
            Assert.That(s.GetArrival(1).First.Value,Is.EqualTo("c"));Assert.That(s.GetArrival(1).Second.Value,Is.EqualTo("d"));
            Array.Reverse(actors);var reversed=s.Preview(actors,4,NoPairs,0,NoEdges,0);Assert.That(reversed,Is.EqualTo(result));
            Assert.That(s.GetArrival(0).First.Value,Is.EqualTo("a"));Assert.That(s.GetPrefix(0).Athlete,Is.EqualTo(actors[0].Athlete));
        }
        [Test] public void EarlierArrivalDoesNotGiveALaterPairUnreachedAdmission()
        {
            double length=Body().LongitudinalSeparation;
            var actors=new[]{Actor("a",0,0,2),Actor("b",0,length+.04),Actor("c",3,0,2),Actor("d",3,length+.08)};
            var s=Scheduler();var hit=s.Preview(actors,4,NoPairs,0,NoEdges,0);
            Assert.That(hit.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(hit.ArrivingPairs,Is.EqualTo(1));
            Assert.That(s.GetArrival(0).First.Value,Is.EqualTo("a"));Assert.That(hit.Seconds,Is.LessThan(.021));
            Assert.Throws<ArgumentOutOfRangeException>(()=>s.GetArrival(1));
        }
        [Test] public void MultipleUnregisteredInitialContactsUseStableIdentityOrder()
        {
            var actors=new[]{Actor("d",3,0),Actor("c",3,Body().LongitudinalSeparation),Actor("b",0,0),Actor("a",0,Body().LongitudinalSeparation)};
            var s=Scheduler();var hit=s.Preview(actors,4,NoPairs,0,NoEdges,0);
            Assert.That(hit.Status,Is.EqualTo(SkiContactScheduleStatus.InitialProximity));Assert.That(hit.First.Value,Is.EqualTo("a"));Assert.That(hit.Second.Value,Is.EqualTo("b"));
            Array.Reverse(actors);Assert.That(s.Preview(actors,4,NoPairs,0,NoEdges,0),Is.EqualTo(hit));
        }
        [Test] public void ExplicitExistingContactCanContinueWhileAnotherPairArrives()
        {
            double length=Body().LongitudinalSeparation,width=Body().LateralSeparation;
            var actors=new[]{Actor("a",0,0,2),Actor("b",0,length+.05),Actor("c",3,0,2),Actor("d",3+width,0,2)};
            var existing=new[]{new SkiReachedContactPair(actors[2].Athlete,actors[3].Athlete)};
            var s=Scheduler();var hit=s.Preview(actors,4,existing,1,NoEdges,0);
            Assert.That(hit.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(hit.ArrivingPairs,Is.EqualTo(1));
            Assert.That(s.GetArrival(0).First.Value,Is.EqualTo("a"));
            Assert.That(s.Preview(actors,4,NoPairs,0,NoEdges,0).Status,Is.EqualTo(SkiContactScheduleStatus.InitialProximity));
            Assert.Throws<ArgumentOutOfRangeException>(()=>s.GetPrefix(0));
        }
        [Test] public void ExplicitCourseEdgeBeforeBodyArrivalCannotBeIgnored()
        {
            double length=Body().LongitudinalSeparation;var actors=new[]{Actor("a",0,0,2),Actor("b",0,length+.05)};
            var edge=new SkiActorCourseEdge(actors[0].Athlete,new SkiCourseEdgePlane("blocked",0,length*.5+Body().EdgeClearance+.02,0,-1));
            var s=Scheduler(2);var hit=s.Preview(actors,2,NoPairs,0,new[]{edge},1);
            Assert.That(hit.Status,Is.EqualTo(SkiContactScheduleStatus.Unresolved));Assert.That(hit.Admission.Status,Is.EqualTo(SkiJointSweepStatus.EdgeInterval));
            Assert.That(hit.Seconds,Is.LessThan(.025));Assert.Throws<ArgumentOutOfRangeException>(()=>s.GetArrival(0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>s.GetPrefix(0));
        }
        [Test] public void WorkLimitAndForeignIdentityDoNotMintPrefixes()
        {
            var actors=new[]{Actor("a",0,0,2),Actor("b",0,Body().LongitudinalSeparation+.05)};
            var s=Scheduler(2,1);Assert.That(s.Preview(actors,2,NoPairs,0,NoEdges,0).Status,Is.EqualTo(SkiContactScheduleStatus.Unresolved));
            Assert.Throws<ArgumentOutOfRangeException>(()=>s.GetPrefix(0));
            Assert.Throws<ArgumentException>(()=>s.Preview(actors,2,new[]{new SkiReachedContactPair(actors[0].Athlete,new AthleteId("foreign"))},1,NoEdges,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>s.GetArrival(0));
        }
        [Test] public void WorldSchedulesRespondsAndContinuesTheSameFixedClock()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var w=f.World;
            var tick=w.BeginTick();var before=w.GetActor(0);var candidates=f.Candidates();var windows=f.Windows();var grades=new[]{0.0,0.0};
            Assert.That(w.TryAppendToFirstContact(tick,candidates,windows,NoPairs,0,NoEdges,0,grades,out var hit),Is.True,w.LastAttempt.Status.ToString());
            Assert.That(hit.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(w.HasPendingContact(tick),Is.True);
            Assert.That(w.TryResolveArrivals(tick),Is.True);var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);
            var reached=new[]{new SkiReachedContactPair(candidates[0].Athlete,candidates[1].Athlete)};
            for(int i=0;i<2;i++)
            {
                var a=w.GetWorkingActor(tick,i);var c=w.GetWorkingCursor(tick,i);
                candidates[i]=new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(
                    new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),0,0,w.GetRemainingSeconds(tick))));
                windows[i]=new ManualScopedWorldWindow(a.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
            }
            Assert.That(w.TryAppendToFirstContact(tick,candidates,windows,reached,1,NoEdges,0,grades,out var tail),Is.True,w.LastAttempt.Status.ToString());
            Assert.That(tail.Status,Is.EqualTo(SkiContactScheduleStatus.Clear));Assert.That(w.TryFinalizeTick(tick,out var full),Is.True);
            w.Commit(full);Assert.That(w.Tick.Value,Is.EqualTo(1));Assert.That(w.GetActor(0).Pose,Is.Not.EqualTo(before.Pose));
            Assert.That(w.GetPhysicalMotion(0).SpeedMetersPerSecond,Is.GreaterThan(0));
        }
        [Test] public void FailedWorldCourseAdmissionKeepsClockPoseAndReceiptsUnchanged()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var w=f.World;
            var tick=w.BeginTick();var before=w.GetActor(0);var windows=f.Windows();var c=w.GetCursor(1);
            windows[1]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,c.CreditedArcNanometers-1);
            Assert.That(w.TryAppendToFirstContact(tick,f.Candidates(),windows,NoPairs,0,NoEdges,0,new[]{0.0,0.0},out var hit),Is.False);
            Assert.That(hit.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(w.GetElapsedSeconds(tick),Is.Zero);
            Assert.That(w.HasPendingContact(tick),Is.False);Assert.That(w.GetWorkingMotionPieceCount(tick),Is.Zero);
            Assert.That(w.GetWorkingActor(tick,0),Is.EqualTo(before));w.AbortTick(tick);
        }
        [Test] public void WarmCommonArrivalSchedulingAllocatesNothing()
        {
            var actors=new[]{Actor("a",0,0,2),Actor("b",0,Body().LongitudinalSeparation+.05),Actor("c",3,0,2),Actor("d",3,Body().LongitudinalSeparation+.05)};
            var s=Scheduler();var expected=s.Preview(actors,4,NoPairs,0,NoEdges,0);
            Assert.That(expected.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));
            for(int i=0;i<128;i++)s.Preview(actors,4,NoPairs,0,NoEdges,0);
            long before=GC.GetAllocatedBytesForCurrentThread();SkiContactSchedulePreview actual=default;
            for(int i=0;i<1000;i++)actual=s.Preview(actors,4,NoPairs,0,NoEdges,0);
            long used=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(used,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
        }
    }
}

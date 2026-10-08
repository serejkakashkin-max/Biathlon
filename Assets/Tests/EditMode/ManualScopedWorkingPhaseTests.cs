using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedWorkingPhaseTests
    {
        private static bool Append(ManualScopedSessionTests.Fixture f,ManualScopedWorldTick tick,double seconds)
        {
            var a=f.World.GetWorkingActor(tick,0);var c=f.World.GetWorkingCursor(tick,0);var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);
            var trajectory=new ManualSkiWorldTrajectory(snow.PreviewTrajectory(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),
                new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),0,0,seconds));
            return f.World.TryAppendInterval(tick,new[]{new SkiWorldMotionCandidate(a.Athlete,trajectory)},
                new[]{new ManualScopedWorldWindow(a.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,2,f.Source.Catalog.Get(c.OccurrenceIndex).LengthNanometers)},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,new[]{a.Observation.GradeAlong(a.Pose)});
        }
        [TestCase("penalty_entry_before")][TestCase("penalty_repeat_before")][TestCase("penalty_return_before")]
        public void MovingPenaltyGatePaysItsSuffixWithoutStoppingOrMutatingLifecycleEarly(string phase)
        {
            var f=new ManualScopedSessionTests.Fixture(phase);var world=f.World;
            var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session},world);var original=world.GetActor(0);var c=world.GetCursor(0);
            var row=f.Source.Catalog.Get(c.OccurrenceIndex);Assert.That(row.End,Is.EqualTo(phase=="penalty_entry_before"?ManualRacePathEnd.PenaltyDecision:ManualRacePathEnd.PenaltyCircuit));
            Assert.That(f.Source.Catalog.TrySample(row.Index,row.LengthNanometers,out var end),Is.True);
            double split=((end.X-original.Pose.X)*end.ForwardX+(end.Z-original.Pose.Z)*end.ForwardZ)/
                (original.Velocity.X*end.ForwardX+original.Velocity.Z*end.ForwardZ);
            Assert.That(split,Is.InRange(.000001,.049));
            var pending=new[]{f.Session.PrepareManualScopedStep(f.Command(),world,0)};var tick=world.BeginTick();int events=f.Events.Count;
            var range=f.Session.CurrentRangeMotion;var state=f.Session.PhysiologyState;
            Assert.That(transaction.TryHandoffWorkingPhases(tick,pending,new[]{0}),Is.False);
            Assert.That(Append(f,tick,split),Is.True,world.LastAttempt.Status.ToString());
            var at=world.GetWorkingActor(tick,0);var motion=world.GetWorkingPhysicalMotion(tick,0);
            Assert.That(transaction.TryHandoffWorkingPhases(tick,pending,new[]{0}),Is.True);
            int expectedTarget=phase=="penalty_entry_before"?f.Source.Catalog.GetPenaltyOccurrence(row.BoutIndex,1):
                phase=="penalty_repeat_before"?f.Source.Catalog.GetPenaltyOccurrence(row.BoutIndex,row.PenaltyOrdinal+1):f.Source.Catalog.GetPenaltyReturnOccurrence(row.BoutIndex);
            var target=world.GetWorkingCursor(tick,0);Assert.That(target.OccurrenceIndex,Is.EqualTo(expectedTarget));
            Assert.That(target.ObservedArcNanometers,Is.Zero);Assert.That(world.GetWorkingActor(tick,0).Pose,Is.EqualTo(at.Pose));
            Assert.That(world.GetWorkingActor(tick,0).Velocity,Is.EqualTo(at.Velocity));Assert.That(world.GetWorkingPhysicalMotion(tick,0),Is.EqualTo(motion));
            Assert.That(world.GetElapsedSeconds(tick),Is.EqualTo(split));Assert.That(world.GetWorkingMotionPieceCount(tick),Is.EqualTo(1));
            Assert.That(f.Session.CurrentRangeMotion,Is.EqualTo(range));Assert.That(f.Session.PhysiologyState,Is.EqualTo(state));Assert.That(f.Events.Count,Is.EqualTo(events));
            Assert.That(Append(f,tick,world.GetRemainingSeconds(tick)),Is.True,world.LastAttempt.Status+" / "+world.LastAttempt.Course.Status);
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);var expected=world.GetProposedPhysicalMotion(proof,0);
            var after=world.GetProposedCursor(proof,0);transaction.Commit(pending,proof);
            Assert.That(f.Session.Motion,Is.EqualTo(expected));Assert.That(f.Session.Motion.SpeedMetersPerSecond,Is.GreaterThan(0));
            if(phase=="penalty_return_before")
            {
                Assert.That(f.Session.GetBoutRangeFrame(row.BoutIndex).Phase,Is.EqualTo(RangeTraversalPhase.Complete));
                Assert.That(f.Session.GetBoutRangeFrame(row.BoutIndex).CompletedLoops,Is.EqualTo(row.PenaltyOrdinal));
                Assert.That(f.Session.RouteNanometers,Is.GreaterThan(c.CreditedRouteNanometers));
            }
            else
            {
                Assert.That(f.Session.CurrentRangeMotion.Value.Phase,Is.EqualTo(RangeTraversalPhase.PenaltyLoop));
                Assert.That(f.Session.CurrentRangeMotion.Value.PenaltyNanometers,Is.EqualTo(after.CreditedArcNanometers));
            }
            Assert.That(f.Session.Tick,Is.EqualTo(world.Tick));Assert.That(world.GetActor(0).Pose.HeadingNanoradians,Is.EqualTo(original.Pose.HeadingNanoradians));
        }
        [Test] public void WarmSuccessfulFutureGateAndAbortAllocateNothing()
        {
            var f=new ManualScopedSessionTests.Fixture("penalty_entry_before");var world=f.World;var c=world.GetCursor(0);var a=world.GetActor(0);
            var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session},world);var p=new[]{f.Session.PrepareManualScopedStep(f.Command(),world,0)};var ids=new[]{0};
            f.Source.Catalog.TrySample(c.OccurrenceIndex,f.Source.Catalog.Get(c.OccurrenceIndex).LengthNanometers,out var end);
            double seconds=((end.X-a.Pose.X)*end.ForwardX+(end.Z-a.Pose.Z)*end.ForwardZ)/(a.Velocity.X*end.ForwardX+a.Velocity.Z*end.ForwardZ);
            var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);var candidates=new[]{new SkiWorldMotionCandidate(a.Athlete,
                new ManualSkiWorldTrajectory(snow.PreviewTrajectory(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,0),0,0,seconds)))};
            var windows=new[]{new ManualScopedWorldWindow(a.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,2,f.Source.Catalog.Get(c.OccurrenceIndex).LengthNanometers)};
            var grades=new[]{a.Observation.GradeAlong(a.Pose)};bool passed=true;
            for(int i=0;i<128;i++)
            {
                var tick=world.BeginTick();passed&=world.TryAppendInterval(tick,candidates,windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,grades);
                passed&=transaction.TryHandoffWorkingPhases(tick,p,ids);world.AbortTick(tick);
            }
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
            {
                var tick=world.BeginTick();passed&=world.TryAppendInterval(tick,candidates,windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,grades);
                passed&=transaction.TryHandoffWorkingPhases(tick,p,ids);world.AbortTick(tick);
            }
            long used=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(passed,Is.True);Assert.That(used,Is.Zero);
        }
        [Test] public void AbortingAProvisionalPenaltyHandoffRestoresTheWholeAcceptedPhaseAndWorld()
        {
            var f=new ManualScopedSessionTests.Fixture("penalty_entry_before");var world=f.World;var c=world.GetCursor(0);var source=world.GetActor(0);
            var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session},world);var p=new[]{f.Session.PrepareManualScopedStep(f.Command(),world,0)};
            var row=f.Source.Catalog.Get(c.OccurrenceIndex);f.Source.Catalog.TrySample(row.Index,row.LengthNanometers,out var end);
            double split=((end.X-source.Pose.X)*end.ForwardX+(end.Z-source.Pose.Z)*end.ForwardZ)/(source.Velocity.X*end.ForwardX+source.Velocity.Z*end.ForwardZ);
            var tick=world.BeginTick();var phase=f.Session.CurrentRangeMotion;int events=f.Events.Count;
            Assert.That(Append(f,tick,split),Is.True);Assert.That(transaction.TryHandoffWorkingPhases(tick,p,new[]{0}),Is.True);
            world.AbortTick(tick);Assert.That(world.GetCursor(0).OccurrenceIndex,Is.EqualTo(c.OccurrenceIndex));Assert.That(world.GetActor(0).Pose,Is.EqualTo(source.Pose));
            Assert.That(f.Session.CurrentRangeMotion,Is.EqualTo(phase));Assert.That(f.Events.Count,Is.EqualTo(events));Assert.That(world.Tick,Is.EqualTo(f.Session.Tick));
            Assert.Throws<ArgumentException>(()=>transaction.TryHandoffWorkingPhases(tick,p,new[]{0}));
        }
    }
}

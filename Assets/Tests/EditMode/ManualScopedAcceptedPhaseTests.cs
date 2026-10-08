using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedAcceptedPhaseTests
    {
        [Test] public void ActualCompletedBoutAuthorizesDepartureThenSharedPowerStartsTheAthleteWithoutWaiting()
        {
            var f=new ManualScopedSessionTests.Fixture("shooting");var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session},f.World);
            var indexes=new[]{0};Assert.That(transaction.TryHandoffAcceptedPhases(indexes),Is.False);
            int shots=0,steps=0;
            while(f.Session.CurrentRangeMotion.Value.Phase==RangeTraversalPhase.Shooting&&steps++<2000)
            {
                var pending=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);
                Assert.That(transaction.TryHandoffAcceptedPhases(indexes),Is.False,"Pending input cannot authorize an accepted future phase.");
                Assert.That(f.Propose(pending,out var proof),Is.True,f.World.LastAttempt.Status.ToString());
                transaction.Commit(new[]{pending},proof);
                if(f.Session.LastFrame.Shooting.Shot.Lifecycle.ShotAccepted)shots++;
            }
            Assert.That(shots,Is.EqualTo(5));Assert.That(f.Session.CurrentRangeMotion.Value.Phase,Is.EqualTo(RangeTraversalPhase.RangeExit));
            Assert.That(f.Session.Lifecycle.CompletedBouts,Is.EqualTo(1));
            var actor=f.World.GetActor(0);var cursor=f.World.GetCursor(0);var motion=f.Session.Motion;var state=f.Session.PhysiologyState;
            var tick=f.Session.Tick;int events=f.Events.Count;
            Assert.That(transaction.TryHandoffAcceptedPhases(indexes),Is.True);
            Assert.That(f.World.GetCursor(0).OccurrenceIndex,Is.EqualTo(cursor.OccurrenceIndex+1));
            Assert.That(f.World.GetCursor(0).ObservedArcNanometers,Is.Zero);Assert.That(f.World.GetCursor(0).CreditedRouteNanometers,Is.EqualTo(cursor.CreditedRouteNanometers));
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(actor.Pose));Assert.That(f.World.GetActor(0).Velocity,Is.EqualTo(actor.Velocity));
            Assert.That(f.Session.Motion,Is.EqualTo(motion));Assert.That(f.World.GetPhysicalMotion(0),Is.EqualTo(motion));
            Assert.That(f.Session.PhysiologyState,Is.EqualTo(state));Assert.That(f.Session.Tick,Is.EqualTo(tick));Assert.That(f.World.Tick,Is.EqualTo(tick));
            Assert.That(f.Events.Count,Is.EqualTo(events));Assert.That(transaction.TryHandoffAcceptedPhases(indexes),Is.False,"Departure receipt cannot repeat.");
            var next=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);Assert.That(next.MovementActive,Is.True);
            Assert.That(f.Propose(next,out var moving),Is.True,f.World.LastAttempt.Status.ToString());transaction.Commit(new[]{next},moving);
            Assert.That(f.Session.Tick.Value,Is.EqualTo(tick.Value+1));Assert.That(f.World.GetActor(0).Pose,Is.Not.EqualTo(actor.Pose));
            Assert.That(f.Session.Motion.SpeedMetersPerSecond,Is.GreaterThan(0));Assert.That(f.Session.Motion.DistanceNanometers,Is.GreaterThan(motion.DistanceNanometers));
        }
        [Test] public void UncompletedOrPendingShootingCannotOpenTheAcceptedPhaseGate()
        {
            var f=new ManualScopedSessionTests.Fixture("shooting");var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session},f.World);
            var indexes=new[]{0};var source=f.World.GetActor(0);var c=f.World.GetCursor(0);int events=f.Events.Count;
            Assert.That(transaction.TryHandoffAcceptedPhases(indexes),Is.False);
            var pending=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);
            Assert.That(f.Propose(pending,out var proof),Is.True);
            Assert.That(transaction.TryHandoffAcceptedPhases(indexes),Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(()=>transaction.TryHandoffAcceptedPhases(new[]{1}));
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(source.Pose));Assert.That(f.World.GetCursor(0).OccurrenceIndex,Is.EqualTo(c.OccurrenceIndex));
            Assert.That(f.Events.Count,Is.EqualTo(events));transaction.Commit(new[]{pending},proof);
            Assert.That(f.Session.CurrentRangeMotion.Value.Phase,Is.EqualTo(RangeTraversalPhase.Shooting));
        }
        [Test] public void WarmAcceptedPhaseRejectionQueriesAllocateNothing()
        {
            var f=new ManualScopedSessionTests.Fixture("shooting");var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session},f.World);var indexes=new[]{0};
            for(int i=0;i<128;i++)transaction.TryHandoffAcceptedPhases(indexes);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)transaction.TryHandoffAcceptedPhases(indexes);
            long used=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(used,Is.Zero);
            Assert.That(f.World.GetCursor(0).OccurrenceIndex,Is.EqualTo(f.Occurrence));
        }
        [TestCase("penalty_entry")][TestCase("penalty_repeat")][TestCase("penalty_return")]
        public void AcceptedPenaltySnapshotAuthorizesOnlyItsActualDebtCircuitOrReturn(string phase)
        {
            // Setup uses an exact clipped legacy boundary snapshot, not a
            // production policy to brake or wait at penalty junctions.
            var f=new ManualScopedSessionTests.Fixture(phase);var transaction=new ManualScopedRosterSessionCommit(new[]{f.Session},f.World);
            var cursor=f.World.GetCursor(0);var actor=f.World.GetActor(0);var motion=f.Session.Motion;var range=f.Session.CurrentRangeMotion;
            var tick=f.Session.Tick;int events=f.Events.Count;
            int completed=f.Session.GetBoutRangeFrame(0).CompletedLoops;
            int target=phase=="penalty_entry"?f.Source.Catalog.GetPenaltyOccurrence(0,1):
                phase=="penalty_repeat"?f.Source.Catalog.GetPenaltyOccurrence(0,completed+1):f.Source.Catalog.GetPenaltyReturnOccurrence(0);
            Assert.That(transaction.TryHandoffAcceptedPhases(new[]{0}),Is.True);
            Assert.That(f.World.GetCursor(0).OccurrenceIndex,Is.EqualTo(target));Assert.That(f.World.GetCursor(0).ObservedArcNanometers,Is.Zero);
            Assert.That(f.World.GetCursor(0).CreditedRouteNanometers,Is.EqualTo(cursor.CreditedRouteNanometers));
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(actor.Pose));Assert.That(f.World.GetActor(0).Velocity,Is.EqualTo(actor.Velocity));
            Assert.That(f.World.Tick,Is.EqualTo(tick));Assert.That(f.Session.Tick,Is.EqualTo(tick));Assert.That(f.Session.Motion,Is.EqualTo(motion));
            Assert.That(f.Events.Count,Is.EqualTo(events));Assert.That(f.Session.CurrentRangeMotion,Is.EqualTo(range));
            Assert.That(transaction.TryHandoffAcceptedPhases(new[]{0}),Is.False);
            var p=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);Assert.That(p.MovementActive,Is.True);
            Assert.That(f.Session.GetPreparedManualMotionInput(p,f.World,0,500).SteeringInput,Is.EqualTo(500));
        }
    }
}

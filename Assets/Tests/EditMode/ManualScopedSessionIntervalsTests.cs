using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedSessionIntervalsTests
    {
        private static readonly SkiReachedContactPair[] NoPairs=Array.Empty<SkiReachedContactPair>();
        private static readonly SkiActorCourseEdge[] NoEdges=Array.Empty<SkiActorCourseEdge>();
        private static bool Append(ManualScopedSessionTests.Fixture f,ManualScopedWorldTick tick,double seconds)
        {
            var world=f.World;var actor=world.GetWorkingActor(tick,0);var cursor=world.GetWorkingCursor(tick,0);
            var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);
            var trajectory=new ManualSkiWorldTrajectory(snow.PreviewTrajectory(new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75),
                new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),0,0,seconds));
            return world.TryAppendInterval(tick,new[]{new SkiWorldMotionCandidate(actor.Athlete,trajectory)},
                new[]{new ManualScopedWorldWindow(actor.Athlete,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,2,
                    f.Source.Catalog.Get(cursor.OccurrenceIndex).LengthNanometers)},NoPairs,0,NoEdges,0,new[]{actor.Observation.GradeAlong(actor.Pose)});
        }
        [TestCase("start_seam")][TestCase("base_seam")]
        public void ARealOrdinarySeamPublishesPhysicalSessionSegmentsWithoutAPaidStop(string phase)
        {
            var f=new ManualScopedSessionTests.Fixture(phase);var world=f.World;var c=world.GetCursor(0);var actor=world.GetActor(0);
            var rangeAtSource=f.Session.CurrentRangeMotion.Value;
            var row=f.Source.Catalog.Get(c.OccurrenceIndex);Assert.That(f.Source.Catalog.TrySample(row.Index,row.LengthNanometers,out var end),Is.True);
            double to=(end.X-actor.Pose.X)*end.ForwardX+(end.Z-actor.Pose.Z)*end.ForwardZ;
            double speed=actor.Velocity.X*end.ForwardX+actor.Velocity.Z*end.ForwardZ;
            double split=to/speed;Assert.That(split,Is.InRange(.000001,.049));
            var command=f.Command();var pending=f.Session.PrepareManualScopedStep(command,world,0);var tick=world.BeginTick();
            Assert.That(Append(f,tick,split),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(world.GetWorkingMotionPieceCount(tick),Is.EqualTo(1));
            Assert.That(world.TryHandoffOrdinary(tick,new[]{0}),Is.True);
            var nextActor=world.GetWorkingActor(tick,0);var input=f.Session.GetPreparedManualMotionInput(pending,world,tick,0,0);
            Assert.That(input.IsValid,Is.True);Assert.That(nextActor.Pose.HeadingNanoradians,Is.EqualTo(actor.Pose.HeadingNanoradians));
            Assert.That(nextActor.Velocity,Is.EqualTo(actor.Velocity));
            Assert.That(Append(f,tick,world.GetRemainingSeconds(tick)),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);Assert.That(world.GetProposedMotionPieceCount(proof),Is.EqualTo(2));
            var first=world.GetProposedMotionPiece(proof,0,0);var second=world.GetProposedMotionPiece(proof,0,1);
            Assert.That(first.OccurrenceIndex,Is.EqualTo(row.Index));Assert.That(second.OccurrenceIndex,Is.EqualTo(row.Index+1));
            Assert.That(first.EndSeconds,Is.EqualTo(second.StartSeconds));Assert.That(first.TravelAfter,Is.EqualTo(second.TravelBefore));
            Assert.That(first.RouteAfter,Is.EqualTo(second.RouteBefore));Assert.That(second.EndSeconds,Is.EqualTo(.05));
            var resolved=f.Session.PrepareManualScopedCommit(pending,proof,0);Assert.That(resolved.Movement.SegmentCount,Is.EqualTo(2));
            var a=f.Session.GetPreparedSegment(resolved,0);var b=f.Session.GetPreparedSegment(resolved,1);
            Assert.That(a.EndSeconds,Is.EqualTo(split));Assert.That(b.StartSeconds,Is.EqualTo(split));
            Assert.That(a.TravelAfter,Is.EqualTo(b.TravelBefore));
            var expected=world.GetProposedPhysicalMotion(proof,0);f.Session.CommitStep(resolved);world.Commit(proof);
            Assert.That(f.Session.Motion,Is.EqualTo(expected));Assert.That(f.Session.Tick,Is.EqualTo(world.Tick));
            Assert.That(f.Session.RouteNanometers,Is.EqualTo(world.GetCursor(0).CreditedRouteNanometers));
            Assert.That(f.Session.Motion.SpeedMetersPerSecond,Is.GreaterThan(0));Assert.That(world.GetActor(0).Pose.HeadingNanoradians,Is.EqualTo(actor.Pose.HeadingNanoradians));
            if(phase=="start_seam")
            {
                Assert.That(a.OnStartApproach,Is.True);Assert.That(b.OnStartApproach,Is.False);
                Assert.That(f.Session.RemainingStartApproachNanometers,Is.Zero);
                Assert.That(f.Session.StartApproachExitTime,Is.EqualTo(FinishTime.InsideTick(f.Session.Tick,20,split)));
                int count=0;for(int i=0;i<f.Events.Count;i++)if(f.Events[i].EventType==RaceEventType.StartApproachCompleted)count++;
                Assert.That(count,Is.EqualTo(1));
            }
            else
            {
                Assert.That(f.Session.CurrentRangeMotion.Value.Phase,Is.EqualTo(RangeTraversalPhase.Approach));
                Assert.That(f.Session.CurrentRangeMotion.Value.RouteNanometers,Is.EqualTo(Math.Max(rangeAtSource.RouteNanometers,f.Session.RouteNanometers)));
            }
            Assert.That(f.Session.PrepareManualScopedStep(f.Command(),world,0).ManualWorldRequired,Is.True);
            Assert.Throws<ArgumentException>(()=>world.GetProposedMotionPiece(proof,0,0));
        }
        [Test] public void MotionHistoryRetainsOnlyAdmittedIntervalsAndCannotSurviveAbortOrConsumption()
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var world=f.World;var tick=world.BeginTick();
            Assert.That(world.GetWorkingMotionPieceCount(tick),Is.Zero);
            var candidates=f.Candidates(); // Original source; duration is the whole tick.
            var cut=new SkiWorldMotionCandidate[2];for(int i=0;i<2;i++)cut[i]=new SkiWorldMotionCandidate(candidates[i].Athlete,candidates[i].Trajectory.Slice(0,.02));
            Assert.That(world.TryAppendInterval(tick,cut,f.Windows(),NoPairs,0,NoEdges,0,new[]{0.0,0.0}),Is.True);
            Assert.That(world.GetWorkingMotionPieceCount(tick),Is.EqualTo(1));var first=world.GetWorkingMotionPiece(tick,1,0);
            Assert.That(world.TryAppendInterval(tick,cut,f.Windows(),NoPairs,0,NoEdges,0,new[]{0.0,0.0}),Is.False);
            Assert.That(world.GetWorkingMotionPieceCount(tick),Is.EqualTo(1));Assert.That(world.GetWorkingMotionPiece(tick,1,0).TravelAfter,Is.EqualTo(first.TravelAfter));
            Assert.Throws<ArgumentOutOfRangeException>(()=>world.GetWorkingMotionPiece(tick,2,0));
            Assert.Throws<ArgumentOutOfRangeException>(()=>world.GetWorkingMotionPiece(tick,1,1));
            world.AbortTick(tick);Assert.Throws<ArgumentException>(()=>world.GetWorkingMotionPieceCount(tick));
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var proof),Is.True);
            Assert.That(world.GetProposedMotionPieceCount(proof),Is.EqualTo(1));world.Commit(proof);
            Assert.Throws<ArgumentException>(()=>world.GetProposedMotionPiece(proof,1,0));
        }
        [Test] public void IntervalWorkLimitCannotMintAPaidStandingSuffix()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(intervalChecks:64);var world=f.World;var tick=world.BeginTick();
            var candidates=f.Candidates();var rows=new SkiWorldMotionCandidate[2];var windows=f.Windows();var grades=new[]{0.0,0.0};
            for(int part=0;part<64;part++)
            {
                for(int i=0;i<2;i++)
                {
                    var actor=world.GetWorkingActor(tick,i);var c=world.GetWorkingCursor(tick,i);var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);
                    rows[i]=new SkiWorldMotionCandidate(actor.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(
                        new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75),new SkiContactMotion(actor.Velocity,0),0,0,.0001)));
                    windows[i]=new ManualScopedWorldWindow(actor.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
                }
                Assert.That(world.TryAppendInterval(tick,rows,windows,NoPairs,0,NoEdges,0,grades),Is.True,world.LastAttempt.Status.ToString());
            }
            double paid=world.GetElapsedSeconds(tick);var prefix=world.GetWorkingActor(tick,1);
            Assert.That(world.TryAppendInterval(tick,rows,windows,NoPairs,0,NoEdges,0,grades),Is.False);
            Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.IntervalLimit));
            Assert.That(world.GetWorkingMotionPieceCount(tick),Is.EqualTo(64));Assert.That(world.GetElapsedSeconds(tick),Is.EqualTo(paid));
            Assert.That(world.GetWorkingActor(tick,1).Pose,Is.EqualTo(prefix.Pose));Assert.That(world.TryFinalizeTick(tick,out _),Is.False);
            Assert.That(world.Tick.Value,Is.Zero);world.AbortTick(tick);
        }
    }
}

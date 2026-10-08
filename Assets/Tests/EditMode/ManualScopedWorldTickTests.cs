using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedWorldTickTests
    {
        private const long N=SkiState.NanometersPerMeter;
        private static readonly SkiReachedContactPair[] NoPairs=Array.Empty<SkiReachedContactPair>();
        private static readonly SkiActorCourseEdge[] NoEdges=Array.Empty<SkiActorCourseEdge>();
        private static readonly double[] FlatGrades={0,0};
        private static SkiWorldMotionCandidate[] Candidates(ManualScopedWorldRosterTests.Fixture f,ManualScopedWorldTick tick,double seconds)
        {
            var result=new SkiWorldMotionCandidate[2];var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);
            for(int i=0;i<2;i++)
            {
                var actor=f.World.GetWorkingActor(tick,i);var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75);
                result[i]=new SkiWorldMotionCandidate(actor.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(
                    body,new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),0,0,seconds)));
            }
            return result;
        }
        private static ManualScopedWorldWindow[] Windows(ManualScopedWorldRosterTests.Fixture f,ManualScopedWorldTick tick)
        {
            var result=new ManualScopedWorldWindow[2];
            for(int i=0;i<2;i++)
            {var c=f.World.GetWorkingCursor(tick,i);result[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,
                f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);}
            return result;
        }
        private static bool Append(ManualScopedWorldRosterTests.Fixture f,ManualScopedWorldTick tick,double seconds)
            =>f.World.TryAppendInterval(tick,Candidates(f,tick,seconds),Windows(f,tick),NoPairs,0,NoEdges,0,FlatGrades);

        [Test] public void CompleteCommonIntervalsPublishOneOwnedClockAndAllActorsTogether()
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var before=f.World.GetActor(1);var tick=f.World.BeginTick();
            Assert.That(f.World.TryFinalizeTick(tick,out var incomplete),Is.False);
            Assert.That(incomplete.IsValid,Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.IncompleteTick));
            Assert.That(Append(f,tick,.02),Is.True,f.World.LastAttempt.Status.ToString());
            Assert.That(f.World.GetElapsedSeconds(tick),Is.EqualTo(.02));
            Assert.That(f.World.GetWorkingPhysicalMotion(tick,1).DistanceNanometers,Is.EqualTo(N+N*4/100));
            Assert.That(f.World.GetActor(1),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
            Assert.That(Append(f,tick,f.World.GetRemainingSeconds(tick)),Is.True);
            Assert.That(f.World.GetRemainingSeconds(tick),Is.Zero);
            Assert.That(f.World.TryFinalizeTick(tick,out var next),Is.True);
            Assert.That(f.World.GetActor(1),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
            Assert.That(f.World.GetProposedPhysicalMotion(next,0).DistanceNanometers,Is.Zero);
            Assert.That(f.World.GetProposedPhysicalMotion(next,1).DistanceNanometers,Is.EqualTo(N+N/10));
            f.World.Commit(next);Assert.That(f.World.Tick.Value,Is.EqualTo(1));
            Assert.That(f.World.GetActor(1).Pose,Is.Not.EqualTo(before.Pose));
            Assert.Throws<ArgumentException>(()=>f.World.Commit(next));
            Assert.Throws<ArgumentException>(()=>f.World.GetWorkingActor(tick,0));
        }
        [Test] public void OrdinarySeamInsideOneTickRetainsDirectionVelocityAndPaysTheWholeSuffix()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(endMask:2,secondEndOffsetMeters:.04);
            var before=f.World.GetActor(1);var beforeCursor=f.World.GetCursor(1);var tick=f.World.BeginTick();
            Assert.That(Append(f,tick,.02),Is.True,f.World.LastAttempt.Status.ToString());
            var end=f.World.GetWorkingActor(tick,1);var cursor=f.World.GetWorkingCursor(tick,1);
            Assert.That(cursor.ObservedArcNanometers,Is.EqualTo(f.Catalogs[1].Get(cursor.OccurrenceIndex).LengthNanometers));
            Assert.That(f.World.TryHandoffOrdinary(tick,new[]{1}),Is.True);
            Assert.That(f.World.GetWorkingCursor(tick,1).OccurrenceIndex,Is.EqualTo(cursor.OccurrenceIndex+1));
            Assert.That(f.World.GetWorkingActor(tick,1).Pose,Is.EqualTo(end.Pose));
            Assert.That(f.World.GetWorkingActor(tick,1).Velocity,Is.EqualTo(end.Velocity));
            Assert.That(f.World.GetElapsedSeconds(tick),Is.EqualTo(.02));
            Assert.That(f.World.GetCursor(1),Is.EqualTo(beforeCursor));Assert.That(f.World.GetActor(1),Is.EqualTo(before));
            Assert.That(Append(f,tick,f.World.GetRemainingSeconds(tick)),Is.True,f.World.LastAttempt.Status.ToString());
            Assert.That(f.World.TryFinalizeTick(tick,out var proposal),Is.True);
            Assert.That(f.World.GetProposedPhysicalMotion(proposal,1).DistanceNanometers,Is.EqualTo(N+N/10));
            Assert.That(f.World.GetProposedPhysicalMotion(proposal,1).SpeedMetersPerSecond,Is.EqualTo(2));
            Assert.That(f.World.GetProposedActor(proposal,1).Pose.HeadingNanoradians,Is.EqualTo(before.Pose.HeadingNanoradians));
            f.World.Commit(proposal);Assert.That(f.World.Tick.Value,Is.EqualTo(1));
            Assert.That(f.World.GetCursor(1).OccurrenceIndex,Is.EqualTo(beforeCursor.OccurrenceIndex+1));
            Assert.That(f.World.GetCursor(1).ObservedArcNanometers,Is.EqualTo(N*6/100));
        }
        [Test] public void ARejectedLastActorKeepsThePaidScratchPrefixAndAllowsRefinementWithoutPublishingIt()
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var accepted=f.World.GetActor(1);var tick=f.World.BeginTick();
            Assert.That(Append(f,tick,.02),Is.True);var prefix=f.World.GetWorkingActor(tick,1);var motion=f.World.GetWorkingPhysicalMotion(tick,1);
            var candidates=Candidates(f,tick,f.World.GetRemainingSeconds(tick));var windows=Windows(f,tick);var c=f.World.GetWorkingCursor(tick,1);
            windows[1]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,c.CreditedArcNanometers);
            Assert.That(f.World.TryAppendInterval(tick,candidates,windows,NoPairs,0,NoEdges,0,FlatGrades),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PhaseLimit));
            Assert.That(f.World.GetWorkingActor(tick,1),Is.EqualTo(prefix));
            Assert.That(f.World.GetWorkingPhysicalMotion(tick,1),Is.EqualTo(motion));Assert.That(f.World.GetElapsedSeconds(tick),Is.EqualTo(.02));
            Assert.That(f.World.GetActor(1),Is.EqualTo(accepted));Assert.That(f.World.Tick.Value,Is.Zero);
            Assert.That(f.World.TryFinalizeTick(tick,out _),Is.False);
            Assert.That(Append(f,tick,f.World.GetRemainingSeconds(tick)),Is.True);
            Assert.That(f.World.TryFinalizeTick(tick,out var full),Is.True);f.World.Commit(full);
            Assert.That(f.World.GetPhysicalMotion(1).DistanceNanometers,Is.EqualTo(N+N/10));Assert.That(f.World.Tick.Value,Is.EqualTo(1));
        }
        [Test] public void AContactAfterAClearPrefixCannotBecomeAWaitingTickOrPublishPartialActors()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1);var initial=f.World.GetActor(0);var tick=f.World.BeginTick();
            Assert.That(Append(f,tick,.02),Is.True);var prefix=f.World.GetWorkingActor(tick,0);
            Assert.That(Append(f,tick,f.World.GetRemainingSeconds(tick)),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.BodyOrEdge));
            Assert.That(f.World.GetElapsedSeconds(tick),Is.EqualTo(.02));Assert.That(f.World.GetWorkingActor(tick,0),Is.EqualTo(prefix));
            Assert.That(f.World.TryFinalizeTick(tick,out _),Is.False);f.World.AbortTick(tick);
            Assert.That(f.World.GetActor(0),Is.EqualTo(initial));Assert.That(f.World.GetPhysicalMotion(0).DistanceNanometers,Is.Zero);
            Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void StaleOverlongOrIncompleteIntervalSourcesDoNotPayAnyRemainingTime()
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var tick=f.World.BeginTick();var stale=Candidates(f,tick,.03);
            Assert.That(Append(f,tick,.02),Is.True);var prefix=f.World.GetWorkingActor(tick,1);
            Assert.That(f.World.TryAppendInterval(tick,stale,Windows(f,tick),NoPairs,0,NoEdges,0,FlatGrades),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.SourceMismatch));
            Assert.That(Append(f,tick,.031),Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.SourceMismatch));
            Assert.Throws<ArgumentException>(()=>f.World.TryAppendInterval(tick,new[]{stale[0]},Windows(f,tick),NoPairs,0,NoEdges,0,FlatGrades));
            Assert.Throws<InvalidOperationException>(()=>f.Prepare(f.Candidates(),f.Windows(),out _));
            Assert.That(f.World.GetWorkingActor(tick,1),Is.EqualTo(prefix));Assert.That(f.World.GetElapsedSeconds(tick),Is.EqualTo(.02));
            Assert.That(f.World.Tick.Value,Is.Zero);f.World.AbortTick(tick);
        }
        [Test] public void NearlyCompleteTimeCannotRoundUpAnUnpaidSuffix()
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var tick=f.World.BeginTick();double duration=f.World.GetRemainingSeconds(tick);
            Assert.That(Append(f,tick,duration-duration*1e-13),Is.True);
            Assert.That(f.World.GetRemainingSeconds(tick),Is.GreaterThan(0));
            Assert.That(f.World.TryFinalizeTick(tick,out _),Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.IncompleteTick));
            Assert.That(Append(f,tick,f.World.GetRemainingSeconds(tick)),Is.True);
            Assert.That(f.World.TryFinalizeTick(tick,out var full),Is.True);f.World.Commit(full);
            Assert.That(f.World.Tick.Value,Is.EqualTo(1));Assert.That(f.World.GetPhysicalMotion(1).DistanceNanometers,Is.EqualTo(N+N/10));
        }
        [Test] public void ForeignAbortedAndFinalizedTransactionsAreNotReusable()
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var other=new ManualScopedWorldRosterTests.Fixture();var tick=f.World.BeginTick();
            Assert.Throws<ArgumentException>(()=>other.World.GetRemainingSeconds(tick));
            Assert.Throws<InvalidOperationException>(()=>f.World.BeginTick());f.World.AbortTick(tick);
            Assert.Throws<ArgumentException>(()=>f.World.GetWorkingCursor(tick,0));
            var fresh=f.World.BeginTick();Assert.Throws<ArgumentException>(()=>Append(f,tick,.02));
            Assert.That(Append(f,fresh,f.World.GetRemainingSeconds(fresh)),Is.True);
            Assert.That(f.World.TryFinalizeTick(fresh,out var first),Is.True);
            Assert.Throws<ArgumentException>(()=>f.World.TryFinalizeTick(fresh,out _));
            Assert.Throws<ArgumentException>(()=>f.World.AbortTick(fresh));
            var replacement=f.World.BeginTick();Assert.Throws<ArgumentException>(()=>f.World.Commit(first));
            f.World.AbortTick(replacement);Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void WorkingOrdinaryHandoffsValidateAllRowsAndStayProvisional()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(endMask:1);var initial=f.World.GetCursor(0);var tick=f.World.BeginTick();
            Assert.That(f.World.TryHandoffOrdinary(tick,new[]{0,1}),Is.False);
            Assert.That(f.World.GetWorkingCursor(tick,0),Is.EqualTo(initial));
            Assert.That(f.World.TryHandoffOrdinary(tick,new[]{0}),Is.True);
            Assert.That(f.World.GetWorkingCursor(tick,0).OccurrenceIndex,Is.EqualTo(initial.OccurrenceIndex+1));
            Assert.That(f.World.GetElapsedSeconds(tick),Is.Zero);Assert.That(f.World.GetCursor(0),Is.EqualTo(initial));
            Assert.That(f.World.TryHandoffOrdinary(new[]{0}),Is.False);
            f.World.AbortTick(tick);Assert.That(f.World.GetCursor(0),Is.EqualTo(initial));
        }
        [Test] public void PhysicalOdometerRoundsAccumulatedTravelOnceAcrossFourIntervals()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(secondSpeed:4e-8);var tick=f.World.BeginTick();
            for(int i=0;i<4;i++)Assert.That(Append(f,tick,i==3?f.World.GetRemainingSeconds(tick):.0125),Is.True);
            Assert.That(f.World.TryFinalizeTick(tick,out var full),Is.True);
            Assert.That(f.World.GetProposedPhysicalMotion(full,1).DistanceNanometers,Is.EqualTo(N+2));
        }
        [Test] public void OneFrozenSnowIntervalCanPayPrefixAndSuffixWithoutReapplyingItsImpulse()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(secondSideSpeed:.01);var roots=new ManualSkiWorldTrajectory[2];
            var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,20);var candidates=new SkiWorldMotionCandidate[2];
            for(int i=0;i<2;i++)
            {
                var actor=f.World.GetActor(i);var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75);
                roots[i]=new ManualSkiWorldTrajectory(snow.PreviewTrajectory(body,new SkiContactMotion(actor.Velocity,0),4,0,.05));
                candidates[i]=new SkiWorldMotionCandidate(actor.Athlete,roots[i].Slice(0,.02));
            }
            var tick=f.World.BeginTick();Assert.That(f.World.TryAppendInterval(tick,candidates,Windows(f,tick),NoPairs,0,NoEdges,0,FlatGrades),Is.True);
            Assert.That(f.World.GetWorkingActor(tick,1).Velocity,Is.EqualTo(roots[1].VelocityAt(.4)));
            for(int i=0;i<2;i++)candidates[i]=new SkiWorldMotionCandidate(f.World.GetWorkingActor(tick,i).Athlete,roots[i].Slice(.02,.05));
            Assert.That(f.World.TryAppendInterval(tick,candidates,Windows(f,tick),NoPairs,0,NoEdges,0,FlatGrades),Is.True);
            Assert.That(f.World.TryFinalizeTick(tick,out var full),Is.True);
            Assert.That(f.World.GetProposedActor(full,1).Pose,Is.EqualTo(roots[1].Sample(1)));
            Assert.That(f.World.GetProposedActor(full,1).Velocity,Is.EqualTo(roots[1].VelocityAt(1)));
            Assert.That(f.World.GetProposedPhysicalMotion(full,1).DistanceNanometers,Is.EqualTo(N+(long)Math.Round(roots[1].PhysicalTravelDistanceAt(1,0)*N)));
            f.World.Commit(full);Assert.That(f.World.Tick.Value,Is.EqualTo(1));
        }
        [Test] public void WarmSplitTickAndOrdinarySeamStagingAllocateNothing()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(endMask:2,secondEndOffsetMeters:.04);var indexes=new[]{1};
            var setup=f.World.BeginTick();var first=Candidates(f,setup,.02);var firstWindows=Windows(f,setup);
            Assert.That(f.World.TryAppendInterval(setup,first,firstWindows,NoPairs,0,NoEdges,0,FlatGrades),Is.True);
            Assert.That(f.World.TryHandoffOrdinary(setup,indexes),Is.True);
            var second=Candidates(f,setup,f.World.GetRemainingSeconds(setup));var secondWindows=Windows(f,setup);f.World.AbortTick(setup);
            bool Cycle()
            {
                var tick=f.World.BeginTick();bool ok=f.World.TryAppendInterval(tick,first,firstWindows,NoPairs,0,NoEdges,0,FlatGrades);
                ok&=f.World.TryHandoffOrdinary(tick,indexes);
                ok&=f.World.TryAppendInterval(tick,second,secondWindows,NoPairs,0,NoEdges,0,FlatGrades);
                ok&=f.World.TryFinalizeTick(tick,out _);return ok;
            }
            for(int i=0;i<128;i++)Assert.That(Cycle(),Is.True);
            long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=Cycle();
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);
        }
    }
}

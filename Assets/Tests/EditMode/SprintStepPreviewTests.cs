using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class SprintStepPreviewTests
    {
        private static SprintAthleteInput Input(SprintAthleteSession s)
        { var tick=s.Tick.Next(); return new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,s.AthleteId,800),1,1,0),new WeaponAimCommand(tick,s.AthleteId,0,0),true); }
        [Test] public void CompleteStagedRaceDefersEveryAuthoritativeChangeAndMatchesOriginalStep()
        {
            var log=new RaceEventLog(4000); var otherLog=new RaceEventLog(4000);
            var a=SprintFieldTests.Create(log,1).GetAthlete(0); var b=SprintFieldTests.Create(otherLog,1).GetAthlete(0);
            bool mat=false, penalty=false, moving=false;
            while(!a.HasFinished && a.Tick.Value<30000)
            {
                var input=Input(a); var oldTick=a.Tick; var oldState=a.PhysiologyState; var oldMotion=a.Motion;
                var oldRange=a.CurrentRangeMotion; var oldPhase=a.Lifecycle.Phase; var oldShooting=a.ActiveShooting?.Phase;
                int oldShots=a.ActiveShooting?.ShotsFired ?? 0, oldEvents=log.Count, oldSplits=a.Checkpoints.Count;
                var preview=a.PrepareStep(input);
                Assert.That(a.Tick,Is.EqualTo(oldTick)); Assert.That(a.PhysiologyState,Is.EqualTo(oldState)); Assert.That(a.Motion,Is.EqualTo(oldMotion));
                Assert.That(a.CurrentRangeMotion,Is.EqualTo(oldRange)); Assert.That(a.Lifecycle.Phase,Is.EqualTo(oldPhase));
                Assert.That(a.ActiveShooting?.Phase,Is.EqualTo(oldShooting)); Assert.That(a.ActiveShooting?.ShotsFired ?? 0,Is.EqualTo(oldShots));
                Assert.That(log.Count,Is.EqualTo(oldEvents)); Assert.That(a.Checkpoints.Count,Is.EqualTo(oldSplits));
                mat|=preview.ShootingActive; penalty|=oldPhase==CompetitionPhase.Penalty; moving|=preview.Movement.SegmentCount>0;
                for(int i=0;i<preview.Movement.SegmentCount;i++)
                { var segment=a.GetPreparedSegment(preview,i); Assert.That(segment.TravelAfter,Is.GreaterThanOrEqualTo(segment.TravelBefore)); }
                var actual=a.CommitStep(preview); var expected=b.Step(input);
                Assert.That(actual,Is.EqualTo(expected)); Assert.That(actual.Physiology,Is.EqualTo(preview.Physiology));
            }
            Assert.That(a.HasFinished && mat && penalty && moving,Is.True); Assert.That(log.Count,Is.EqualTo(otherLog.Count));
            for(int i=0;i<log.Count;i++) Assert.That(log[i],Is.EqualTo(otherLog[i]));
        }
        [Test] public void HandlesAreOwnedSingleUseAndReplacedEvenAfterFailedPreparation()
        {
            var log=new RaceEventLog(4000); var a=SprintFieldTests.Create(log,1).GetAthlete(0); var b=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0);
            var first=a.PrepareStep(Input(a)); Assert.Catch(()=>b.CommitStep(first)); Assert.Catch(()=>a.CommitStep(default));
            var second=a.PrepareStep(Input(a)); Assert.Catch(()=>a.CommitStep(first)); a.CommitStep(second);
            Assert.Catch(()=>a.CommitStep(second)); Assert.That(a.Tick.Value,Is.EqualTo(1));
            var valid=a.PrepareStep(Input(a)); var copy=a.GetPreparedSegment(valid,0);
            Assert.Catch(()=>a.PrepareStep(default)); Assert.Catch(()=>a.CommitStep(valid));
            Assert.That(a.Tick.Value,Is.EqualTo(1)); var next=a.PrepareStep(Input(a));
            Assert.That(a.GetPreparedSegment(next,0),Is.EqualTo(copy));
            Assert.Catch(()=>a.GetPreparedSegment(next,-1)); Assert.Catch(()=>a.GetPreparedSegment(next,next.Movement.SegmentCount));
            a.CommitStep(next); Assert.Catch(()=>a.GetPreparedSegment(next,0));
        }
        [Test] public void WarmPrepareAndCommitDoNotAllocate()
        {
            var a=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0);
            for(int i=0;i<100;i++) a.Step(Input(a));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<100;i++){var preview=a.PrepareStep(Input(a)); a.CommitStep(preview);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}

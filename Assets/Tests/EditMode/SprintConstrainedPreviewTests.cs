using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class SprintConstrainedPreviewTests
    {
        private static SprintAthleteInput Input(SprintAthleteSession s)
        { var tick=s.Tick.Next(); return new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,s.AthleteId,800),1,1,0),new WeaponAimCommand(tick,s.AthleteId,0,0),true); }
        [Test] public void BlockedMotionRetainsPhysiologyAndDoesNotCreateProgressOrAcceptStaleHandles()
        {
            var a=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0);
            for(int i=0;i<100;i++) a.Step(Input(a));
            var old=a.Motion; long route=a.RouteNanometers; var preview=a.PrepareStep(Input(a));
            Assert.Catch(()=>a.ConstrainPreparedTravel(preview,-1));
            Assert.Catch(()=>a.ConstrainPreparedTravel(preview,preview.Movement.Motion.DistanceNanometers-old.DistanceNanometers+1));
            var blocked=a.ConstrainPreparedTravel(preview,0); Assert.Catch(()=>a.CommitStep(preview));
            var frame=a.CommitStep(blocked);
            Assert.That(frame.Physiology,Is.EqualTo(preview.Physiology)); Assert.That(a.RouteNanometers,Is.EqualTo(route));
            Assert.That(a.Motion.DistanceNanometers,Is.EqualTo(old.DistanceNanometers)); Assert.That(a.Motion.SpeedMetersPerSecond,Is.Zero);
            Assert.That(a.Lifecycle.Phase,Is.EqualTo(CompetitionPhase.Skiing));
        }
        [Test] public void CappedMatAndFinishCannotEmitPrematureObligations()
        {
            var a=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0); bool mat=false,finish=false;
            while(!a.HasFinished && a.Tick.Value<30000)
            {
                var preview=a.PrepareStep(Input(a)); long distance=preview.MovementActive?preview.Movement.Motion.DistanceNanometers-a.Motion.DistanceNanometers:0;
                if(!mat && preview.Movement.AtFiringLine && distance>0)
                {
                    var cap=a.ConstrainPreparedTravel(preview,distance-1); a.CommitStep(cap); mat=true;
                    Assert.That(a.ActiveShooting,Is.Null); Assert.That(a.CurrentRangeMotion?.Phase,Is.Not.EqualTo(RangeTraversalPhase.Shooting));
                }
                else if(!finish && preview.Movement.AtRouteStop && distance>0)
                {
                    var cap=a.ConstrainPreparedTravel(preview,distance-1); a.CommitStep(cap); finish=true;
                    Assert.That(a.HasFinished,Is.False); Assert.That(a.Lifecycle.CompletedLaps,Is.EqualTo(2));
                }
                else a.CommitStep(preview);
            }
            Assert.That(a.HasFinished && mat && finish,Is.True); Assert.That(a.TotalPenaltyLoops,Is.EqualTo(8));
        }
        [Test] public void PartialPenaltyTravelKeepsMainRouteFixedAndDoesNotCreditFullLoop()
        {
            var a=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0);
            while(a.CurrentRangeMotion?.Phase!=RangeTraversalPhase.PenaltyLoop && a.Tick.Value<10000) a.Step(Input(a));
            Assert.That(a.CurrentRangeMotion?.Phase,Is.EqualTo(RangeTraversalPhase.PenaltyLoop));
            var oldRange=a.CurrentRangeMotion.Value; long oldRoute=a.RouteNanometers, oldTravel=a.Motion.DistanceNanometers;
            int obligations=a.Lifecycle.PendingPenaltyLoops; var preview=a.PrepareStep(Input(a));
            long available=preview.Movement.Motion.DistanceNanometers-oldTravel; long cap=Math.Min(available/2,oldRange.DistanceToBoundaryNanometers/2);
            Assert.That(cap,Is.GreaterThan(0)); var changed=a.ConstrainPreparedTravel(preview,cap); var frame=a.CommitStep(changed);
            Assert.That(a.RouteNanometers,Is.EqualTo(oldRoute)); Assert.That(a.Motion.DistanceNanometers-oldTravel,Is.EqualTo(cap));
            Assert.That(a.CurrentRangeMotion.Value.PenaltyNanometers-oldRange.PenaltyNanometers,Is.EqualTo(cap));
            Assert.That(a.Lifecycle.PendingPenaltyLoops,Is.EqualTo(obligations)); Assert.That(frame.Physiology,Is.EqualTo(preview.Physiology));
        }
    }
}

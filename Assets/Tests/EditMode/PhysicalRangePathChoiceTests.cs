using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathChoiceTests
    {
        private const long M=SkiState.NanometersPerMeter;
        private static int Assignments(RaceEventLog log)
        {int n=0;for(int i=0;i<log.Count;i++)if(log[i].EventType==RaceEventType.PhysicalRangePathAssigned)n++;return n;}
        private static SprintStepPreview Crossing(SprintAthleteSession s)
        {
            while(s.Tick.Value<3000)
            {
                var preview=s.PrepareStep(SprintAthleteSessionTests.Script(s));
                if(preview.MovementActive&&preview.Movement.RouteNanometers>=5*M)return preview;
                s.CommitStep(preview);
            }
            throw new Exception("Entry not reached.");
        }
        [Test] public void ReplacedAndCancelledChoiceDoesNotPublishAuthorityOrConsumeAnAssignment()
        {
            var log=new RaceEventLog(4000);var s=PhysicalRangePathAthleteTests.Create(log,out var original,preparedChoices:true);
            var replaced=Crossing(s);var oldState=s.PhysiologyState;var oldTick=s.Tick;var oldRange=s.CurrentRangeMotion;var oldLane=s.AssignedShootingLane;int count=log.Count;
            var choice=s.CreateRangePathChoice(2);var staged=s.PrepareRangePathStep(SprintAthleteSessionTests.Script(s),choice);
            Assert.That(s.GetPreparedRangePath(staged),Is.SameAs(s.GetRangePathChoice(1)));Assert.That(s.RangePath,Is.SameAs(original));
            Assert.That(s.AssignedShootingLane,Is.EqualTo(oldLane));Assert.That(s.Tick,Is.EqualTo(oldTick));Assert.That(s.PhysiologyState,Is.EqualTo(oldState));
            Assert.That(s.CurrentRangeMotion,Is.EqualTo(oldRange));Assert.That(log.Count,Is.EqualTo(count));
            Assert.Throws<ArgumentException>(()=>s.CommitStep(replaced));
            s.CommitStep(s.ConstrainPreparedTravel(staged,0));
            Assert.That(s.RangePath,Is.SameAs(original));Assert.That(s.AssignedShootingLane,Is.EqualTo(oldLane));Assert.That(Assignments(log),Is.Zero);
            Assert.That(s.ActiveShooting,Is.Null);Assert.Throws<ArgumentException>(()=>s.CommitStep(staged));
            while(Assignments(log)==0&&s.Tick.Value<3000)
                s.CommitStep(s.PrepareRangePathStep(SprintAthleteSessionTests.Script(s),s.CreateRangePathChoice(2)));
            Assert.That(Assignments(log),Is.EqualTo(1));Assert.That(s.RangePath.LaneOrdinal,Is.EqualTo(2));
            Assert.That(s.AssignedShootingLane.LaneId,Is.EqualTo(s.RangePath.LaneId));
            Assert.Throws<InvalidOperationException>(()=>s.CreateRangePathChoice(1));
        }
        [Test] public void PreEntryChoiceRemainsTentativeUntilAcceptedArrival()
        {
            var log=new RaceEventLog(4000);var s=PhysicalRangePathAthleteTests.Create(log,out var original,preparedChoices:true);
            for(int i=0;i<5;i++)
            {
                var preview=s.PrepareRangePathStep(SprintAthleteSessionTests.Script(s),s.CreateRangePathChoice(2));
                Assert.That(preview.Movement.RouteNanometers,Is.LessThan(5*M));s.CommitStep(preview);
                Assert.That(s.RangePath,Is.SameAs(original));Assert.That(Assignments(log),Is.Zero);
            }
            Assert.That(s.Tick.Value,Is.EqualTo(5));
        }
        [Test] public void ChoicesAreOwnedRevisionBoundAndInvalidPreparationReplacesThePreview()
        {
            var s=PhysicalRangePathAthleteTests.Create(new RaceEventLog(4000),out _,preparedChoices:true);
            var other=PhysicalRangePathAthleteTests.Create(new RaceEventLog(4000),out _,preparedChoices:true);
            var old=s.CreateRangePathChoice(2);var prepared=s.PrepareStep(SprintAthleteSessionTests.Script(s));
            Assert.Throws<ArgumentException>(()=>s.PrepareRangePathStep(SprintAthleteSessionTests.Script(s),old));
            Assert.Throws<ArgumentException>(()=>s.CommitStep(prepared));
            Assert.Throws<ArgumentException>(()=>s.PrepareRangePathStep(SprintAthleteSessionTests.Script(s),other.CreateRangePathChoice(2)));
            var valid=s.PrepareStep(SprintAthleteSessionTests.Script(s));
            Assert.Throws<ArgumentException>(()=>s.PrepareRangePathStep(SprintAthleteSessionTests.Script(s),default));
            Assert.Throws<ArgumentException>(()=>s.CommitStep(valid));Assert.That(s.Tick.Value,Is.Zero);
        }
        [Test] public void WarmStagedChoicesAreAllocationFreeAndCannotChangeTheAcceptedTraversal()
        {
            var log=new RaceEventLog(4000);var s=PhysicalRangePathAthleteTests.Create(log,out _,preparedChoices:true);
            while(s.RouteNanometers<7*M&&s.Tick.Value<3000)s.Step(SprintAthleteSessionTests.Script(s));
            Assert.That(s.RouteNanometers,Is.LessThan(10*M));var oldRange=s.CurrentRangeMotion;var oldTick=s.Tick;int oldEvents=log.Count;
            var input=SprintAthleteSessionTests.Script(s);SprintStepPreview preview=default;
            for(int i=0;i<1000;i++)preview=s.PrepareRangePathStep(input,s.CreateRangePathChoice(2));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)preview=s.PrepareRangePathStep(input,s.CreateRangePathChoice(2));
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(s.Tick,Is.EqualTo(oldTick));
            Assert.That(s.CurrentRangeMotion,Is.EqualTo(oldRange));Assert.That(log.Count,Is.EqualTo(oldEvents));
            s.CommitStep(preview);Assert.That(s.RangePath.LaneOrdinal,Is.EqualTo(2));Assert.That(Assignments(log),Is.EqualTo(1));
        }
        private static SprintAthleteFrame Advance(SprintAthleteSession s,RaceEventLog log)
        {
            int bout=s.ActiveBout;int choices=Assignments(log);
            if(bout>=0&&choices<=bout&&s.CurrentRangeMotion?.Phase==RangeTraversalPhase.Approach)
                return s.CommitStep(s.PrepareRangePathStep(SprintAthleteSessionTests.Script(s),s.CreateRangePathChoice(bout==0?2:1)));
            return s.Step(SprintAthleteSessionTests.Script(s));
        }
        [Test] public void CompleteRepeatedRaceChangesNextBoutOnlyAfterThePreviousExitAndPreservesDistance()
        {
            var la=new RaceEventLog(4000);var lb=new RaceEventLog(4000);var a=PhysicalRangePathAthleteTests.Create(la,out _,preparedChoices:true);
            var b=PhysicalRangePathAthleteTests.Create(lb,out _,preparedChoices:true);bool oldExit=false;int stops=0;
            while(!a.HasFinished&&a.Tick.Value<30000)
            {
                if(a.ActiveBout==1&&Assignments(la)==1&&a.CurrentRangeMotion?.Phase!=RangeTraversalPhase.Approach)
                {
                    Assert.Throws<InvalidOperationException>(()=>a.CreateRangePathChoice(1));Assert.That(a.RangePath.LaneOrdinal,Is.EqualTo(2));oldExit=true;
                }
                bool wasMat=a.CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
                Assert.That(Advance(a,la),Is.EqualTo(Advance(b,lb)));
                if(!wasMat&&a.CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting)
                {
                    int expected=stops==0?2:1;Assert.That(a.RangePath.LaneOrdinal,Is.EqualTo(expected));
                    Assert.That(a.RouteNanometers,Is.EqualTo(a.RangePath.FiringRouteNanometers(stops)));stops++;
                }
            }
            Assert.That(a.HasFinished&&b.HasFinished,Is.True);Assert.That(oldExit,Is.True);Assert.That(stops,Is.EqualTo(2));
            Assert.That(Assignments(la),Is.EqualTo(2));Assert.That(a.Motion.DistanceNanometers,Is.EqualTo((900+150*a.TotalPenaltyLoops)*M));
            Assert.That(la.Count,Is.EqualTo(lb.Count));for(int i=0;i<la.Count;i++)Assert.That(la[i],Is.EqualTo(lb[i]));
        }
    }
}

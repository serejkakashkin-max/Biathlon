using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingStrategyTests
    {
        public static ShootingStrategyDefinition Definition()=>ShootingStrategyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_strategy_provisional_v1.json"));
        [Test] public void UrgencyTradesStabilityForTimeAndInvalidDataIsRejected()
        {
            var d=Definition(); var p=new ShootingStrategyPlanner(d); var careful=p.Evaluate(0,0); var fast=p.Evaluate(1,0);
            Assert.That(fast.FireTolerance,Is.GreaterThan(careful.FireTolerance));
            Assert.That(fast.StableSeconds,Is.LessThan(careful.StableSeconds));
            Assert.That(fast.MaximumAimSeconds,Is.LessThan(careful.MaximumAimSeconds));
            Assert.That(p.Evaluate(1,1).Risk,Is.LessThan(fast.Risk));
            Assert.That(ShootingStrategyDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
            Assert.Catch(()=>p.Evaluate(double.NaN,0)); Assert.Catch(()=>p.Evaluate(0,2));
            Assert.Catch(()=>ShootingStrategyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),"fast_tolerance_rad","0.00001")));
            Assert.Catch(()=>ShootingStrategyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),"fast_maximum_aim_seconds","0.01")));
            long before=GC.GetAllocatedBytesForCurrentThread(); for(int i=0;i<1000;i++) p.Evaluate(0.5,0.5);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void FastPolicyTriggersEarlierButDoesNotImproveTrackingAndInvalidPlanIsAtomic()
        {
            var model=ShootingSessionTests.Model(); var id=new AthleteId("shooter"); var p=new ShootingStrategyPlanner(Definition());
            var perfect=ShootingAiDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(ShootingAiTests.Definition().ToJson(),"tracking_error_rad","0"));
            ShootingAiController Create()=>new ShootingAiController(perfect,model.Targets,model.Aim,model.Stances.Standing,id,20,new SeededRandomSource(42));
            var a=Create(); var b=Create(); var c=Create(); long fast=0,slow=0;
            double yaw=Math.Atan(model.Targets.CenterX(0)/model.Targets.DistanceMeters);
            var observation=new ShootingAiObservation(ShootingPhase.Aiming,0,0,yaw,0);
            Assert.Catch(()=>a.Step(new SimulationTick(1),observation,default(ShootingStrategyPlan)));
            for(int i=1;i<=300 && (fast==0 || slow==0);i++)
            {
                var tick=new SimulationTick(i); var fa=a.Step(tick,observation,p.Evaluate(1,0)); var fc=c.Step(tick,observation,p.Evaluate(1,0));
                var fb=b.Step(tick,observation,p.Evaluate(0,0)); Assert.That(fa,Is.EqualTo(fc));
                if(fa.Trigger && fast==0) fast=i; if(fb.Trigger && slow==0) slow=i;
            }
            Assert.That(fast,Is.GreaterThan(0)); Assert.That(slow,Is.GreaterThan(fast));
        }
        [TestCase(0.0)] [TestCase(1.0)]
        public void CompletePhysicalBoutAndRecordedCommandsReplayExactly(double urgency)
        {
            var log=new RaceEventLog(3000); var replayLog=new RaceEventLog(3000);
            var session=ShootingSessionTests.Create(log); var replay=ShootingSessionTests.Create(replayLog);
            var model=ShootingSessionTests.Model();
            var ai=new ShootingAiController(ShootingAiTests.Definition(),model.Targets,model.Aim,model.Stances.Prone,session.AthleteId,20,new SeededRandomSource(23));
            var planner=new ShootingStrategyPlanner(Definition()); var clock=new TacticalDecisionClock(TacticalCadenceTests.Definition(),20);
            var plan=planner.Evaluate(urgency,0.5); var tape=new ShootingCommandTape("strategy_bout",session.ContextJson,session.AthleteId,10000);
            while(session.Phase!=ShootingPhase.Complete && session.Tick.Value<5000)
            {
                var tick=session.Tick.Next(); if(clock.Step(tick).DecisionDue) plan=planner.Evaluate(urgency,0.5);
                var sight=session.Sight; var output=ai.Step(tick,new ShootingAiObservation(session.Phase,session.ClosedMask,session.ShotsFired,sight.YawRadians,sight.PitchRadians),plan);
                var action=tick.Value==1?ShootingAction.EnterRange:tick.Value==2?ShootingAction.AssignLane:output.Trigger?ShootingAction.Trigger:ShootingAction.None;
                var input=new ShootingSessionInput(output.Aim,action,tick.Value==2?new ShootingLaneAssignment("training_range","lane",1):default,output.HoldBreath,170,0.2,0.1,new WindVector(1,0));
                var frame=tape.StepAndRecord(session,input); Assert.That(replay.Step(input),Is.EqualTo(frame));
            }
            Assert.That(session.Phase,Is.EqualTo(ShootingPhase.Complete)); Assert.That(session.ShotsFired,Is.EqualTo(5));
            var restored=ShootingCommandTape.FromJson(tape.ToJson()); var persistedReplay=ShootingSessionTests.Create(new RaceEventLog(3000));
            for(int i=0;i<restored.Count;i++) restored.ReplayNext(persistedReplay);
            Assert.That(persistedReplay.ClosedMask,Is.EqualTo(session.ClosedMask)); Assert.That(persistedReplay.Tick,Is.EqualTo(session.Tick));
            Assert.That(log.Count,Is.EqualTo(replayLog.Count)); for(int i=0;i<log.Count;i++) Assert.That(log[i],Is.EqualTo(replayLog[i]));
            TestContext.Out.WriteLine("Strategy urgency="+urgency+", ticks="+session.Tick.Value+", closed_mask="+session.ClosedMask);
        }
    }
}

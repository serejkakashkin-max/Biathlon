using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class TerrainPaceIntegrationTests
    {
        [Test] public void CompleteHillySprintUsesPlannedEffortThroughSharedPhysicsAndReplays()
        {
            var log=new RaceEventLog(4000); var replayLog=new RaceEventLog(4000);
            var field=SprintFieldTests.Create(log,1,trackId:"synthetic_climb_v1"); var replay=SprintFieldTests.Create(replayLog,1,trackId:"synthetic_climb_v1");
            var self=field.GetAthlete(0).AthleteId; var source=new SprintPerceptionSource(field);
            var clock=new TacticalDecisionClock(TacticalCadenceTests.Definition(),20);
            var planner=new TerrainPacePlanner(TerrainPlanningTests.Definition()); var control=new PaceController(PaceControllerTests.Definition(),self,20,new SeededRandomSource(42));
            var offsets=new[]{default(TrackDistance),TrackDistance.FromMeters(10),TrackDistance.FromMeters(20),TrackDistance.FromMeters(40)};
            var ahead=new AiRouteLookahead[4]; var neighbors=Array.Empty<AiRouteNeighbor>(); var inputs=new SprintAthleteInput[1]; int boosted=0;
            while(!field.IsComplete && field.Tick.Value<40000)
            {
                var observed=source.Capture(0,default,offsets,ahead,neighbors); var tick=field.Tick.Next(); var window=clock.Step(tick);
                TacticalIntentDecision? decision=null; int adjustment=0;
                if(window.DecisionDue)
                {
                    decision=new TacticalIntentDecision("terrain_pace",self,TacticalIntentKind.Hold,tick,window.NextDecisionTick);
                    // Main-route lookahead is useful while skiing; penalty/range control uses its own branch.
                    if(observed.Phase==CompetitionPhase.Skiing)
                        adjustment=planner.Evaluate(observed.SpeedMetersPerSecond,clock.PlanningHorizonTicks/20.0,observed.Physiology,ahead).EffortAdjustment;
                    if(adjustment>0) boosted++;
                }
                var frame=control.Step(tick,decision,adjustment);
                Assert.That(frame.TargetEffort,Is.EqualTo(Math.Max(0,Math.Min(1000,frame.NominalEffort+frame.ControlError+frame.PlannedEffortAdjustment))));
                inputs[0]=new SprintAthleteInput(new TimeTrialInput(frame.Input,1,1,0),new WeaponAimCommand(tick,self,0,0),true);
                field.Step(inputs); replay.Step(inputs);
                Assert.That(field.GetAthlete(0).LastFrame,Is.EqualTo(replay.GetAthlete(0).LastFrame));
            }
            Assert.That(field.IsComplete,Is.True); Assert.That(boosted,Is.GreaterThan(0));
            Assert.That(field.Result.ToJson(),Is.EqualTo(replay.Result.ToJson())); Assert.That(log.Count,Is.EqualTo(replayLog.Count));
            for(int i=0;i<log.Count;i++) Assert.That(log[i],Is.EqualTo(replayLog[i]));
        }
        [Test] public void AdjustmentIsHeldAndInvalidUpdateCannotConsumeRandom()
        {
            var random=new SeededRandomSource(1); var self=new AthleteId("self"); var control=new PaceController(PaceControllerTests.Definition(),self,20,random);
            var decision=new TacticalIntentDecision("terrain",self,TacticalIntentKind.Hold,new SimulationTick(1),new SimulationTick(5));
            ulong initial=random.State; Assert.Catch(()=>control.Step(new SimulationTick(1),decision,1001)); Assert.That(random.State,Is.EqualTo(initial));
            var first=control.Step(new SimulationTick(1),decision,-100); initial=random.State;
            Assert.Catch(()=>control.Step(new SimulationTick(2),plannedEffortAdjustment:100)); Assert.That(random.State,Is.EqualTo(initial));
            var held=control.Step(new SimulationTick(2)); Assert.That(held.PlannedEffortAdjustment,Is.EqualTo(-100)); Assert.That(held.TargetEffort,Is.EqualTo(first.TargetEffort));
        }
    }
}

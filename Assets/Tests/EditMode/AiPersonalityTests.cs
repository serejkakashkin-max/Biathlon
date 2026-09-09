using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiPersonalityTests
    {
        public static AiPersonalityDefinition Definition(string name)=>AiPersonalityDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ai_personality_"+name+"_v1.json"));
        [Test] public void TraitsChangePreferencesAndShootingRiskButCannotGrantIneligibleActions()
        {
            var id=new AthleteId("self"); var a=new AiPersonalityPolicy(id,Definition("attacker")); var b=new AiPersonalityPolicy(id,Definition("patient"));
            var preferences=new double[8]; var features=new double[8]; features[1]=1; features[6]=0.1;
            var candidates=TacticalUtilityTests.Candidates(); var scores=new TacticalUtilityScore[8]; var evaluator=new TacticalUtilityEvaluator(TacticalUtilityTests.Definition());
            TacticalIntentDecision Select(AiPersonalityPolicy p) { p.CopyPreferences(false,preferences); return evaluator.Evaluate(id,default,new SimulationTick(5),features,preferences,candidates,scores); }
            Assert.That(Select(a).Kind,Is.EqualTo(TacticalIntentKind.Attack)); Assert.That(Select(b).Kind,Is.Not.EqualTo(TacticalIntentKind.Attack));
            candidates[5]=new TacticalCandidate(TacticalIntentKind.Attack,false); Assert.That(Select(a).Kind,Is.Not.EqualTo(TacticalIntentKind.Attack));
            var neutral=new AiPersonalityPolicy(id,Definition("neutral")); neutral.CopyPreferences(false,preferences);
            for(int i=0;i<8;i++) Assert.That(preferences[i],Is.EqualTo(0.5));
            a.CopyPreferences(true,preferences); Assert.That(preferences[5],Is.EqualTo(a.Definition.FinalLapAggression));
            var shooting=new ShootingStrategyPlanner(ShootingStrategyTests.Definition());
            Assert.That(a.PlanShooting(shooting,0.5,0.2,0.8).Risk,Is.GreaterThan(b.PlanShooting(shooting,0.5,0.2,0.8).Risk));
            var resistant=AiPersonalityDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(b.Definition.ToJson(),"pressure_resistance","1"));
            Assert.That(new AiPersonalityPolicy(id,resistant).PlanShooting(shooting,0.5,0,1).Risk,Is.GreaterThan(b.PlanShooting(shooting,0.5,0,1).Risk));
        }
        [Test] public void ProfilesRoundTripValidateAndWarmProjectionAllocatesNothing()
        {
            foreach(string name in new[]{"neutral","patient","attacker"})
            {
                var d=Definition(name); Assert.That(AiPersonalityDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
                foreach(string trait in new[]{"aggression","patience","draft_preference","shooting_risk","shooting_tempo","final_lap_aggression","pressure_resistance","overtake_confidence"})
                    Assert.Catch(()=>AiPersonalityDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),trait,"1.01")));
            }
            var p=new AiPersonalityPolicy(new AthleteId("self"),Definition("neutral")); var preferences=new double[8];
            var planner=new ShootingStrategyPlanner(ShootingStrategyTests.Definition());
            Assert.Catch(()=>p.CopyPreferences(false,new double[7])); Assert.Catch(()=>p.PlanShooting(planner,0,0,double.NaN));
            Assert.That(new AiPersonalityPolicy(p.AthleteId,AiPersonalityDefinition.FromJson(p.Definition.ToJson())).ContextJson,Is.EqualTo(p.ContextJson));
            Assert.That(new AiPersonalityPolicy(new AthleteId("other"),p.Definition).ContextJson,Is.Not.EqualTo(p.ContextJson));
            p.CopyPreferences(false,preferences); p.PlanShooting(planner,0.5,0.5,0.5);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++) { p.CopyPreferences(i%2==0,preferences); p.PlanShooting(planner,0.5,0.5,0.5); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void DistinctPersistentPoliciesDriveCompleteRacesThroughIdenticalPhysicsAndReplay()
        {
            var attackCounts=new int[2];
            for(int run=0;run<2;run++)
            {
                var log=new RaceEventLog(15000); var replayLog=new RaceEventLog(15000);
                var field=PackSprintFieldTests.Create(log,1); var replay=PackSprintFieldTests.Create(replayLog,1);
                Assert.That(field.ContextJson,Is.EqualTo(replay.ContextJson));
                var policy=new AiPersonalityPolicy(field.GetAthlete(0).AthleteId,Definition(run==0?"attacker":"patient"));
                var clock=new TacticalDecisionClock(TacticalCadenceTests.Definition(),20);
                var pace=new PaceController(PaceControllerTests.Definition(),policy.AthleteId,20,new SeededRandomSource(42));
                var utility=new TacticalUtilityEvaluator(TacticalUtilityTests.Definition());
                var preferences=new double[8]; var features=new double[8]; var candidates=new TacticalCandidate[8]; var scores=new TacticalUtilityScore[8];
                var commands=new SprintAthleteInput[1]; var lines=new double[1];
                while(!field.IsComplete && field.Tick.Value<50000)
                {
                    var tick=field.Tick.Next(); var window=clock.Step(tick); var athlete=field.GetAthlete(0); TacticalIntentDecision? decision=null;
                    if(window.DecisionDue)
                    {
                        bool final=athlete.Lifecycle.CompletedLaps==2;
                        policy.CopyPreferences(final,preferences);
                        features[1]=athlete.PhysiologyState.AnaerobicReserve; features[2]=athlete.PhysiologyState.MuscularFatigue;
                        features[6]=0.25; features[7]=final?1:0;
                        for(int i=0;i<8;i++) candidates[i]=new TacticalCandidate((TacticalIntentKind)i,i==0 || i==1 || i==5 || (i==6 && final));
                        decision=utility.Evaluate(policy.AthleteId,tick,window.NextDecisionTick,features,preferences,candidates,scores);
                        if(decision.Value.Kind==TacticalIntentKind.Attack) attackCounts[run]++;
                    }
                    var effort=pace.Step(tick,decision);
                    commands[0]=new SprintAthleteInput(new TimeTrialInput(effort.Input,1,1,0),new WeaponAimCommand(tick,policy.AthleteId,0,0),true);
                    field.Step(commands,lines); replay.Step(commands,lines);
                    Assert.That(field.GetAthlete(0).LastFrame,Is.EqualTo(replay.GetAthlete(0).LastFrame));
                }
                Assert.That(field.IsComplete,Is.True); Assert.That(field.Result.ToJson(),Is.EqualTo(replay.Result.ToJson()));
                Assert.That(log.Count,Is.EqualTo(replayLog.Count)); for(int i=0;i<log.Count;i++) Assert.That(log[i],Is.EqualTo(replayLog[i]));
                TestContext.Out.WriteLine(policy.Definition.Id+": ticks="+field.Tick.Value+", attack_decisions="+attackCounts[run]);
            }
            Assert.That(attackCounts[0],Is.GreaterThan(attackCounts[1]));
        }
    }
}

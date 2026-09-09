using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiDifficultyTests
    {
        public static AiDifficultyDefinition Definition(string level)=>AiDifficultyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ai_difficulty_"+level+"_v1.json"));
        [Test] public void DifficultyChangesOnlyDecisionAndControlFields()
        {
            var pace=PaceControllerTests.Definition(); var aim=ShootingAiTests.Definition();
            foreach(string level in new[]{"developing","expert"})
            {
                var d=Definition(level); var p=d.Apply(pace); var a=d.Apply(aim);
                for(int i=0;i<8;i++) Assert.That(p.Effort((TacticalIntentKind)i),Is.EqualTo(pace.Effort((TacticalIntentKind)i)));
                Assert.That(p.ResponsePerSecond,Is.EqualTo(pace.ResponsePerSecond));
                Assert.That(a.AimGain,Is.EqualTo(aim.AimGain)); Assert.That(a.TrackingFrequency,Is.EqualTo(aim.TrackingFrequency));
                Assert.That(a.FireTolerance,Is.EqualTo(aim.FireTolerance)); Assert.That(a.StableSeconds,Is.EqualTo(aim.StableSeconds));
                Assert.That(a.MaximumAimSeconds,Is.EqualTo(aim.MaximumAimSeconds));
                Assert.That(AiDifficultyDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
                Assert.That(PaceControllerDefinition.FromJson(p.ToJson()).ToJson(),Is.EqualTo(p.ToJson()));
                Assert.That(ShootingAiDefinition.FromJson(a.ToJson()).ToJson(),Is.EqualTo(a.ToJson()));
            }
        }
        [Test] public void DecisionCadenceAndReactionDifferencesAreRealAndRepeatable()
        {
            var easy=Definition("developing"); var hard=Definition("expert"); var model=ShootingSessionTests.Model(); var id=new AthleteId("self");
            var ec=new TacticalDecisionClock(easy.Cadence(),20); var hc=new TacticalDecisionClock(hard.Cadence(),20);
            ShootingAiController Create(AiDifficultyDefinition d)=>new ShootingAiController(d.Apply(ShootingAiTests.Definition()),model.Targets,model.Aim,model.Stances.Standing,id,20,new SeededRandomSource(1));
            var ea=Create(easy); var ha=Create(hard); var replay=Create(hard); int ed=0,hd=0,er=0,hr=0;
            for(int i=1;i<=200;i++)
            {
                var tick=new SimulationTick(i); if(ec.Step(tick).DecisionDue) ed++; if(hc.Step(tick).DecisionDue) hd++;
                var observation=new ShootingAiObservation(ShootingPhase.Aiming,0,0,0,0);
                if(ea.Step(tick,observation).DecisionMade) er++; var frame=ha.Step(tick,observation); if(frame.DecisionMade) hr++;
                Assert.That(frame,Is.EqualTo(replay.Step(tick,observation)));
            }
            Assert.That(hd,Is.GreaterThan(ed)); Assert.That(hr,Is.GreaterThan(er)); Assert.That(hc.PlanningHorizonTicks,Is.GreaterThan(ec.PlanningHorizonTicks));
        }
        [Test] public void InvalidDifficultyCannotCreateUnboundedControllers()
        {
            var d=Definition("expert");
            foreach(var pair in new[]{new[]{"decision_hz","6"},new[]{"planning_horizon_seconds","0"},new[]{"reaction_seconds","0"},new[]{"tracking_error_rad","0.1"},new[]{"pace_control_error","1001"}})
                Assert.Catch(()=>AiDifficultyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),pair[0],pair[1])));
            Assert.Catch(()=>d.Apply((PaceControllerDefinition)null)); Assert.Catch(()=>d.Apply((ShootingAiDefinition)null));
        }
    }
}

using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class OwnerFollowupTests
    {
        [TestCase("mass_start")][TestCase("sprint")]
        public void ShortenedPlayableFormatsInitializeAndAdvanceTheWholeRoster(string format)
        {
            var f=ManualProductionFieldTests.Initial(format,30);
            for(int i=0;i<400;i++)f.runner.Step();
            Assert.That(f.runner.Field.Tick.Value,Is.EqualTo(400));
            Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
        }
        [Test] public void MovementRewritePreservesTheCompleteShootingCommand()
        {
            var id=new AthleteId("owner");var tick=new SimulationTick(1);
            var original=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,650),1,1,0),
                new WeaponAimCommand(tick,id,2,3),true,true,cycleBolt:true,manualBoltCycle:true,rangeTransit:true);
            var rewritten=original.WithSki(new TimeTrialInput(new SimulationInput(tick,id,0),1,1,0));
            Assert.That(rewritten.ManualBoltCycle,Is.True);Assert.That(rewritten.CycleBolt,Is.True);
            Assert.That(rewritten.RangeTransit,Is.True);
            Assert.That(rewritten.Trigger,Is.True);Assert.That(rewritten.HoldBreath,Is.True);Assert.That(rewritten.Aim,Is.EqualTo(original.Aim));
        }
        [Test] public void ManualHandlingMovesOnReloadAndDoesNotKickOnDischarge()
        {
            var s=new ShootingSession(new SimulationConfig(1,"manual_handling",20,123),ShootingSessionTests.Model(),
                ShootingSessionTests.Athlete(),ShootingStance.Prone,"range",new RaceEventLog(1000),manualBoltCycle:true);
            ShootingSessionFrame Step(ShootingAction action= ShootingAction.None)=>s.Step(new ShootingSessionInput(
                new WeaponAimCommand(s.Tick.Next(),s.AthleteId,0,0),action,
                action==ShootingAction.AssignLane?new ShootingLaneAssignment("range","lane",1):default,false,130,0,0));
            Step(ShootingAction.EnterRange);Step(ShootingAction.AssignLane);while(s.Phase==ShootingPhase.Setup)Step();
            var loaded=Step(ShootingAction.CycleBolt);Assert.That(loaded.Handling.VerticalRadians,Is.GreaterThan(0));
            while(s.Phase==ShootingPhase.BoltCycle)Step();
            double before=s.LastFrame.Handling.VerticalRadians;
            var shot=Step(ShootingAction.Trigger);Assert.That(shot.Shot.Lifecycle.ShotAccepted,Is.True);
            Assert.That(shot.Handling.VerticalRadians,Is.LessThan(before));Assert.That(s.NeedsBoltCycle,Is.True);
            for(int i=0;i<100;i++)Assert.That(Step(ShootingAction.Trigger).Shot.Lifecycle.ShotAccepted,Is.False);
        }
        [Test] public void ProductionAttackNeedsSustainedRecoveryAndTrainedStatsStillMatter()
        {
            var d=PhysiologyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("physiology_gameplay_v2.json"));
            var a=new AnaerobicSystem(d,20);double reserve=1;
            for(int t=0;t<600;t++)reserve=a.Step(reserve,1.3,0).Value;
            double spent=reserve;for(int t=0;t<600;t++)reserve=a.Step(reserve,.2,.02).Value;
            TestContext.WriteLine($"30s attack reserve={spent:F4}; 30s tucked recovery reserve={reserve:F4}");
            Assert.That(spent,Is.LessThan(.55));Assert.That(reserve,Is.LessThan(.75));
            var trained=PhysiologyStatTests.Curves().Apply(d,new AthletePhysiologyStats(80,50,80,80));
            Assert.That(new AnaerobicSystem(trained,20).Step(.5,.2,0).Value,Is.GreaterThan(a.Step(.5,.2,0).Value));
            Assert.That(trained.FatigueGainPerSecond,Is.LessThan(d.FatigueGainPerSecond));
        }
        [Test] public void CalmProneSwayRequiresControlAndBreathingReducesIt()
        {
            double Sample(double hr,bool hold)
            {
                var model=new ShootingModelDefinitions(ProductionGameplayDefinitions.Bind(PhysiologyContractTests.DefinitionJson));
                var s=new ShootingSession(new SimulationConfig(1,"sway",20,123),model,ShootingSessionTests.Athlete(),
                    ShootingStance.Prone,"range",new RaceEventLog(10000),manualBoltCycle:true);
                double sum=0;int count=0;
                for(int t=1;t<=160;t++)
                {
                    var f=s.Step(new ShootingSessionInput(new WeaponAimCommand(new SimulationTick(t),s.AthleteId,0,0),
                        t==1?ShootingAction.EnterRange:t==2?ShootingAction.AssignLane:ShootingAction.None,
                        t==2?new ShootingLaneAssignment("range","lane",1):default,hold&&t>60,hr,0,0));
                    if(t>60&&t<=120){sum+=f.Sway.HorizontalRadians*f.Sway.HorizontalRadians+f.Sway.VerticalRadians*f.Sway.VerticalRadians;count++;}
                }
                return Math.Sqrt(sum/count);
            }
            double calm=Sample(95,false),held=Sample(95,true),high=Sample(180,false);
            TestContext.WriteLine($"RMS radians calm={calm:R} held={held:R} highHR={high:R}");
            Assert.That(calm,Is.GreaterThan(.00045));Assert.That(held,Is.LessThan(calm*.7));Assert.That(high,Is.GreaterThan(calm));
        }
        [Test] public void EveryAssistedBoutKeepsManualBoltPolicyIncludingThird()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",1);var field=f.runner.Field;int reloads=0;bool third=false;
            for(int t=0;t<25000&&!third;t++)
            {
                var actor=field.GetAthlete(0);var s=actor.ActiveShooting;var tick=field.Tick.Next();
                bool cycle=s?.NeedsBoltCycle==true&&(actor.ActiveBout!=2||reloads==0);
                if(actor.ActiveBout==2&&cycle)reloads++;
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,650),1,1,0),
                    new WeaponAimCommand(tick,actor.AthleteId,1000,1000),trigger:true,cycleBolt:cycle,manualBoltCycle:true);
                f.runner.StepWithManualPlayer(0,input,0,true);
#if !UNITY_EDITOR
                if(t%1000==0){var body=f.stepper.World.GetActor(0);var cursor=f.stepper.World.GetCursor(0);var row=f.stepper.World.GetCatalog(0).Get(cursor.OccurrenceIndex);System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"biathlon-followup-third.txt"),$"tick={t} bout={actor.ActiveBout} phase={actor.Lifecycle.Phase} loop={body.Observation.LoopId} pose={body.Pose.X:R},{body.Pose.Z:R} speed={actor.Motion.SpeedMetersPerSecond:R} brake={f.runner.GetLastManualBrake(0):R} effort={f.runner.GetLastInput(0).Ski.Effort.RequestedEffort} arc={cursor.ObservedArcNanometers} len={row.LengthNanometers} edges={f.stepper.World.GetCourseEdgeResponses(0)}");}
#endif
                if(actor.ActiveShooting!=null&&actor.ActiveShooting.Phase==ShootingPhase.Aiming)
                    Assert.That(f.runner.GetLastInput(0).ManualBoltCycle,Is.True);
                if(actor.ActiveBout==2&&actor.ActiveShooting?.ShotsFired==1&&actor.ActiveShooting.NeedsBoltCycle)
                {
                    for(int k=0;k<100;k++)
                    {
                        tick=field.Tick.Next();f.runner.StepWithManualPlayer(0,new SprintAthleteInput(
                            new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,650),1,1,0),new WeaponAimCommand(tick,actor.AthleteId,0,0),trigger:true,manualBoltCycle:true),0,true);
                    }
                    Assert.That(actor.ActiveShooting.ShotsFired,Is.EqualTo(1));third=true;
                }
            }
            Assert.That(third,Is.True,"Must reach the third real bout before testing repeated left clicks.");
        }
        [Test] public void FirstMassArrivalPlantsAtTheAssignedMats()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);var seen=new bool[30];int count=0;double worst=0;
            for(int t=0;t<15000&&count<30;t++)
            {
                f.runner.Step();
#if !UNITY_EDITOR
                if(t%200==0)System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"biathlon-followup-arrivals.txt"),$"tick={t} arrived={count} worst={worst:R} player_loop={f.stepper.World.GetActor(0).Observation.LoopId} pose={f.stepper.World.GetActor(0).Pose.X:R},{f.stepper.World.GetActor(0).Pose.Z:R}");
#endif
                for(int a=0;a<30;a++)
                {
                    var actor=f.runner.Field.GetAthlete(a);if(seen[a]||actor.ActiveShooting==null)continue;
                    seen[a]=true;count++;var body=f.stepper.World.GetActor(a);var catalog=f.stepper.World.GetCatalog(a);
                    var row=catalog.Get(catalog.GetFiringOccurrence(0));catalog.TrySample(row.Index,row.LengthNanometers,out var target);
                    double error=Math.Sqrt(Math.Pow(body.Pose.X-target.X,2)+Math.Pow(body.Pose.Z-target.Z,2));worst=Math.Max(worst,error);
                    TestContext.WriteLine($"actor={a} tick={f.runner.Field.Tick.Value} mat_error_m={error:R} pose={body.Pose.X:R},{body.Pose.Z:R} goal={target.X:R},{target.Z:R}");
                }
            }
            for(int a=0;a<30;a++)if(!seen[a]){var actor=f.runner.Field.GetAthlete(a);var b=f.stepper.World.GetActor(a);var input=f.runner.GetLastInput(a);TestContext.WriteLine($"missing_actor={a} phase={actor.Lifecycle.Phase} loop={b.Observation.LoopId} pose={b.Pose.X:R},{b.Pose.Z:R} speed={actor.Motion.SpeedMetersPerSecond:R} effort={input.Ski.Effort.RequestedEffort} tuck={input.Ski.TuckRequested} heading={b.Pose.HeadingRadians:R} edges={f.stepper.World.GetCourseEdgeResponses(a)}");}
            TestContext.WriteLine($"arrivals={count} worst_mat_error_m={worst:R}");
            Assert.That(count,Is.EqualTo(30));Assert.That(worst,Is.LessThanOrEqualTo(
                ManualMatCaptureDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_mat_capture_v1.json")).PositionTolerance));
        }
    }
}

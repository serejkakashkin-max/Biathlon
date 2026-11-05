#if !UNITY_EDITOR
using System;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class PursuitOwnerStartContactTests
    {
        [Test,Explicit("Local read-only owner career fixture; portable equivalent is ManualStartReleaseGateTests"),Timeout(900000)] public void QualifiedPursuitLaunchDoesNotIntroducePenetratingBodies()
        {
            var save=CareerSave.FromJson(PhysiologyContractTests.ProjectFile("artifacts/m16-6/start-contact/owner-profile.json"));
            var d=CareerDefinitions.ForSave(PhysiologyContractTests.DefinitionJson,save);
            var week=CareerSeasonEngine.PrepareWeek(save,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));
            TestContext.WriteLine("event="+week.Event.Id+" seed="+week.RaceSeed);
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var p=new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,d,Read).Prepare(CareerSave.FromJson(week.RaceSnapshotJson),week.Event,week.RaceSeed);
            var runner=p.Runner;var movement=ManualProductionFieldComposition.Create(runner.Field,p.Venue,p.ManualRead,p.CourseReactionPolicy);
            runner.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson(ManualAiSteeringDefinition.ResourceForVersion(p.MovingOvertakeVersion))),
                ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("player_course_assistance_v1.json")),true);
            TestContext.WriteLine("player="+p.PlayerIndex+" start="+runner.Field.GetAthlete(p.PlayerIndex).ScheduledStart.Value);
            for(int t=0;t<1500;t++)
            {
                var a=runner.Field.GetAthlete(p.PlayerIndex);var tick=runner.Field.Tick.Next();
                try{runner.StepWithManualPlayer(p.PlayerIndex,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,0),1,1,0),new WeaponAimCommand(tick,a.AthleteId,0,0)),0);}
                catch
                {
                    var c=movement.World.LastOwnedContacts;TestContext.WriteLine($"pair={c.Limit.First}:{c.Limit.Second} gap={c.Geometry.GapMeters:R} status={c.Geometry.Status}");
                    for(int i=0;i<runner.Field.Count;i++)if(runner.Field.GetAthlete(i).AthleteId.Equals(c.Limit.First)||runner.Field.GetAthlete(i).AthleteId.Equals(c.Limit.Second))
                    {var actor=movement.World.GetActor(i);var race=runner.Field.GetAthlete(i);TestContext.WriteLine($"actor={i} start={race.ScheduledStart.Value} tick={race.Tick.Value} pose={actor.Pose.X:R},{actor.Pose.Z:R},{actor.Pose.HeadingRadians:R} velocity={actor.Velocity.X:R},{actor.Velocity.Z:R} spin={actor.Pose.YawRateRadiansPerSecond:R}");}
                    throw;
                }
            }
            Assert.That(runner.Field.Tick.Value,Is.EqualTo(1500));
            Assert.That(movement.World.IsStartReleaseHeld(28),Is.True);
            int aiming=0;
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<100000)
            {
                var a=runner.Field.GetAthlete(p.PlayerIndex);if(a.HasFinished){runner.StepRemainingField(p.PlayerIndex);continue;}
                var tick=runner.Field.Tick.Next();var shooting=a.ActiveShooting;
                aiming=shooting?.Phase==ShootingPhase.Aiming?aiming+1:0;
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,650),1,1,0),
                    new WeaponAimCommand(tick,a.AthleteId,0,1000),aiming>=40,false,runner.RaceWind,cycleBolt:shooting?.NeedsBoltCycle==true,manualBoltCycle:true);
                runner.StepWithManualPlayer(p.PlayerIndex,input,0,true);
            }
            Assert.That(runner.Field.IsComplete,Is.True);
            Assert.That(movement.World.IsStartReleaseHeld(28),Is.False);
            Assert.That(movement.World.GetStartReleaseTick(28),Is.GreaterThan(1500));
            var result=p.CollectResult();Assert.That(result.Count,Is.EqualTo(30));
            TestContext.WriteLine("owner_pursuit_complete_tick="+runner.Field.Tick.Value+" gate28_release="+movement.World.GetStartReleaseTick(28)+" authority="+result.AuthoritySha256);
        }
    }
}
#endif

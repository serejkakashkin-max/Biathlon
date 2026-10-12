using System;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerOwnerBalanceTests
    {
        private static string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
        private static string Venue(string n)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+n+".json");
        [TestCase(900),TestCase(1000),Timeout(900000)]
        public void NoviceControlledAtFullEffortCannotWinByAnUncontestedMinute(int requestedEffort)
        {
            var d=CareerDefinitions.ForNewProductionCareer(Read,"junior_novice");
            var save=CareerFactory.Create(d.Roster,d.Calendar,"novice_balance","Alex North","NOR","player_appearance",new SeededRandomSource(71000),CareerStartDefinition.FromJson(Read("career_start_v1.json")),"junior_novice");
            var p=new CareerRaceProvider(Read,d,Venue).Prepare(save,d.Calendar.Get(0),123);var r=p.Runner;int player=p.PlayerIndex;
            var movement=ManualProductionFieldComposition.Create(r.Field,p.Venue,Read);r.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")));
            var model=new ShootingModelDefinitions(ProductionGameplayDefinitions.Bind(Read));ShootingAiController shooter=null;ShootingSession previous=null;
            while(!r.Field.IsComplete)
            {
                var a=r.Field.GetAthlete(player);var tick=r.Field.Tick.Next();
                if(a.HasFinished){r.StepRemainingField(player);continue;}
                var shooting=a.ActiveShooting;WeaponAimCommand aim=new WeaponAimCommand(tick,a.AthleteId,0,0);bool fire=false,hold=false,bolt=false;
                if(shooting!=null)
                {
                    if(!ReferenceEquals(previous,shooting)){previous=shooting;var precise=ShootingAiDefinition.FromJson(Read("shooting_ai_v1.json").Replace("\"reaction_seconds\":0.2","\"reaction_seconds\":0.05").Replace("\"tracking_error_rad\":0.00015","\"tracking_error_rad\":0").Replace("\"aim_gain_per_s\":2.5","\"aim_gain_per_s\":10"));shooter=new ShootingAiController(precise,model.Targets,model.Aim,model.Stances.Get(r.Field.Profile.GetBout(a.ActiveBout).Stance),a.AthleteId,20,new SeededRandomSource(444+(ulong)a.ActiveBout),r.Field.Tick);}
                    var sight=shooting.Sight;var frame=shooter.Step(tick,new ShootingAiObservation(shooting.Phase,shooting.ClosedMask,shooting.ShotsFired,sight.YawRadians,sight.PitchRadians));
                    aim=frame.Aim;fire=frame.Trigger;hold=frame.HoldBreath;bolt=shooting.NeedsBoltCycle;
                }
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,requestedEffort),1,1,0),aim,fire&&!bolt,hold,cycleBolt:bolt,manualBoltCycle:true);
                // An exact route controller isolates pace/physiology/shooting from human driving mistakes.
                r.StepWithManualPlayer(player,input,0,finalMatAssistance:true);Assert.That(r.Field.Tick.Value,Is.LessThan(300000));
            }
            var result=p.CollectResult();int rank=0;for(int i=0;i<result.Count;i++)if(result.Get(i).AthleteId==save.PlayerId)rank=i;
            var playerRow=result.Get(rank);long bestRival=long.MaxValue;for(int i=0;i<result.Count;i++)if(i!=rank)bestRival=Math.Min(bestRival,result.Get(i).ElapsedMilliseconds);
            TestContext.WriteLine("NOVICE_FULL_EFFORT effort="+requestedEffort+" rank="+(rank+1)+" player_ms="+playerRow.ElapsedMilliseconds+" best_rival_ms="+bestRival+" lead_ms="+(bestRival-playerRow.ElapsedMilliseconds)+" penalties="+playerRow.Penalties+" result_sha="+result.AuthoritySha256);
            Assert.That(bestRival-playerRow.ElapsedMilliseconds,Is.LessThan(60000),"A novice with perfect route execution and full effort should still face credible opposition.");
        }
        [Test,Timeout(900000)]
        public void ProductionCareerFieldReportsRealPaceRangeAndBrakingCosts()
        {
            var d=CareerDefinitions.ForNewProductionCareer(Read);
            var save=CareerFactory.Create(d.Roster,d.Calendar,"owner_balance_probe","Alex North","NOR","player_appearance",new SeededRandomSource(71000));
            var p=new CareerRaceProvider(Read,d,Venue).Prepare(save,d.Calendar.Get(0),123);
            var r=p.Runner;var movement=ManualProductionFieldComposition.Create(r.Field,p.Venue,Read);
            r.BindManualScopedCandidate(movement,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")));
            var skiTicks=new long[r.Field.Count];var effort=new long[skiTicks.Length];var brakes=new long[skiTicks.Length];var range=new long[skiTicks.Length];
            while(!r.Field.IsComplete)
            {
                r.Step();Assert.That(r.Field.Tick.Value,Is.LessThan(300000));
                for(int i=0;i<skiTicks.Length;i++)
                {
                    var a=r.Field.GetAthlete(i);if(a.HasFinished)continue;
                    if(a.Lifecycle.Phase==CompetitionPhase.Range)range[i]++;
                    else {skiTicks[i]++;effort[i]+=r.GetLastInput(i).Ski.Effort.RequestedEffort;if(r.GetLastManualBrake(i)>.05)brakes[i]++;}
                }
            }
            var result=p.CollectResult();
            for(int rank=0;rank<result.Count;rank++)
            {
                var row=result.Get(rank);int index=0;while(save.Athlete(index).Id!=row.AthleteId)index++;
                TestContext.WriteLine("BALANCE rank="+(rank+1)+" athlete="+row.AthleteId+" ms="+row.ElapsedMilliseconds+" misses="+row.Penalties+" effort="+(effort[index]/Math.Max(1,skiTicks[index]))+" brake_pct="+(100*brakes[index]/Math.Max(1,skiTicks[index]))+" range_s="+(range[index]/20.0)+" endurance="+save.Athlete(index).Stat(CareerStat.Endurance));
            }
        }
    }
}

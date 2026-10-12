using System;
using System.Collections.Generic;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerStartProfileTests
    {
        private static string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
        private static CareerStartDefinition Starts()=>CareerStartDefinition.FromJson(Read("career_start_v1.json"));
        private static CareerSave Create(string id="junior_novice",int? age=null)
        {
            var d=CareerDefinitions.ForNewProductionCareer(Read,id);
            return CareerFactory.Create(d.Roster,d.Calendar,"start_profiles","Alex North","NOR","player_appearance",new SeededRandomSource(71000),Starts(),id,age);
        }
        private sealed class OrderedBoundary:ICareerRaceProvider
        {
            public CareerRaceRecord Run(CareerSave save,CareerCalendarEvent e,ulong seed)
            {
                var rows=new List<CareerFinish>();for(int i=0;i<save.AthleteCount;i++)if(e.Eligible(save.Athlete(i)))rows.Add(new CareerFinish(save.Athlete(i).Id,300000+i*1000,300000+i*1000));
                return new CareerRaceRecord(save.SeasonId+"_"+e.Id,save.SeasonId,e.Id,e.Format,seed,new string('a',64),rows);
            }
        }
        [Test] public void ExplicitStartsKeepNewcomerWeakAndPeersHeterogeneousAndSeeded()
        {
            var a=Create();Assert.That(a.ToJson(),Is.EqualTo(Create().ToJson()));Assert.That(a.StartProfileId,Is.EqualTo("junior_novice"));Assert.That(a.Athlete(0).Age,Is.EqualTo(18));Assert.That(a.CurrentSeasonYear,Is.EqualTo(2026));
            int min=100000,max=0,above=0;
            for(int i=0;i<a.AthleteCount;i++)
            {
                Assert.That(a.Athlete(i).Age,Is.InRange(18,20));int average=0;for(int s=0;s<CareerUnits.StatCount;s++)average+=a.Athlete(i).Stat((CareerStat)s);average/=CareerUnits.StatCount;
                if(i==0)Assert.That(average,Is.LessThan(55000));else {min=Math.Min(min,average);max=Math.Max(max,average);if(average>50000)above++;}
            }
            Assert.That(max-min,Is.GreaterThan(15000));Assert.That(above,Is.GreaterThan(15));
            var senior=Create("senior_debut",24);Assert.That(senior.Athlete(0).Age,Is.EqualTo(24));Assert.That(senior.Athlete(0).Tier,Is.EqualTo(CareerTier.National));
            Assert.Catch(()=>Create("junior_novice",21));Assert.Catch(()=>Create("senior_debut",20));Assert.Catch(()=>Create("unknown"));
        }
        [Test] public void JuniorAgeEligibilityAndAgeOutPersistWithoutLosingPlayerProgress()
        {
            var save=Create(age:20);string playerId=save.PlayerId;int before=save.Athlete(0).Stat(CareerStat.Endurance);
            for(int week=1;week<=52;week++)
            {
                var d=CareerDefinitions.ForSave(Read,save);save=CareerSeasonEngine.AdvanceWeek(save,d,Array.Empty<CareerTrainingKind>(),new OrderedBoundary(),s=>new SeededRandomSource(s));
            }
            Assert.That(save.CalendarId,Is.EqualTo("woodland_development_calendar_v1"));Assert.That(save.Athlete(0).Id,Is.EqualTo(playerId));Assert.That(save.Athlete(0).Age,Is.EqualTo(21));Assert.That(save.Athlete(0).Stat(CareerStat.Endurance),Is.GreaterThanOrEqualTo(before));Assert.That(save.HistoryCount,Is.EqualTo(1));
            Assert.That(save.Athlete(1).TrainingSessions,Is.Zero,"New circuit has a new permanent opponent cohort.");Assert.That(save.NextAthleteOrdinal,Is.EqualTo(59));
            var restored=CareerSave.FromJson(save.ToJson());Assert.That(CareerDefinitions.ForSave(Read,restored).Calendar.Circuit,Is.EqualTo(CareerCircuit.Development));
            var junior=CareerDefinitions.ForNewProductionCareer(Read,"junior_novice").Calendar;
            Assert.That(junior.Get(0).Eligible(save.Athlete(0)),Is.False);
        }
        [Test] public void EarnedPromotionLoadsActualWorldCupCohortAndScoring()
        {
            var save=Create("senior_debut");
            for(int week=1;week<=52;week++){var d=CareerDefinitions.ForSave(Read,save);save=CareerSeasonEngine.AdvanceWeek(save,d,Array.Empty<CareerTrainingKind>(),new OrderedBoundary(),s=>new SeededRandomSource(s));}
            var after=CareerDefinitions.ForSave(Read,save);Assert.That(after.Calendar.Circuit,Is.EqualTo(CareerCircuit.WorldCup));Assert.That(save.Athlete(0).Tier,Is.EqualTo(CareerTier.International));
            Assert.That(after.Scoring.Points(1,CompetitionFormat.Sprint),Is.EqualTo(90));Assert.That(after.Scoring.Points(40,CompetitionFormat.Sprint),Is.EqualTo(1));Assert.That(after.Scoring.Points(21,CompetitionFormat.MassStart),Is.EqualTo(20));Assert.That(after.Scoring.Points(30,CompetitionFormat.MassStart),Is.EqualTo(2));
            int stronger=0;for(int i=1;i<save.AthleteCount;i++){Assert.That(after.Calendar.Get(0).Eligible(save.Athlete(i)),Is.True);if(save.Athlete(i).Stat(CareerStat.Endurance)>70000)stronger++;}Assert.That(stronger,Is.GreaterThan(20));
        }
        [Test] public void WeeklyAiDevelopmentIsRealAndLegacyFactoryKeepsItsOriginalSave()
        {
            var save=Create();var d=CareerDefinitions.ForSave(Read,save);
            for(int week=1;week<=5;week++)save=CareerSeasonEngine.AdvanceWeek(save,d,Array.Empty<CareerTrainingKind>(),new OrderedBoundary(),s=>new SeededRandomSource(s));
            Assert.That(save.Athlete(1).TrainingSessions,Is.GreaterThan(0));bool changed=false;var baseline=Create();for(int s=0;s<CareerUnits.StatCount;s++)changed|=save.Athlete(1).Stat((CareerStat)s)!=baseline.Athlete(1).Stat((CareerStat)s);Assert.That(changed,Is.True);
            Assert.That(CareerFoundationTests.Create().StartProfileId,Is.Null);Assert.That(CareerDefinitions.ForSave(Read,CareerFoundationTests.Create()).Calendar.Circuit,Is.EqualTo(CareerCircuit.Open));
        }
        [Test] public void ManualCareerPlayerUsesSameProjectedFormAndPressureAsAi()
        {
            var save=Create();var d=CareerDefinitions.ForSave(Read,save);
            var p=new CareerRaceProvider(Read,d,n=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+n+".json")).Prepare(save,d.Calendar.Get(0),123);
            var actor=p.Runner.Field.GetAthlete(p.PlayerIndex);var tick=p.Runner.Field.Tick.Next();
            var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,800),1.1,1,0),new WeaponAimCommand(tick,actor.AthleteId,0,0));
            p.Runner.StepWithPlayer(p.PlayerIndex,input,0);var accepted=p.Runner.GetLastInput(p.PlayerIndex);
            Assert.That(accepted.Ski.Form,Is.EqualTo(CareerForm.RaceMultiplier(save.Athlete(0),d.Form)));Assert.That(accepted.Ski.Form,Is.Not.EqualTo(input.Ski.Form));
            Assert.That(accepted.Pressure.Confidence,Is.EqualTo(save.Athlete(0).Confidence/(double)CareerUnits.Basis));Assert.That(accepted.Ski.Effort.RequestedEffort,Is.EqualTo(800));
        }
        [Test] public void WorldCupStandingsCountEveryFinishingPlaceAndShareTiedRacePoints()
        {
            var save=CareerFoundationTests.Create();var scoring=CareerScoringDefinition.FromJson("{\"schema_version\":1,\"id\":\"countback_test\",\"countback_by_places\":true,\"rank_points\":[5,4,3,2]}");
            for(int race=0;race<2;race++)
            {
                var finish=new CareerFinish[save.AthleteCount];for(int rank=0;rank<finish.Length;rank++){int i=race==1&&rank==1?3:race==1&&rank==3?1:rank;finish[rank]=new CareerFinish(save.Athlete(i).Id,300000+rank*1000,300000+rank*1000);}
                CareerStandings.Register(save,new CareerRaceRecord("countback_"+race,save.SeasonId,"event_"+race,CompetitionFormat.Sprint,1,new string('a',64),finish));
            }
            var table=CareerStandings.Table(save,scoring);int a=Array.FindIndex(table,x=>x.AthleteId==save.Athlete(1).Id),b=Array.FindIndex(table,x=>x.AthleteId==save.Athlete(2).Id);
            Assert.That(table[a].Points,Is.EqualTo(table[b].Points));Assert.That(a,Is.LessThan(b),"One second place outranks two thirds after equal points/wins.");Assert.That(table[a].FinishesAt(2),Is.EqualTo(1));
            var tied=new CareerRaceRecord("tied",save.SeasonId,"tied",CompetitionFormat.Sprint,1,new string('a',64),new[]{new CareerFinish(save.Athlete(0).Id,1000,1000),new CareerFinish(save.Athlete(1).Id,1000,1000)});
            Assert.That(CareerStandings.ResultRank(tied,1,scoring),Is.EqualTo(1));Assert.That(CareerStandings.ResultRank(tied,1,CareerScoringDefinition.FromJson(Read("career_scoring_v1.json"))),Is.EqualTo(2));
        }
    }
}

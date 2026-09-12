using System;
using Biathlon.Career;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerSeasonTests
    {
        public static CareerDefinitions Definitions()=>new CareerDefinitions(PhysiologyContractTests.DefinitionJson);
        private sealed class FixtureRaces : ICareerRaceProvider
        {public CareerRaceRecord Run(CareerSave save,CareerCalendarEvent e,ulong seed){var rows=new CareerFinish[save.AthleteCount];for(int i=0;i<rows.Length;i++)rows[i]=new CareerFinish(save.Athlete(i).Id,300000+i*1000,300000+i*1000);return new CareerRaceRecord(save.SeasonId+"_"+e.Id,save.SeasonId,e.Id,e.Format,seed,new string('a',64),rows);}}
        private sealed class BrokenRaces : ICareerRaceProvider {public CareerRaceRecord Run(CareerSave save,CareerCalendarEvent e,ulong seed)=>throw new InvalidOperationException("Injected race failure");}
        public static CareerTrainingKind[] Plan(CareerSave save,CareerDefinitions d)
        {CareerAthlete player=null;for(int i=0;i<save.AthleteCount;i++)if(save.Athlete(i).Id==save.PlayerId)player=save.Athlete(i);var plan=CareerAiTraining.Plan(player,d.Training,d.Calendar,d.Policy,save.Week);var result=new CareerTrainingKind[plan.Count];for(int i=0;i<result.Length;i++)result[i]=plan.Get(i);return result;}
        [Test] public void WeeklyAccountingConsumesCampOnceAndRollbackPreservesAllSourceState()
        {
            var d=Definitions();var save=CareerFoundationTests.Create();CareerEconomy.BuyCamp(save.Athlete(0),d.Economy,d.Adaptation);
            save=CareerSeasonEngine.AdvanceWeek(save,d,Plan(save,d),new FixtureRaces(),s=>new SeededRandomSource(s));Assert.That(save.Athlete(0).PendingCampQuality,Is.Zero);
            while(save.Week<8)save=CareerSeasonEngine.AdvanceWeek(save,d,Plan(save,d),new FixtureRaces(),s=>new SeededRandomSource(s));
            string before=save.ToJson();Assert.Catch(()=>CareerSeasonEngine.AdvanceWeek(save,d,Plan(save,d),new BrokenRaces(),s=>new SeededRandomSource(s)));Assert.That(save.ToJson(),Is.EqualTo(before));
            var copy=CareerSave.FromJson(before);for(int i=0;i<45;i++){save=CareerSeasonEngine.AdvanceWeek(save,d,Plan(save,d),new FixtureRaces(),s=>new SeededRandomSource(s));copy=CareerSeasonEngine.AdvanceWeek(copy,d,Plan(copy,d),new FixtureRaces(),s=>new SeededRandomSource(s));}
            Assert.That(save.ToJson(),Is.EqualTo(copy.ToJson()));Assert.That(save.Season,Is.EqualTo(2));Assert.That(save.Week,Is.EqualTo(1));Assert.That(save.HistoryCount,Is.EqualTo(1));Assert.That(save.Athlete(0).Races,Is.EqualTo(6));Assert.That(save.Athlete(0).Wins,Is.EqualTo(6));
        }
    }
    public class CareerActualSeasonTests
    {
#if !UNITY_EDITOR
        [Test,Timeout(1200000)] public void CompleteSeasonUsesAuthoritativeRacesAndSavedSprintQualifications()=>Run();
#else
        [UnityEngine.TestTools.UnityTest,Timeout(1200000)] public System.Collections.IEnumerator CompleteSeasonUsesAuthoritativeRacesAndSavedSprintQualifications()
        {var work=System.Threading.Tasks.Task.Run((Action)Run);while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}
#endif
        private static void Run()
        {
            var d=CareerSeasonTests.Definitions();var save=CareerFoundationTests.Create();var provider=new CareerRaceProvider(PhysiologyContractTests.DefinitionJson,n=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+n),d);int races=0;
            for(int week=1;week<=52;week++)
            {
                var e=d.Calendar.AtWeek(save.Week);save=CareerSeasonEngine.AdvanceWeek(save,d,CareerSeasonTests.Plan(save,d),provider,s=>new SeededRandomSource(s));
                if(e!=null){races++;Assert.That(provider.LastShots,Is.EqualTo(30*(e.Format==CompetitionFormat.Sprint?2:4)*5));Assert.That(provider.LastTicks,Is.GreaterThan(0));TestContext.Out.WriteLine(e.Id+" ticks="+provider.LastTicks+" events="+provider.LastEventHash);}
                save=CareerSave.FromJson(save.ToJson());
            }
            Assert.That(races,Is.EqualTo(6));Assert.That(save.Season,Is.EqualTo(2));Assert.That(save.History(0).RaceCount,Is.EqualTo(6));Assert.That(save.Athlete(0).Races,Is.EqualTo(6));
            string hash=BalanceRegression.Hash(save.ToJson());TestContext.Out.WriteLine("career_actual_season_sha256="+hash);
            var root=new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);while(root!=null&&!System.IO.File.Exists(System.IO.Path.Combine(root.FullName,"AGENTS.md")))root=root.Parent;string path=System.IO.Path.Combine(root.FullName,"artifacts","balance");System.IO.Directory.CreateDirectory(path);
#if UNITY_EDITOR
            string prefix="unity";
#else
            string prefix="dotnet";
#endif
            System.IO.File.WriteAllText(System.IO.Path.Combine(path,"wp121-"+prefix+"-save.json"),save.ToJson());
        }
    }
}

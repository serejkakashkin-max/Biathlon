using Biathlon.Career;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerHistoryTests
    {
        public static CareerHistoryDefinition Definition()=>CareerHistoryDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_history_v1.json"));
        [Test] public void RivalryUsesActualSharedFinishesAndArchivedHistoryHasABoundedSaveFootprint()
        {
            var save=CareerFoundationTests.Create();
            for(int season=0;season<30;season++)
            {
                CareerStandings.Register(save,CareerStandingsTests.Record(save,"opening_sprint",CompetitionFormat.Sprint));
                var summary=CareerHistory.Summarize(save,CareerStandingsTests.Scoring(),Definition());Assert.That(summary.Get(5).CloseFinishes,Is.EqualTo(1));Assert.That(summary.Get(6).CloseFinishes,Is.Zero);Assert.That(summary.Get(1).SharedRaces,Is.EqualTo(1));Assert.That(summary.Get(1).SharedPodiums,Is.EqualTo(1));Assert.That(summary.Get(1).StandingsBattle,Is.True);
                save=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(save.ToJson(),"week","52"));string before=save.ToJson();
                var next=CareerHistory.SealSeason(save,CareerFoundationTests.Calendar(),CareerStandingsTests.Scoring(),Definition());Assert.That(save.ToJson(),Is.EqualTo(before));save=CareerSave.FromJson(next.ToJson());Assert.That(save.RaceCount,Is.Zero);Assert.That(save.HistoryCount,Is.EqualTo(System.Math.Min(season+1,20)));
            }
            Assert.That(save.History(0).Season,Is.EqualTo(11));Assert.That(save.History(19).Season,Is.EqualTo(30));Assert.That(save.ToJson().Length,Is.LessThan(250000));
        }
        [Test] public void EarlySealingIsRejectedAndNonParticipantsDoNotBecomeRivals()
        {
            var save=CareerFoundationTests.Create();string before=save.ToJson();Assert.Catch(()=>CareerHistory.SealSeason(save,CareerFoundationTests.Calendar(),CareerStandingsTests.Scoring(),Definition()));Assert.That(save.ToJson(),Is.EqualTo(before));
            var summary=CareerHistory.Summarize(save,CareerStandingsTests.Scoring(),Definition());for(int i=0;i<summary.Count;i++){Assert.That(summary.Get(i).SharedRaces,Is.Zero);Assert.That(summary.Get(i).StandingsBattle,Is.False);}
        }
    }
}

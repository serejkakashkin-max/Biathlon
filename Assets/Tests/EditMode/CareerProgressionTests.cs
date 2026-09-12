using Biathlon.Career;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerProgressionTests
    {
        public static CareerProgressionDefinition Definition()=>CareerProgressionDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_progression_v1.json"));
        [Test] public void PromotionRequiresParticipationAndAdvancesAtMostOneTierPerSeason()
        {
            var save=CareerFoundationTests.Create();var calendar=CareerFoundationTests.Calendar();
            for(int season=0;season<2;season++)
            {
                for(int race=0;race<3;race++)CareerStandings.Register(save,CareerStandingsTests.Record(save,"event_"+race,CompetitionFormat.Sprint));
                save=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(save.ToJson(),"week","52"));
                save=CareerProgression.ApplySeason(save,calendar,CareerStandingsTests.Scoring(),Definition());Assert.That(save.Athlete(0).Tier,Is.EqualTo(season==0?CareerTier.National:CareerTier.International));
                Assert.That(CareerProgression.ApplySeason(save,calendar,CareerStandingsTests.Scoring(),Definition()).ToJson(),Is.EqualTo(save.ToJson()));
                Assert.That(save.Athlete(29).Tier,Is.EqualTo(CareerTier.Regional));save=CareerHistory.SealSeason(save,calendar,CareerStandingsTests.Scoring(),CareerHistoryTests.Definition());
            }
            var restricted=CareerCalendar.FromJson(calendar.ToJson().Replace("\"minimum_tier\":0","\"minimum_tier\":2"));Assert.That(restricted.Get(0).Eligible(save.Athlete(0)),Is.True);Assert.That(restricted.Get(0).Eligible(save.Athlete(29)),Is.False);
        }
        [Test] public void EmptyAndPrematureSeasonsCannotGrantPromotion()
        {
            var save=CareerFoundationTests.Create();Assert.Catch(()=>CareerProgression.ApplySeason(save,CareerFoundationTests.Calendar(),CareerStandingsTests.Scoring(),Definition()));
            save=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(save.ToJson(),"week","52"));var next=CareerProgression.ApplySeason(save,CareerFoundationTests.Calendar(),CareerStandingsTests.Scoring(),Definition());Assert.That(next.Athlete(0).Tier,Is.EqualTo(CareerTier.Regional));
        }
    }
}

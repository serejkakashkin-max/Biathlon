using System;
using System.Linq;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class AthleteCareerSummaryTests
    {
        private static string Read(string file)=>PhysiologyContractTests.DefinitionJson(file);
        private static CareerSave World()
        {
            var draft=CareerCreationDraftTests.Draft();var d=CareerDefinitions.ForNewProductionCareer(Read,draft.StartProfileId);
            var save=CareerWorld.Initialize(draft.Confirm("athlete_cards",d.Calendar,s=>new SeededRandomSource(s)),d,s=>new SeededRandomSource(s));
            CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,s=>new SeededRandomSource(s),draft.Tier.BaselineStat);return save;
        }
        [Test] public void CardsReadActualAbilityAndSeparateTemporaryReadinessWithoutChangingWorld()
        {
            var save=World();var d=CareerDefinitions.ForSave(Read,save);string before=save.ToJson();ulong random=save.RandomState;
            var card=new AthleteCareerSummary(save,d,save.PlayerId);
            Assert.That(card.Ratings.Overall,Is.EqualTo(BiathlonRatingEngine.Calculate(card.Athlete,d.Ratings).Overall));
            Assert.That(card.OverallDelta,Is.Zero);Assert.That(card.PeakAge,Is.EqualTo(card.Athlete.Age));Assert.That(card.PeakSeason,Is.EqualTo(1));
            Assert.That(card.ObservationCount,Is.EqualTo(1));Assert.That(card.Observation(0).Current,Is.True);
            Assert.That(card.StrongestSeason,Is.Null);Assert.That(card.SeasonRank,Is.Null);Assert.That(card.SeasonPoints,Is.Null);Assert.That(card.Trend,Is.EqualTo(ObservedDevelopmentTrend.Unestablished));
            for(int stat=0;stat<13;stat++){Assert.That(card.Ability((CareerStat)stat),Is.EqualTo(BiathlonRatingEngine.StatRating(card.Athlete.Stat((CareerStat)stat))));Assert.That(card.AbilityDelta((CareerStat)stat),Is.Zero);}
            var tired=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(before,"fatigue","9000"));var other=new AthleteCareerSummary(tired,d,tired.PlayerId);
            Assert.That(other.Ratings.Overall,Is.EqualTo(card.Ratings.Overall));Assert.That(other.Athlete.Fatigue,Is.EqualTo(9000));Assert.That(save.ToJson(),Is.EqualTo(before));Assert.That(save.RandomState,Is.EqualTo(random));
        }
        [Test] public void SeasonalChangesAndPeakAreDerivedFromObservedStatsRatherThanPotential()
        {
            var save=World();var d=CareerDefinitions.ForSave(Read,save);var a=save.FindWorldAthlete(save.PlayerId);int baseline=BiathlonRatingEngine.Calculate(a,d.Ratings).Overall;
            for(int i=0;i<400;i++)CareerAdaptation.Apply(a,d.Training.Get(CareerTrainingKind.Endurance),d.Adaptation,10000,d.Growth);
            var growing=new AthleteCareerSummary(save,d,a.Id);Assert.That(growing.OverallDelta,Is.EqualTo(growing.Ratings.Overall-baseline));Assert.That(growing.OverallDelta,Is.GreaterThan(0));
            Assert.That(growing.AbilityDelta(CareerStat.Endurance),Is.GreaterThan(0));Assert.That(growing.AbilityDelta(CareerStat.ProneStability),Is.Zero);Assert.That(growing.GroupDelta(BiathlonRating.Ski),Is.GreaterThan(0));
            Assert.That(growing.PeakOverall,Is.EqualTo(growing.Ratings.Overall));Assert.That(growing.Observation(0).Overall,Is.EqualTo(baseline));
            CareerDevelopment.SealSeason(save,d.Growth,d.Ratings);string before=save.ToJson();var sealedCard=new AthleteCareerSummary(save,d,a.Id);
            Assert.That(sealedCard.StrongestSeason,Is.EqualTo(1));Assert.That(sealedCard.PeakOverall,Is.EqualTo(growing.PeakOverall));Assert.That(sealedCard.OverallDelta,Is.Zero,"A sealed season becomes the next baseline.");
            Assert.That(sealedCard.Trend,Is.EqualTo(ObservedDevelopmentTrend.Improving));
            Assert.That(save.ToJson(),Is.EqualTo(before));Assert.Throws<ArgumentOutOfRangeException>(()=>sealedCard.GroupDelta(BiathlonRating.Overall));
        }
        [Test] public void LegacyCardDoesNotInventPastHistoryOrInitializeUnpersistedMetadata()
        {
            var save=CareerFoundationTests.Create();var d=CareerDefinitions.ForSave(Read,save);string before=save.ToJson();var card=new AthleteCareerSummary(save,d,save.PlayerId);
            Assert.That(card.HasDevelopmentBaseline,Is.False);Assert.That(card.ObservationCount,Is.EqualTo(1));Assert.That(card.Observation(0).Season,Is.EqualTo(save.Season));
            Assert.That(card.StrongestSeason,Is.Null);Assert.That(card.Trend,Is.EqualTo(ObservedDevelopmentTrend.Unestablished));Assert.That(save.ToJson(),Is.EqualTo(before));
            Assert.Throws<ArgumentException>(()=>new AthleteCareerSummary(save,d,"unknown"));
            Assert.Throws<ArgumentException>(()=>new AthleteCareerSummary(save,CareerDefinitions.ForNewProductionCareer(Read),save.PlayerId));
        }
        [Test] public void WorldFiltersPreserveStableIdentitiesAndSortAbilitySeparatelyFromPoints()
        {
            var save=World();var d=CareerDefinitions.ForSave(Read,save);string before=save.ToJson();
            var all=AthleteCareerSummary.Search(save,d,false,null,null,CareerRosterSort.Overall);Assert.That(all.Length,Is.EqualTo(90));Assert.That(all.Select(x=>x.Athlete.Id).Distinct().Count(),Is.EqualTo(90));
            for(int i=1;i<all.Length;i++)Assert.That(all[i].Ratings.Overall,Is.LessThanOrEqualTo(all[i-1].Ratings.Overall));
            var active=AthleteCareerSummary.Search(save,d,true,null,null,CareerRosterSort.Age);Assert.That(active.Length,Is.EqualTo(30));Assert.That(active.All(x=>x.InActiveField),Is.True);
            for(int i=1;i<active.Length;i++)Assert.That(active[i].Athlete.Age,Is.GreaterThanOrEqualTo(active[i-1].Athlete.Age));
            string nation=all[0].Athlete.Nation;var filtered=AthleteCareerSummary.Search(save,d,false,nation,null,CareerRosterSort.Name);Assert.That(filtered.All(x=>x.Athlete.Nation==nation),Is.True);
            var exact=AthleteCareerSummary.Search(save,d,false,nation,all[0].Athlete.Name.ToUpperInvariant(),CareerRosterSort.Overall);Assert.That(exact.Any(x=>x.Athlete.Id==all[0].Athlete.Id),Is.True);
            var pool=save.Pool(0);var card=new AthleteCareerSummary(save,d,pool.Athlete.Id);Assert.That(card.Circuit,Is.EqualTo(pool.Circuit));Assert.That(card.InActiveField,Is.False);Assert.That(card.SeasonPoints,Is.Null);
            var worldCup=AthleteCareerSummary.Search(save,d,false,null,null,CareerRosterSort.Overall,CareerCircuit.WorldCup);Assert.That(worldCup.Length,Is.EqualTo(30));Assert.That(worldCup.All(x=>x.Circuit==CareerCircuit.WorldCup),Is.True);
            CareerStandings.Register(save,CareerStandingsTests.Record(save,"junior_open_sprint",CompetitionFormat.Sprint));
            var points=AthleteCareerSummary.Search(save,d,true,null,null,CareerRosterSort.Points);Assert.That(points[0].SeasonRank,Is.EqualTo(1));Assert.That(points[0].Athlete.Id,Is.EqualTo(save.PlayerId));
            Assert.That(before,Is.Not.EqualTo(save.ToJson()),"Only explicitly registering a result changes the world.");
        }
        [Test] public void MilestonesUseSavedCountersAndObservedPeakWithoutInventedRewardsOrResults()
        {
            var catalog=CareerAchievementCatalog.FromJson(Read("career_achievements_v1.json"));var save=World();var d=CareerDefinitions.ForSave(Read,save);var player=save.FindWorldAthlete(save.PlayerId);string before=save.ToJson();
            Assert.That(catalog.Count,Is.EqualTo(6));Assert.That(catalog.Get(0).Earned(player,d.Ratings),Is.False);Assert.That(catalog.Get(2).Earned(player,d.Ratings),Is.False);
            var first=catalog.Get(0);Assert.That(first.Title("ru"),Is.EqualTo("Первый финиш"));Assert.That(first.Title("en"),Is.EqualTo("First finish"));Assert.That(first.Value(player,d.Ratings),Is.Zero);
            // A persisted-counter fixture. Viewing milestones never creates a race result or changes these counters.
            var raced=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(PhysiologyContractTests.ReplaceNumber(before,"races","12"),"wins","2"));var veteran=raced.FindWorldAthlete(raced.PlayerId);
            Assert.That(catalog.Get(0).Earned(veteran,d.Ratings),Is.True);Assert.That(catalog.Get(1).Earned(veteran,d.Ratings),Is.True);Assert.That(catalog.Get(2).Earned(veteran,d.Ratings),Is.True);Assert.That(catalog.Get(3).Earned(veteran,d.Ratings),Is.False);
            Assert.That(catalog.Get(4).Value(player,d.Ratings),Is.EqualTo(BiathlonRatingEngine.Calculate(player,d.Ratings).Overall));Assert.That(save.ToJson(),Is.EqualTo(before));
            Assert.Throws<ArgumentException>(()=>first.Title("de"));
        }
        [Test] public void MilestoneCatalogRejectsUnknownMetricsDuplicateIdsAndInvalidThresholds()
        {
            string json=Read("career_achievements_v1.json");
            Assert.Catch(()=>CareerAchievementCatalog.FromJson(json.Replace("\"metric\":0","\"metric\":3")));
            Assert.Catch(()=>CareerAchievementCatalog.FromJson(json.Replace("\"target\":1","\"target\":0")));
            Assert.Catch(()=>CareerAchievementCatalog.FromJson(json.Replace("\"ten_finishes\"","\"first_finish\"")));
            Assert.Catch(()=>CareerAchievementCatalog.FromJson(json.Replace("\"target\":80","\"target\":100")));
        }
    }
}

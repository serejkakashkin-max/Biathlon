using System;
using Biathlon.Career;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RealAthleteRosterPackTests
    {
        // Fictional contract fixture only; it is neither a licensed person nor shipped content.
        private static string Json()=>"{\"schema_version\":1,\"id\":\"roster_contract_fixture\",\"season_year\":2026,\"statistics_version\":\"fixture_v1\",\"statistics_date\":\"2026-10-15\",\"statistics_sources\":[\"https://www.biathlonresults.com/\"],\"review_reference\":\"fixture_review_only\",\"distribution_rights_status\":0,\"athletes\":[{\"id\":\"external_fixture_1\",\"name\":\"Fictional Contract Athlete\",\"nation\":\"NOR\",\"birth_year\":1996,\"start_profile_id\":\"world_cup_debut\",\"stats\":[80000,80000,80000,80000,80000,80000,80000,80000,80000,80000,80000,80000,80000],\"identity_review_reference\":\"fixture_identity_review_only\",\"identity_rights_status\":0}],\"media\":[]}";
        [Test] public void PackStoresSeasonSourcesAndActualAbilitiesAndNeverInventsApproval()
        {
            var pack=RealAthleteRosterPack.FromJson(Json());Assert.That(pack.SeasonYear,Is.EqualTo(2026));Assert.That(pack.Get(0).BirthYear,Is.EqualTo(1996));Assert.That(pack.Get(0).Overall(BiathlonRatingEngineTests.Definition()),Is.EqualTo(79));
            Assert.That(pack.SourceCount,Is.EqualTo(1));Assert.That(pack.MediaCount,Is.Zero);Assert.Throws<InvalidOperationException>(()=>pack.ValidateForDistribution());
            Assert.That(RealAthleteRosterPack.FromJson(pack.ToJson()).ToJson(),Is.EqualTo(pack.ToJson()));
        }
        [Test] public void HeaderAndEveryAthleteRequireIndependentRecordedApproval()
        {
            string json=Json().Replace("\"distribution_rights_status\":0","\"distribution_rights_status\":1");Assert.Throws<InvalidOperationException>(()=>RealAthleteRosterPack.FromJson(json).ValidateForDistribution());
            json=json.Replace("\"identity_rights_status\":0","\"identity_rights_status\":1");Assert.DoesNotThrow(()=>RealAthleteRosterPack.FromJson(json).ValidateForDistribution());
            string media="{\"id\":\"media_fixture\",\"resource_id\":\"fixture_portrait\",\"sha256\":\""+new string('a',64)+"\",\"review_reference\":\"fixture_review\",\"status\":0}";
            json=json.Replace("\"media\":[]","\"media\":["+media+"]");Assert.Throws<InvalidOperationException>(()=>RealAthleteRosterPack.FromJson(json).ValidateForDistribution());
        }
        [Test] public void MissingSourcesBadStatsAndAdultJuniorRecastingAreRejected()
        {
            Assert.Catch(()=>RealAthleteRosterPack.FromJson(Json().Replace("world_cup_debut","junior_novice")));
            Assert.Catch(()=>RealAthleteRosterPack.FromJson(Json().Replace("2026-10-15","not-a-date")));
            Assert.Catch(()=>RealAthleteRosterPack.FromJson(Json().Replace("https://www.biathlonresults.com/","file:///private")));
            Assert.Catch(()=>RealAthleteRosterPack.FromJson(Json().Replace("80000,80000,80000]","80000]")));
            Assert.Catch(()=>RealAthleteRosterPack.FromJson(Json().Replace("fixture_review_only","")));
        }
    }
}

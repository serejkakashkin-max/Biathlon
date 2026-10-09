using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CompetitionProfileTests
    {
        public static CompetitionProfile TestProfile() => CompetitionProfile.FromJson(PhysiologyContractTests.DefinitionJson("sprint_test_v1.json"));
        [Test] public void ShootingLaneAuthorityRoundTripsAndRejectsUnknownRevisions()
        {
            string legacy=TestProfile().ToJson();
            Assert.That(TestProfile().ShootingLaneRulesRevision,Is.Null);
            string current=legacy.Replace("\"rules_revision\":","\"shooting_lane_rules_revision\":\"ibu_ecr_2026\",\"rules_revision\":");
            var restored=CompetitionProfile.FromJson(current);
            Assert.That(CompetitionProfile.FromJson(restored.ToJson()).ShootingLaneRulesRevision,Is.EqualTo("ibu_ecr_2026"));
            Assert.Catch(()=>CompetitionProfile.FromJson(current.Replace("ibu_ecr_2026","nearest_mat")));
        }
        [TestCase("sprint_standard_men_v1", 10000, 3, 0)] [TestCase("sprint_standard_women_v1", 7500, 3, 0)]
        [TestCase("pursuit_standard_men_v1", 12500, 5, 1)] [TestCase("pursuit_standard_women_v1", 10000, 5, 1)]
        [TestCase("mass_start_standard_men_v1", 15000, 5, 2)] [TestCase("mass_start_standard_women_v1", 12500, 5, 2)]
        public void StandardDataMatchesAuditedSeniorDistancesStartsAndSequence(string id, int distance, int laps, int startType)
        {
            var p = CompetitionProfile.FromJson(PhysiologyContractTests.DefinitionJson(id + ".json"));
            Assert.That(p.Distance.Millimeters, Is.EqualTo(distance * 1000L)); Assert.That(p.LapCount, Is.EqualTo(laps));
            Assert.That((int)p.StartType, Is.EqualTo(startType)); Assert.That(p.ShotsPerBout, Is.EqualTo(5));
            Assert.That(p.PenaltyLoopDistance.Meters, Is.EqualTo(150)); Assert.That(p.RulesRevision, Is.EqualTo("ibu_ecr_2025_11"));
            for (int i = 0; i < p.BoutCount; i++) Assert.That(p.GetBout(i).Stance, Is.EqualTo(i < p.BoutCount / 2 ? ShootingStance.Prone : ShootingStance.Standing));
            var restored = CompetitionProfile.FromJson(p.ToJson()); Assert.That(restored.ToJson(), Is.EqualTo(p.ToJson()));
            var requirements = p.ToRouteRequirements(); Assert.That(requirements.Id, Is.EqualTo(p.Id));
            Assert.That(requirements.Distance, Is.EqualTo(p.Distance)); Assert.That(requirements.RequiresRange(laps - 1), Is.False);
            for (int i = 0; i < laps - 1; i++) Assert.That(requirements.RequiresRange(i), Is.True);
        }
        [Test] public void ReducedTestProfileProjectsToExistingTrackRouteWithoutChangingStandards()
        {
            var p = TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var requirements = p.ToRouteRequirements();
            var plan = RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", requirements);
            var route = new RaceRoute(track, plan, requirements);
            Assert.That(p.Category, Is.EqualTo(CompetitionCategory.Test)); Assert.That(route.Length, Is.EqualTo(p.Distance));
        }
        [Test] public void UnknownSchemaBrokenSequenceStartAndFractionalMillimetersFail()
        {
            string json = TestProfile().ToJson();
            foreach (var pair in new[] { new[] { "schema_version", "2" }, new[] { "format", "10" }, new[] { "start_interval_s", "0" },
                new[] { "after_lap", "3" }, new[] { "shots_per_bout", "6" }, new[] { "field_capacity", "0" }, new[] { "distance_m", "3000.0001" } })
                Assert.Catch(() => CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair[0], pair[1])), pair[0]);
        }
    }
}

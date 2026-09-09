using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class RaceRouteTests
    {
        private static string PlanJson() => PhysiologyContractTests.DefinitionJson("synthetic_route_plan_v1.json");
        private static string RequirementsJson() => PhysiologyContractTests.DefinitionJson("synthetic_route_requirements_v1.json");
        public static RaceRoute Route() => new RaceRoute(TrackContractTests.Track(), RaceRoutePlan.FromJson(PlanJson()), CompetitionRouteRequirements.FromJson(RequirementsJson()));
        [Test]
        public void MixedLoopLapsComposeExactDistanceAndObservableSections()
        {
            var route = Route();
            Assert.That(route.Length.Meters, Is.EqualTo(4500));
            Assert.That(route.LapCount, Is.EqualTo(3));
            Assert.That(route.SectionCount, Is.EqualTo(4));
            Assert.That(route.GetLapStart(1).Meters, Is.EqualTo(1000));
            Assert.That(route.GetLapLength(1).Meters, Is.EqualTo(2500));
            Assert.That(route.GetLapStart(2).Meters, Is.EqualTo(3500));
            Assert.That(route.GetSection(1).HasRange, Is.False);
            Assert.That(route.GetSection(2).HasRange, Is.True);
            Assert.That(route.GetSection(2).Start.Meters, Is.EqualTo(2500));
        }
        [Test]
        public void LapJunctionAndFinishSamplingAreUnambiguous()
        {
            var route = Route();
            Assert.That(route.Sample(new TrackDistance(999999)).LapIndex, Is.Zero);
            Assert.That(route.Sample(TrackDistance.FromMeters(1000)).LapIndex, Is.EqualTo(1));
            Assert.That(route.Sample(TrackDistance.FromMeters(2500)).SectionIndex, Is.EqualTo(2));
            Assert.That(route.Sample(TrackDistance.FromMeters(3000)).Track.Grade, Is.EqualTo(0.1).Within(1e-12));
            Assert.That(route.Sample(TrackDistance.FromMeters(3500)).LapIndex, Is.EqualTo(2));
            Assert.That(route.Sample(route.Length).Track.SegmentFraction, Is.EqualTo(1));
            Assert.That(route.Sample(new TrackDistance(long.MaxValue), true).RouteDistance, Is.EqualTo(route.Length));
            Assert.Catch(() => route.Sample(new TrackDistance(route.Length.Millimeters + 1)));
        }
        [Test]
        public void ProfilePlanAndRangeMismatchesAreRejected()
        {
            var track = TrackContractTests.Track();
            var plan = RaceRoutePlan.FromJson(PlanJson());
            var requirements = CompetitionRouteRequirements.FromJson(RequirementsJson());
            Assert.Catch(() => new RaceRoute(track, plan, CompetitionRouteRequirements.FromJson(RequirementsJson().Replace("synthetic_route_profile_v1", "other"))));
            Assert.Catch(() => new RaceRoute(track, plan, CompetitionRouteRequirements.FromJson(PhysiologyContractTests.ReplaceNumber(RequirementsJson(), "lap_count", "2"))));
            Assert.Catch(() => new RaceRoute(track, plan, CompetitionRouteRequirements.FromJson(PhysiologyContractTests.ReplaceNumber(RequirementsJson(), "distance_m", "4499"))));
            Assert.Catch(() => new RaceRoute(track, RaceRoutePlan.FromJson(PlanJson().Replace("track_contract_fixture_v1", "other")), requirements));
            var missingRange = RaceRoutePlan.FromJson(PlanJson().Replace("main_short", "main_long"));
            Assert.That(Assert.Catch(() => new RaceRoute(track, missingRange, requirements)).Message, Does.Contain("no range access"));
            Assert.That(Assert.Catch(() => new RaceRoute(track, RaceRoutePlan.FromJson(PlanJson().Replace("main_long", "penalty_150")), requirements)).Message, Does.Contain("Penalty loops"));
            Assert.Catch(() => new RaceRoute(track, RaceRoutePlan.FromJson(PlanJson().Replace("main_long", "missing")), requirements));
        }
        [Test]
        public void RoutingContractsRoundTripAndRejectInvalidSchemas()
        {
            var plan = RaceRoutePlan.FromJson(PlanJson());
            var requirements = CompetitionRouteRequirements.FromJson(RequirementsJson());
            Assert.That(RaceRoutePlan.FromJson(plan.ToJson()).ToJson(), Is.EqualTo(plan.ToJson()));
            Assert.That(CompetitionRouteRequirements.FromJson(requirements.ToJson()).ToJson(), Is.EqualTo(requirements.ToJson()));
            Assert.That(requirements.RequiresRange(0), Is.True);
            Assert.That(requirements.RequiresRange(2), Is.False);
            Assert.Catch(() => RaceRoutePlan.FromJson("{}"));
            Assert.Catch(() => RaceRoutePlan.FromJson(PlanJson().Replace("lap_2", "lap_1")));
            Assert.Catch(() => CompetitionRouteRequirements.FromJson(RequirementsJson().Replace("[1, 2]", "[2, 1]")));
            Assert.Catch(() => CompetitionRouteRequirements.FromJson(RequirementsJson().Replace("[1, 2]", "[1, 4]")));
            Assert.Catch(() => CompetitionRouteRequirements.FromJson(PhysiologyContractTests.ReplaceNumber(RequirementsJson(), "schema_version", "2")));
        }
        [Test]
        public void SamplingCompleteRaceAllocatesNothingAndKeepsFixedLapBaseline()
        {
            var route = Route();
            route.Sample(default);
            long sum = 0;
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += route.Sample(new TrackDistance(i * 1000L), true).LapIndex;
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(sum, Is.EqualTo(15500));
        }
    }
}

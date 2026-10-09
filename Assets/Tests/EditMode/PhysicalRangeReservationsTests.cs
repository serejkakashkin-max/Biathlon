using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangeReservationsTests
    {
        [TestCase(false,1,true)][TestCase(true,2,false)]
        public void IbuPursuitAndSprintUseTheirOwnLaneRules(bool pursuit,int second,bool denied)
        {
            var f=Fixture(pursuit:pursuit);var ids=Roster(f);
            var profile=CompetitionProfile.FromJson(f.Profile.ToJson().Replace("\"rules_revision\":","\"shooting_lane_rules_revision\":\"ibu_ecr_2026\",\"rules_revision\":"));
            var book=new PhysicalRangeReservations(profile,ids,f.Layout,OccupancyTests.Definition(),20);
            var proposal=book.Prepare(new SimulationTick(1),ReadOnlySpan<PhysicalRangeRelease>.Empty,
                new[]{Arrival(book,ids,0,0,1,100000000),Arrival(book,ids,1,0,1,200000000)});
            Assert.That(book.GetOwnedLane(0),Is.Zero);Assert.That(book.GetPreparedRecord(proposal,0,0).LaneOrdinal,Is.EqualTo(1));
            Assert.That(book.WasPreparedDenied(proposal,1),Is.EqualTo(denied));book.Commit(proposal);
            Assert.That(book.GetRecord(0,0).ArrivalRank,Is.EqualTo(1));
            Assert.That(book.GetOwnedLane(1),Is.EqualTo(denied?0:second));
            if(!denied)Assert.That(book.GetRecord(1,0).ArrivalRank,Is.EqualTo(2));
        }
        [Test] public void IbuSprintStandingCannotUseAFreeProneLane()
        {
            var f=Fixture();var ids=Roster(f);
            var profile=CompetitionProfile.FromJson(f.Profile.ToJson().Replace("\"rules_revision\":","\"shooting_lane_rules_revision\":\"ibu_ecr_2026\",\"rules_revision\":"));
            var book=new PhysicalRangeReservations(profile,ids,f.Layout,OccupancyTests.Definition(),20);
            book.Commit(book.Prepare(new SimulationTick(1),ReadOnlySpan<PhysicalRangeRelease>.Empty,new[]{Arrival(book,ids,0,0,1,100000000)}));
            book.Commit(book.Prepare(new SimulationTick(2),new[]{Release(book,ids,0,0,1,2,100000000)},new[]{Arrival(book,ids,0,1,2,200000000)}));
            Assert.That(book.GetOwner(1),Is.EqualTo(-1));Assert.That(book.GetOwnedLane(0),Is.EqualTo(2));
        }
        [Test] public void ProductionProfilesExplicitlyBindCurrentIbuLaneRules()
        {
            foreach(string format in new[]{"sprint","pursuit","mass_start"})
            {
                var p=CompetitionProfile.FromJson(ManualScopedFieldStepperTests.Read("Biathlon/Venues/woodland_basin_v1/"+format+"_profile"));
                Assert.That(p.ShootingLaneRulesRevision,Is.EqualTo("ibu_ecr_2026"));
            }
        }
        internal static PhysicalRangePackTests.Fixture Fixture(int count = 3, bool pursuit = false)
        {
            var f = PhysicalRangePackTests.CreateFixture(count);
            string json = (pursuit ? PursuitQualificationTests.Profile() : CompetitionProfileTests.TestProfile()).ToJson();
            json = PhysiologyContractTests.ReplaceNumber(json, "distance_m", pursuit ? "1500" : "900");
            if (!pursuit) json = PhysiologyContractTests.ReplaceNumber(json, "start_interval_s", "0.05");
            f.Profile = CompetitionProfile.FromJson(json);
            var requirements = f.Profile.ToRouteRequirements();
            f.Route = new RaceRoute(f.Track, RaceRoutePlan.TimeTrial("reservation_test_route", f.Track.Id, "main", requirements), requirements);
            f.Map = new PhysicalTrackMap(f.Track, f.Route);
            var paths = PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"), f.Track, f.Junctions, f.Connections);
            f.Layout = new PhysicalRangeFieldLayout(f.Map, f.Track, f.Route, paths);
            return f;
        }
        internal static AthleteId[] Roster(PhysicalRangePackTests.Fixture f)
        {
            var ids = new AthleteId[f.Entrants.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = f.Entrants[i].AthleteId;
            return ids;
        }
        private static PhysicalRangeReservations Create(PhysicalRangePackTests.Fixture f)
            => new PhysicalRangeReservations(f.Profile, Roster(f), f.Layout, OccupancyTests.Definition(), 20);
        private static RaceMotionSegment Segment(long before, long after, double end)
            => (RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { before, after, before, after, 0.0, end, false, false }, null);
        private static RaceMotionSegment[] Crossing(long line, long fraction, out RaceMotionPoint initial)
        {
            initial = new RaceMotionPoint(fraction == 0 ? line : line - 10, fraction == 0 ? line : line - 10);
            return new[] { Segment(initial.RouteNanometers, fraction == 0 ? line + 10 : line,
                fraction == 0 ? .05 : fraction / (double)JunctionAdmissionSession.FractionParts / 20) };
        }
        private static PhysicalRangeArrival Arrival(PhysicalRangeReservations reservations, AthleteId[] ids, int actor,
            int bout, long tick, long fraction)
        {
            long line = reservations.AdmissionRouteNanometers(bout);
            var segments = Crossing(line, fraction, out var initial);
            Assert.That(PhysicalRangeArrival.TryFromAcceptedMotion(ids[actor], bout, new SimulationTick(tick), 20,
                line, initial, segments, out var row), Is.True);
            Assert.That(row.Fraction, Is.EqualTo(fraction)); return row;
        }
        private static PhysicalRangeRelease Release(PhysicalRangeReservations reservations, AthleteId[] ids, int actor,
            int bout, int lane, long tick, long fraction)
        {
            long line = reservations.ClearanceRouteNanometers(lane, bout);
            var segments = Crossing(line, fraction, out var initial);
            Assert.That(PhysicalRangeRelease.TryFromAcceptedMotion(ids[actor], bout, lane, new SimulationTick(tick), 20,
                line, initial, segments, out var row), Is.True);
            Assert.That(row.Fraction, Is.EqualTo(fraction)); return row;
        }
        private static void Fill(PhysicalRangeReservations reservations, AthleteId[] ids)
            => reservations.Commit(reservations.Prepare(reservations.Tick.Next(), ReadOnlySpan<PhysicalRangeRelease>.Empty,
                new[] { Arrival(reservations, ids, 1, 0, 1, 400000000), Arrival(reservations, ids, 0, 0, 1, 400000000) }));

        [TestCase(200000000, 300000000, true)]
        [TestCase(300000000, 300000000, true)]
        [TestCase(400000000, 300000000, false)]
        [TestCase(0, 0, true)]
        [TestCase(1000000000, 1000000000, true)]
        public void ChronologicalReleaseAndArrivalUseOwnedSlotsWithoutConsumingDeniedRank(long releaseFraction, long arrivalFraction, bool granted)
        {
            var f = Fixture(); var ids = Roster(f); var reservations = Create(f); Fill(reservations, ids);
            var original = reservations.GetRecord(0, 0);
            var preview = reservations.Prepare(new SimulationTick(2),
                new[] { Release(reservations, ids, 0, 0, 1, 2, releaseFraction) },
                new[] { Arrival(reservations, ids, 2, 0, 2, arrivalFraction) });
            Assert.That(reservations.WasPreparedDenied(preview, 2), Is.EqualTo(!granted));
            Assert.That(reservations.GetArrivalCount(0), Is.EqualTo(2));
            Assert.That(reservations.GetRecord(0, 0).IsReleased, Is.False);
            Assert.That(reservations.GetPreparedRecord(preview, 2, 0).IsValid, Is.EqualTo(granted));
            reservations.Commit(preview);
            Assert.That(reservations.GetRecord(0, 0).Arrival, Is.EqualTo(original.Arrival));
            Assert.That(reservations.GetRecord(0, 0).Release.Fraction, Is.EqualTo(releaseFraction));
            Assert.That(reservations.GetArrivalCount(0), Is.EqualTo(granted ? 3 : 2));
            Assert.That(reservations.GetOwnedLane(2), Is.EqualTo(granted ? 1 : 0));
            Assert.That(reservations.GetLastAssignedBout(2), Is.EqualTo(granted ? 0 : -1));
            if (granted)
            {
                Assert.That(reservations.GetRecord(2, 0).ArrivalRank, Is.EqualTo(3));
                Assert.That(reservations.LastChangeCount, Is.EqualTo(2));
                Assert.That(reservations.GetLastChange(0).IsRelease, Is.True);
            }
            else
            {
                Assert.That(reservations.LastChangeCount, Is.EqualTo(1));
                reservations.Commit(reservations.Prepare(new SimulationTick(3), ReadOnlySpan<PhysicalRangeRelease>.Empty,
                    new[] { Arrival(reservations, ids, 2, 0, 3, 100000000) }));
                Assert.That(reservations.GetRecord(2, 0).ArrivalRank, Is.EqualTo(3));
                Assert.That(reservations.GetOwnedLane(2), Is.EqualTo(1));
            }
        }

        [Test] public void ArrivalUsesAnotherFreeLaneAndCopiesCallerReceiptsBeforeCommit()
        {
            var f = Fixture(); var ids = Roster(f); var reservations = Create(f);
            reservations.Commit(reservations.Prepare(new SimulationTick(1), ReadOnlySpan<PhysicalRangeRelease>.Empty,
                new[] { Arrival(reservations, ids, 0, 0, 1, 100000000) }));
            var rows = new[] { Arrival(reservations, ids, 2, 0, 2, 700000000) };
            var saved = rows[0]; var preview = reservations.Prepare(new SimulationTick(2), ReadOnlySpan<PhysicalRangeRelease>.Empty, rows);
            rows[0] = default;
            Assert.That(reservations.GetPreparedRecord(preview, 2, 0).LaneOrdinal, Is.EqualTo(2));
            reservations.Commit(preview);
            Assert.That(reservations.GetRecord(2, 0).Arrival, Is.EqualTo(saved));
            Assert.That(reservations.GetOwnedLane(0), Is.EqualTo(1));
        }

        [Test] public void SequentialBoutsNeedAcceptedReleaseAndRetainHistoricalRecords()
        {
            var f = Fixture(pursuit: true); var ids = Roster(f); var reservations = Create(f);
            for (int bout = 0; bout < f.Profile.BoutCount; bout++)
            {
                long tick = reservations.Tick.Value + 1;
                var rows = new[] { Arrival(reservations, ids, 0, bout, tick, 200000000) };
                var releases = bout == 0 ? Array.Empty<PhysicalRangeRelease>() :
                    new[] { Release(reservations, ids, 0, bout - 1, 1, tick, 100000000) };
                if (bout > 0) Assert.Throws<ArgumentException>(() => reservations.Prepare(new SimulationTick(tick), ReadOnlySpan<PhysicalRangeRelease>.Empty, rows));
                reservations.Commit(reservations.Prepare(new SimulationTick(tick), releases, rows));
                Assert.That(reservations.GetRecord(0, bout).ArrivalRank, Is.EqualTo(1));
                if (bout > 0) Assert.That(reservations.GetRecord(0, bout - 1).IsReleased, Is.True);
            }
            Assert.That(reservations.GetLastAssignedBout(0), Is.EqualTo(3));
            Assert.That(reservations.GetRecord(0, 0).Arrival.Tick.Value, Is.EqualTo(1));
        }

        [Test] public void InvalidReplacementForeignAndConsumedHandlesLeaveCommittedOwnershipUnchanged()
        {
            var f = Fixture(); var ids = Roster(f); var a = Create(f); var b = Create(f); Fill(a, ids); Fill(b, ids);
            var original = a.GetRecord(0, 0);
            var release = Release(a, ids, 0, 0, 1, 2, 100000000);
            var replaced = a.Prepare(new SimulationTick(2), new[] { release }, ReadOnlySpan<PhysicalRangeArrival>.Empty);
            var foreign = b.Prepare(new SimulationTick(2), ReadOnlySpan<PhysicalRangeRelease>.Empty, ReadOnlySpan<PhysicalRangeArrival>.Empty);
            Assert.Throws<ArgumentException>(() => a.Commit(foreign));
            Assert.Throws<ArgumentException>(() => a.Prepare(new SimulationTick(2), new[] { release, release }, ReadOnlySpan<PhysicalRangeArrival>.Empty));
            Assert.Throws<ArgumentException>(() => a.Commit(replaced));
            Assert.That(a.Tick.Value, Is.EqualTo(1)); Assert.That(a.GetOwnedLane(0), Is.EqualTo(1));
            Assert.That(a.GetRecord(0, 0), Is.EqualTo(original)); Assert.That(a.LastChangeCount, Is.EqualTo(2));
            var valid = a.Prepare(new SimulationTick(2), ReadOnlySpan<PhysicalRangeRelease>.Empty, ReadOnlySpan<PhysicalRangeArrival>.Empty);
            a.Commit(valid); Assert.Throws<ArgumentException>(() => a.Commit(valid));
        }

        [Test] public void FullCapacityDenialChangesNoRecordRankOrOwnedLane()
        {
            var f = Fixture(); var ids = Roster(f); var reservations = Create(f); Fill(reservations, ids);
            var preview = reservations.Prepare(new SimulationTick(2), ReadOnlySpan<PhysicalRangeRelease>.Empty,
                new[] { Arrival(reservations, ids, 2, 0, 2, 500000000) });
            Assert.That(reservations.WasPreparedDenied(preview, 2), Is.True); reservations.Commit(preview);
            Assert.That(reservations.GetArrivalCount(0), Is.EqualTo(2)); Assert.That(reservations.LastChangeCount, Is.Zero);
            Assert.That(reservations.GetRecord(2, 0).IsValid, Is.False); Assert.That(reservations.GetOwnedLane(2), Is.Zero);
        }

        [Test] public void ClearanceIsStrictBodyHalfLengthPastPrivateExitAndUsesMinimumRetimedFraction()
        {
            var f = Fixture(); var reservations = Create(f); var ids = Roster(f);
            long half = (long)Math.Ceiling(OccupancyTests.Definition().LongitudinalSeparation * SkiState.NanometersPerMeter / 2);
            long line = reservations.ClearanceRouteNanometers(1, 0);
            Assert.That(line, Is.EqualTo(50 * SkiState.NanometersPerMeter + half + 1));
            long before = line - 1; var initial = new RaceMotionPoint(before, before);
            var stopped = new[] { Segment(before, before, .05) };
            Assert.That(PhysicalRangeRelease.TryFromAcceptedMotion(ids[0], 0, 1, new SimulationTick(1), 20, line, initial, stopped, out _), Is.False);
            var segments = new[] { Segment(before, before + 3, .05) };
            Assert.That(PhysicalRangeRelease.TryFromAcceptedMotion(ids[0], 0, 1, new SimulationTick(1), 20, line, initial, segments, out var release), Is.True);
            Assert.That(release.Fraction, Is.EqualTo(333333334));
            Assert.That(FractionalMotionTimeline.Sample(segments, initial, release.Fraction - 1, .05).RouteNanometers, Is.LessThan(line));
            Assert.That(FractionalMotionTimeline.Sample(segments, initial, release.Fraction, .05).RouteNanometers, Is.EqualTo(line));
        }

        [Test] public void ExactProfileCapacityAndForeignConfigurationRejectAtConstruction()
        {
            var f = Fixture(); var profile = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(f.Profile.ToJson(), "field_capacity", "2"));
            Assert.Throws<ArgumentException>(() => new PhysicalRangeReservations(profile, Roster(f), f.Layout, OccupancyTests.Definition(), 20));
            Assert.That(Create(f).ContextJson, Is.EqualTo(Create(f).ContextJson));
            Assert.That(Create(f).ContextJson, Is.Not.EqualTo(new PhysicalRangeReservations(f.Profile, Roster(f), f.Layout, OccupancyTests.Definition(), 40).ContextJson));
        }

        [Test] public void WarmDeniedAndReleaseAcquisitionPreparationsAllocateNothing()
        {
            var f = Fixture(); var ids = Roster(f); var reservations = Create(f); Fill(reservations, ids);
            var arrivals = new[] { Arrival(reservations, ids, 2, 0, 2, 500000000) };
            var releases = new[] { Release(reservations, ids, 0, 0, 1, 2, 300000000) };
            for (int i = 0; i < 1000; i++) { reservations.Prepare(new SimulationTick(2), ReadOnlySpan<PhysicalRangeRelease>.Empty, arrivals); reservations.Prepare(new SimulationTick(2), releases, arrivals); }
            long before = GC.GetAllocatedBytesForCurrentThread(); PhysicalRangeReservationPreview preview = default;
            for (int i = 0; i < 1000; i++) { reservations.Prepare(new SimulationTick(2), ReadOnlySpan<PhysicalRangeRelease>.Empty, arrivals); preview = reservations.Prepare(new SimulationTick(2), releases, arrivals); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            reservations.Commit(preview); Assert.That(allocated, Is.Zero);
        }
    }
}

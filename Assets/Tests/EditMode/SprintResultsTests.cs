using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintResultsTests
    {
        public static ResultTimingDefinition Timing() => ResultTimingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("result_timing_v1.json"));
        internal static CompetitionLifecycle Finisher(string id, long start, long duration, int fraction = 0)
        {
            var s = new CompetitionLifecycle(CompetitionProfileTests.TestProfile(), new AthleteId(id), 20, new RaceEventLog(40));
            for (long tick = 1; tick < start; tick++) s.Step(new SimulationTick(tick));
            s.Step(new SimulationTick(start), CompetitionAction.Start);
            s.Step(new SimulationTick(start + 1), CompetitionAction.EnterRange);
            s.Step(new SimulationTick(start + 2), CompetitionAction.CompleteBout, 31, 5);
            s.Step(new SimulationTick(start + 3), CompetitionAction.ReachLapBoundary);
            s.Step(new SimulationTick(start + 4), CompetitionAction.EnterRange);
            s.Step(new SimulationTick(start + 5), CompetitionAction.CompleteBout, 31, 5);
            s.Step(new SimulationTick(start + 6), CompetitionAction.ReachLapBoundary);
            long finishTick = start + duration + (fraction > 0 ? 1 : 0);
            for (long tick = start + 7; tick < finishTick; tick++) s.Step(new SimulationTick(tick));
            s.Step(new SimulationTick(finishTick), CompetitionAction.Finish, finish: new FinishTime(start + duration, fraction, 20));
            return s;
        }
        [Test] public void ExactRegistrationBoundariesAndConfiguredRoundingAvoidFloatingPointRanking()
        {
            var t = Timing(); Assert.That(t.Register(new FinishTime(21, 999999999, 20)), Is.EqualTo(10));
            Assert.That(t.Register(new FinishTime(22, 0, 20)), Is.EqualTo(11));
            Assert.That(t.Register(new FinishTime(33, 0, 30)), Is.EqualTo(11));
            var rounded = ResultTimingDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(t.ToJson(), "rounding", "1"));
            Assert.That(rounded.Register(new FinishTime(21, 0, 20)), Is.EqualTo(10));
            Assert.That(rounded.Register(new FinishTime(23, 0, 20)), Is.EqualTo(12));
            var seconds = ResultTimingDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(rounded.ToJson(), "units_per_second", "1"));
            Assert.That(seconds.Register(new FinishTime(30, 0, 20)), Is.EqualTo(2));
            Assert.Catch(() => t.Register(default)); Assert.Catch(() => t.Register(new FinishTime(long.MaxValue, 0, 1)));
            Assert.Catch(() => ResultTimingDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(t.ToJson(), "schema_version", "2")));
            Assert.Catch(() => ResultTimingDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(t.ToJson(), "rounding", "5")));
        }
        [Test] public void EqualRegisteredTimesShareRankAndSkipPlaceRegardlessOfArrivalOrRosterOrder()
        {
            var a = Finisher("a", 601, 200, 900000000); var z = Finisher("z", 1, 201, 500000000); var b = Finisher("b", 1201, 202);
            var ids = new[] { new AthleteId("z"), new AthleteId("b"), new AthleteId("a") };
            var log = new RaceEventLog(10); var book = new SprintResultBook(CompetitionProfileTests.TestProfile(), Timing(), ids, 20, log);
            book.Record(z); Assert.Catch(() => book.Complete("results")); book.Record(a); book.Record(b);
            var snapshot = book.Complete("results");
            Assert.That(snapshot.Get(0).AthleteId, Is.EqualTo("a")); Assert.That(snapshot.Get(1).AthleteId, Is.EqualTo("z"));
            Assert.That(snapshot.Get(0).Rank, Is.EqualTo(1)); Assert.That(snapshot.Get(1).Rank, Is.EqualTo(1));
            Assert.That(snapshot.Get(2).Rank, Is.EqualTo(3)); Assert.That(snapshot.Get(2).GapUnits, Is.EqualTo(1));
            Assert.That(snapshot.Get(0).ElapsedFractionParts, Is.EqualTo(900000000));
            Array.Reverse(ids); var other = new SprintResultBook(CompetitionProfileTests.TestProfile(), Timing(), ids, 20, new RaceEventLog(10));
            other.Record(b); other.Record(a); other.Record(z);
            Assert.That(other.Complete("results").ToJson(), Is.EqualTo(snapshot.ToJson()));
            Assert.That(SprintResultSnapshot.FromJson(snapshot.ToJson()).ToJson(), Is.EqualTo(snapshot.ToJson()));
            Assert.That(log.Count, Is.EqualTo(3)); Assert.That(log[0].EventType, Is.EqualTo(RaceEventType.CompetitionResultRecorded));
        }
        [Test] public void TerminalFailuresHaveNoRankOrInventedTimeAndReceiptValidationIsAtomic()
        {
            var p = CompetitionProfileTests.TestProfile(); var log = new RaceEventLog(20);
            var dns = new CompetitionLifecycle(p, new AthleteId("dns"), 20, log); dns.Step(new SimulationTick(1), CompetitionAction.MarkDidNotStart);
            var dnf = new CompetitionLifecycle(p, new AthleteId("dnf"), 20, log); dnf.Step(new SimulationTick(1), CompetitionAction.Start);
            var invalid = Finisher("invalid", 1, 20); invalid.Step(invalid.Tick.Next(), CompetitionAction.MarkInvalid);
            var records = new RaceEventLog(10);
            var book = new SprintResultBook(p, Timing(), new[] { dns.AthleteId, dnf.AthleteId, invalid.AthleteId }, 20, records);
            Assert.Catch(() => book.Record(dnf)); Assert.That(book.Count, Is.Zero); Assert.That(records.Count, Is.Zero);
            dnf.Step(new SimulationTick(2), CompetitionAction.MarkDidNotFinish);
            book.Record(dns); book.Record(dnf); book.Record(invalid);
            Assert.Catch(() => book.Record(dns)); Assert.That(book.Count, Is.EqualTo(3));
            var snapshot = book.Complete("all_failed");
            for (int i = 0; i < snapshot.Count; i++)
            { var row = snapshot.Get(i); Assert.That(row.Rank, Is.Zero); Assert.That(row.RegisteredUnits, Is.Null);
              Assert.That(row.GapUnits, Is.Null); Assert.That(row.ElapsedWholeTicks, Is.Null); }
            Assert.That(SprintResultSnapshot.FromJson(snapshot.ToJson()).ToJson(), Is.EqualTo(snapshot.ToJson()));
            var changed = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(p.ToJson(), "distance_m", "6000"));
            var foreign = new CompetitionLifecycle(changed, dns.AthleteId, 20, log); foreign.Step(new SimulationTick(1), CompetitionAction.MarkDidNotStart);
            var clean = new SprintResultBook(p, Timing(), new[] { dns.AthleteId }, 20, records);
            Assert.Catch(() => clean.Record(foreign)); Assert.That(clean.Count, Is.Zero);
        }
        [Test] public void SnapshotRejectsAlteredSchemaRankingGapAndRegistration()
        {
            var book = new SprintResultBook(CompetitionProfileTests.TestProfile(), Timing(), new[] { new AthleteId("a") }, 20, new RaceEventLog(10));
            book.Record(Finisher("a", 1, 200)); string json = book.Complete("results").ToJson();
            foreach (var pair in new[] { new[] { "schema_version", "2" }, new[] { "rank", "9" }, new[] { "gap_units", "2" },
                new[] { "registered_units", "0" }, new[] { "status", "3" }, new[] { "elapsed_fraction_parts", "1000000000" } })
                Assert.Catch(() => SprintResultSnapshot.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair[0], pair[1])), pair[0]);
        }
    }
}

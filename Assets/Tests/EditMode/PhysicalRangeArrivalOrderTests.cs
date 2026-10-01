using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangeArrivalOrderTests
    {
        private static readonly AthleteId[] Roster = MassStartGridTests.Roster();
        private static PhysicalRangeArrivalOrder Create(int frequency = 20)
        {
            var profile = MassStartGridTests.Profile();
            var grid = new MassStartGrid(profile, MassStartGridTests.Definition(), OccupancyTests.Definition(), Roster, 6, 30);
            return new PhysicalRangeArrivalOrder(profile, grid,
                MassStartRangePolicyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json")), frequency);
        }
        private static PhysicalRangeArrival Arrival(int athlete, int bout, long tick, long fraction,
            string foreign = null, int frequency = 20)
        {
            double end = fraction == 0 ? 1.0 / frequency : fraction / (double)JunctionAdmissionSession.FractionParts / frequency;
            var segment = (RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment), BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { 0L, 1000L, 0L, 1000L, 0.0, end, false }, null);
            bool found = PhysicalRangeArrival.TryFromAcceptedMotion(foreign == null ? Roster[athlete] : new AthleteId(foreign), bout,
                new SimulationTick(tick), frequency, fraction == 0 ? 0 : 1000, default, new[] { segment }, out var arrival);
            Assert.That(found, Is.True); Assert.That(arrival.Fraction, Is.EqualTo(fraction)); return arrival;
        }
        private static void FirstBout(PhysicalRangeArrivalOrder order, int count = 30)
        {
            var rows = new PhysicalRangeArrival[count];
            for (int i = 0; i < count; i++) rows[i] = Arrival(i, 0, order.Tick.Value + 1, (count - i) * 10000000L);
            order.Commit(order.Prepare(order.Tick.Next(), rows));
        }

        [Test] public void FirstBoutUsesBibAndLaterFractionalArrivalOverridesRosterAndInputOrder()
        {
            var a = Create(); var b = Create(); FirstBout(a); FirstBout(b);
            for (int i = 0; i < 30; i++) Assert.That(a.GetAssignedOrdinal(i, 0), Is.EqualTo(i + 1));
            var rows = new[] { Arrival(0, 1, 2, 900000000), Arrival(2, 1, 2, 100000000), Arrival(1, 1, 2, 100000000) };
            var reverse = new[] { rows[2], rows[1], rows[0] };
            var pa = a.Prepare(new SimulationTick(2), rows); var pb = b.Prepare(new SimulationTick(2), reverse);
            Assert.That(a.Tick.Value, Is.EqualTo(1)); Assert.That(a.GetArrivalCount(1), Is.Zero);
            Assert.That(a.GetAssignedOrdinal(1, 1), Is.Zero);
            Assert.That(a.GetPreparedOrdinal(pa, 1, 1), Is.EqualTo(1));
            Assert.That(a.GetPreparedOrdinal(pa, 2, 1), Is.EqualTo(2));
            Assert.That(a.GetPreparedOrdinal(pa, 0, 1), Is.EqualTo(3));
            a.Commit(pa); b.Commit(pb);
            for (int i = 0; i < 30; i++)
            {
                Assert.That(a.GetAssignedOrdinal(i, 1), Is.EqualTo(b.GetAssignedOrdinal(i, 1)));
                Assert.That(a.GetLastAssignedBout(i), Is.EqualTo(b.GetLastAssignedBout(i)));
            }
            Assert.That(a.GetAcceptedArrival(1, 1).Fraction, Is.EqualTo(100000000));
            Assert.That(a.GetArrivalCount(1), Is.EqualTo(3));
        }

        [Test] public void ReplacedRejectedAndEmptyBatchesConsumeNoOrdinal()
        {
            var order = Create(); FirstBout(order);
            var abandoned = order.Prepare(new SimulationTick(2), new[] { Arrival(0, 1, 2, 100000000) });
            var accepted = order.Prepare(new SimulationTick(2), new[] { Arrival(1, 1, 2, 700000000) });
            Assert.Throws<ArgumentException>(() => order.Commit(abandoned));
            Assert.That(order.GetPreparedOrdinal(accepted, 0, 1), Is.Zero);
            Assert.That(order.GetPreparedOrdinal(accepted, 1, 1), Is.EqualTo(1)); order.Commit(accepted);
            order.Commit(order.Prepare(new SimulationTick(3), ReadOnlySpan<PhysicalRangeArrival>.Empty));
            Assert.That(order.GetArrivalCount(1), Is.EqualTo(1)); Assert.That(order.Tick.Value, Is.EqualTo(3));
            order.Commit(order.Prepare(new SimulationTick(4), new[] { Arrival(0, 1, 4, 500000000) }));
            Assert.That(order.GetAssignedOrdinal(0, 1), Is.EqualTo(2));
        }

        [Test] public void DifferentBoutsKeepIndependentCountersAndSequentialAthleteVisits()
        {
            var order = Create(); FirstBout(order, 2);
            order.Commit(order.Prepare(new SimulationTick(2), new[] { Arrival(0, 1, 2, 500000000) }));
            var preview = order.Prepare(new SimulationTick(3), new[] {
                Arrival(2, 0, 3, 800000000), Arrival(1, 1, 3, 100000000), Arrival(0, 2, 3, 300000000) });
            Assert.That(order.GetPreparedOrdinal(preview, 2, 0), Is.EqualTo(3));
            Assert.That(order.GetPreparedOrdinal(preview, 1, 1), Is.EqualTo(2));
            Assert.That(order.GetPreparedOrdinal(preview, 0, 2), Is.EqualTo(1));
            Assert.That(order.GetPreparedArrivalCount(preview, 0), Is.EqualTo(3));
            Assert.That(order.GetPreparedArrivalCount(preview, 1), Is.EqualTo(2));
            Assert.That(order.GetPreparedArrivalCount(preview, 2), Is.EqualTo(1)); order.Commit(preview);
        }

        [Test] public void ForeignStaleConsumedAndInvalidReplacementHandlesCannotPublish()
        {
            var a = Create(); var b = Create();
            var first = a.Prepare(new SimulationTick(1), ReadOnlySpan<PhysicalRangeArrival>.Empty);
            var foreign = b.Prepare(new SimulationTick(1), ReadOnlySpan<PhysicalRangeArrival>.Empty);
            Assert.Throws<ArgumentException>(() => a.Commit(foreign));
            Assert.Throws<ArgumentException>(() => a.Prepare(new SimulationTick(1), new[] { default(PhysicalRangeArrival) }));
            Assert.Throws<ArgumentException>(() => a.Commit(first));
            Assert.That(a.Tick.Value, Is.Zero); Assert.That(a.GetArrivalCount(0), Is.Zero);
            var valid = a.Prepare(new SimulationTick(1), new[] { Arrival(0, 0, 1, 0) }); a.Commit(valid);
            Assert.Throws<ArgumentException>(() => a.Commit(valid));
            Assert.Throws<ArgumentException>(() => a.GetPreparedOrdinal(valid, 0, 0));
            Assert.Throws<ArgumentException>(() => a.Commit(default));
        }

        [TestCase("duplicate")][TestCase("unknown")][TestCase("clock")][TestCase("tick")]
        [TestCase("skipped")][TestCase("bout")][TestCase("capacity")]
        public void InvalidWholeBatchLeavesAllAuthorityUnchanged(string reason)
        {
            var order = Create(); var good = Arrival(0, 0, 1, 100000000); PhysicalRangeArrival[] rows;
            switch (reason)
            {
                case "duplicate": rows = new[] { good, good }; break;
                case "unknown": rows = new[] { good, Arrival(1, 0, 1, 200000000, "outside_grid") }; break;
                case "clock": rows = new[] { good, Arrival(1, 0, 1, 200000000, frequency: 10) }; break;
                case "tick": rows = new[] { good, Arrival(1, 0, 2, 200000000) }; break;
                case "skipped": rows = new[] { good, Arrival(1, 1, 1, 200000000) }; break;
                case "bout": rows = new[] { good, Arrival(1, order.BoutCount, 1, 200000000) }; break;
                default: rows = new PhysicalRangeArrival[31]; for (int i = 0; i < rows.Length; i++) rows[i] = good; break;
            }
            var old = order.Prepare(new SimulationTick(1), new[] { good });
            Assert.Throws<ArgumentException>(() => order.Prepare(new SimulationTick(1), rows));
            Assert.Throws<ArgumentException>(() => order.Commit(old));
            Assert.That(order.Tick.Value, Is.Zero); Assert.That(order.GetArrivalCount(0), Is.Zero);
            for (int i = 0; i < 30; i++)
            { Assert.That(order.GetAssignedOrdinal(i, 0), Is.Zero); Assert.That(order.GetLastAssignedBout(i), Is.EqualTo(-1)); }
        }

        [Test] public void EndpointAndFullCapacityVisitsAreRecordedOnceAcrossRepeatedPermutations()
        {
            var a = Create(); var b = Create();
            for (int bout = 0; bout < a.BoutCount; bout++)
            {
                var rows = new PhysicalRangeArrival[30]; var reverse = new PhysicalRangeArrival[30];
                for (int i = 0; i < rows.Length; i++)
                {
                    rows[i] = Arrival(i, bout, bout + 1, (i + 1) * (JunctionAdmissionSession.FractionParts / 30));
                    if (i == 29) rows[i] = Arrival(i, bout, bout + 1, JunctionAdmissionSession.FractionParts);
                    reverse[29 - i] = rows[i];
                }
                a.Commit(a.Prepare(a.Tick.Next(), rows)); b.Commit(b.Prepare(b.Tick.Next(), reverse));
                Assert.That(a.GetArrivalCount(bout), Is.EqualTo(30));
                for (int i = 0; i < 30; i++)
                {
                    Assert.That(a.GetAssignedOrdinal(i, bout), Is.EqualTo(i + 1));
                    Assert.That(a.GetAssignedOrdinal(i, bout), Is.EqualTo(b.GetAssignedOrdinal(i, bout)));
                    Assert.That(a.GetAcceptedArrival(i, bout), Is.EqualTo(b.GetAcceptedArrival(i, bout)));
                }
            }
            Assert.Throws<ArgumentException>(() => a.Prepare(a.Tick.Next(), new[] { Arrival(0, 3, 5, 500000000) }));
            Assert.That(a.GetArrivalCount(3), Is.EqualTo(30)); Assert.That(a.Tick.Value, Is.EqualTo(4));
        }

        [Test] public void WarmPreparationAndEmptyTickCommitsAllocateNothing()
        {
            var order = Create(); var rows = new[] { Arrival(0, 0, 1, 100000000), Arrival(1, 0, 1, 300000000) };
            for (int i = 0; i < 1000; i++) order.Prepare(new SimulationTick(1), rows);
            long before = GC.GetAllocatedBytesForCurrentThread(); PhysicalRangeArrivalPreview preview = default;
            for (int i = 0; i < 1000; i++) preview = order.Prepare(new SimulationTick(1), rows);
            long preparationBytes = GC.GetAllocatedBytesForCurrentThread() - before; order.Commit(preview);
            for (int i = 0; i < 1000; i++) order.Commit(order.Prepare(order.Tick.Next(), ReadOnlySpan<PhysicalRangeArrival>.Empty));
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) order.Commit(order.Prepare(order.Tick.Next(), ReadOnlySpan<PhysicalRangeArrival>.Empty));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(preparationBytes, Is.Zero);
            Assert.That(order.GetArrivalCount(0), Is.EqualTo(2));
        }

        [Test] public void ConstructorContextAndQueryBoundsRejectForeignConfiguration()
        {
            var profile = MassStartGridTests.Profile(); var grid = MassStartGridTests.Grid();
            var policy = MassStartRangePolicyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json"));
            Assert.Throws<ArgumentException>(() => new PhysicalRangeArrivalOrder(profile, grid, policy, 0));
            Assert.Throws<ArgumentException>(() => new PhysicalRangeArrivalOrder(CompetitionProfileTests.TestProfile(), grid, policy, 20));
            var changed = CompetitionProfile.FromJson(profile.ToJson().Replace(profile.Id, "other_mass_profile"));
            Assert.Throws<ArgumentException>(() => new PhysicalRangeArrivalOrder(changed, grid, policy, 20));
            Assert.Throws<ArgumentNullException>(() => new PhysicalRangeArrivalOrder(null, grid, policy, 20));
            var a = Create(); Assert.That(a.ContextJson, Is.EqualTo(Create().ContextJson));
            Assert.That(a.ContextJson, Is.Not.EqualTo(Create(10).ContextJson));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.GetAssignedOrdinal(30, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => a.GetArrivalCount(-1));
            Assert.Throws<InvalidOperationException>(() => a.GetAcceptedArrival(0, 0));
            Assert.Throws<ArgumentException>(() => a.Prepare(new SimulationTick(2), ReadOnlySpan<PhysicalRangeArrival>.Empty));
        }
    }
}

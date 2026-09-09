using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class OccupancyTests
    {
        public static OccupancyDefinition Definition() => OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        [Test]
        public void BodiesRespectWidthClearanceAndCorridorIdentity()
        {
            var d = Definition(); var system = new OccupancySystem(d);
            Assert.That(system.FitsWidth(1 - d.EdgeMargin, 2), Is.True);
            Assert.That(system.FitsWidth(1 - d.EdgeMargin + 0.001, 2), Is.False);
            Assert.That(system.FitsWidth(0, d.EdgeMargin), Is.False);
            var a = DraftingTests.Participant("a", 10);
            Assert.That(system.Overlaps(a, DraftingTests.Participant("b", 10, lateral: d.LateralSeparation)), Is.False);
            Assert.That(system.Overlaps(a, DraftingTests.Participant("b", 10, lateral: d.LateralSeparation - 0.001)), Is.True);
            Assert.That(system.Overlaps(a, DraftingTests.Participant("b", 10, corridor: "other")), Is.False);
            Assert.That(system.QueryMove(a, DraftingTests.Participant("a", 11, 1), 2, Array.Empty<SkiPlacement>()).OutsideTrack, Is.True);
        }
        [Test]
        public void ContinuousSweepPreventsLongitudinalAndLateralTunnelling()
        {
            var system = new OccupancySystem(Definition());
            var a = DraftingTests.Participant("a", 10); var end = DraftingTests.Participant("a", 20);
            var blocker = DraftingTests.Participant("b", 15);
            Assert.That(system.Overlaps(a, blocker), Is.False); Assert.That(system.Overlaps(end, blocker), Is.False);
            var hit = system.QueryMove(a, end, 6, new[] { blocker });
            Assert.That(hit.Blocked, Is.True); Assert.That(hit.FirstContactFraction, Is.EqualTo(0.35).Within(1e-12));
            var left = DraftingTests.Participant("a", 10, -1); var right = DraftingTests.Participant("a", 10, 1);
            Assert.That(system.QueryMove(left, right, 6, new[] { DraftingTests.Participant("b", 10) }).Blocked, Is.True);
            Assert.That(system.QueryMove(left, DraftingTests.Participant("a", 20, -1), 6, new[] { blocker }).Blocked, Is.False);
        }
        [Test]
        public void TouchingClearanceCanMoveAwayAndEarliestBlockerIsOrderIndependent()
        {
            var d = Definition(); var s = new OccupancySystem(d);
            var a = DraftingTests.Participant("a", 10);
            var behind = DraftingTests.Participant("behind", 10 - d.LongitudinalSeparation);
            Assert.That(s.QueryMove(a, DraftingTests.Participant("a", 11), 6, new[] { behind }).Blocked, Is.False);
            var near = DraftingTests.Participant("near", 13); var far = DraftingTests.Participant("far", 16);
            var end = DraftingTests.Participant("a", 20);
            Assert.That(s.QueryMove(a, end, 6, new[] { far, near }).BlockerId.Value, Is.EqualTo("near"));
            Assert.That(s.QueryMove(a, end, 6, new[] { near, far }).FirstContactFraction,
                Is.EqualTo(s.QueryMove(a, end, 6, new[] { far, near }).FirstContactFraction));
            var tie = DraftingTests.Participant("alpha", 13);
            Assert.That(s.QueryMove(a, end, 6, new[] { near, tie }).BlockerId.Value, Is.EqualTo("alpha"));
        }
        [Test]
        public void RosterAndDefinitionsRejectInvalidInputs()
        {
            var d = Definition(); var s = new OccupancySystem(d); var a = DraftingTests.Participant("a", 10);
            Assert.That(OccupancyDefinition.FromJson(d.ToJson()).ToJson(), Is.EqualTo(d.ToJson()));
            Assert.Catch(() => OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(), "body_width_m", "0")));
            Assert.Catch(() => s.ValidateRoster(new[] { a, a }));
            Assert.Catch(() => s.ValidateRoster(new[] { a, DraftingTests.Participant("b", 10) }));
            Assert.Catch(() => s.ValidateRoster(new SkiPlacement[1]));
            Assert.Catch(() => s.QueryMove(a, DraftingTests.Participant("b", 11), 6, Array.Empty<SkiPlacement>()));
            Assert.Catch(() => s.FitsWidth(double.NaN, 6));
            s.ValidateRoster(new[] { a, DraftingTests.Participant("b", 12), DraftingTests.Participant("c", 10, 1) });
        }
        [Test]
        public void DenseQueriesAllocateNothingAndLargeDistancesRetainPrecision()
        {
            var system = new OccupancySystem(Definition());
            var field = new SkiPlacement[100];
            for (int i = 0; i < field.Length; i++) field[i] = DraftingTests.Participant("p" + i, 20 + i * 2);
            system.ValidateRoster(field);
            var a = DraftingTests.Participant("a", 10); var end = DraftingTests.Participant("a", 30);
            system.QueryMove(a, end, 6, field); int count = 0; long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) if (system.QueryMove(a, end, 6, field).Blocked) count++;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(count, Is.EqualTo(10000));
            long origin = long.MaxValue - 10000000000L;
            var farA = new SkiPlacement(new AthleteId("a"), "far", new SkiState(origin, 10), 0);
            var farB = new SkiPlacement(new AthleteId("b"), "far", new SkiState(origin + 1499999999, 10), 0);
            Assert.That(system.Overlaps(farA, farB), Is.True, "One nanometre inside clearance remains visible far from origin.");
        }
    }
}

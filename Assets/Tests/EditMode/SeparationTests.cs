using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SeparationTests
    {
        private static SeparationResolver Resolver(int count, RaceEventLog log) => new SeparationResolver(new OccupancySystem(OccupancyTests.Definition()), count, 20, log);
        private static SkiPlacement Move(SkiPlacement p, double meters, double? lateral = null) => new SkiPlacement(p.AthleteId, p.CorridorId,
            new SkiState(checked(p.Motion.DistanceNanometers + (long)(meters * SkiState.NanometersPerMeter)), p.Motion.SpeedMetersPerSecond), lateral ?? p.LateralMeters);
        private static void AssertSafe(SkiPlacement[] before, SkiPlacement[] after)
        {
            var occupancy = new OccupancySystem(OccupancyTests.Definition()); occupancy.ValidateRoster(after);
            for (int i = 0; i < before.Length; i++)
                for (int j = 0; j < i; j++)
                    Assert.That(occupancy.QueryRelativeMove(before[i], after[i], before[j], after[j]).Blocked, Is.False, $"pair {i}/{j}");
        }
        [Test]
        public void RearEndCatchIsLimitedAndContactEpisodesAreRecordedOnce()
        {
            var log = new RaceEventLog(100); var resolver = Resolver(2, log);
            var before = new[] { DraftingTests.Participant("follower", 98), DraftingTests.Participant("leader", 100) };
            var proposed = new[] { Move(before[0], 5), Move(before[1], 1) }; var after = new SkiPlacement[2]; var widths = new[] { 6.0, 6.0 };
            resolver.Resolve(new SimulationTick(1), before, proposed, widths, after); AssertSafe(before, after);
            Assert.That(after[1].Motion.DistanceMeters, Is.EqualTo(101));
            Assert.That(after[0].Motion.DistanceMeters, Is.EqualTo(99.5).Within(1e-8));
            Assert.That(resolver.GetContact(0).BlockerId.Value, Is.EqualTo("leader"));
            Assert.That(log.Count, Is.EqualTo(1)); Assert.That(log[0].EventType, Is.EqualTo(RaceEventType.ContactStarted));
            var next = new[] { Move(after[0], 1), Move(after[1], 1) };
            resolver.Resolve(new SimulationTick(2), after, next, widths, before); AssertSafe(after, before);
            Assert.That(log.Count, Is.EqualTo(2)); Assert.That(log[1].EventType, Is.EqualTo(RaceEventType.ContactEnded));
        }
        [Test]
        public void SimultaneousLaneSwapCannotCrossAndInputOrderDoesNotChangePlacements()
        {
            var before = new[] { DraftingTests.Participant("a", 10, -1), DraftingTests.Participant("b", 10, 1) };
            var wish = new[] { Move(before[0], 0, 1), Move(before[1], 0, -1) }; var widths = new[] { 6.0, 6.0 }; var result = new SkiPlacement[2];
            Resolver(2, new RaceEventLog(100)).Resolve(new SimulationTick(1), before, wish, widths, result); AssertSafe(before, result);
            Assert.That(result[0].LateralMeters, Is.LessThan(result[1].LateralMeters));
            var reversed = new SkiPlacement[2];
            Resolver(2, new RaceEventLog(100)).Resolve(new SimulationTick(1), new[] { before[1], before[0] }, new[] { wish[1], wish[0] }, widths, reversed);
            Assert.That(reversed[1], Is.EqualTo(result[0])); Assert.That(reversed[0], Is.EqualTo(result[1]));
        }
        [Test]
        public void EdgeClampingAndDifferentCorridorsAreIndependent()
        {
            var before = new[] { DraftingTests.Participant("a", 10), DraftingTests.Participant("b", 10, corridor: "parallel") };
            var proposed = new[] { Move(before[0], 1, 5), Move(before[1], 2) }; var output = new SkiPlacement[2];
            var resolver = Resolver(2, new RaceEventLog(100)); resolver.Resolve(new SimulationTick(1), before, proposed, new[] { 2.0, 2.0 }, output);
            Assert.That(output[0].LateralMeters, Is.EqualTo(0.6).Within(1e-9));
            Assert.That(resolver.GetContact(0).Reason, Is.EqualTo(ContactReason.TrackEdge));
            Assert.That(output[1], Is.EqualTo(proposed[1])); AssertSafe(before, output);
        }
        [Test]
        public void InvalidFrameDoesNotMutateOutputTickOrEvents()
        {
            var log = new RaceEventLog(100); var resolver = Resolver(2, log);
            var before = new[] { DraftingTests.Participant("a", 10), DraftingTests.Participant("b", 12) }; var output = (SkiPlacement[])before.Clone();
            var invalid = new[] { Move(before[0], 1), DraftingTests.Participant("wrong", 13) };
            Assert.Catch(() => resolver.Resolve(new SimulationTick(1), before, invalid, new[] { 6.0, 6.0 }, output));
            Assert.That(output, Is.EqualTo(before)); Assert.That(log.Count, Is.Zero); Assert.That(resolver.Tick.Value, Is.Zero);
            Assert.Catch(() => resolver.Resolve(new SimulationTick(1), before, new[] { Move(before[0], -1), before[1] }, new[] { 6.0, 6.0 }, output));
            Assert.That(resolver.Count, Is.Zero);
        }
        [Test]
        public void DenseRepeatedResolutionIsSafeAndAllocationFree()
        {
            const int count = 8;
            var resolver = Resolver(count, new RaceEventLog(1000)); var occupancy = new OccupancySystem(OccupancyTests.Definition());
            var state = new SkiPlacement[count]; var desired = new SkiPlacement[count]; var result = new SkiPlacement[count]; var widths = new double[count];
            for (int i = 0; i < count; i++) { state[i] = DraftingTests.Participant("p" + i, 20 + i * 2); desired[i] = Move(state[i], 0.5); widths[i] = 6; }
            resolver.Resolve(new SimulationTick(1), state, desired, widths, result);
            Array.Copy(result, state, count);
            long beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 2; tick <= 501; tick++)
            {
                for (int i = 0; i < count; i++) desired[i] = Move(state[i], i == count - 1 ? 0.4 : 0.6);
                resolver.Resolve(new SimulationTick(tick), state, desired, widths, result);
                for (int i = 0; i < count; i++)
                    for (int j = 0; j < i; j++)
                        if (occupancy.QueryRelativeMove(state[i], result[i], state[j], result[j]).Blocked) throw new Exception("Resolved trajectories overlap.");
                Array.Copy(result, state, count);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocation;
            Assert.That(allocated, Is.Zero); occupancy.ValidateRoster(result);
            Assert.That(result[0].Motion.DistanceMeters, Is.GreaterThan(200));
        }
    }
}

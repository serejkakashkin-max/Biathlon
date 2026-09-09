using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalPerceptionTests
    {
        [Test] public void SharedDraftGeometryAndStableNeighborsIgnoreOtherBranchesAndInputOrder()
        {
            var definition = DraftingTests.Definition(); var source = new PhysicalPerceptionSource(definition);
            var self = DraftingTests.Participant("self", 10); var a = DraftingTests.Participant("a", 12);
            var b = DraftingTests.Participant("b", 8); var other = DraftingTests.Participant("branch", 11, corridor: "penalty");
            var rows = new[] { self, b, other, a }; var output = new AiPhysicalNeighbor[3]; var radius = TrackDistance.FromMeters(20);
            var state = source.Capture(new SimulationTick(7), 0, rows, DraftingTests.Flat(), radius, output);
            Assert.That(state.Available, Is.True); Assert.That(state.NeighborCount, Is.EqualTo(2));
            Assert.That(output[0].AthleteId.Value, Is.EqualTo("a")); Assert.That(output[0].GapNanometers, Is.EqualTo(2000000000L));
            Assert.That(output[1].AthleteId.Value, Is.EqualTo("b")); Assert.That(output[1].GapNanometers, Is.EqualTo(-2000000000L));
            Assert.That(state.Draft, Is.EqualTo(new DraftingSystem(definition).Evaluate(self, rows, DraftingTests.Flat())));
            var reordered = source.Capture(new SimulationTick(7), 1, new[] { a, self, b, other }, DraftingTests.Flat(), radius, output);
            Assert.That(reordered, Is.EqualTo(state)); Assert.That(output[0].AthleteId.Value, Is.EqualTo("a"));
        }
        [Test] public void InvalidAndStalePhysicalSnapshotsFailWithoutOutputMutation()
        {
            var source = new PhysicalPerceptionSource(DraftingTests.Definition()); var self = DraftingTests.Participant("athlete_0", 0);
            var output = new AiPhysicalNeighbor[1]; var track = DraftingTests.Flat();
            Assert.Catch(() => source.Capture(default, 0, new[] { self, self }, track, default, output));
            Assert.That(output[0].AthleteId.IsValid, Is.False);
            Assert.Catch(() => source.Capture(default, 0, new[] { self }, track, default, output, double.NaN));
            var field = SprintFieldTests.Create(new RaceEventLog(4000), 1); var race = new SprintPerceptionSource(field)
                .Capture(0, default, Array.Empty<TrackDistance>(), Array.Empty<AiRouteLookahead>(), Array.Empty<AiRouteNeighbor>());
            var current = source.Capture(default, 0, new[] { self }, track, default, output); current.ValidateAgainst(race);
            Assert.Catch(() => source.Capture(new SimulationTick(1), 0, new[] { self }, track, default, output).ValidateAgainst(race));
            Assert.Catch(() => default(AiPhysicalPerception).ValidateAgainst(race));
        }
        [Test] public void PhysicalCaptureDoesNotAllocateInWarmedLoop()
        {
            var source = new PhysicalPerceptionSource(DraftingTests.Definition());
            var rows = new[] { DraftingTests.Participant("self", 10), DraftingTests.Participant("a", 12) };
            var output = new AiPhysicalNeighbor[1]; var track = DraftingTests.Flat(); var radius = TrackDistance.FromMeters(10);
            source.Capture(default, 0, rows, track, radius, output);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) source.Capture(new SimulationTick(i), 0, rows, track, radius, output);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

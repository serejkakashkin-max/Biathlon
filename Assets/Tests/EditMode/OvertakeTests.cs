using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class OvertakeTests
    {
        public static OvertakeDefinition Definition() => OvertakeDefinition.FromJson(PhysiologyContractTests.DefinitionJson("overtake_provisional_v1.json"));
        private static OvertakeController Controller(RaceEventLog log, OvertakeDefinition definition = null) =>
            new OvertakeController(definition ?? Definition(), new OccupancySystem(OccupancyTests.Definition()), new AthleteId("a"), 20, log);
        private static OvertakeOutput Step(OvertakeController c, SkiPlacement position, SkiPlacement[] field, bool request = false, bool cancel = false, int effort = 800) =>
            c.Step(new SimulationInput(c.Tick.Next(), position.AthleteId, effort), position, DraftingTests.Flat(), field, request, cancel);
        private static SkiPlacement Apply(SkiPlacement p, OvertakeOutput output, double? distance = null) =>
            new SkiPlacement(p.AthleteId, p.CorridorId, new SkiState(distance.HasValue ? (long)(distance.Value * SkiState.NanometersPerMeter) : p.Motion.DistanceNanometers, p.Motion.SpeedMetersPerSecond), output.DesiredLateralMeters);
        [Test]
        public void PassMovesOutWaitsForClearanceThenReturnsAndRecordsEvents()
        {
            var log = new RaceEventLog(100); var c = Controller(log); var p = DraftingTests.Participant("a", 10);
            var field = new[] { DraftingTests.Participant("b", 15) };
            var first = Step(c, p, field, request: true);
            Assert.That(first.Phase, Is.EqualTo(OvertakePhase.MovingOut));
            Assert.That(first.EffectiveEffortControl, Is.GreaterThan(800));
            Assert.That(first.PropulsionMultiplier, Is.LessThan(1));
            p = Apply(p, first);
            for (int i = 0; i < 30; i++) p = Apply(p, Step(c, p, field));
            Assert.That(c.Phase, Is.EqualTo(OvertakePhase.Passing));
            Assert.That(p.LateralMeters, Is.LessThan(0));
            p = DraftingTests.Participant("a", 16, p.LateralMeters);
            Assert.That(Step(c, p, field).Phase, Is.EqualTo(OvertakePhase.Passing), "Do not cut in before full body clearance.");
            p = DraftingTests.Participant("a", 18, p.LateralMeters);
            for (int i = 0; i < 30; i++) p = Apply(p, Step(c, p, field));
            Assert.That(c.Phase, Is.EqualTo(OvertakePhase.Idle)); Assert.That(p.LateralMeters, Is.Zero);
            Assert.That(log.Count, Is.EqualTo(2)); Assert.That(log[0].EventType, Is.EqualTo(RaceEventType.OvertakeStarted));
            Assert.That(log[1].EventType, Is.EqualTo(RaceEventType.OvertakeCompleted));
        }
        [Test]
        public void OccupiedSidesAndRestrictedSegmentsPreventInitiation()
        {
            var log = new RaceEventLog(100); var c = Controller(log); var p = DraftingTests.Participant("a", 10);
            var field = new[] { DraftingTests.Participant("b", 15), DraftingTests.Participant("left", 13, -0.8), DraftingTests.Participant("right", 13, 0.8) };
            var output = Step(c, p, field, true);
            Assert.That(output.Blocked, Is.True); Assert.That(c.Phase, Is.EqualTo(OvertakePhase.Idle)); Assert.That(log.Count, Is.Zero);
            var restricted = c.Step(new SimulationInput(c.Tick.Next(), p.AthleteId, 800), p, TuckTests.Descent(), new[] { field[0] }, true);
            Assert.That(restricted.Phase, Is.EqualTo(OvertakePhase.Idle));
            var right = Step(c, p, new[] { field[0], field[1] }, true);
            Assert.That(right.DesiredLateralMeters, Is.GreaterThan(0));
        }
        [Test]
        public void CancellationAndTimeoutReturnSafelyWithoutFalseCompletion()
        {
            foreach (bool cancel in new[] { false, true })
            {
                var log = new RaceEventLog(100);
                var definition = OvertakeDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "timeout_seconds", "0.2"));
                var c = Controller(log, definition); var p = DraftingTests.Participant("a", 10);
                var field = new[] { DraftingTests.Participant("b", 15) };
                p = Apply(p, Step(c, p, field, true));
                p = Apply(p, Step(c, p, field, cancel: cancel));
                for (int i = 0; i < 30; i++) p = Apply(p, Step(c, p, field));
                Assert.That(c.Phase, Is.EqualTo(OvertakePhase.Idle)); Assert.That(p.LateralMeters, Is.Zero);
                Assert.That(log.Count, Is.EqualTo(2)); Assert.That(log[1].EventType, Is.EqualTo(RaceEventType.OvertakeAborted));
            }
        }
        [Test]
        public void BlockedReturnWaitsAndMaximumEffortStillPaysCoordinationCost()
        {
            var log = new RaceEventLog(100); var c = Controller(log); var p = DraftingTests.Participant("a", 10);
            var field = new[] { DraftingTests.Participant("b", 15) };
            var first = Step(c, p, field, true, effort: 1000);
            Assert.That(first.EffectiveEffortControl, Is.EqualTo(1000)); Assert.That(first.PropulsionMultiplier, Is.LessThan(1));
            p = Apply(p, first);
            for (int i = 0; i < 30; i++) p = Apply(p, Step(c, p, field));
            p = DraftingTests.Participant("a", 18, p.LateralMeters);
            var blockedField = new[] { field[0], DraftingTests.Participant("blocker", 18, 0) };
            OvertakeOutput output = default;
            for (int i = 0; i < 10; i++) { output = Step(c, p, blockedField); p = Apply(p, output); }
            Assert.That(output.Blocked, Is.True); Assert.That(c.Phase, Is.EqualTo(OvertakePhase.Returning));
            Assert.That(new OccupancySystem(OccupancyTests.Definition()).Overlaps(p, blockedField[1]), Is.False);
        }
        [Test]
        public void ActivePassIsAllocationFreeAndReplayingPlacementsPreservesEvents()
        {
            var d = OvertakeDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "timeout_seconds", "1000"));
            var logA = new RaceEventLog(100); var logB = new RaceEventLog(100);
            var a = Controller(logA, d); var b = Controller(logB, d);
            var p = DraftingTests.Participant("a", 10); var field = new[] { DraftingTests.Participant("b", 15) }; var sample = DraftingTests.Flat();
            for (int i = 0; i < 30; i++)
            {
                var input = new SimulationInput(a.Tick.Next(), p.AthleteId, 800);
                var x = a.Step(input, p, sample, field, i == 0);
                var y = b.Step(input, p, sample, field, i == 0);
                Assert.That(x.DesiredLateralMeters, Is.EqualTo(y.DesiredLateralMeters)); p = Apply(p, x);
            }
            Assert.That(a.Phase, Is.EqualTo(OvertakePhase.Passing));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                var input = new SimulationInput(a.Tick.Next(), p.AthleteId, 800);
                a.Step(input, p, sample, field, false); b.Step(input, p, sample, field, false);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(a.Phase, Is.EqualTo(b.Phase));
            Assert.That(logA.Count, Is.EqualTo(logB.Count)); Assert.That(logA[0], Is.EqualTo(logB[0]));
        }
        [Test]
        public void InvalidInputIsAtomicAndIdleTickLoopDoesNotAllocate()
        {
            var log = new RaceEventLog(100); var c = Controller(log); var p = DraftingTests.Participant("a", 10);
            var field = Array.Empty<SkiPlacement>(); var sample = DraftingTests.Flat();
            Assert.Catch(() => c.Step(new SimulationInput(new SimulationTick(2), p.AthleteId, 800), p, sample, field, true));
            Assert.Catch(() => c.Step(new SimulationInput(new SimulationTick(1), p.AthleteId, 800), p, sample, new SkiPlacement[1], true));
            Assert.That(c.Tick.Value, Is.Zero); Assert.That(c.Phase, Is.EqualTo(OvertakePhase.Idle)); Assert.That(log.Count, Is.Zero);
            c.Step(new SimulationInput(c.Tick.Next(), p.AthleteId, 800), p, sample, field, false);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) c.Step(new SimulationInput(c.Tick.Next(), p.AthleteId, 800), p, sample, field, false);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(OvertakeDefinition.FromJson(Definition().ToJson()).ToJson(), Is.EqualTo(Definition().ToJson()));
            Assert.Catch(() => OvertakeDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "lateral_speed_mps", "0")));
        }
    }
}

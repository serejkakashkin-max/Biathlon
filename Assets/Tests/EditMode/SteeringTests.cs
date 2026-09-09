using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SteeringTests
    {
        public static SteeringDefinition Definition() => SteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("steering_provisional_v1.json"));
        private static SteeringSystem System() => new SteeringSystem(Definition(), new OccupancySystem(OccupancyTests.Definition()), 20);
        private static PlayerCommand Command(SkiPlacement p, int intent) => new PlayerCommand(new SimulationTick(1), p.AthleteId, 800, intent);
        [Test]
        public void LineIntentMapsUsableWidthAndMovesSymmetricallyWithoutTeleportation()
        {
            var s = System(); var p = DraftingTests.Participant("a", 10); var track = DraftingTests.Flat();
            var left = s.Evaluate(Command(p, -1000), p, track, Array.Empty<SkiPlacement>());
            var right = s.Evaluate(Command(p, 1000), p, track, Array.Empty<SkiPlacement>());
            Assert.That(left.TargetLateralMeters, Is.EqualTo(-right.TargetLateralMeters));
            Assert.That(left.ProposedPlacement.LateralMeters, Is.EqualTo(-right.ProposedPlacement.LateralMeters));
            Assert.That(right.ProposedPlacement.LateralMeters, Is.LessThan(right.TargetLateralMeters));
            for (int i = 0; i < 100; i++) p = s.Evaluate(Command(p, 1000), p, track, Array.Empty<SkiPlacement>()).ProposedPlacement;
            Assert.That(p.LateralMeters, Is.EqualTo(right.TargetLateralMeters));
            Assert.That(new OccupancySystem(OccupancyTests.Definition()).FitsWidth(p.LateralMeters, track.WidthMeters), Is.True);
            var centered = DraftingTests.Participant("a", 10);
            Assert.That(s.Evaluate(Command(centered, 1000), centered, track, Array.Empty<SkiPlacement>(), corridorMinimumWidth: 2).TargetLateralMeters,
                Is.EqualTo(0.6).Within(1e-9), "Conservative corridor width anticipates downstream narrowing.");
        }
        [Test]
        public void TuckAndHighSpeedReduceResponseAndMovementCostsPropulsion()
        {
            var s = System(); var p = DraftingTests.Participant("a", 10); var track = DraftingTests.Flat();
            var normal = s.Evaluate(Command(p, 1000), p, track, Array.Empty<SkiPlacement>());
            var tucked = s.Evaluate(Command(p, 1000), p, track, Array.Empty<SkiPlacement>(), TuckTests.Definition().SteeringResponseMultiplier);
            Assert.That(tucked.ProposedPlacement.LateralMeters, Is.LessThan(normal.ProposedPlacement.LateralMeters));
            var fast = DraftingTests.Participant("a", 10, speed: 20);
            Assert.That(s.Evaluate(Command(fast, 1000), fast, track, Array.Empty<SkiPlacement>()).ProposedPlacement.LateralMeters, Is.LessThan(normal.ProposedPlacement.LateralMeters));
            Assert.That(normal.PropulsionMultiplier, Is.LessThan(1)); Assert.That(normal.LineQuality, Is.LessThan(1));
            Assert.That(s.Evaluate(Command(p, 0), p, track, Array.Empty<SkiPlacement>()).LineQuality, Is.EqualTo(1));
        }
        [Test]
        public void OccupiedLaneStopsSteeringAndCenterAssistHasExplicitDeadzone()
        {
            var s = System(); var p = DraftingTests.Participant("a", 10); var blocker = DraftingTests.Participant("b", 10, 0.7); var track = DraftingTests.Flat();
            var output = s.Evaluate(Command(p, 1000), p, track, new[] { blocker });
            Assert.That(output.Blocked, Is.True); Assert.That(output.ProposedPlacement, Is.EqualTo(p)); Assert.That(output.PropulsionMultiplier, Is.EqualTo(1));
            Assert.That(s.Evaluate(Command(p, 40), p, track, Array.Empty<SkiPlacement>(), assistedCenter: true).TargetLateralMeters, Is.Zero);
            Assert.That(s.Evaluate(Command(p, 40), p, track, Array.Empty<SkiPlacement>()).TargetLateralMeters, Is.GreaterThan(0));
        }
        [Test]
        public void EvaluationIsAllocationFreeAndInvalidInputsFail()
        {
            var s = System(); var p = DraftingTests.Participant("a", 10); var command = Command(p, 1000); var track = DraftingTests.Flat(); var roster = Array.Empty<SkiPlacement>();
            Assert.Catch(() => s.Evaluate(default, p, track, roster));
            Assert.Catch(() => s.Evaluate(command, p, track, roster, double.NaN));
            Assert.That(SteeringDefinition.FromJson(Definition().ToJson()).ToJson(), Is.EqualTo(Definition().ToJson()));
            Assert.Catch(() => SteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "assisted_center_deadzone", "1000")));
            s.Evaluate(command, p, track, roster); long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) p = s.Evaluate(command, p, track, roster).ProposedPlacement;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before; Assert.That(allocated, Is.Zero);
        }
    }
}

using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingAiTests
    {
        public static ShootingAiDefinition Definition() => ShootingAiDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_ai_v1.json"));
        private static readonly AthleteId Athlete = new AthleteId("ai_shooter");
        private static ShootingAiController Create(ShootingAiDefinition d = null) => new ShootingAiController(d ?? Definition(), TargetGeometryTests.Definition(),
            WeaponAimTests.Definition(), ShootingStanceTests.Definition().Standing, Athlete, 20, new SeededRandomSource(123));
        [Test] public void PerfectControllerUsesSharedAimAndGeometryToCompleteFiveTargets()
        {
            var d = ShootingAiDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "tracking_error_rad", "0"));
            var ai = Create(d); var stance = ShootingStanceTests.Definition().Standing;
            var aim = new WeaponAimSystem(WeaponAimTests.Definition(), Athlete, 20, stance: stance);
            var shots = new ShotResolutionSystem(ShootingStateTests.Definition(), TargetGeometryTests.Definition(), ShootingWindTests.Definition(), stance,
                Athlete, "range", 20, new RaceEventLog(100));
            for (int i = 1; i <= 3000 && shots.Phase != ShootingPhase.Complete; i++)
            {
                var tick = new SimulationTick(i);
                var command = ai.Step(tick, new ShootingAiObservation(shots.Phase, shots.ClosedMask, shots.ShotsFired, aim.State.YawRadians, aim.State.PitchRadians));
                var ray = aim.Step(command.Aim).Ray;
                var action = i == 1 ? ShootingAction.EnterRange : i == 2 ? ShootingAction.AssignLane : command.Trigger ? ShootingAction.Trigger : ShootingAction.None;
                shots.Step(tick, action, i == 2 ? new ShootingLaneAssignment("range", "lane", 1) : default, ray);
            }
            Assert.That(shots.Phase, Is.EqualTo(ShootingPhase.Complete)); Assert.That(shots.ShotsFired, Is.EqualTo(5)); Assert.That(shots.ClosedMask, Is.EqualTo(31));
        }
        [Test] public void ReactionsHoldCommandsAndSeededObservationsReplay()
        {
            var a = Create(); var b = Create();
            Assert.Catch(() => a.Step(new SimulationTick(1), default));
            int previous = 0;
            for (int i = 1; i <= 100; i++)
            {
                var observation = new ShootingAiObservation(ShootingPhase.Aiming, 0, 0, i % 2 == 0 ? -0.03 : 0.03, 0);
                var fa = a.Step(new SimulationTick(i), observation); var fb = b.Step(new SimulationTick(i), observation);
                Assert.That(fa, Is.EqualTo(fb));
                if (!fa.DecisionMade) { Assert.That(fa.Aim.Horizontal, Is.EqualTo(previous)); Assert.That(fa.Trigger, Is.False); }
                Assert.That(fa.Aim.Horizontal, Is.InRange(-1000, 1000)); previous = fa.Aim.Horizontal;
            }
        }
        [Test] public void PressureDelaysDecisionsWithoutChangingAuthorityAndLoopAllocatesNothing()
        {
            var normal = Create(); var pressured = Create(); int normalDecisions = 0, pressuredDecisions = 0;
            for (int i = 1; i <= 100; i++)
            {
                if (normal.Step(new SimulationTick(i), new ShootingAiObservation(ShootingPhase.Aiming, 0, 0, 0, 0)).DecisionMade) normalDecisions++;
                if (pressured.Step(new SimulationTick(i), new ShootingAiObservation(ShootingPhase.Aiming, 0, 0, 0, 0, 2)).DecisionMade) pressuredDecisions++;
            }
            Assert.That(pressuredDecisions, Is.LessThan(normalDecisions));
            var observation = new ShootingAiObservation(ShootingPhase.Aiming, 0, 0, 0, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) normal.Step(new SimulationTick(i), observation);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

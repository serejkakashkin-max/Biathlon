using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingPressureTests
    {
        public static ShootingPressureDefinition Definition() => ShootingPressureDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_pressure_v1.json"));
        [Test] public void ContextComposureAndStanceChangeBoundedPressure()
        {
            var d = Definition(); var stances = ShootingStanceTests.Definition();
            ShootingPressureFrame Evaluate(ShootingPressureContext c, int skill, ShootingStanceProfile stance) => new ShootingPressureSystem(d, 20, stance).Step(new SimulationTick(1), c, skill);
            var context = new ShootingPressureContext(0.8, 0.8, 0.8, 1);
            var novice = Evaluate(context, 0, stances.Standing); var expert = Evaluate(context, 100, stances.Standing);
            Assert.That(expert.Level, Is.LessThan(novice.Level)); Assert.That(Evaluate(context, 0, stances.Prone).Level, Is.LessThan(novice.Level));
            Assert.That(Evaluate(new ShootingPressureContext(0.8, 0.8, 0.8, 0, true, true), 0, stances.Standing).Target, Is.GreaterThan(novice.Target));
            Assert.That(Evaluate(new ShootingPressureContext(1, 1, 1, 0, true, true), 0, stances.Standing).Target, Is.EqualTo(1));
            Assert.That(Evaluate(default, 0, stances.Standing).Level, Is.Zero);
        }
        [Test] public void PressureRecoversAndOnlyChangesSwayNotGeometry()
        {
            var s = new ShootingPressureSystem(Definition(), 20); var context = new ShootingPressureContext(1, 1, 1, 0, true, true);
            for (int i = 1; i <= 200; i++) s.Step(new SimulationTick(i), context, 0);
            var high = s.LastFrame;
            for (int i = 201; i <= 1200; i++) s.Step(new SimulationTick(i), default, 0);
            Assert.That(s.LastFrame.Level, Is.LessThan(0.001));
            var stance = ShootingStanceTests.Definition().Standing; var muscular = new MuscularSwayFrame(0.001, 0.001, 0.001);
            var neutral = ShootingSwaySystem.Compose(stance, default, default, muscular);
            var pressured = ShootingSwaySystem.Compose(stance, default, default, muscular, pressure: high);
            Assert.That(pressured.HorizontalRadians, Is.GreaterThan(neutral.HorizontalRadians));
            Assert.That(new TargetGeometrySystem(TargetGeometryTests.Definition()).Intersect(new ShotRay(0, 0, 0, 0, 0, 1), ShootingStance.Prone).Hit, Is.True);
        }
        [Test] public void ReplayAtomicityAndAllocationRemainStable()
        {
            var d = Definition(); var a = new ShootingPressureSystem(d, 20); var b = new ShootingPressureSystem(d, 20);
            var context = new ShootingPressureContext(1, 0.8, 0.9, 0.5);
            Assert.Catch(() => a.Step(new SimulationTick(1), context, 101));
            for (int i = 1; i <= 100; i++) Assert.That(a.Step(new SimulationTick(i), context, 50), Is.EqualTo(b.Step(new SimulationTick(i), context, 50)));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) a.Step(new SimulationTick(i), context, 50);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

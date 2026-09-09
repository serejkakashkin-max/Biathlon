using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class WeaponAimTests
    {
        public static WeaponAimDefinition Definition() => WeaponAimDefinition.FromJson(PhysiologyContractTests.DefinitionJson("weapon_aim_v1.json"));
        private static readonly AthleteId Athlete = new AthleteId("aiming");
        [TestCase(20)] [TestCase(30)] [TestCase(144)]
        public void OneSecondRateAndFineControlAreFrequencyIndependent(int hz)
        {
            var d = Definition(); var normal = new WeaponAimSystem(d, Athlete, hz); var fine = new WeaponAimSystem(d, Athlete, hz);
            for (int i = 1; i <= hz; i++)
            { normal.Step(new WeaponAimCommand(new SimulationTick(i), Athlete, 1000, 0)); fine.Step(new WeaponAimCommand(new SimulationTick(i), Athlete, 1000, 0, true)); }
            Assert.That(normal.State.YawRadians, Is.EqualTo(d.RateRadiansPerSecond).Within(1e-6));
            Assert.That(fine.State.YawRadians, Is.EqualTo(d.RateRadiansPerSecond * d.FineMultiplier).Within(1e-6));
        }
        [Test] public void DiagonalRateBoundsAndInvalidCommandAreAtomic()
        {
            var d = Definition(); var s = new WeaponAimSystem(d, Athlete, 20);
            var diagonal = s.Step(new WeaponAimCommand(new SimulationTick(1), Athlete, 1000, 1000));
            Assert.That(Math.Sqrt(diagonal.YawRadians * diagonal.YawRadians + diagonal.PitchRadians * diagonal.PitchRadians),
                Is.EqualTo(d.RateRadiansPerSecond / 20).Within(1e-9));
            Assert.Catch(() => s.Step(new WeaponAimCommand(new SimulationTick(3), Athlete, 0, 0)));
            Assert.Catch(() => s.Step(new WeaponAimCommand(new SimulationTick(2), new AthleteId("other"), 0, 0)));
            Assert.That(s.State, Is.EqualTo(diagonal));
            for (int i = 2; i <= 1000; i++) s.Step(new WeaponAimCommand(new SimulationTick(i), Athlete, 1000, -1000));
            Assert.That(s.State.YawRadians, Is.EqualTo(d.MaxYawRadians)); Assert.That(s.State.PitchRadians, Is.EqualTo(-d.MaxPitchRadians));
        }
        [Test] public void AimRayReachesEachTargetAndRepeatedCommandsReplayWithoutAllocation()
        {
            var geometry = TargetGeometryTests.Definition(); var g = new TargetGeometrySystem(geometry);
            for (int i = 0; i < 5; i++)
            {
                var state = new WeaponAimState(Math.Atan(geometry.CenterX(i) / geometry.DistanceMeters), 0);
                Assert.That(g.Intersect(state.Ray, ShootingStance.Prone).TargetIndex, Is.EqualTo(i));
            }
            var a = new WeaponAimSystem(Definition(), Athlete, 20); var b = new WeaponAimSystem(Definition(), Athlete, 20);
            for (int i = 1; i <= 100; i++)
            {
                var c = new WeaponAimCommand(new SimulationTick(i), Athlete, i % 2 == 0 ? -350 : 200, 125, true);
                Assert.That(a.Step(c), Is.EqualTo(b.Step(c)));
            }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) a.Step(new WeaponAimCommand(new SimulationTick(i), Athlete, 500, -500));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

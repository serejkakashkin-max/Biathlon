using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingWindTests
    {
        public static ShootingWindDefinition Definition() => ShootingWindDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_wind_v1.json"));
        [Test] public void RotationExposureAndSignFollowLaneCoordinates()
        {
            var s = new ShootingWindSystem(Definition());
            var right = s.Evaluate(new WindVector(5, 0), 0, 1, 50);
            var left = s.Evaluate(new WindVector(-5, 0), 0, 1, 50);
            Assert.That(right.CrosswindMetersPerSecond, Is.EqualTo(5)); Assert.That(left.HorizontalDriftMeters, Is.EqualTo(-right.HorizontalDriftMeters));
            Assert.That(right.Indicator.CrosswindDirection, Is.EqualTo(1)); Assert.That(left.Indicator.CrosswindDirection, Is.EqualTo(-1));
            var rotated = s.Evaluate(new WindVector(5, 0), Math.PI / 2, 1, 50);
            Assert.That(rotated.CrosswindMetersPerSecond, Is.Zero); Assert.That(rotated.HeadwindMetersPerSecond, Is.EqualTo(-5));
            Assert.That(rotated.HorizontalDriftMeters, Is.Zero);
            Assert.That(s.Evaluate(new WindVector(5, 0), 0, 0, 50).HorizontalDriftMeters, Is.Zero);
            Assert.That(s.Evaluate(new WindVector(5, 0), 0, 0.5, 50).HorizontalDriftMeters, Is.EqualTo(right.HorizontalDriftMeters / 2).Within(1e-9));
        }
        [Test] public void GddDriftMovesAnOtherwisePerfectRayWithoutHiddenCompensation()
        {
            var d = Definition(); var s = new ShootingWindSystem(d); var g = new TargetGeometrySystem(TargetGeometryTests.Definition());
            var zero = s.Evaluate(default, 0, 1, 50); var wind = s.Evaluate(new WindVector(5, 0), 0, 1, 50);
            Assert.That(wind.HorizontalDriftMeters, Is.EqualTo(5 * d.BallisticScale * 50 / d.ProjectileSpeed).Within(1e-9));
            Assert.That(g.Intersect(new ShotRay(0, 0, 0, zero.HorizontalDriftMeters, 0, 50), ShootingStance.Prone).Hit, Is.True);
            Assert.That(g.Intersect(new ShotRay(0, 0, 0, wind.HorizontalDriftMeters, 0, 50), ShootingStance.Prone).Hit, Is.False);
            Assert.That(zero.Indicator.Strength, Is.EqualTo(WindStrength.Calm)); Assert.That(wind.Indicator.Strength, Is.EqualTo(WindStrength.Moderate));
        }
        [Test] public void InvalidWindAndHotLoopAreControlled()
        {
            var s = new ShootingWindSystem(Definition());
            Assert.Catch(() => s.Evaluate(new WindVector(40, 40), 0, 1, 50));
            Assert.Catch(() => s.Evaluate(default, 0, double.NaN, 50));
            var expected = s.Evaluate(new WindVector(3, -2), 0.5, 0.8, 50);
            Assert.That(s.Evaluate(new WindVector(3, -2), 0.5, 0.8, 50), Is.EqualTo(expected));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) s.Evaluate(new WindVector(3, -2), 0.5, 0.8, 50);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

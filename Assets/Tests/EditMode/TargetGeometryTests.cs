using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TargetGeometryTests
    {
        public static TargetGeometryDefinition Definition() => TargetGeometryDefinition.FromJson(PhysiologyContractTests.DefinitionJson("target_geometry_v1.json"));
        private static ShotRay At(double x, double y, double distance = 50) => new ShotRay(0, 0, 0, x, y, distance);
        [TestCase(ShootingStance.Prone)] [TestCase(ShootingStance.Standing)]
        public void AllFiveCentersEdgesAndOneNanometerOutsideAreDeterministic(ShootingStance stance)
        {
            var d = Definition(); var g = new TargetGeometrySystem(d);
            for (int i = 0; i < 5; i++)
            {
                double x = d.CenterX(i), r = d.Radius(stance);
                Assert.That(g.Intersect(At(x, 0), stance).TargetIndex, Is.EqualTo(i));
                Assert.That(g.Intersect(At(x + r, 0), stance).TargetIndex, Is.EqualTo(i));
                Assert.That(g.Intersect(At(x - r, 0), stance).TargetIndex, Is.EqualTo(i));
                Assert.That(g.Intersect(At(x, r), stance).TargetIndex, Is.EqualTo(i));
                Assert.That(g.Intersect(At(x + r + 1e-9, 0), stance).Hit, Is.False);
                Assert.That(g.Intersect(At(x, -r - 1e-9), stance).Hit, Is.False);
            }
        }
        [Test] public void ForwardIntersectionUsesOriginAndScaleWhileParallelAndBehindMiss()
        {
            var g = new TargetGeometrySystem(Definition());
            Assert.That(g.Intersect(new ShotRay(1, 2, 10, -1, -2, 40), ShootingStance.Prone).TargetIndex, Is.EqualTo(2));
            Assert.That(g.Intersect(new ShotRay(1, 2, 10, -2, -4, 80), ShootingStance.Prone).TargetIndex, Is.EqualTo(2));
            Assert.That(g.Intersect(new ShotRay(0, 0, 0, 1, 0, 0), ShootingStance.Prone).ReachesPlane, Is.False);
            Assert.That(g.Intersect(new ShotRay(0, 0, 0, 0, 0, -1), ShootingStance.Prone).ReachesPlane, Is.False);
            Assert.That(g.Intersect(At(0, 0.04), ShootingStance.Prone).Hit, Is.False);
            Assert.That(g.Intersect(At(0, 0.04), ShootingStance.Standing).Hit, Is.True);
        }
        [Test] public void IndependentClosuresAndBadGeometryCannotCorruptState()
        {
            var bank = new TargetBankState();
            for (int i = 4; i >= 0; i--)
            { Assert.That(bank.Close(i), Is.True); Assert.That(bank.Close(i), Is.False); Assert.That(bank.IsClosed(i), Is.True); }
            Assert.That(bank.ClosedMask, Is.EqualTo(31)); Assert.Throws<ArgumentOutOfRangeException>(() => bank.Close(5));
            Assert.That(bank.ClosedMask, Is.EqualTo(31));
            Assert.Catch(() => TargetGeometryDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "spacing_m", "0.1")));
            Assert.Catch(() => new TargetGeometrySystem(Definition()).Intersect(default, ShootingStance.Prone));
        }
        [Test] public void ActiveGeometryDoesNotAllocate()
        {
            var g = new TargetGeometrySystem(Definition()); var ray = At(0, 0);
            g.Intersect(ray, ShootingStance.Prone);
            long before = GC.GetAllocatedBytesForCurrentThread(); int hits = 0;
            for (int i = 0; i < 10000; i++) if (g.Intersect(ray, ShootingStance.Prone).Hit) hits++;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(hits, Is.EqualTo(10000));
        }
    }
}

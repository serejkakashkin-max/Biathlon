using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CorneringTests
    {
        public static CorneringDefinition Definition() => CorneringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("cornering_provisional_v1.json"));
        [Test]
        public void SafeSpeedObeysCurvatureAndSkillWithoutDirectionBias()
        {
            var s = new CorneringSystem(Definition(), SkiForceTests.Definition());
            var state = new SkiState(0, 15);
            var expert = s.Evaluate(state, TuckTests.Descent(0.04), 100, 100, 1, 0);
            Assert.That(expert.SafeSpeedMetersPerSecond, Is.EqualTo(10).Within(1e-12));
            Assert.That(s.Evaluate(state, TuckTests.Descent(-0.04), 100, 100, 1, 0).SafeSpeedMetersPerSecond, Is.EqualTo(expert.SafeSpeedMetersPerSecond));
            Assert.That(s.Evaluate(state, TuckTests.Descent(0.08), 100, 100, 1, 0).SafeSpeedMetersPerSecond, Is.LessThan(expert.SafeSpeedMetersPerSecond));
            Assert.That(s.Evaluate(state, TuckTests.Descent(0.04), 0, 100, 1, 0).SafeSpeedMetersPerSecond, Is.LessThan(expert.SafeSpeedMetersPerSecond));
            Assert.That(s.Evaluate(state, TuckTests.Descent(0.04), 100, 0, 1, 0).SafeSpeedMetersPerSecond, Is.LessThan(expert.SafeSpeedMetersPerSecond));
            var straight = s.Evaluate(state, TuckTests.Descent(), 0, 0, 0, 1);
            Assert.That(straight.SafeSpeedMetersPerSecond, Is.EqualTo(SkiForceTests.Definition().MaximumSpeedMetersPerSecond));
            Assert.That(straight.BrakingNewtons, Is.Zero);
        }
        [Test]
        public void PoorLineRiskAndFatigueReduceSafeCapability()
        {
            var s = new CorneringSystem(Definition(), SkiForceTests.Definition()); var state = new SkiState(0, 15);
            var clean = s.Evaluate(state, TuckTests.Descent(0.04), 100, 100, 1, 0);
            Assert.That(s.Evaluate(state, TuckTests.Descent(0.04), 100, 100, 0, 0).SafeSpeedMetersPerSecond, Is.LessThan(clean.SafeSpeedMetersPerSecond));
            Assert.That(s.Evaluate(state, TuckTests.Descent(0.04), 100, 100, 1, 1).SafeSpeedMetersPerSecond, Is.LessThan(clean.SafeSpeedMetersPerSecond));
            Assert.That(s.Evaluate(state, TuckTests.Descent(0.04, 1), 100, 100, 1, 0).SafeSpeedMetersPerSecond, Is.LessThan(clean.SafeSpeedMetersPerSecond));
        }
        [Test]
        public void OverspeedProducesBoundedBrakingAndIntegratedDistanceNotTeleportation()
        {
            var physics = SkiForceTests.Definition(); var d = Definition();
            var corner = new CorneringSystem(d, physics); var forces = new SkiForceSystem(physics, 20);
            var track = TuckTests.Descent(0.04); var state = new SkiState(0, 20);
            var response = corner.Evaluate(state, track, 100, 100, 1, 0);
            Assert.That(response.ExcessSpeedMetersPerSecond, Is.EqualTo(10));
            Assert.That(response.BrakingNewtons, Is.EqualTo(d.MaximumBraking * physics.MassKilograms));
            var next = forces.Step(state, 0, track, brakingNewtons: response.BrakingNewtons);
            Assert.That(next.State.SpeedMetersPerSecond, Is.InRange(response.SafeSpeedMetersPerSecond, 20));
            Assert.That(next.State.DistanceMeters, Is.EqualTo((20 + next.State.SpeedMetersPerSecond) / 40).Within(1e-9));
            var safe = corner.Evaluate(new SkiState(0, 9), track, 100, 100, 1, 0);
            Assert.That(safe.IsOverspeed, Is.False); Assert.That(safe.BrakingNewtons, Is.Zero);
            var a = state; var b = state;
            for (int i = 0; i < 1200; i++)
            {
                a = forces.Step(a, 0, track, brakingNewtons: corner.Evaluate(a, track, 100, 100, 1, 0).BrakingNewtons).State;
                b = forces.Step(b, 0, track).State;
            }
            Assert.That(a.SpeedMetersPerSecond, Is.InRange(10, 11));
            Assert.That(a.DistanceNanometers, Is.LessThan(b.DistanceNanometers));
        }
        [Test]
        public void LongRunIsRepeatableAllocationFreeAndContractsRejectInvalidData()
        {
            var json = Definition().ToJson();
            Assert.That(CorneringDefinition.FromJson(json).ToJson(), Is.EqualTo(json));
            foreach (var pair in new[] { ("schema_version", "0"), ("expert_lateral_acceleration_mps2", "1"), ("poor_line_modifier", "0"), ("full_downhill_grade", "0"), ("braking_response_seconds", "0") })
                Assert.Catch(() => CorneringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair.Item1, pair.Item2)));
            var corner = new CorneringSystem(Definition(), SkiForceTests.Definition()); var forces = new SkiForceSystem(SkiForceTests.Definition(), 20);
            var track = TuckTests.Descent(0.04); var initial = new SkiState(0, 20);
            Assert.Catch(() => corner.Evaluate(initial, track, 101, 100, 1, 0));
            Assert.Catch(() => corner.Evaluate(initial, track, 100, 100, double.NaN, 0));
            Assert.Catch(() => corner.Evaluate(initial, default, 100, 100, 1, 0));
            Assert.Catch(() => forces.Step(initial, 0, track, brakingNewtons: -1));
            var a = initial; var b = initial;
            corner.Evaluate(a, track, 50, 50, 0.8, 0.2); forces.Step(a, 0, track);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                a = forces.Step(a, 0, track, brakingNewtons: corner.Evaluate(a, track, 50, 50, 0.8, 0.2).BrakingNewtons).State;
                b = forces.Step(b, 0, track, brakingNewtons: corner.Evaluate(b, track, 50, 50, 0.8, 0.2).BrakingNewtons).State;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(a, Is.EqualTo(b));
            TestContext.WriteLine(FormattableString.Invariant($"corner_probe={a.DistanceNanometers}:{a.SpeedMetersPerSecond:R}"));
        }
    }
}

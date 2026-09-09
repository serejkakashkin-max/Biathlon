using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SkiForceTests
    {
        public static SkiPhysicsDefinition Definition() => SkiPhysicsDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_physics_provisional_v1.json"));
        private static TrackSample Sample(string id) => new TrackSampler(SyntheticTrackTests.Generated(id).GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
        private static SkiState Run(SkiForceSystem system, TrackSample sample, double watts, int ticks, double wind = 0)
        {
            var state = default(SkiState);
            for (int i = 0; i < ticks; i++) state = system.Step(state, watts, sample, wind).State;
            return state;
        }
        public static string DeterministicProbe()
        {
            var result = Run(new SkiForceSystem(Definition(), 20), Sample("synthetic_flat_v1"), 350, 10000);
            return FormattableString.Invariant($"{result.DistanceNanometers}:{result.SpeedMetersPerSecond:R}");
        }
        [Test]
        public void ForceBreakdownMatchesUnitsAndGradeGeometry()
        {
            var system = new SkiForceSystem(Definition(), 20);
            var result = system.Step(new SkiState(0, 10), 350, Sample("synthetic_climb_v1"));
            Assert.That(result.PropulsiveNewtons, Is.EqualTo(35));
            Assert.That(result.AirDragNewtons, Is.EqualTo(30.625).Within(1e-10));
            Assert.That(result.GravityNewtons, Is.EqualTo(75 * 9.81 * 0.1 / Math.Sqrt(1.01)).Within(1e-10));
            Assert.That(result.SnowNewtons, Is.EqualTo(75 * 9.81 * 0.015 / Math.Sqrt(1.01)).Within(1e-10));
            Assert.That(result.NetNewtons, Is.LessThan(0));
            Assert.That(result.AppliedAcceleration, Is.EqualTo(result.NetNewtons / 75).Within(1e-7));
        }
        [Test]
        public void FlatEquilibriumBalancesPowerAndUphillCostsSpeed()
        {
            var system = new SkiForceSystem(Definition(), 20);
            var flat = Sample("synthetic_flat_v1");
            var state = Run(system, flat, 350, 3600);
            var forces = system.Step(state, 350, flat);
            Assert.That(forces.NetNewtons, Is.EqualTo(0).Within(1e-4));
            Assert.That(state.SpeedMetersPerSecond, Is.InRange(8, 11));
            Assert.That(state.DistanceMeters, Is.GreaterThan(1000));
            Assert.That(Run(system, Sample("synthetic_climb_v1"), 350, 3600).SpeedMetersPerSecond, Is.LessThan(state.SpeedMetersPerSecond));
            Assert.That(Run(system, Sample("synthetic_technical_descent_v1"), 0, 3600).SpeedMetersPerSecond, Is.GreaterThan(state.SpeedMetersPerSecond));
        }
        [Test]
        public void SignedExposedWindChangesDragInCorrectDirection()
        {
            var system = new SkiForceSystem(Definition(), 20);
            var flat = Sample("synthetic_flat_v1");
            var state = new SkiState(0, 5);
            var calm = system.Step(state, 350, flat);
            var head = system.Step(state, 350, flat, 10);
            var tail = system.Step(state, 350, flat, -20);
            Assert.That(head.AirDragNewtons, Is.GreaterThan(calm.AirDragNewtons));
            Assert.That(tail.AirDragNewtons, Is.LessThan(0));
            Assert.That(tail.State.SpeedMetersPerSecond, Is.GreaterThan(calm.State.SpeedMetersPerSecond));
        }
        [Test]
        public void RestDoesNotReverseAndStoppingInsideTickDoesNotAddExtraDistance()
        {
            var system = new SkiForceSystem(Definition(), 20);
            var flat = Sample("synthetic_flat_v1");
            Assert.That(system.Step(default, 0, flat).State.DistanceNanometers, Is.Zero);
            Assert.That(system.Step(default, 0, Sample("synthetic_climb_v1")).State.SpeedMetersPerSecond, Is.Zero);
            var stopped = system.Step(new SkiState(0, 0.001), 0, flat);
            double stopTime = 0.001 / (-stopped.NetNewtons / 75);
            Assert.That(stopped.State.SpeedMetersPerSecond, Is.Zero);
            Assert.That(stopped.State.DistanceMeters, Is.EqualTo(0.5 * 0.001 * stopTime).Within(1e-9));
            Assert.That(stopped.State.Distance.Millimeters, Is.Zero);
            Assert.That(stopped.State.DistanceNanometers, Is.GreaterThan(0), "Sub-millimetre motion must be retained.");
        }
        [Test]
        public void ZeroSpeedPropulsionAndNumericEnvelopeAreBounded()
        {
            var d = Definition();
            var system = new SkiForceSystem(d, 20);
            var output = system.Step(default, d.MaximumPowerWatts, Sample("synthetic_flat_v1"));
            Assert.That(output.PropulsiveNewtons, Is.EqualTo(d.MaximumPropulsiveForceNewtons));
            var limited = SkiPhysicsDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(), "maximum_speed_mps", "0.1"));
            var capped = new SkiForceSystem(limited, 20).Step(default, 2000, Sample("synthetic_flat_v1"));
            Assert.That(capped.State.SpeedMetersPerSecond, Is.EqualTo(0.1));
            Assert.That(capped.State.DistanceMeters, Is.LessThan(0.1 / 20));
            Assert.That(capped.State.DistanceMeters, Is.GreaterThan(0.1 / 40));
        }
        [Test]
        public void InvalidInputDefinitionsAndDistanceOverflowFail()
        {
            var system = new SkiForceSystem(Definition(), 20);
            var flat = Sample("synthetic_flat_v1");
            Assert.Catch(() => system.Step(default, double.NaN, flat));
            Assert.Catch(() => system.Step(default, 2001, flat));
            Assert.Catch(() => system.Step(default, 350, default));
            Assert.Catch(() => system.Step(default, 350, flat, 41));
            Assert.Catch(() => system.Step(new SkiState(long.MaxValue, 10), 350, flat));
            foreach (string field in new[] { "mass_kg", "power_force_min_speed_mps", "maximum_speed_mps" })
                Assert.Catch(() => SkiPhysicsDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), field, "0")));
            Assert.That(SkiPhysicsDefinition.FromJson(Definition().ToJson()).ToJson(), Is.EqualTo(Definition().ToJson()));
        }
        [Test]
        public void PhysiologyPowerDrivesRepeatableAllocationFreeMovement()
        {
            var system = new SkiForceSystem(Definition(), 20);
            var sampler = new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long"));
            var physiology = PhysiologyIntegrationTests.Create(new RaceEventLog(100));
            var state = default(SkiState);
            var track = sampler.Sample(state.Distance, TrackBoundaryMode.Wrap);
            var frame = physiology.Step(PhysiologyIntegrationTests.Input(physiology, 800));
            state = system.Step(state, frame.Effort.DeliveredPowerWatts, track).State;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                track = sampler.Sample(state.Distance, TrackBoundaryMode.Wrap);
                frame = physiology.Step(PhysiologyIntegrationTests.Input(physiology, 800, terrain: track.TerrainMuscleFactor));
                state = system.Step(state, frame.Effort.DeliveredPowerWatts, track).State;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(state.DistanceMeters, Is.GreaterThan(4000));
            string probe = DeterministicProbe();
            Assert.That(probe, Is.EqualTo("4602282710307:9.311353007"), "WP017 fixed-tick distance/speed regression.");
            Assert.That(DeterministicProbe(), Is.EqualTo(probe));
            TestContext.WriteLine("ski_probe=" + probe);
        }
    }
}

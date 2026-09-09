using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SkiBoundaryStepperTests
    {
        private static TrackSample Sample(string track) => new TrackSampler(SyntheticTrackTests.Generated(track).GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
        [Test] public void FullDurationPreservesExistingFixedTickPhysicsExactly()
        {
            var system = new SkiForceSystem(SkiForceTests.Definition(), 20); var sample = Sample("synthetic_flat_v1");
            var state = new SkiState(1000000000, 8);
            Assert.That(system.StepDuration(state, 350, sample, 0.05), Is.EqualTo(system.Step(state, 350, sample)));
            Assert.Catch(() => system.StepDuration(state, 350, sample, 0)); Assert.Catch(() => system.StepDuration(state, 350, sample, 0.051));
            Assert.Catch(() => system.StepDuration(state, 350, sample, double.NaN));
            Assert.That(system.StepDuration(state, 350, sample, double.Epsilon).AppliedAcceleration, Is.Zero);
        }
        [Test] public void SurfaceBoundarySplitsTimeAndRetainsSpeedBeforeNextSurface()
        {
            var d = SkiForceTests.Definition(); var forces = new SkiForceSystem(d, 20); var stepper = new SkiBoundaryStepper(d, 20);
            var flat = Sample("synthetic_flat_v1"); var climb = Sample("synthetic_climb_v1"); var initial = new SkiState(0, 10);
            var first = stepper.Step(initial, 350, flat, 0.05, 200000000);
            Assert.That(first.ReachedBoundary, Is.True); Assert.That(first.Forces.State.DistanceNanometers, Is.EqualTo(200000000));
            Assert.That(first.ConsumedSeconds + first.RemainingSeconds, Is.EqualTo(0.05));
            Assert.That(first.ConsumedSeconds, Is.InRange(0.019, 0.021));
            double acceleration = forces.Step(initial, 350, flat).NetNewtons / d.MassKilograms;
            double physicalDistance = 10 * first.ConsumedSeconds + acceleration * first.ConsumedSeconds * first.ConsumedSeconds * 0.5;
            Assert.That(physicalDistance, Is.EqualTo(0.2).Within(1e-9));
            var second = stepper.Step(first.Forces.State, 350, climb, first.RemainingSeconds, 1000000000);
            var wrongSurface = stepper.Step(first.Forces.State, 350, flat, first.RemainingSeconds, 1000000000);
            Assert.That(second.ReachedBoundary, Is.False); Assert.That(second.RemainingSeconds, Is.Zero);
            Assert.That(second.Forces.State.SpeedMetersPerSecond, Is.LessThan(wrongSurface.Forces.State.SpeedMetersPerSecond));
        }
        [Test] public void StoppingShortCannotGrantBoundaryAndLargeCoordinatesRemainExact()
        {
            var stepper = new SkiBoundaryStepper(SkiForceTests.Definition(), 20); var flat = Sample("synthetic_flat_v1");
            var stopped = stepper.Step(new SkiState(0, 0.001), 0, flat, 0.05, 100000000);
            Assert.That(stopped.ReachedBoundary, Is.False); Assert.That(stopped.Forces.State.SpeedMetersPerSecond, Is.Zero);
            long start = long.MaxValue - 100000000;
            var large = stepper.Step(new SkiState(start, 10), 350, flat, 0.05, long.MaxValue);
            Assert.That(large.ReachedBoundary, Is.True); Assert.That(large.Forces.State.DistanceNanometers, Is.EqualTo(long.MaxValue));
            Assert.That(large.ConsumedSeconds, Is.InRange(0.009, 0.011));
        }
        [Test] public void BoundarySolvingIsDeterministicAndDoesNotAllocate()
        {
            var stepper = new SkiBoundaryStepper(SkiForceTests.Definition(), 20); var sample = Sample("synthetic_flat_v1"); var state = new SkiState(0, 10);
            var expected = stepper.Step(state, 350, sample, 0.05, 100000000); SkiBoundaryStep actual = default;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) actual = stepper.Step(state, 350, sample, 0.05, 100000000);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(actual, Is.EqualTo(expected));
        }
    }
}

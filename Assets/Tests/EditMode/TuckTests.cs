using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TuckTests
    {
        public static TuckDefinition Definition() => TuckDefinition.FromJson(PhysiologyContractTests.DefinitionJson("tuck_provisional_v1.json"));
        public static TrackSample Descent(double curvature = 0, double risk = 0, double grade = -0.1)
        {
            string json = SyntheticTrackTests.Generated("synthetic_technical_descent_v1").ToJson();
            json = PhysiologyContractTests.ReplaceNumber(json, "curvature_per_m", curvature.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            json = PhysiologyContractTests.ReplaceNumber(json, "risk_level", risk.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            json = PhysiologyContractTests.ReplaceNumber(json, "start_grade", grade.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            json = PhysiologyContractTests.ReplaceNumber(json, "end_grade", grade.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            return new TrackSampler(TrackDefinition.FromJson(json).GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
        }
        [Test]
        public void PostureIsConstrainedByGradeSpeedCurvatureRiskAndSkill()
        {
            var system = new TuckSystem(Definition()); var state = new SkiState(0, 10);
            Assert.That(system.Evaluate(true, 800, 100, state, Descent()).Active, Is.True);
            Assert.That(system.Evaluate(false, 800, 100, state, Descent()).Rejection, Is.EqualTo(TuckRejection.NotRequested));
            Assert.That(system.Evaluate(true, 800, 100, state, Descent(grade: 0)).Rejection, Is.EqualTo(TuckRejection.Grade));
            Assert.That(system.Evaluate(true, 800, 100, default, Descent()).Rejection, Is.EqualTo(TuckRejection.Speed));
            Assert.That(system.Evaluate(true, 800, 0, state, Descent(curvature: 0.02)).Rejection, Is.EqualTo(TuckRejection.Curvature));
            Assert.That(system.Evaluate(true, 800, 100, state, Descent(curvature: -0.02)).Active, Is.True);
            Assert.That(system.Evaluate(true, 800, 0, state, Descent(risk: 0.5)).Rejection, Is.EqualTo(TuckRejection.Risk));
            Assert.That(system.Evaluate(true, 800, 100, state, Descent(risk: 0.5)).Active, Is.True);
            Assert.That(default(TuckOutput).Active, Is.False);
        }
        [Test]
        public void AerodynamicGainCostsActiveEffortAndSteeringResponse()
        {
            var system = new TuckSystem(Definition()); var state = new SkiState(0, 15); var track = Descent();
            var tuck = system.Evaluate(true, 1000, 50, state, track);
            var upright = system.Evaluate(false, 1000, 50, state, track);
            Assert.That(tuck.EffectiveEffortControl, Is.EqualTo(Definition().MaximumEffortControl));
            Assert.That(tuck.SteeringResponseMultiplier, Is.LessThan(upright.SteeringResponseMultiplier));
            Assert.That(system.Evaluate(true, 0, 50, state, track).EffectiveEffortControl, Is.Zero);
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20);
            var a = forces.Step(state, 0, track, dragAreaMultiplier: tuck.DragAreaMultiplier);
            var b = forces.Step(state, 0, track);
            Assert.That(a.AirDragNewtons / b.AirDragNewtons, Is.EqualTo(Definition().DragAreaMultiplier).Within(1e-12));
            Assert.That(a.State.SpeedMetersPerSecond, Is.GreaterThan(b.State.SpeedMetersPerSecond));
        }
        [Test]
        public void WorkloadReductionRecoversThroughSharedPhysiologyWithoutBonus()
        {
            var definition = PhysiologyContractTests.Definition();
            var initial = new PhysiologyState(175, 0.3, 0.3, 100);
            var a = PhysiologyIntegrationTests.Create(new RaceEventLog(100), initial);
            var b = PhysiologyIntegrationTests.Create(new RaceEventLog(100), initial);
            var posture = new TuckSystem(Definition()).Evaluate(true, 800, 100, new SkiState(0, 10), Descent());
            for (int i = 0; i < 1200; i++)
            {
                a.Step(PhysiologyIntegrationTests.Input(a, posture.EffectiveEffortControl));
                b.Step(PhysiologyIntegrationTests.Input(b, 800));
            }
            Assert.That(a.LastFrame.Effort.DeliveredPowerWatts, Is.LessThan(b.LastFrame.Effort.DeliveredPowerWatts));
            Assert.That(a.State.HeartRateBpm, Is.LessThan(b.State.HeartRateBpm));
            Assert.That(a.State.AnaerobicReserve, Is.GreaterThan(b.State.AnaerobicReserve));
            Assert.That(a.State.MuscularFatigue, Is.LessThan(b.State.MuscularFatigue));
        }
        [Test]
        public void EvaluationIsAllocationFreeAndInvalidDataFails()
        {
            string json = Definition().ToJson();
            Assert.That(TuckDefinition.FromJson(json).ToJson(), Is.EqualTo(json));
            foreach (var pair in new[] { ("schema_version", "2"), ("maximum_grade", "0"), ("expert_max_curvature_per_m", "0.001"), ("drag_area_multiplier", "1.1"), ("maximum_effort_control", "1001") })
                Assert.Catch(() => TuckDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair.Item1, pair.Item2)));
            var system = new TuckSystem(Definition()); var state = new SkiState(0, 10); var track = Descent();
            Assert.Catch(() => system.Evaluate(true, -1, 100, state, track));
            Assert.Catch(() => system.Evaluate(true, 800, 101, state, track));
            Assert.Catch(() => system.Evaluate(true, 800, 100, state, default));
            system.Evaluate(true, 800, 100, state, track);
            int sum = 0; long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += system.Evaluate(true, 800, 100, state, track).EffectiveEffortControl;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(sum, Is.EqualTo(2000000));
            Assert.Catch(() => new SkiForceSystem(SkiForceTests.Definition(), 20).Step(state, 0, track, dragAreaMultiplier: double.NaN));
        }
    }
}

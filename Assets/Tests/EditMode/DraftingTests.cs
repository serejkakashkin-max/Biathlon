using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class DraftingTests
    {
        public static DraftDefinition Definition() => DraftDefinition.FromJson(PhysiologyContractTests.DefinitionJson("draft_provisional_v1.json"));
        public static SkiPlacement Participant(string id, double meters, double lateral = 0, double speed = 10, string corridor = "race_path") =>
            new SkiPlacement(new AthleteId(id), corridor, new SkiState((long)(meters * SkiState.NanometersPerMeter), speed), lateral);
        public static TrackSample Flat() => new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(default);
        [Test]
        public void FieldRespectsAheadGapLateralCorridorAndPermission()
        {
            var system = new DraftingSystem(Definition()); var f = Participant("f", 10); var sample = Flat();
            Assert.That(system.Evaluate(f, new[] { Participant("a", 12) }, sample).Strength, Is.GreaterThan(0));
            foreach (var invalid in new[] { Participant("a", 9), Participant("a", 10), Participant("a", 10.5), Participant("a", 18), Participant("a", 12, 3), Participant("a", 12, corridor: "other"), Participant("f", 12) })
                Assert.That(system.Evaluate(f, new[] { invalid }, sample).Strength, Is.Zero);
            Assert.That(system.Evaluate(f, new[] { Participant("a", 12) }, TuckTests.Descent()).Strength, Is.Zero);
            Assert.That(default(DraftOutput).DragAreaMultiplier, Is.EqualTo(1));
        }
        [Test]
        public void LeaderOrderAndDuplicatesDoNotStackAndTiesUseStableId()
        {
            var system = new DraftingSystem(Definition()); var f = Participant("f", 10); var sample = Flat();
            var a = Participant("a", 12); var b = Participant("b", 12);
            var one = system.Evaluate(f, new[] { a }, sample);
            var multiple = system.Evaluate(f, new[] { b, a, a }, sample);
            Assert.That(multiple.Strength, Is.EqualTo(one.Strength));
            Assert.That(multiple.LeaderId.Value, Is.EqualTo("a"));
            Assert.That(system.Evaluate(f, new[] { a, b }, sample).LeaderId, Is.EqualTo(multiple.LeaderId));
            Assert.That(multiple.DragReduction, Is.LessThanOrEqualTo(Definition().MaximumDragReduction));
        }
        [Test]
        public void SlowClimbAndCrosswindReduceDraftWhileHeadwindIncreasesIt()
        {
            var system = new DraftingSystem(Definition()); var f = Participant("f", 10); var leader = new[] { Participant("a", 14) }; var sample = Flat();
            var calm = system.Evaluate(f, leader, sample);
            Assert.That(system.Evaluate(Participant("f", 10, speed: 2), new[] { Participant("a", 14, speed: 2) }, sample).Strength, Is.LessThan(calm.Strength));
            Assert.That(system.Evaluate(f, leader, sample, headwindMetersPerSecond: 10).Strength, Is.GreaterThan(calm.Strength));
            Assert.That(system.Evaluate(f, leader, sample, crosswindMetersPerSecond: 20).Strength, Is.LessThan(calm.Strength));
            var cross = system.Evaluate(f, leader, sample, crosswindMetersPerSecond: 10);
            double shift = 10 * sample.WindExposure * 4 / 10 * Definition().CrosswindDisplacementFactor;
            Assert.That(system.Evaluate(Participant("f", 10, lateral: shift), leader, sample, crosswindMetersPerSecond: 10).Strength, Is.GreaterThan(cross.Strength));
            Assert.That(system.Evaluate(f, leader, sample, headwindMetersPerSecond: -40).Strength, Is.Zero);
        }
        [Test]
        public void ReducedDragSavesMechanicalPowerWithoutRefundingPhysiology()
        {
            var system = new DraftingSystem(Definition()); var f = Participant("f", 10); var sample = Flat();
            var draft = system.Evaluate(f, new[] { Participant("a", 12) }, sample);
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20);
            var solo = forces.Step(f.Motion, 350, sample);
            var sheltered = forces.Step(f.Motion, 350, sample, dragAreaMultiplier: draft.DragAreaMultiplier);
            Assert.That(sheltered.PropulsiveNewtons, Is.EqualTo(solo.PropulsiveNewtons));
            Assert.That(sheltered.AirDragNewtons, Is.LessThan(solo.AirDragNewtons));
            Assert.That(sheltered.State.SpeedMetersPerSecond, Is.GreaterThan(solo.State.SpeedMetersPerSecond));
            double wattsSaved = (solo.AirDragNewtons - sheltered.AirDragNewtons) * f.Motion.SpeedMetersPerSecond;
            var equalAcceleration = forces.Step(f.Motion, 350 - wattsSaved, sample, dragAreaMultiplier: draft.DragAreaMultiplier);
            Assert.That(equalAcceleration.NetNewtons, Is.EqualTo(solo.NetNewtons).Within(1e-10));
        }
        [Test]
        public void DenseFieldIsAllocationFreeAndMalformedInputFails()
        {
            var d = Definition(); var system = new DraftingSystem(d); var f = Participant("f", 10); var sample = Flat();
            Assert.That(DraftDefinition.FromJson(d.ToJson()).ToJson(), Is.EqualTo(d.ToJson()));
            foreach (var pair in new[] { ("maximum_gap_m", "1"), ("reference_speed_mps", "0"), ("maximum_drag_reduction", "1.1"), ("schema_version", "0") })
                Assert.Catch(() => DraftDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(), pair.Item1, pair.Item2)));
            Assert.Catch(() => system.Evaluate(default, Array.Empty<SkiPlacement>(), sample));
            Assert.Catch(() => system.Evaluate(f, new SkiPlacement[1], sample));
            Assert.Catch(() => system.Evaluate(f, Array.Empty<SkiPlacement>(), sample, crosswindMetersPerSecond: double.NaN));
            var field = new SkiPlacement[100];
            for (int i = 0; i < field.Length; i++) field[i] = Participant("leader_" + i, 12 + i * 0.01, (i % 3) * 0.2);
            system.Evaluate(f, field, sample); double sum = 0; long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += system.Evaluate(f, field, sample).Strength;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(sum, Is.GreaterThan(0));
        }
    }
}

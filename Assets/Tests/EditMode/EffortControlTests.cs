using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class EffortControlTests
    {
        public static EffortControlDefinition Definition() => EffortControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("effort_control_provisional_v1.json"));
        private static readonly AthleteId Athlete = new AthleteId("player");
        private static PlayerCommand Command(EffortControlSystem s, int analog = 1000, EffortPreset preset = EffortPreset.Analog) => new PlayerCommand(s.Tick.Next(), Athlete, analog, 0, preset);
        [Test]
        public void AnalogDeadzoneSaturationAndIntermediateMappingAreMonotone()
        {
            var s = new EffortControlSystem(Definition(), Athlete, 20);
            Assert.That(s.AnalogTarget(20), Is.Zero); Assert.That(s.AnalogTarget(980), Is.EqualTo(1000));
            Assert.That(s.AnalogTarget(500), Is.EqualTo(500)); int prior = -1;
            for (int i = 0; i <= 1000; i++) { int next = s.AnalogTarget(i); Assert.That(next, Is.GreaterThanOrEqualTo(prior)); prior = next; }
        }
        [Test]
        public void PresetsAreExplicitAndFinalSprintUsesZoneAwareRequest()
        {
            var s = new EffortControlSystem(Definition(), Athlete, 20);
            Assert.That(s.Step(Command(s, preset: EffortPreset.Recovery), false).AppliedControl, Is.EqualTo(350));
            Assert.That(s.Step(Command(s, preset: EffortPreset.Race), false).AppliedControl, Is.EqualTo(800));
            Assert.That(s.Step(Command(s, preset: EffortPreset.Attack), false).AppliedControl, Is.EqualTo(1000));
            var sprint = s.Step(Command(s, preset: EffortPreset.FinalSprint), false);
            Assert.That(sprint.AppliedControl, Is.EqualTo(800)); Assert.That(sprint.FinalSprintRequested, Is.True);
            var zone = new FinalSprintSystem(SprintFinishTests.Definition(), TrackDistance.FromMeters(1000));
            Assert.That(zone.Evaluate(sprint.FinalSprintRequested, sprint.AppliedControl, default).EffectiveEffortControl, Is.EqualTo(800));
            Assert.That(zone.Evaluate(sprint.FinalSprintRequested, sprint.AppliedControl, new SkiState(900000000000, 10)).EffectiveEffortControl, Is.EqualTo(1000));
        }
        [TestCase(20)]
        [TestCase(30)]
        [TestCase(144)]
        public void AssistedResponsePreservesFractionalProgressAcrossTickRates(int frequency)
        {
            var s = new EffortControlSystem(Definition(), Athlete, frequency); EffortControlOutput output = default;
            for (int i = 0; i < frequency; i++) output = s.Step(Command(s), true);
            Assert.That(output.AppliedControl, Is.EqualTo(500));
            for (int i = 0; i < frequency; i++) output = s.Step(Command(s, analog: 0), true);
            Assert.That(output.AppliedControl, Is.Zero);
            Assert.That(s.Step(Command(s), false).AppliedControl, Is.EqualTo(1000));
        }
        [Test]
        public void InvalidCommandIsAtomicAndResponseLoopDoesNotAllocate()
        {
            var s = new EffortControlSystem(Definition(), Athlete, 20);
            Assert.Catch(() => s.Step(default, true)); Assert.That(s.Tick.Value, Is.Zero); Assert.That(s.ContinuousControl, Is.Zero);
            s.Step(Command(s), true); long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) s.Step(Command(s, i % 1001), true);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before; Assert.That(allocated, Is.Zero);
            Assert.That(EffortControlDefinition.FromJson(Definition().ToJson()).ToJson(), Is.EqualTo(Definition().ToJson()));
            Assert.Catch(() => EffortControlDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "analog_saturation", "20")));
            Assert.Catch(() => EffortControlDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "recovery_control", "900")));
        }
    }
}

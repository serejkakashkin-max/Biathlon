using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class DeviceCommandTests
    {
        private static DeviceCommandBuilder Create(int frequency = 20) => new DeviceCommandBuilder(
            DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_v1.json")),
            new AthleteId("player"), frequency);
        [TestCase(20)] [TestCase(30)] [TestCase(144)]
        public void KeyboardEffortUsesTicksAndSaturates(int frequency)
        {
            var b = Create(frequency); b.Sample(new DeviceControlSample(false, 1, 0));
            PlayerCommand c = default;
            for (int i = 1; i <= frequency * 3; i++)
            {
                c = b.Build(new SimulationTick(i));
                if (i == frequency) Assert.That(c.AnalogEffort, Is.EqualTo(500));
            }
            Assert.That(c.AnalogEffort, Is.EqualTo(1000));
        }
        [Test]
        public void BatchingAndRepeatedSamplesPreserveCommandsAndConsumePulseOnce()
        {
            var a = Create(); var b = Create(); var sample = new DeviceControlSample(false, 1, -1, tuck: true);
            a.Sample(sample, true); b.Sample(sample, true);
            for (int i = 1; i <= 100; i++)
            {
                b.Sample(sample);
                var ca = a.Build(new SimulationTick(i)); var cb = b.Build(new SimulationTick(i));
                Assert.That(ca.ToJson(), Is.EqualTo(cb.ToJson()));
                Assert.That(ca.OvertakeRequested, Is.EqualTo(i == 1));
            }
        }
        [Test]
        public void InvalidTickDoesNotConsumeCancelAndGamepadCurveIsSymmetric()
        {
            var b = Create(); b.Sample(new DeviceControlSample(true, 0.8, 0.1), true);
            b.Sample(new DeviceControlSample(true, 0.8, 0.1), cancelPressed: true);
            Assert.Throws<ArgumentException>(() => b.Build(new SimulationTick(2)));
            var c = b.Build(new SimulationTick(1));
            Assert.That(c.CancelOvertake, Is.True); Assert.That(c.OvertakeRequested, Is.False);
            Assert.That(c.LineIntent, Is.Zero); Assert.That(c.AnalogEffort, Is.EqualTo(800));
            b.Sample(new DeviceControlSample(true, 1, 0.6));
            int positive = b.Build(new SimulationTick(2)).LineIntent;
            b.Sample(new DeviceControlSample(true, 1, -0.6));
            Assert.That(b.Build(new SimulationTick(3)).LineIntent, Is.EqualTo(-positive));
            Assert.That(positive, Is.InRange(1, 599));
        }
        [Test]
        public void DeviceCommandsReplayCompleteCourseAndEvents()
        {
            var log = new RaceEventLog(1000); var race = PlayerReplayTests.Create(log);
            var builder = new DeviceCommandBuilder(DeviceControlDefinition.FromJson(
                PhysiologyContractTests.DefinitionJson("device_control_v1.json")), race.AthleteId, race.Clock.Frequency);
            var tape = new PlayerCommandTape("device_course", race.ContextJson, race.AthleteId, 100000);
            while (!race.HasFinished && tape.Count < 100000)
            {
                builder.Sample(new DeviceControlSample(true, 0.8, tape.Count % 400 < 200 ? -0.6 : 0.6, tuck: true));
                tape.StepAndRecord(race, builder.Build(race.Clock.Tick.Next()));
            }
            Assert.That(race.HasFinished, Is.True);
            var replayLog = new RaceEventLog(1000); var replay = PlayerReplayTests.Create(replayLog);
            var restored = PlayerCommandTape.FromJson(tape.ToJson());
            for (int i = 0; i < restored.Count; i++) restored.ReplayNext(replay);
            Assert.That(replay.Placement, Is.EqualTo(race.Placement));
            Assert.That(replay.PhysiologyState, Is.EqualTo(race.PhysiologyState));
            Assert.That(replay.FinishTime, Is.EqualTo(race.FinishTime));
            Assert.That(replayLog.Count, Is.EqualTo(log.Count));
            for (int i = 0; i < log.Count; i++) Assert.That(replayLog[i], Is.EqualTo(log[i]));
        }
        [Test]
        public void InvalidDeviceTuningAndSamplesAreRejected()
        {
            string json = PhysiologyContractTests.DefinitionJson("device_control_v1.json");
            Assert.Catch(() => DeviceControlDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, "stick_deadzone", "1")));
            Assert.Catch(() => DeviceControlDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, "schema_version", "2")));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeviceControlSample(true, double.NaN, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new DeviceControlSample(false, 0, double.PositiveInfinity));
        }
        [Test]
        public void ActiveCommandGenerationDoesNotAllocate()
        {
            var b = Create(); b.Sample(new DeviceControlSample(true, 0.8, 0.6));
            for (int i = 1; i <= 100; i++) b.Build(new SimulationTick(i));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) b.Build(new SimulationTick(i));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

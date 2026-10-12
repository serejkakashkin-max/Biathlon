using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class DeviceCommandTests
    {
        [TestCase(false)] [TestCase(true)]
        public void DirectionalInputHoldsAchievedLineOnReleaseAndReversesWithoutSpringToCenter(bool gamepad)
        {
            var b=Create();b.Sample(new DeviceControlSample(gamepad,0,-1));
            Assert.That(b.BuildDirectional(new SimulationTick(1),0,2,1).LineIntent,Is.EqualTo(-25));
            b.Sample(new DeviceControlSample(gamepad,0,0));
            Assert.That(b.BuildDirectional(new SimulationTick(2),-.6,2,1).LineIntent,Is.EqualTo(-300));
            // A blocked attempt must not leave a pending target that moves the athlete on release.
            Assert.That(b.BuildDirectional(new SimulationTick(3),-.2,2,1).LineIntent,Is.EqualTo(-100));
            b.Sample(new DeviceControlSample(gamepad,0,1));
            Assert.That(b.BuildDirectional(new SimulationTick(4),-.6,2,1).LineIntent,Is.EqualTo(-275));
            Assert.That(b.BuildDirectional(new SimulationTick(5),2,2,1).LineIntent,Is.EqualTo(1000));
        }
        [Test] public void DirectionalCommandsAreIndependentOfRenderSamplingAndReplayAsAbsoluteTargets()
        {
            var a=Create();var b=Create();double line=0;
            for(int tick=1;tick<=100;tick++)
            {
                var sample=new DeviceControlSample(false,.8,tick<40?-1:tick<70?0:1);
                a.Sample(sample);for(int frame=0;frame<7;frame++)b.Sample(sample);
                var ca=a.BuildDirectional(new SimulationTick(tick),line,2,1);
                var cb=b.BuildDirectional(new SimulationTick(tick),line,2,1);
                Assert.That(ca.ToJson(),Is.EqualTo(cb.ToJson()));
                Assert.That(PlayerCommand.FromJson(ca.ToJson()).LineIntent,Is.EqualTo(ca.LineIntent));
                line=ca.LineIntent*2.0/PlayerCommand.LineIntentUnits;
            }
        }
        private static DeviceCommandBuilder Create(int frequency = 20) => new DeviceCommandBuilder(
            DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_v1.json")),
            new AthleteId("player"), frequency);
        [Test] public void HeldKeyboardCorrectionReachesFullDeflectionWithinQuarterSecond()
        {
            var d=DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_gameplay_v3.json"));
            var builder=new DeviceCommandBuilder(d,new AthleteId("player"),20);
            builder.Sample(new DeviceControlSample(false,0,1));
            ManualSkiCommand command=default;
            for(int t=1;t<=5;t++)command=builder.BuildManual(new SimulationTick(t));
            Assert.That(command.SteeringInput,Is.EqualTo(1000),"A held finish correction must reach full input within 250ms.");
            builder.Sample(new DeviceControlSample(false,0,-1));
            Assert.That(builder.BuildManual(new SimulationTick(6)).SteeringInput,Is.EqualTo(-500));
            builder.Sample(new DeviceControlSample(false,0,0));
            Assert.That(builder.BuildManual(new SimulationTick(7)).SteeringInput,Is.Zero);
        }
        [Test] public void ProductionSteeringRampIsTickBoundAndGamepadRemainsAnalog()
        {
            var d=DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_gameplay_v3.json"));
            var a=new DeviceCommandBuilder(d,new AthleteId("player"),20);
            var b=new DeviceCommandBuilder(d,new AthleteId("player"),20);
            for(int t=1;t<=80;t++)
            {
                var sample=new DeviceControlSample(false,1,t<10?1:t<20?-1:t<30?0:1);
                a.Sample(sample);for(int frame=0;frame<7;frame++)b.Sample(sample);
                if(t==10)Assert.Throws<ArgumentException>(()=>a.BuildManual(new SimulationTick(t+1)));
                Assert.That(a.BuildManual(new SimulationTick(t)).ToJson(),Is.EqualTo(b.BuildManual(new SimulationTick(t)).ToJson()));
            }
            var stick=new DeviceCommandBuilder(d,new AthleteId("player"),20);
            stick.Sample(new DeviceControlSample(true,.8,1));
            Assert.That(stick.BuildManual(new SimulationTick(1)).SteeringInput,Is.EqualTo(1000));
            var warm=new DeviceCommandBuilder(d,new AthleteId("player"),20);
            warm.Sample(new DeviceControlSample(false,1,1));
            for(int t=1;t<=100;t++)warm.BuildManual(new SimulationTick(t));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int t=101;t<=10100;t++)warm.BuildManual(new SimulationTick(t));
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [TestCase(false)] [TestCase(true)]
        public void ManualDirectionIsSignedYawAndNeutralNeverRequestsRecentering(bool gamepad)
        {
            var b=Create();b.Sample(new DeviceControlSample(gamepad,.8,-1,tuck:true),overtakePressed:true);
            var left=b.BuildManual(new SimulationTick(1));
            Assert.That(left.SteeringInput,Is.EqualTo(-ManualSkiCommand.SteeringUnits));
            Assert.That(left.AnalogEffort,Is.EqualTo(gamepad?800:20));
            Assert.That(left.TuckHeld&&left.OvertakeRequested,Is.True);
            Assert.That(left.ToEffortCommand().LineIntent,Is.Zero);
            b.Sample(new DeviceControlSample(gamepad,0,0));
            var neutral=b.BuildManual(new SimulationTick(2));
            Assert.That(neutral.SteeringInput,Is.Zero);
            Assert.That(neutral.OvertakeRequested,Is.False);
            b.Sample(new DeviceControlSample(gamepad,0,1));
            Assert.That(b.BuildManual(new SimulationTick(3)).SteeringInput,
                Is.EqualTo(ManualSkiCommand.SteeringUnits));
            Assert.Throws<ArgumentException>(()=>b.BuildManual(new SimulationTick(3)));
        }
        [Test]
        public void ManualGamepadDeadzoneAndActionEdgesMatchTheFixedTickContract()
        {
            var b=Create();b.Sample(new DeviceControlSample(true,.8,.1),overtakePressed:true);
            b.Sample(new DeviceControlSample(true,.8,.1),cancelPressed:true);
            Assert.Throws<ArgumentException>(()=>b.BuildManual(new SimulationTick(2)));
            var first=b.BuildManual(new SimulationTick(1));
            Assert.That(first.SteeringInput,Is.Zero);
            Assert.That(first.CancelOvertake,Is.True);
            Assert.That(first.OvertakeRequested,Is.False);
            b.Sample(new DeviceControlSample(true,.8,.6));
            int right=b.BuildManual(new SimulationTick(2)).SteeringInput;
            b.Sample(new DeviceControlSample(true,.8,-.6));
            Assert.That(b.BuildManual(new SimulationTick(3)).SteeringInput,Is.EqualTo(-right));
            Assert.That(right,Is.InRange(1,599));
        }
        [Test]
        public void WarmManualDeviceCommandGenerationDoesNotAllocate()
        {
            var b=Create();b.Sample(new DeviceControlSample(true,.8,.6));
            for(int i=1;i<=100;i++)b.BuildManual(new SimulationTick(i));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=101;i<=10100;i++)b.BuildManual(new SimulationTick(i));
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
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

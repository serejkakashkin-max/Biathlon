using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualSkiCommandTapeTests
    {
        private static readonly AthleteId Athlete=new AthleteId("manual_player");
        private static DeviceCommandBuilder Builder()=>new DeviceCommandBuilder(
            DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_v1.json")),
            Athlete,20);

        [Test]
        public void SampledYawAndActionEdgesPersistWithoutLegacyLineTargets()
        {
            var builder=Builder();var tape=new ManualSkiCommandTape("manual_input_capture_v2",
                "manual_authority_context",Athlete,4);
            builder.Sample(new DeviceControlSample(false,1,-1,tuck:true),overtakePressed:true);
            tape.Append(builder.BuildManual(new SimulationTick(1)));
            builder.Sample(new DeviceControlSample(false,0,0));
            tape.Append(builder.BuildManual(new SimulationTick(2)));
            builder.Sample(new DeviceControlSample(true,.7,.6),cancelPressed:true);
            tape.Append(builder.BuildManual(new SimulationTick(3)));
            string json=tape.ToJson();
            var restored=ManualSkiCommandTape.FromJson(json);
            restored.ValidateContext("manual_authority_context",Athlete);
            Assert.That(restored.ToJson(),Is.EqualTo(json));
            Assert.That(restored.Count,Is.EqualTo(3));
            for(int i=0;i<3;i++)
            {
                Assert.That(restored.GetCommand(i).ToJson(),Is.EqualTo(tape.GetCommand(i).ToJson()));
                Assert.That(restored.GetCommand(i).ToEffortCommand().LineIntent,Is.Zero);
            }
            Assert.That(restored.GetCommand(0).SteeringInput,Is.EqualTo(-1000));
            Assert.That(restored.GetCommand(0).TuckHeld&&restored.GetCommand(0).OvertakeRequested,Is.True);
            Assert.That(restored.GetCommand(1).SteeringInput,Is.Zero);
            Assert.That(restored.GetCommand(2).SteeringInput,Is.GreaterThan(0));
            Assert.That(restored.GetCommand(2).CancelOvertake,Is.True);
            Assert.That(json,Does.Not.Contain("line_intent"));
            Assert.That(json,Does.Contain("manual_ski_commands_v2"));
        }

        [Test]
        public void ForeignOrGappedCommandsAndDifferentContextsRejectBeforeAppendOrUse()
        {
            var tape=new ManualSkiCommandTape("manual_input_capture_v2","context_a",Athlete,2);
            var one=new ManualSkiCommand(new SimulationTick(1),Athlete,500,100);
            Assert.Throws<ArgumentException>(()=>tape.Append(new ManualSkiCommand(
                new SimulationTick(2),Athlete,500,100)));
            Assert.Throws<ArgumentException>(()=>tape.Append(new ManualSkiCommand(
                new SimulationTick(1),new AthleteId("other"),500,100)));
            Assert.That(tape.Count,Is.Zero);
            tape.Append(one);
            Assert.Throws<ArgumentException>(()=>tape.Append(one));
            Assert.Throws<ArgumentException>(()=>tape.ValidateContext("context_b",Athlete));
            Assert.Throws<ArgumentException>(()=>tape.ValidateContext("context_a",new AthleteId("other")));
            Assert.Throws<ArgumentOutOfRangeException>(()=>tape.GetCommand(1));
            tape.Append(new ManualSkiCommand(new SimulationTick(2),Athlete,500,-100));
            Assert.Throws<InvalidOperationException>(()=>tape.Append(new ManualSkiCommand(
                new SimulationTick(3),Athlete,500,0)));
            string json=tape.ToJson();
            Assert.Catch(()=>ManualSkiCommandTape.FromJson(json.Replace("manual_ski_commands_v2","legacy_commands_v1")));
            Assert.Catch(()=>ManualSkiCommandTape.FromJson(json.Replace("\"schema_version\":2","\"schema_version\":1")));
            Assert.Catch(()=>PlayerCommandTape.FromJson(json));
            var legacy=new PlayerCommandTape("old","context_a",Athlete,1);
            legacy.Append(new PlayerCommand(new SimulationTick(1),Athlete,500,100));
            Assert.Catch(()=>ManualSkiCommandTape.FromJson(legacy.ToJson()));
        }

        [Test]
        public void WarmManualTapeAppendDoesNotAllocate()
        {
            var tape=new ManualSkiCommandTape("manual_alloc_v2","context",Athlete,10000);
            for(int i=1;i<=100;i++)tape.Append(new ManualSkiCommand(new SimulationTick(i),Athlete,500,100));
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=101;i<=10000;i++)tape.Append(new ManualSkiCommand(new SimulationTick(i),Athlete,500,100));
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}

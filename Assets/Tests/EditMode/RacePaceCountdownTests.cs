using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class RacePaceCountdownTests
    {
        [Test] public void PaceDialStartsStoppedRisesHoldsFallsAndStaysStopped()
        {
            var d=DeviceControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("device_control_gameplay_v3.json"));
            var b=new DeviceCommandBuilder(d,new AthleteId("player"),20);long tick=0;
            Assert.That(b.BuildManual(new SimulationTick(++tick)).BrakeFraction,Is.EqualTo(1));
            b.Sample(new DeviceControlSample(false,1,0));ManualSkiCommand c=default;
            for(int i=0;i<32;i++)c=b.BuildManual(new SimulationTick(++tick));Assert.That(c.AnalogEffort,Is.EqualTo(800));
            b.Sample(new DeviceControlSample(false,0,0));c=b.BuildManual(new SimulationTick(++tick));Assert.That(c.AnalogEffort,Is.EqualTo(800));
            b.Sample(new DeviceControlSample(false,-1,0,brake:true));for(int i=0;i<32;i++)c=b.BuildManual(new SimulationTick(++tick));
            Assert.That(c.AnalogEffort,Is.Zero);b.Sample(new DeviceControlSample(false,0,0));c=b.BuildManual(new SimulationTick(++tick));
            Assert.That(c.BrakeFraction,Is.EqualTo(1));Assert.That(ManualSkiCommand.FromJson(c.ToJson()),Is.EqualTo(c));
        }
        [Test] public void CountdownShowsThreeTwoOneAndDoesNotSkipOnLargeFrame()
        {
            var d=RaceStartDefinition.FromJson(PhysiologyContractTests.DefinitionJson("race_start_v1.json"));
            var c=new RaceStartCountdown(d.CountdownSeconds);Assert.That(c.Number,Is.EqualTo(3));
            c.Advance(1);Assert.That(c.Number,Is.EqualTo(2));c.Advance(1);Assert.That(c.Number,Is.EqualTo(1));
            c.Advance(.99);Assert.That(c.Ready,Is.False);c.Advance(1);Assert.That(c.Ready,Is.True);Assert.That(c.Remaining,Is.Zero);
        }
    }
}

using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiParityAuditTests
    {
        [TestCase("expert","synthetic_flat_v1")]
        [TestCase("developing","synthetic_flat_v1")]
        [TestCase("expert","synthetic_rolling_v1")]
        [TestCase("developing","synthetic_rolling_v1")]
        public void RecordedAiCommandsHaveIdenticalCapabilityWithoutAnyAiController(string level,string track)
        {
            const int count=2;const ulong seed=61000;
            var events=new RaceEventLog(30000);var manualEvents=new RaceEventLog(30000);
            var runner=AiTelemetryTests.Create(count,events,new AiDecisionBuffer(8),track,level,seed);
            var manual=PackSprintFieldTests.Create(manualEvents,count,2,track,seed:seed);
            var inputs=new SprintAthleteInput[count];var lines=new double[count];
            int skiTicks=0,shootingTicks=0;
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<100000)
            {
                runner.Step();
                for(int i=0;i<count;i++){inputs[i]=runner.GetLastInput(i);lines[i]=runner.GetLastLateral(i);}
                // This field contains no AI instance and knows nothing about difficulty.
                manual.Step(inputs,lines);
                for(int i=0;i<count;i++)
                {
                    var a=runner.Field.GetAthlete(i);var b=manual.GetAthlete(i);
                    Assert.That(b.LastFrame,Is.EqualTo(a.LastFrame));
                    Assert.That(b.PhysiologyState,Is.EqualTo(a.PhysiologyState));
                    Assert.That(b.Motion,Is.EqualTo(a.Motion));
                    Assert.That(manual.GetDraft(i),Is.EqualTo(runner.Field.GetDraft(i)));
                    Assert.That(manual.GetLateral(i),Is.EqualTo(runner.Field.GetLateral(i)));
                    if(a.Lifecycle.Phase==CompetitionPhase.Skiing)skiTicks++;
                    if(a.ActiveShooting!=null)shootingTicks++;
                }
            }
            Assert.That(manual.IsComplete&&runner.Field.IsComplete,Is.True);
            Assert.That(manual.Result.ToJson(),Is.EqualTo(runner.Field.Result.ToJson()));
            Assert.That(skiTicks,Is.GreaterThan(0));Assert.That(shootingTicks,Is.GreaterThan(0));
            // AI adds policy events; every authoritative physical event must survive in order.
            int source=0;
            for(int i=0;i<manualEvents.Count;i++)
            {
                while(source<events.Count&&!events[source].Equals(manualEvents[i]))source++;
                Assert.That(source,Is.LessThan(events.Count),"Missing physical event "+i);source++;
            }
            TestContext.Out.WriteLine(level+" / "+track+": "+manual.Tick.Value+" ticks, "+manualEvents.Count+" physical events; exact frames and results.");
        }
    }
}

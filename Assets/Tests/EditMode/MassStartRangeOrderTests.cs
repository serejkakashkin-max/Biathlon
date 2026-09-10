using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class MassStartRangeOrderTests
    {
        [Test] public void PolicyRejectsUnknownRulesAndSchema()
        {
            var json=PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json");
            Assert.That(MassStartRangePolicyDefinition.FromJson(json).FirstBout,Is.EqualTo("start_number"));
            Assert.Catch(()=>MassStartRangePolicyDefinition.FromJson(json.Replace("start_number","random")));
            Assert.Catch(()=>MassStartRangePolicyDefinition.FromJson(json.Replace("arrival_order","fixed")));
            Assert.Catch(()=>MassStartRangePolicyDefinition.FromJson(json.Replace("\"schema_version\":1","\"schema_version\":2")));
        }
        [Test] public void FirstBibThenActualEntryOrderMatchesBodiesShootingAndReplay()
        {
            var log=new RaceEventLog(100000);var repeated=new RaceEventLog(100000);
            var a=MassStartFieldTests.Create(6,log,new AiDecisionCounts(),orderedLanes:true);
            var b=MassStartFieldTests.Create(6,repeated,new AiDecisionCounts(),orderedLanes:true);
            while(!a.Field.IsComplete && a.Field.Tick.Value<50000)
            {
                a.Step();b.Step();MassStartFieldTests.AssertClear(a.Field);
                for(int i=0;i<6;i++)
                {
                    var actor=a.Field.GetAthlete(i);Assert.That(actor.LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                    Assert.That(a.Field.GetAssignedRangeLane(i),Is.EqualTo(actor.AssignedShootingLane.Ordinal));
                    if(actor.ActiveShooting!=null)Assert.That(a.Field.GetPhysicalPoint(i).LoopId,Is.EqualTo("range_lane_"+actor.AssignedShootingLane.Ordinal));
                }
            }
            Assert.That(a.Field.IsComplete && b.Field.IsComplete,Is.True);
            var visits=new int[6];var next=new int[4];bool changed=false;int assignments=0;
            Assert.That(log.Count,Is.EqualTo(repeated.Count));
            for(int e=0;e<log.Count;e++)
            {
                Assert.That(log[e],Is.EqualTo(repeated[e]));if(log[e].EventType!=RaceEventType.MassStartRangeLaneAssigned)continue;
                int i=int.Parse(log[e].AthleteId.Value.Substring(5));int bout=visits[i]++;
                Assert.That(bout,Is.LessThan(4));int expected=bout==0?i+1:++next[bout];
                Assert.That(log[e].Payload,Is.EqualTo(expected));changed|=bout>0&&expected!=i+1;assignments++;
            }
            Assert.That(assignments,Is.EqualTo(24));Assert.That(changed,Is.True);
            foreach(int visitsForAthlete in visits)Assert.That(visitsForAthlete,Is.EqualTo(4));
            var result=a.Field.MassStartResult;Assert.That(result.ToJson(),Is.EqualTo(b.Field.MassStartResult.ToJson()));
            Assert.Catch(()=>MassStartResultSnapshot.FromJson(result.ToJson().Replace("\"rank\":1","\"rank\":30"),a.Field.StartGrid,20,new SimulationTick(1)));
            Assert.Catch(()=>MassStartResultSnapshot.FromJson(result.ToJson(),a.Field.StartGrid,21,new SimulationTick(1)));
            Assert.That(a.Field.PrecisionFallbackTicks,Is.Zero);
            using(var sha=System.Security.Cryptography.SHA256.Create())TestContext.Out.WriteLine("OrderedMassStart6 ticks="+a.Field.Tick.Value+"; assignments="+assignments+"; result_sha256="+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(result.ToJson()))).Replace("-","").ToLowerInvariant());
        }
    }
}

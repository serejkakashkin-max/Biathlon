using System;
using System.IO;
using System.Globalization;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class AiTelemetryTests
    {
        [Test] public void RacePolicyRejectsMalformedBoundsAndRequiresFreshComposition()
        {
            string json=PhysiologyContractTests.DefinitionJson("ai_race_policy_provisional_v1.json");
            var definition=AiRacePolicyDefinition.FromJson(json);
            Assert.That(AiRacePolicyDefinition.FromJson(definition.ToJson()).ToJson(),Is.EqualTo(definition.ToJson()));
            foreach(string field in new[]{"physical_radius_m","preview_stride_m","preview_samples","attack_opportunity","attack_minimum_reserve","final_sprint_distance_m","shooting_time_urgency"})
                Assert.Catch(()=>AiRacePolicyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,field,"-1")));
            Assert.Catch(()=>AiRacePolicyDefinition.FromJson("{}"));
            var log=new RaceEventLog(1000);var runner=Create(1,log,new AiDecisionBuffer(8));runner.Step();
            Assert.Catch(()=>new PackAiRaceRunner(runner.Field,PhysiologyContractTests.DefinitionJson,
                new[]{AiPersonalityTests.Definition("attacker")},AiDifficultyTests.Definition("expert"),123,log,new AiDecisionBuffer(8)));
        }
        public static PackAiRaceRunner Create(int count, RaceEventLog events, IAiDecisionSink sink, string track="synthetic_flat_v1", string level="expert", ulong seed=123)
        {
            var field=PackSprintFieldTests.Create(events,count,2,track,seed:seed);
            var profiles=new AiPersonalityDefinition[count];
            for(int i=0;i<count;i++) profiles[i]=AiPersonalityTests.Definition(i%2==0?"attacker":"patient");
            return new PackAiRaceRunner(field,PhysiologyContractTests.DefinitionJson,profiles,AiDifficultyTests.Definition(level),seed,events,sink);
        }
        [Test] public void FullAiCompositionReplaysDecisionsAndCapturesOwnedScores()
        {
            var log=new RaceEventLog(30000);var replayLog=new RaceEventLog(30000);
            var trace=new AiDecisionBuffer(128);var replayTrace=new AiDecisionBuffer(128);
            var a=Create(2,log,trace);var b=Create(2,replayLog,replayTrace);Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));
            AiDecisionTrace first=default;
            while(!a.Field.IsComplete && a.Field.Tick.Value<100000)
            {
                a.Step();b.Step();if(a.Field.Tick.Value==1) first=trace.Get(0);
                for(int i=0;i<2;i++) Assert.That(a.Field.GetAthlete(i).LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                Assert.That(trace.Total,Is.EqualTo(replayTrace.Total));
                if(trace.Count>0) Assert.That(trace.Get(trace.Count-1),Is.EqualTo(replayTrace.Get(replayTrace.Count-1)));
            }
            Assert.That(a.Field.IsComplete,Is.True);Assert.That(a.Field.Result.ToJson(),Is.EqualTo(b.Field.Result.ToJson()));
            Assert.That(a.ShootingDecisions,Is.GreaterThan(0));Assert.That(trace.Overwritten,Is.GreaterThan(0));
            Assert.That(first.Tick.Value,Is.EqualTo(1));Assert.That(first.Utilities.Get(1).Eligible,Is.True);
            Assert.That(log.Count,Is.EqualTo(replayLog.Count));int decisions=0;
            for(int i=0;i<log.Count;i++) { Assert.That(log[i],Is.EqualTo(replayLog[i]));if(log[i].EventType==RaceEventType.AiIntentChanged) decisions++; }
            Assert.That(decisions,Is.GreaterThan(0));
            var csv=new StringWriter();trace.WriteCsv(csv);Assert.That(csv.ToString(),Does.Contain("personality,total"));
            var culture=CultureInfo.CurrentCulture;
            try { CultureInfo.CurrentCulture=new CultureInfo("fr-FR");var other=new StringWriter();trace.WriteCsv(other);Assert.That(other.ToString(),Is.EqualTo(csv.ToString())); }
            finally { CultureInfo.CurrentCulture=culture; }
            TestContext.Out.WriteLine("AI integrated: ticks="+a.Field.Tick.Value+", tactical="+a.TacticalDecisions+", shooting="+a.ShootingDecisions+", trace_rows="+trace.Total);
        }
        [Test] public void TraceBufferHasExplicitOverwriteAndSteadyRunnerAllocatesNothing()
        {
            Assert.Catch(()=>new AiDecisionBuffer(0));var buffer=new AiDecisionBuffer(2);
            for(int i=0;i<3;i++) buffer.Record(new AiDecisionTrace(new SimulationTick(i),new AthleteId("self"),AiTraceKind.Tactical,default,default,default,default,default,default,default,default,0));
            Assert.That(buffer.Count,Is.EqualTo(2));Assert.That(buffer.Overwritten,Is.EqualTo(1));Assert.That(buffer.Get(0).Tick.Value,Is.EqualTo(1));
            Assert.Catch(()=>buffer.Get(2));
            var a=Create(1,new RaceEventLog(30000),new AiDecisionBuffer(64));for(int i=0;i<100;i++) a.Step();
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++) a.Step();
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}

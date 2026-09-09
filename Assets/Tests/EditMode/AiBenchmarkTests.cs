using Biathlon.AI;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiBenchmarkTests
    {
        [Test] public void CountingObserverPreservesCompleteRaceAndAccountsEveryDecision()
        {
            var counts = new AiDecisionCounts();
            var events = new RaceEventLog(30000); var replayEvents = new RaceEventLog(30000);
            var a = AiTelemetryTests.Create(1, events, counts);
            var b = AiTelemetryTests.Create(1, replayEvents, new AiDecisionBuffer(1));
            while (!a.Field.IsComplete && a.Field.Tick.Value < 120000) { a.Step(); b.Step(); }
            Assert.That(a.Field.IsComplete && b.Field.IsComplete, Is.True);
            Assert.That(a.Field.Result.ToJson(), Is.EqualTo(b.Field.Result.ToJson()));
            Assert.That(events.Count, Is.EqualTo(replayEvents.Count));
            for (int i = 0; i < events.Count; i++) Assert.That(events[i], Is.EqualTo(replayEvents[i]));
            Assert.That(counts.Tactical, Is.EqualTo(a.TacticalDecisions));
            Assert.That(counts.Shooting, Is.EqualTo(a.ShootingDecisions).And.GreaterThan(0));
            long intents = 0, efforts = 0;
            for (int i = 0; i < 8; i++) intents += counts.IntentCount(i);
            for (int i = 0; i <= 1000; i++) efforts += counts.EffortCount(i);
            Assert.That(intents, Is.EqualTo(counts.Tactical)); Assert.That(efforts, Is.EqualTo(intents));
        }
        [Test] public void HistogramMatchesPublishedSamplesAndWarmCountingDoesNotAllocate()
        {
            var counts = new AiDecisionCounts();
            var buffer = new AiDecisionBuffer(1024);
            var runner = AiTelemetryTests.Create(2, new RaceEventLog(10000), buffer);
            for (int i = 0; i < 200; i++) runner.Step();
            Assert.That(buffer.Overwritten, Is.Zero);
            Assert.That(buffer.Count, Is.GreaterThan(0));
            var intents = new long[8]; var efforts = new long[1001];
            Assert.That(counts.Tactical + counts.Shooting, Is.Zero);
            for (int i = 0; i < buffer.Count; i++)
            {
                var row = buffer.Get(i); counts.Record(row);
                if (row.Kind != AiTraceKind.Tactical) continue;
                intents[(int)row.Intent.Kind]++; efforts[row.Pace.Input.RequestedEffort]++;
            }
            for (int i = 0; i < 8; i++) Assert.That(counts.IntentCount(i), Is.EqualTo(intents[i]));
            for (int i = 0; i <= 1000; i++) Assert.That(counts.EffortCount(i), Is.EqualTo(efforts[i]));
            var sample = buffer.Get(0);
            for (int i = 0; i < 100; i++) counts.Record(sample);
            long before = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) counts.Record(sample);
            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero, "Authoritative allocation counter is the standalone runtime.");
        }
    }
}

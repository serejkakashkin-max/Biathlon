using System;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintCapacityTests
    {
        public static string Benchmark()
        {
            long probeBefore = GC.GetAllocatedBytesForCurrentThread();
            var allocationProbe = new byte[4096];
            long probeBytes = GC.GetAllocatedBytesForCurrentThread() - probeBefore;
            GC.KeepAlive(allocationProbe);
            var events = new RaceEventLog(30000); var field = SprintFieldTests.Create(events, 60); var commands = new SprintAthleteInput[60];
            SprintFieldTests.Fill(field, commands); field.Step(commands);
            var watch = new Stopwatch(); long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) { SprintFieldTests.Fill(field, commands); field.Step(commands); }
            long activeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread(); watch.Start();
            while (!field.IsComplete && field.Tick.Value < 100000) { SprintFieldTests.Fill(field, commands); field.Step(commands); }
            watch.Stop(); long completeBytes = GC.GetAllocatedBytesForCurrentThread() - before;
            if (!field.IsComplete || field.Result.Count != 60) throw new InvalidOperationException("Capacity race did not finish.");
            int starts = 0, shots = 0, penaltyEvents = 0, resultEvents = 0, totalPenalties = 0;
            for (int i = 0; i < field.Count; i++)
            {
                var s = field.GetAthlete(i); totalPenalties += s.TotalPenaltyLoops;
                if (s.Lifecycle.CompletedBouts != 2 || s.Lifecycle.CompletedLaps != 3 ||
                    s.Motion.DistanceNanometers != (3000 + 150L * s.TotalPenaltyLoops) * 1000000000)
                    throw new InvalidOperationException("Capacity athlete obligations/distance mismatch.");
            }
            var trace = new StringBuilder();
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.EventType == RaceEventType.ScheduledStartDue) starts++;
                if (e.EventType == RaceEventType.ShotResolved) shots++;
                if (e.EventType == RaceEventType.PenaltyLoopCompleted) penaltyEvents++;
                if (e.EventType == RaceEventType.CompetitionResultRecorded) resultEvents++;
                trace.Append(e.Tick.Value).Append(',').Append(e.AthleteId.Value).Append(',').Append((int)e.EventType).Append(',').Append(e.Payload).Append('\n');
            }
            if (starts != 60 || shots != 600 || penaltyEvents != totalPenalties || resultEvents != 60) throw new InvalidOperationException("Capacity event accounting mismatch.");
            string Hash(string value)
            { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant(); }
            return "{\"schema_version\":1,\"id\":\"m5_sprint_60_capacity\",\"athletes\":60,\"finished\":60,\"ticks\":" + field.Tick.Value +
                ",\"simulated_seconds\":" + (field.Tick.Value / 20.0).ToString("R", CultureInfo.InvariantCulture) +
                ",\"allocation_counter_probe_bytes\":" + probeBytes + ",\"allocation_counter_supported\":" + (probeBytes >= 4096 ? "true" : "false") +
                ",\"measured_from_tick\":101,\"elapsed_ms\":" + watch.Elapsed.TotalMilliseconds.ToString("0.####", CultureInfo.InvariantCulture) +
                ",\"active_100_ticks_allocated_bytes\":" + activeBytes + ",\"remaining_race_allocated_bytes\":" + completeBytes +
                ",\"events\":" + events.Count + ",\"shots\":" + shots + ",\"penalties\":" + totalPenalties +
                ",\"events_sha256\":\"" + Hash(trace.ToString()) + "\",\"results_sha256\":\"" + Hash(field.Result.ToJson()) + "\"}";
        }
        [Test] public void SixtyAthletesCompleteWithAccountedEventsAndAllocationFreeSteadyTicks()
        {
            string result = Benchmark(); TestContext.WriteLine(result);
            Assert.That(result, Does.Contain("\"finished\":60")); Assert.That(result, Does.Contain("\"active_100_ticks_allocated_bytes\":0"));
            Assert.That(result, Does.Contain("ce1320db3c7b1aa7e021cf49aa77ea04994148de655c66ed1c5db8d1a96414d5"));
            Assert.That(result, Does.Contain("dd9cd3c46e6107b50aebd86936e07a246be1ff72f550fbad77d5e46394182299"));
        }
    }
}

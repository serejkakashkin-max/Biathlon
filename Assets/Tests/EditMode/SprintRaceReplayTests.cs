using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintRaceReplayTests
    {
        private static void Fill(SprintFieldSession field, SprintAthleteInput[] inputs)
        {
            SprintFieldTests.Fill(field, inputs);
            if (field.Tick.Value % 1000 < 500) return;
            var v = inputs[0];
            inputs[0] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(v.Ski.Effort.Tick, v.Ski.Effort.AthleteId, 950), 1, 0.8, 0.3,
                sprintRequested: true, movementEfficiency: 0.97), new WeaponAimCommand(v.Aim.Tick, v.Aim.AthleteId, 1, -1, true), true, true,
                new WindVector(1, 0), new ShootingPressureContext(0.8, 0.5, 0.7, 0.6, true));
        }
        [Test] public void PersistedRunsReproduceCompleteFieldFramesEventsAndResultsWithIndependentCursors()
        {
            var la = new RaceEventLog(4000); var lb = new RaceEventLog(4000); var lc = new RaceEventLog(4000);
            var a = SprintFieldTests.Create(la, 2); var tape = new SprintRaceCommandTape("race_replay", a, 100);
            var inputs = new SprintAthleteInput[2];
            while (!a.IsComplete && tape.TickCount < 100000) { Fill(a, inputs); tape.StepAndRecord(a, inputs); }
            Assert.That(a.IsComplete, Is.True); Assert.That(tape.RunCount, Is.LessThan(100));
            string json = tape.ToJson(); var restored = SprintRaceCommandTape.FromJson(json);
            Assert.That(restored.ToJson(), Is.EqualTo(json)); Assert.That(restored.TickCount, Is.EqualTo(tape.TickCount));
            var b = SprintFieldTests.Create(lb, 2); var c = SprintFieldTests.Create(lc, 2);
            var direct = SprintFieldTests.Create(new RaceEventLog(4000), 2);
            var replay = restored.CreateReplay(b); var independent = restored.CreateReplay(c);
            while (replay.HasNext)
            {
                replay.Step(); independent.Step();
                Fill(direct, inputs); direct.Step(inputs);
                for (int i = 0; i < b.Count; i++)
                { Assert.That(b.GetAthlete(i).LastFrame, Is.EqualTo(c.GetAthlete(i).LastFrame));
                  Assert.That(b.GetAthlete(i).LastFrame, Is.EqualTo(direct.GetAthlete(i).LastFrame)); }
            }
            Assert.That(b.Result.ToJson(), Is.EqualTo(a.Result.ToJson())); Assert.That(c.Result.ToJson(), Is.EqualTo(a.Result.ToJson()));
            Assert.That(lb.Count, Is.EqualTo(la.Count)); Assert.That(lc.Count, Is.EqualTo(la.Count));
            for (int i = 0; i < la.Count; i++) { Assert.That(lb[i], Is.EqualTo(la[i])); Assert.That(lc[i], Is.EqualTo(la[i])); }
            Assert.Catch(() => replay.Step());
        }
        [Test] public void CapacityContextAndMalformedCoverageFailBeforeRaceAdvances()
        {
            var log = new RaceEventLog(4000); var field = SprintFieldTests.Create(log, 2); var tape = new SprintRaceCommandTape("bounded", field, 2);
            var inputs = new SprintAthleteInput[2]; SprintFieldTests.Fill(field, inputs); tape.StepAndRecord(field, inputs);
            SprintFieldTests.Fill(field, inputs); var v = inputs[1];
            inputs[1] = new SprintAthleteInput(v.Ski, new WeaponAimCommand(v.Aim.Tick, v.Aim.AthleteId, 1, 0));
            int count = log.Count; Assert.Catch(() => tape.StepAndRecord(field, inputs));
            Assert.That(field.Tick.Value, Is.EqualTo(1)); Assert.That(tape.TickCount, Is.EqualTo(1)); Assert.That(log.Count, Is.EqualTo(count));
            Assert.Catch(() => tape.CreateReplay(SprintFieldTests.Create(new RaceEventLog(4000), 2, 124)));
            string json = tape.ToJson();
            foreach (var pair in new[] { new[] { "schema_version", "2" }, new[] { "tick_count", "2" }, new[] { "first_tick", "2" },
                new[] { "last_tick", "0" }, new[] { "athlete_index", "8" }, new[] { "effort", "1001" }, new[] { "aim_x", "1001" } })
                Assert.Catch(() => SprintRaceCommandTape.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair[0], pair[1])), pair[0]);
        }
        [Test] public void UnchangedIntentRecordingAndReplayDoNotAllocatePerTick()
        {
            var field = SprintFieldTests.Create(new RaceEventLog(4000), 2); var tape = new SprintRaceCommandTape("gc", field, 2); var inputs = new SprintAthleteInput[2];
            SprintFieldTests.Fill(field, inputs); tape.StepAndRecord(field, inputs);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) { SprintFieldTests.Fill(field, inputs); tape.StepAndRecord(field, inputs); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(tape.RunCount, Is.EqualTo(2));
            var restored = SprintRaceCommandTape.FromJson(tape.ToJson()); var replayField = SprintFieldTests.Create(new RaceEventLog(4000), 2);
            var replay = restored.CreateReplay(replayField); replay.Step();
            before = GC.GetAllocatedBytesForCurrentThread(); while (replay.HasNext) replay.Step();
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(replayField.Tick, Is.EqualTo(field.Tick));
        }
    }
}

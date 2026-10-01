using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PackSprintRaceReplayTests
    {
        private static void FillOrdered(PackSprintFieldSession field, SprintAthleteInput[] commands, double[] lines)
        {
            for (int i = 0; i < field.Count; i++)
            {
                var actor = field.GetAthlete(i); var tick = field.Tick.Next();
                commands[i] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, actor.AthleteId, 800), 1, 1, 0),
                    new WeaponAimCommand(tick, actor.AthleteId, 0, 0), true);
                var point = field.GetPhysicalPoint(i);
                long offset = actor.RouteNanometers % (300 * SkiState.NanometersPerMeter);
                long centerAt = (actor.RangePath.LaneOrdinal == 1 ? 5 : 25) * SkiState.NanometersPerMeter;
                lines[i] = point.LoopId == "main" && (offset < centerAt || offset >= 100 * SkiState.NanometersPerMeter)
                    ? (i == 0 ? -1 : 1) : 0;
            }
        }
        private static void AssertPublishedState(PackSprintFieldSession actual, PackSprintFieldSession expected)
        {
            Assert.That(actual.Tick, Is.EqualTo(expected.Tick));
            for (int i = 0; i < actual.Count; i++)
            {
                Assert.That(actual.GetAthlete(i).LastFrame, Is.EqualTo(expected.GetAthlete(i).LastFrame));
                Assert.That(actual.GetLateral(i), Is.EqualTo(expected.GetLateral(i)));
                // Points intentionally retain their map owner; rebuilt replay maps have distinct references.
                var actualPoint = actual.GetPhysicalPoint(i); var expectedPoint = expected.GetPhysicalPoint(i);
                Assert.That(actualPoint.LoopId, Is.EqualTo(expectedPoint.LoopId));
                Assert.That(actualPoint.LocalNanometers, Is.EqualTo(expectedPoint.LocalNanometers));
                Assert.That(actualPoint.LoopLengthNanometers, Is.EqualTo(expectedPoint.LoopLengthNanometers));
                Assert.That(actualPoint.Closed, Is.EqualTo(expectedPoint.Closed));
                Assert.That(actualPoint.IsValid, Is.EqualTo(expectedPoint.IsValid));
                Assert.That(actual.GetDraft(i), Is.EqualTo(expected.GetDraft(i)));
                Assert.That(actual.GetJunctionWaitSince(i), Is.EqualTo(expected.GetJunctionWaitSince(i)));
                if (actual.GetAthlete(i).RangePath != null)
                    Assert.That(actual.GetAthlete(i).RangePath.LaneOrdinal, Is.EqualTo(expected.GetAthlete(i).RangePath.LaneOrdinal));
            }
        }
        private static void AssertEvents(RaceEventLog actual, RaceEventLog expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < actual.Count; i++) Assert.That(actual[i], Is.EqualTo(expected[i]));
        }

        [Test] public void PersistedOrderedPathRaceRebuildsLayoutAndReproducesIndependentCursorsFramesArrivalsAndResults()
        {
            var policy = MassStartRangePolicyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json"));
            var sourceFixture = PhysicalRangePackTests.CreateFixture(gridColumns: 2);
            var sourceLog = new RaceEventLog(30000);
            var source = PhysicalRangePackTests.CreatePack(sourceFixture, sourceLog, policy);
            var tape = new PackSprintRaceCommandTape("ordered_path_replay", source, 100);
            var commands = new SprintAthleteInput[2]; var lines = new double[2];
            bool privatePath = false, exit = false, penalty = false, lateralMovement = false;
            while (!source.IsComplete && source.Tick.Value < 80000)
            {
                FillOrdered(source, commands, lines); tape.StepAndRecord(source, commands, lines);
                for (int i = 0; i < 2; i++)
                {
                    string loop = source.GetPhysicalPoint(i).LoopId;
                    privatePath |= loop == "mat_1" || loop == "mat_2"; exit |= loop == "exit_aisle"; penalty |= loop == "penalty";
                    lateralMovement |= Math.Abs(source.GetLateral(i)) > 0.6;
                }
            }
            Assert.That(source.IsComplete, Is.True); Assert.That(privatePath && exit && penalty && lateralMovement, Is.True);
            string json = tape.ToJson(); var persisted = PackSprintRaceCommandTape.FromJson(json);
            Assert.That(persisted.ToJson(), Is.EqualTo(json)); Assert.That(persisted.RunCount, Is.LessThan(100));
            var replayFixture = PhysicalRangePackTests.CreateFixture(gridColumns: 2);
            Assert.That(replayFixture.Layout, Is.Not.SameAs(sourceFixture.Layout));
            var replayLog = new RaceEventLog(30000); var independentLog = new RaceEventLog(30000); var directLog = new RaceEventLog(30000);
            var replayField = PhysicalRangePackTests.CreatePack(replayFixture, replayLog, policy);
            var independentField = PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(gridColumns: 2), independentLog, policy);
            var direct = PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(gridColumns: 2), directLog, policy);
            var replay = persisted.CreateReplay(replayField); var independent = persisted.CreateReplay(independentField);
            while (replay.HasNext)
            {
                FillOrdered(direct, commands, lines); direct.Step(commands, lines);
                replay.Step(); independent.Step();
                AssertPublishedState(replayField, direct); AssertPublishedState(independentField, direct);
            }
            Assert.That(replayField.IsComplete && independentField.IsComplete && direct.IsComplete, Is.True);
            Assert.That(independent.HasNext, Is.False);
            Assert.That(replayField.MassStartResult.ToJson(), Is.EqualTo(source.MassStartResult.ToJson()));
            Assert.That(independentField.MassStartResult.ToJson(), Is.EqualTo(source.MassStartResult.ToJson()));
            Assert.That(direct.MassStartResult.ToJson(), Is.EqualTo(source.MassStartResult.ToJson()));
            int assignments = 0;
            for (int i = 0; i < sourceLog.Count; i++) if (sourceLog[i].EventType == RaceEventType.PhysicalRangePathAssigned) assignments++;
            Assert.That(assignments, Is.EqualTo(8));
            for (int bout = 0; bout < 4; bout++)
            {
                Assert.That(replayField.GetRangeArrivalCount(bout), Is.EqualTo(2));
                for (int actor = 0; actor < 2; actor++)
                    Assert.That(replayField.GetAcceptedRangeArrival(actor, bout), Is.EqualTo(source.GetAcceptedRangeArrival(actor, bout)));
            }
            Assert.That(replayField.GetAcceptedRangeArrival(0, 0).Tick, Is.EqualTo(replayField.GetAcceptedRangeArrival(1, 0).Tick));
            AssertEvents(replayLog, sourceLog); AssertEvents(independentLog, sourceLog); AssertEvents(directLog, sourceLog);
            Assert.Throws<InvalidOperationException>(() => replay.Step());
            TestContext.Out.WriteLine("Ordered path persisted replay: ticks=" + tape.TickCount + ", runs=" + tape.RunCount + ", events=" + sourceLog.Count);
        }

        [Test] public void LateralOnlyIntentChangesAreRecordedEvenWhenTheActualBodyHasNotReachedItsTarget()
        {
            var log = new RaceEventLog(1000); var source = PackSprintFieldTests.Create(log, 1);
            var tape = new PackSprintRaceCommandTape("lateral_requests", source, 10);
            var commands = new SprintAthleteInput[1]; var lines = new double[1];
            string technique = new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson).Technique.GetProfile(0).Id;
            for (int i = 0; i < 4; i++)
            {
                var tick = source.Tick.Next(); var id = source.GetAthlete(0).AthleteId;
                commands[0] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, id, 833), 0.95, 0.81, 0.3,
                    true, true, technique, 0.97), new WeaponAimCommand(tick, id, 123, -234, true), true, true,
                    new WindVector(1.25, -0.5), new ShootingPressureContext(0.8, 0.7, 0.6, 0.5, true, true));
                lines[0] = i < 2 ? 2 : -2; tape.StepAndRecord(source, commands, lines);
            }
            Assert.That(tape.RunCount, Is.EqualTo(2)); Assert.That(Math.Abs(source.GetLateral(0)), Is.LessThan(2));
            string json = tape.ToJson();
            foreach (string field in new[] { "\"effort\":833", "\"form\":0.95", "\"line_quality\":0.81", "\"headwind_mps\":0.3",
                "\"tuck\":true", "\"sprint\":true", "\"movement_efficiency\":0.97", "\"aim_x\":123", "\"aim_y\":-234", "\"fine_aim\":true",
                "\"trigger\":true", "\"hold_breath\":true", "\"wind_east_mps\":1.25", "\"wind_north_mps\":-0.5", "\"race_importance\":0.8",
                "\"position_importance\":0.7", "\"rival_proximity\":0.6", "\"confidence\":0.5", "\"final_standing\":true", "\"first_podium\":true",
                "\"requested_lateral_m\":2", "\"requested_lateral_m\":-2", "\"technique_id\":\"" + technique + "\"" })
                Assert.That(json, Does.Contain(field));
            var restored = PackSprintRaceCommandTape.FromJson(json);
            var replayLog = new RaceEventLog(1000); var replayField = PackSprintFieldTests.Create(replayLog, 1); var replay = restored.CreateReplay(replayField);
            while (replay.HasNext) replay.Step();
            AssertPublishedState(replayField, source); AssertEvents(replayLog, log);
        }

        [TestCase("short_inputs")][TestCase("short_lateral")][TestCase("nan")][TestCase("infinity")][TestCase("out_of_range")]
        [TestCase("wrong_athlete")][TestCase("wrong_tick")][TestCase("unknown_technique")]
        public void InvalidCompleteBatchIsRejectedBeforeAnyFieldTapeOrEventChanges(string defect)
        {
            var log = new RaceEventLog(1000); var field = PackSprintFieldTests.Create(log, 2);
            var tape = new PackSprintRaceCommandTape("atomic_batch", field, 10);
            var commands = new SprintAthleteInput[2]; var lines = new double[2]; PackSprintFieldTests.Fill(field, commands);
            string before = tape.ToJson(); int events = log.Count; var last = commands[1];
            switch (defect)
            {
                case "short_inputs": Array.Resize(ref commands, 1); break;
                case "short_lateral": Array.Resize(ref lines, 1); break;
                case "nan": lines[1] = double.NaN; break;
                case "infinity": lines[1] = double.PositiveInfinity; break;
                case "out_of_range": lines[1] = 1000001; break;
                case "wrong_athlete": commands[1] = commands[0]; break;
                case "wrong_tick": commands[1] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(new SimulationTick(2), last.Aim.AthleteId, 800), 1, 1, 0),
                    new WeaponAimCommand(new SimulationTick(2), last.Aim.AthleteId, 0, 0)); break;
                case "unknown_technique": commands[1] = new SprintAthleteInput(new TimeTrialInput(last.Ski.Effort, 1, 1, 0, techniqueId: "missing"), last.Aim); break;
            }
            Assert.Catch(() => tape.StepAndRecord(field, commands, lines));
            Assert.That(tape.ToJson(), Is.EqualTo(before)); Assert.That(field.Tick.Value, Is.Zero); Assert.That(log.Count, Is.EqualTo(events));
            for (int i = 0; i < field.Count; i++)
            {
                Assert.That(field.GetAthlete(i).Tick.Value, Is.Zero); Assert.That(field.GetAthlete(i).Motion.DistanceNanometers, Is.Zero);
                Assert.That(field.GetLateral(i), Is.Zero); Assert.That(field.GetJunctionWaitSince(i), Is.Zero);
            }
            // A rejected row does not consume capacity or prevent retry with the correct full batch.
            commands = new SprintAthleteInput[2]; lines = new double[2]; PackSprintFieldTests.Fill(field, commands);
            tape.StepAndRecord(field, commands, lines); Assert.That(field.Tick.Value, Is.EqualTo(1)); Assert.That(tape.RunCount, Is.EqualTo(2));
        }

        [Test] public void CapacityAndExternalClockAdvanceFailBeforeRecordingChanges()
        {
            var log = new RaceEventLog(1000); var field = PackSprintFieldTests.Create(log, 2);
            var tape = new PackSprintRaceCommandTape("bounded_pack", field, 2);
            var commands = new SprintAthleteInput[2]; var lines = new double[2];
            PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            string before = tape.ToJson(); int events = log.Count; var frame = field.GetAthlete(0).LastFrame;
            PackSprintFieldTests.Fill(field, commands); lines[1] = 1;
            Assert.Catch(() => tape.StepAndRecord(field, commands, lines));
            Assert.That(field.Tick.Value, Is.EqualTo(1)); Assert.That(tape.ToJson(), Is.EqualTo(before));
            Assert.That(log.Count, Is.EqualTo(events)); Assert.That(field.GetAthlete(0).LastFrame, Is.EqualTo(frame));
            lines[1] = 0; field.Step(commands, lines); PackSprintFieldTests.Fill(field, commands); events = log.Count;
            Assert.Catch(() => tape.StepAndRecord(field, commands, lines));
            Assert.That(field.Tick.Value, Is.EqualTo(2)); Assert.That(tape.ToJson(), Is.EqualTo(before)); Assert.That(log.Count, Is.EqualTo(events));
            Assert.Catch(() => new PackSprintRaceCommandTape("too_late", field, 10));
        }

        [Test] public void ExactContextRosterAndFreshClockAreRequiredBeforeReplayOrRecording()
        {
            var field = PackSprintFieldTests.Create(new RaceEventLog(1000), 2);
            var tape = new PackSprintRaceCommandTape("exact_context", field, 10);
            var commands = new SprintAthleteInput[2]; var lines = new double[2]; PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            var mismatchLog = new RaceEventLog(1000); var mismatch = PackSprintFieldTests.Create(mismatchLog, 2, seed: 124);
            Assert.Catch(() => tape.CreateReplay(mismatch));
            PackSprintFieldTests.Fill(mismatch, commands); int events = mismatchLog.Count;
            Assert.Catch(() => tape.StepAndRecord(mismatch, commands, lines));
            Assert.That(mismatch.Tick.Value, Is.Zero); Assert.That(mismatchLog.Count, Is.EqualTo(events));
            Assert.Catch(() => tape.CreateReplay(field));
            Assert.Catch(() => tape.CreateReplay(PackSprintFieldTests.Create(new RaceEventLog(1000), 1)));
            string json = tape.ToJson();
            var reversed = PackSprintRaceCommandTape.FromJson(json.Replace("\"athletes\":[\"athlete_0\",\"athlete_1\"]", "\"athletes\":[\"athlete_1\",\"athlete_0\"]"));
            Assert.Catch(() => reversed.CreateReplay(PackSprintFieldTests.Create(new RaceEventLog(1000), 2)));
            var policy = MassStartRangePolicyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json"));
            var authored = PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(), new RaceEventLog(1000), policy);
            var authoredTape = new PackSprintRaceCommandTape("path_context", authored, 2);
            var changedGeometry = PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(flatPrivate: true), new RaceEventLog(1000), policy);
            Assert.Catch(() => authoredTape.CreateReplay(changedGeometry)); Assert.That(changedGeometry.Tick.Value, Is.Zero);
        }

        [Test] public void MalformedPersistedCoveragePayloadAndTypeNeverAdvanceAReplayTarget()
        {
            var field = PackSprintFieldTests.Create(new RaceEventLog(1000), 2);
            var tape = new PackSprintRaceCommandTape("malformed_pack", field, 10);
            var commands = new SprintAthleteInput[2]; var lines = new double[2]; PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            string json = tape.ToJson(); var log = new RaceEventLog(1000); var target = PackSprintFieldTests.Create(log, 2); int count = log.Count;
            foreach (var pair in new[] { new[] { "schema_version", "2" }, new[] { "tick_count", "2" }, new[] { "tick_count", "-1" },
                new[] { "first_tick", "2" }, new[] { "last_tick", "0" }, new[] { "athlete_index", "9" }, new[] { "effort", "1001" },
                new[] { "aim_x", "1001" }, new[] { "form", "2" }, new[] { "line_quality", "2" }, new[] { "movement_efficiency", "2" },
                new[] { "wind_east_mps", "101" }, new[] { "confidence", "2" }, new[] { "requested_lateral_m", "1000001" } })
            {
                Assert.Catch(() => PackSprintRaceCommandTape.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair[0], pair[1])), pair[0]);
                Assert.That(target.Tick.Value, Is.Zero); Assert.That(log.Count, Is.EqualTo(count));
            }
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(json.Replace("pack_sprint_race_commands_v1", "other_tape")));
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(Regex.Replace(json, ",?\"requested_lateral_m\"\\s*:\\s*0", "")));
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(json.Replace("\"athlete_1\"", "\"athlete_0\"")));
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(new Regex("(\"athlete_index\"\\s*:\\s*)1").Replace(json, "${1}0", 1)));
            var oldField = SprintFieldTests.Create(new RaceEventLog(1000), 2); var oldTape = new SprintRaceCommandTape("legacy", oldField, 2);
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(oldTape.ToJson()));
            Assert.Catch(() => SprintRaceCommandTape.FromJson(json));
            var valid = PackSprintRaceCommandTape.FromJson(json).CreateReplay(target); valid.Step();
            AssertPublishedState(target, field);
        }

        [Test] public void PersistedRunGapsOverlapsAndRedundantIntentAreRejected()
        {
            var field = PackSprintFieldTests.Create(new RaceEventLog(1000), 1);
            var tape = new PackSprintRaceCommandTape("run_chain", field, 10); var commands = new SprintAthleteInput[1]; var lines = new double[1];
            PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            PackSprintFieldTests.Fill(field, commands); lines[0] = 1; tape.StepAndRecord(field, commands, lines);
            PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            string json = tape.ToJson();
            // Both modified runs still have legal bounds and global ordering: only chain continuity fails.
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(new Regex("(\"first_tick\"\\s*:\\s*)3").Replace(json, "${1}2", 1)));
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(new Regex("(\"first_tick\"\\s*:\\s*)3").Replace(json, "${1}4", 1)));
            Assert.Catch(() => PackSprintRaceCommandTape.FromJson(PhysiologyContractTests.ReplaceNumber(json, "requested_lateral_m", "0")));
        }

        [Test] public void PersistedLastRowWithUnknownContextTechniqueCannotPartiallyCommitOrAdvanceItsCursor()
        {
            var source = PackSprintFieldTests.Create(new RaceEventLog(1000), 2);
            var tape = new PackSprintRaceCommandTape("context_payload", source, 2);
            var commands = new SprintAthleteInput[2]; var lines = new double[2];
            PackSprintFieldTests.Fill(source, commands); tape.StepAndRecord(source, commands, lines);
            string json = tape.ToJson();
            var matches = Regex.Matches(json, "\"technique_id\"\\s*:\\s*null");
            Assert.That(matches.Count, Is.EqualTo(2));
            var last = matches[1];
            string malformed = json.Substring(0, last.Index) + "\"technique_id\":\"missing\"" + json.Substring(last.Index + last.Length);
            // The payload is structurally valid; its technique is rejected against the actual field context.
            var persisted = PackSprintRaceCommandTape.FromJson(malformed);
            var log = new RaceEventLog(1000); var target = PackSprintFieldTests.Create(log, 2); int events = log.Count;
            var replay = persisted.CreateReplay(target);
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Assert.Catch(() => replay.Step());
                Assert.That(replay.HasNext, Is.True); Assert.That(target.Tick.Value, Is.Zero); Assert.That(log.Count, Is.EqualTo(events));
                for (int i = 0; i < target.Count; i++)
                { Assert.That(target.GetAthlete(i).Tick.Value, Is.Zero); Assert.That(target.GetLateral(i), Is.Zero); }
            }
            var valid = PackSprintRaceCommandTape.FromJson(json).CreateReplay(target); valid.Step(); AssertPublishedState(target, source);
        }

        [Test] public void ReplayCursorFreezesItsRecordedPrefixAndRejectsExternalAdvancement()
        {
            var field = PackSprintFieldTests.Create(new RaceEventLog(1000), 1); var tape = new PackSprintRaceCommandTape("prefix", field, 10);
            var commands = new SprintAthleteInput[1]; var lines = new double[1]; PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            var target = PackSprintFieldTests.Create(new RaceEventLog(1000), 1); var replay = tape.CreateReplay(target);
            PackSprintFieldTests.Fill(field, commands); lines[0] = 1; tape.StepAndRecord(field, commands, lines);
            replay.Step(); Assert.That(replay.HasNext, Is.False); Assert.That(target.Tick.Value, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => replay.Step());
            var external = PackSprintFieldTests.Create(new RaceEventLog(1000), 1); var other = tape.CreateReplay(external);
            PackSprintFieldTests.Fill(external, commands); external.Step(commands, lines);
            Assert.Throws<InvalidOperationException>(() => other.Step()); Assert.That(external.Tick.Value, Is.EqualTo(1));
        }

        [Test] public void EmptyTapeRoundTripsWithoutCoverageAndWarmRecordingAndReplayAllocateNoMemory()
        {
            var field = PackSprintFieldTests.Create(new RaceEventLog(4000), 2); var tape = new PackSprintRaceCommandTape("gc_pack", field, 2);
            var empty = PackSprintRaceCommandTape.FromJson(tape.ToJson()); var emptyReplay = empty.CreateReplay(PackSprintFieldTests.Create(new RaceEventLog(1000), 2));
            Assert.That(empty.TickCount, Is.Zero); Assert.That(empty.RunCount, Is.Zero); Assert.That(emptyReplay.HasNext, Is.False);
            Assert.Throws<InvalidOperationException>(() => emptyReplay.Step());
            var commands = new SprintAthleteInput[2]; var lines = new double[2]; PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) { PackSprintFieldTests.Fill(field, commands); tape.StepAndRecord(field, commands, lines); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(tape.RunCount, Is.EqualTo(2));
            var persisted = PackSprintRaceCommandTape.FromJson(tape.ToJson()); var target = PackSprintFieldTests.Create(new RaceEventLog(4000), 2);
            var replay = persisted.CreateReplay(target); replay.Step(); before = GC.GetAllocatedBytesForCurrentThread();
            while (replay.HasNext) replay.Step(); allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); AssertPublishedState(target, field);
        }
    }
}

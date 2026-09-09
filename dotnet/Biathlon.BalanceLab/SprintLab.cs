using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;

internal static class SprintLab
{
    public static int Execute(string data, string tracks, string output)
    {
        string Read(string name) => File.ReadAllText(Path.Combine(data, name));
        var suite = SprintRegressionSuite.FromJson(Read("sprint_regression_v1.json"));
        var track = TrackDefinition.FromJson(File.ReadAllText(Path.Combine(tracks, "synthetic_flat_v1.json")));
        var summaries = new List<object>(); var hashes = new StringBuilder();
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "summary.json"), "{\"schema_version\":1,\"id\":\"sprint_regression_result\",\"status\":\"incomplete\"}");
        File.WriteAllText(Path.Combine(output, "suite.json"), suite.ToJson());
        for (int i = 0; i < suite.Count; i++)
        {
            var scenario = suite.Get(i); var events = new RaceEventLog(4000);
            var runner = new SprintRegressionRunner(suite, scenario, Read, track, events);
            var tape = new SprintRaceCommandTape(scenario.Id + "_commands", runner.Field, suite.MaximumCommandRuns);
            var inputs = new SprintAthleteInput[1]; string folder = Path.Combine(output, scenario.Id); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "context.json"), runner.Field.ContextJson);
            string csv = Path.Combine(folder, "race.csv");
            using (var writer = new StreamWriter(csv, false, new UTF8Encoding(false)))
            {
                writer.Write(SprintCsv.Header + "\n");
                while (!runner.Field.IsComplete)
                { inputs[0] = runner.NextInput(); tape.StepAndRecord(runner.Field, inputs); SprintCsv.WriteFrame(writer, runner.Field.GetAthlete(0).LastFrame); }
            }
            string commandPath = Path.Combine(folder, "commands.json"); File.WriteAllText(commandPath, tape.ToJson());
            File.WriteAllText(Path.Combine(folder, "results.json"), runner.Field.Result.ToJson());
            var replayEvents = new RaceEventLog(4000); var replay = new SprintRegressionRunner(suite, scenario, Read, track, replayEvents);
            var restored = SprintRaceCommandTape.FromJson(File.ReadAllText(commandPath)); var cursor = restored.CreateReplay(replay.Field);
            while (cursor.HasNext) cursor.Step();
            if (replay.Field.Result.ToJson() != runner.Field.Result.ToJson() || events.Count != replayEvents.Count) throw new InvalidOperationException("Sprint replay differs.");
            var eventText = new StringBuilder("tick,athlete,type,payload\n");
            for (int j = 0; j < events.Count; j++)
            {
                if (!events[j].Equals(replayEvents[j])) throw new InvalidOperationException("Sprint event replay differs.");
                var e = events[j]; eventText.Append(e.Tick.Value).Append(',').Append(e.AthleteId.Value).Append(',').Append((int)e.EventType).Append(',').Append(e.Payload).Append('\n');
            }
            string eventPath = Path.Combine(folder, "events.csv"); File.WriteAllText(eventPath, eventText.ToString(), new UTF8Encoding(false));
            string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
            string traceHash = Hash(csv), eventHash = Hash(eventPath);
            hashes.Append(scenario.Id).Append(' ').Append(traceHash).Append(' ').Append(eventHash).Append('\n');
            var athlete = runner.Field.GetAthlete(0);
            summaries.Add(new { id = scenario.Id, ticks = runner.Field.Tick.Value, finish_seconds = athlete.Lifecycle.ElapsedFinishTime.Seconds,
                penalties = athlete.TotalPenaltyLoops, distance_m = athlete.Motion.DistanceMeters, hr_bpm = athlete.PhysiologyState.HeartRateBpm,
                fatigue = athlete.PhysiologyState.MuscularFatigue, reserve = athlete.PhysiologyState.AnaerobicReserve,
                command_runs = tape.RunCount, csv_sha256 = traceHash, events_sha256 = eventHash });
        }
        File.WriteAllText(Path.Combine(output, "traces.sha256"), hashes.ToString(), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new { schema_version = 1, id = "sprint_regression_result", status = "passed", scenarios = summaries }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(File.ReadAllText(Path.Combine(output, "summary.json"))); return 0;
    }
}

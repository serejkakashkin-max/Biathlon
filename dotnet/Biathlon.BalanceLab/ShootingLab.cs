using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;

internal static class ShootingLab
{
    public static int Execute(string data, string output)
    {
        string Read(string name) => File.ReadAllText(Path.Combine(data, name));
        var scenario = ShootingTrainingScenario.FromJson(Read("shooting_training_v1.json"));
        var model = new ShootingModelDefinitions(Read); var athlete = ShootingAthleteDefinition.FromJson(Read("shooting_athlete_v1.json"));
        var events = new RaceEventLog(1000); var runner = new ShootingScenarioRunner(scenario, model, athlete, events);
        var tape = new ShootingCommandTape(scenario.Id + "_commands", runner.Session.ContextJson, runner.Session.AthleteId, scenario.MaximumTicks);
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "summary.json"), "{\"schema_version\":1,\"id\":\"shooting_training_result\",\"status\":\"incomplete\"}");
        File.WriteAllText(Path.Combine(output, "scenario.json"), scenario.ToJson());
        File.WriteAllText(Path.Combine(output, "context.json"), runner.Session.ContextJson);
        string csv = Path.Combine(output, "training.csv");
        using (var writer = new StreamWriter(csv, false, new UTF8Encoding(false)))
        {
            writer.Write(ShootingCsv.Header + "\n");
            while (!runner.IsComplete) ShootingCsv.WriteFrame(writer, tape.StepAndRecord(runner.Session, runner.NextInput()));
        }
        File.WriteAllText(Path.Combine(output, "commands.json"), tape.ToJson());
        var replayEvents = new RaceEventLog(1000); var replay = new ShootingScenarioRunner(scenario, model, athlete, replayEvents);
        var restored = ShootingCommandTape.FromJson(tape.ToJson());
        for (int i = 0; i < restored.Count; i++) restored.ReplayNext(replay.Session);
        if (!replay.Session.LastFrame.Equals(runner.Session.LastFrame) || replayEvents.Count != events.Count) throw new InvalidOperationException("Training replay differs.");
        var eventText = new StringBuilder("tick,type,payload\n");
        for (int i = 0; i < events.Count; i++)
        {
            if (!events[i].Equals(replayEvents[i])) throw new InvalidOperationException("Training replay event differs.");
            eventText.Append(events[i].Tick.Value).Append(',').Append((int)events[i].EventType).Append(',').Append(events[i].Payload).Append('\n');
        }
        string eventPath = Path.Combine(output, "events.csv"); File.WriteAllText(eventPath, eventText.ToString(), new UTF8Encoding(false));
        string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new {
            schema_version = 1, id = "shooting_training_result", status = "passed", ticks = tape.Count, shots = runner.Session.ShotsFired,
            closed_mask = runner.Session.ClosedMask, events = events.Count, csv_sha256 = Hash(csv), events_sha256 = Hash(eventPath)
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine(File.ReadAllText(Path.Combine(output, "summary.json"))); return 0;
    }
}

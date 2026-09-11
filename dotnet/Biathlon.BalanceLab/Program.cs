using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;

internal static class Program
{
    private static int Main(string[] args)
    {
        if(args.Length==6 && args[0]=="batch")
        {
            try { CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;return BatchLab.Execute(args[1],args[2],args[3],args[4],int.Parse(args[5],CultureInfo.InvariantCulture)); }
            catch(Exception ex){Console.Error.WriteLine(ex.Message);return 1;}
        }
        if (!((args.Length == 3 && (args[0] == "physiology" || args[0] == "generate-tracks" || args[0] == "shooting" || args[0] == "shooting-balance")) || (args.Length == 4 && (args[0] == "ski" || args[0] == "sprint" || args[0] == "ai" || args[0] == "ai-queues" || args[0] == "ai-stadium" || args[0] == "championship"))))
        {
            Console.Error.WriteLine("Usage: Biathlon.BalanceLab physiology|generate-tracks|shooting|shooting-balance <data-directory> <output-directory>; ski|sprint|ai|ai-queues|ai-stadium|championship <data-directory> <track-directory> <output-directory>");
            return 2;
        }
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            if (args[0] == "championship") return ChampionshipLab.Execute(Path.GetFullPath(args[1]),Path.GetFullPath(args[2]),Path.GetFullPath(args[3]));
            if (args[0] == "ai") return AiBenchmarkLab.Execute(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetFullPath(args[3]));
            if (args[0] == "ai-queues") return AiBenchmarkLab.QueueDiagnostics(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetFullPath(args[3]));
            if (args[0] == "ai-stadium") return AiBenchmarkLab.Execute(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetFullPath(args[3]), true);
            if (args[0] == "ski") return TimeTrialLab.Execute(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetFullPath(args[3]));
            if (args[0] == "sprint") return SprintLab.Execute(Path.GetFullPath(args[1]), Path.GetFullPath(args[2]), Path.GetFullPath(args[3]));
            string data = Path.GetFullPath(args[1]), output = Path.GetFullPath(args[2]);
            if (args[0] == "generate-tracks") return SyntheticTrackGenerator.Generate(data, output);
            if (args[0] == "shooting") return ShootingLab.Execute(data, output);
            if (args[0] == "shooting-balance") return ShootingStatisticalLab.Execute(data, output);
            var definition = PhysiologyDefinition.FromJson(File.ReadAllText(Path.Combine(data, "physiology_provisional_v1.json")));
            var curves = PhysiologyStatCurves.FromJson(File.ReadAllText(Path.Combine(data, "physiology_stat_curves_v1.json")));
            string workloads = File.ReadAllText(Path.Combine(data, "physiology_scenarios_v1.json"));
            var suite = PhysiologyScenarioSuite.FromJson(workloads);
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "workloads.json"), workloads, new UTF8Encoding(false));
            var results = new List<object>();
            for (int index = 0; index < suite.Count; index++)
            {
                var scenario = suite.GetScenario(index);
                var events = new RaceEventLog();
                var runner = new PhysiologyScenarioRunner(suite, scenario, definition, curves, events);
                string csvPath = Path.Combine(output, scenario.Id + ".csv");
                double hrSum = 0, maxHr = runner.Session.State.HeartRateBpm, minReserve = runner.Session.State.AnaerobicReserve;
                string tickTraceHash;
                using (var trace = new PhysiologyTraceFingerprint(runner.Session))
                using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
                {
                    writer.NewLine = "\n";
                    writer.WriteLine(PhysiologyCsv.Header);
                    while (!runner.IsComplete)
                    {
                        var frame = runner.Step();
                        trace.Append(runner.LastInput, frame);
                        PhysiologyCsv.WriteFrame(writer, runner.LastInput, frame, suite.TickFrequency);
                        hrSum += frame.State.HeartRateBpm;
                        maxHr = Math.Max(maxHr, frame.State.HeartRateBpm);
                        minReserve = Math.Min(minReserve, frame.State.AnaerobicReserve);
                    }
                    tickTraceHash = trace.Complete();
                }
                string traceHash;
                using (var file = File.OpenRead(csvPath))
                using (var sha = SHA256.Create()) traceHash = Convert.ToHexString(sha.ComputeHash(file)).ToLowerInvariant();
                var state = runner.Session.State;
                string fingerprint = PhysiologyFingerprint.Compute(runner.Session, events);
                var eventRows = new List<object>();
                for (int i = 0; i < events.Count; i++) eventRows.Add(new {
                    tick = events[i].Tick.Value, tick_frequency = events[i].TickFrequency, athlete_id = events[i].AthleteId.Value,
                    event_type = events[i].EventType.ToString(), event_type_id = (int)events[i].EventType, payload = events[i].Payload
                });
                File.WriteAllText(Path.Combine(output, scenario.Id + "-events.json"), JsonSerializer.Serialize(new {
                    schema_version = 1, id = scenario.Id + "_events", events = eventRows
                }, new JsonSerializerOptions { WriteIndented = true }));
                results.Add(new {
                    id = scenario.Id, ticks = scenario.TotalTicks, seconds = scenario.TotalTicks / (double)suite.TickFrequency,
                    mean_hr_bpm = hrSum / scenario.TotalTicks, max_hr_bpm = maxHr, min_reserve = minReserve,
                    final_hr_bpm = state.HeartRateBpm, final_reserve = state.AnaerobicReserve,
                    final_fatigue = state.MuscularFatigue, final_high_load_s = state.HighLoadSeconds,
                    final_sha256 = fingerprint, tick_trace_sha256 = tickTraceHash, csv_sha256 = traceHash, event_count = events.Count,
                    definition = JsonSerializer.Deserialize<JsonElement>(runner.Session.Definition.ToJson())
                });
                Console.WriteLine($"{scenario.Id}: ticks={scenario.TotalTicks} HR={state.HeartRateBpm:F3} reserve={state.AnaerobicReserve:F6} fatigue={state.MuscularFatigue:F6} sha256={fingerprint}");
            }
            File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new {
                schema_version = 1, id = suite.Id + "_results", numeric_policy = "ADR-0002", runtime = Environment.Version.ToString(),
                tick_frequency = suite.TickFrequency, seed = suite.Seed, scenarios = results
            }, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); return 1; }
    }
}

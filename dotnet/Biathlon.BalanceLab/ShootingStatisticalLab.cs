using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;

internal static class ShootingStatisticalLab
{
    public static int Execute(string data, string output)
    {
        string Read(string name) => File.ReadAllText(Path.Combine(data, name));
        var suite = ShootingBalanceSuite.FromJson(Read("shooting_balance_v1.json"));
        var model = new ShootingModelDefinitions(Read); var athlete = ShootingAthleteDefinition.FromJson(Read("shooting_athlete_v1.json"));
        var profiles = new ShootingAiDefinition[suite.Count];
        for (int i = 0; i < suite.Count; i++)
        {
            profiles[i] = ShootingAiDefinition.FromJson(Read(suite.Get(i).AiDefinition + ".json"));
            // Cold first-bout validation before writing any scenario results.
            ShootingBalanceRunner.Run(suite, suite.Get(i), model, athlete, profiles[i], suite.SeedStart);
        }
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "summary.json"), "{\"schema_version\":1,\"id\":\"shooting_balance_results\",\"status\":\"incomplete\"}");
        File.WriteAllText(Path.Combine(output, "suite.json"), suite.ToJson()); File.WriteAllText(Path.Combine(output, "model-context.json"), model.ContextJson);
        File.WriteAllText(Path.Combine(output, "athlete.json"), athlete.ToJson());
        var summary = new List<object>(); var csv = new StringBuilder("scenario,seed,ticks,shots,closed_mask,hits\n");
        for (int i = 0; i < suite.Count; i++)
        {
            var scenario = suite.Get(i); long totalTicks = 0; int hits = 0, shots = 0, clean = 0;
            var distribution = new int[TargetGeometryDefinition.TargetCount + 1];
            File.WriteAllText(Path.Combine(output, "ai-profile-" + i + ".json"), profiles[i].ToJson());
            for (int sample = 0; sample < suite.Samples; sample++)
            {
                var result = ShootingBalanceRunner.Run(suite, scenario, model, athlete, profiles[i], suite.SeedStart + (ulong)sample);
                totalTicks += result.Ticks; hits += result.Hits; shots += result.Shots; distribution[result.Hits]++; if (result.Hits == 5) clean++;
                csv.Append(scenario.Id).Append(',').Append(result.Seed).Append(',').Append(result.Ticks).Append(',').Append(result.Shots).Append(',').Append(result.ClosedMask).Append(',').Append(result.Hits).Append('\n');
            }
            summary.Add(new { id = scenario.Id, samples = suite.Samples, hits, shots, hit_rate = hits / (double)shots,
                clean_bouts = clean, mean_seconds = totalTicks / (double)(suite.Samples * suite.Frequency), hit_distribution = distribution });
            Console.WriteLine($"{scenario.Id}: {hits}/{shots}, clean={clean}/{suite.Samples}, mean={totalTicks / (double)(suite.Samples * suite.Frequency):F3}s");
        }
        byte[] bytes = Encoding.UTF8.GetBytes(csv.ToString()); File.WriteAllBytes(Path.Combine(output, "bouts.csv"), bytes);
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new { schema_version = 1, id = "shooting_balance_results", status = "passed",
            seed_start = suite.SeedStart, samples_per_scenario = suite.Samples, bouts_sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(), scenarios = summary }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}

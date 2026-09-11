using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Biathlon.Domain;

internal static class SyntheticTrackGenerator
{
    public static int Generate(string dataDirectory, string outputDirectory)
    {
        string templateText = File.ReadAllText(Path.Combine(dataDirectory, "track_contract_fixture_v1.json"));
        var template = TrackDefinition.FromJson(templateText);
        JsonNode recipe = JsonNode.Parse(File.ReadAllText(Path.Combine(dataDirectory, "synthetic_track_recipes_v1.json")));
        if (recipe["schema_version"].GetValue<int>() != 1 || string.IsNullOrWhiteSpace(recipe["id"].GetValue<string>()))
            throw new ArgumentException("Unsupported recipe schema/ID.");
        string targetLoopId = recipe["target_loop_id"].GetValue<string>();
        bool normalizeZero = recipe["normalize_signed_zero"]?.GetValue<bool>() ?? false;
        if (template.GetLoop(targetLoopId).Kind != TrackLoopKind.Race) throw new ArgumentException("Recipe target must be a race loop.");
        var originalRangeLoop = template.GetLoop(template.Range.Entry.LoopId);
        var tail = originalRangeLoop.GetSegment(originalRangeLoop.SegmentCount - 1);
        if (template.Range.Entry.Distance.Millimeters < tail.StartDistance.Millimeters)
            throw new ArgumentException("Template range must be in final shared tail.");
        var outputs = new List<(string Id, string Json)>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (JsonNode profile in recipe["profiles"].AsArray())
        {
            string id = profile["id"].GetValue<string>();
            if (string.IsNullOrWhiteSpace(id) || !System.Text.RegularExpressions.Regex.IsMatch(id, "^[a-z0-9_-]+$") || !ids.Add(id))
                throw new ArgumentException("Recipe IDs must be unique safe filenames.");
            int count = profile["segment_count"].GetValue<int>();
            if (count < 1 || count > 10000) throw new ArgumentException("Recipe segment count outside tooling limits.");
            long segmentMm = TrackDistance.FromMeters(profile["segment_length_m"].GetValue<double>()).Millimeters;
            if (segmentMm == 0) throw new ArgumentException("Recipe segments must be positive.");
            long profileMm = checked(segmentMm * count);
            string pattern = profile["pattern"].GetValue<string>();
            if (pattern != "constant" && pattern != "alternating") throw new ArgumentException("Unknown recipe pattern.");
            JsonNode generated = JsonNode.Parse(templateText);
            generated["id"] = id;
            JsonNode target = null;
            JsonNode rangeLoop = null;
            foreach (JsonNode loop in generated["loops"].AsArray())
            {
                if (loop["id"].GetValue<string>() == targetLoopId) target = loop;
                if (loop["id"].GetValue<string>() == originalRangeLoop.Id) rangeLoop = loop;
            }
            JsonNode tailNode = rangeLoop["segments"].AsArray()[originalRangeLoop.SegmentCount - 1].DeepClone();
            JsonNode sourceSegment = target["segments"].AsArray()[0];
            var segments = new JsonArray();
            for (int i = 0; i < count; i++)
            {
                JsonNode segment = sourceSegment.DeepClone();
                double sign = pattern == "alternating" && i % 2 == 1 ? -1 : 1;
                segment["id"] = "generated_" + i.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
                segment["length_m"] = segmentMm / 1000.0;
                double grade = profile["grade"].GetValue<double>() * sign;
                if (normalizeZero && grade == 0) grade = 0;
                segment["start_grade"] = grade; segment["end_grade"] = grade;
                double curvature = profile["curvature_per_m"].GetValue<double>() * sign;
                segment["curvature_per_m"] = normalizeZero && curvature == 0 ? 0 : curvature;
                segment["width_m"] = profile["width_m"].GetValue<double>();
                segment["terrain_muscle_factor"] = profile["terrain_muscle_factor"].GetValue<double>();
                segment["draft_allowed"] = profile["draft_allowed"].GetValue<bool>();
                segment["overtake_allowed"] = profile["overtake_allowed"].GetValue<bool>();
                segment["semantic_zone"] = profile[sign < 0 ? "negative_zone" : "zone"].GetValue<string>();
                segments.Add(segment);
            }
            tailNode["id"] = "shared_range_tail";
            segments.Add(tailNode);
            target["segments"] = segments;
            target["closed"] = profile["closed"].GetValue<bool>();
            foreach (string key in new[] { "entry", "firing_line", "exit" })
                Relocate(generated["range"][key], targetLoopId, profileMm, tail.StartDistance.Millimeters);
            foreach (string key in new[] { "entry", "rejoin" })
                Relocate(generated["penalty"][key], targetLoopId, profileMm, tail.StartDistance.Millimeters);
            generated["checkpoints"] = new JsonArray(
                Checkpoint("start", targetLoopId, 0), Checkpoint("profile_end", targetLoopId, profileMm),
                Checkpoint("finish", targetLoopId, checked(profileMm + tail.Length.Millimeters)));
            string json = generated.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
            TrackDefinition.FromJson(json); // Validate every graph before any output mutation.
            outputs.Add((id, json));
        }
        if (outputs.Count == 0) throw new ArgumentException("Recipe profiles required.");
        Directory.CreateDirectory(outputDirectory);
        foreach (var item in outputs)
        {
            File.WriteAllText(Path.Combine(outputDirectory, item.Id + ".json"), item.Json, new UTF8Encoding(false));
            Console.WriteLine("generated " + item.Id);
        }
        return 0;
    }
    private static JsonNode Checkpoint(string id, string loopId, long mm) => new JsonObject {
        ["id"] = id, ["anchor"] = new JsonObject { ["loop_id"] = loopId, ["distance_m"] = mm / 1000.0 }
    };
    private static void Relocate(JsonNode anchor, string targetLoopId, long profileMm, long originalTailStart)
    {
        long oldMm = TrackDistance.FromMeters(anchor["distance_m"].GetValue<double>()).Millimeters;
        anchor["loop_id"] = targetLoopId;
        anchor["distance_m"] = checked(profileMm + oldMm - originalTailStart) / 1000.0;
    }
}

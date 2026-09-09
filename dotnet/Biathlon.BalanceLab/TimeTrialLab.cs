using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;

internal static class TimeTrialLab
{
    private sealed class Run
    {
        internal string Id;
        internal JsonElement Scenario;
        internal RaceEventLog Events;
        internal TimeTrialSession Session;
        internal TimeTrialInput Input;
    }
    internal static int Execute(string data, string tracks, string output)
    {
        var sources = new SortedDictionary<string, string>(StringComparer.Ordinal);
        string ReadData(string name)
        {
            string raw = File.ReadAllText(Path.Combine(data, name)); sources[name] = raw; return raw;
        }
        string suiteJson = ReadData("time_trial_scenarios_v1.json");
        using var document = JsonDocument.Parse(suiteJson);
        var suite = document.RootElement;
        if (suite.GetProperty("schema_version").GetInt32() != 1) throw new ArgumentException("Unsupported time-trial suite schema.");
        string suiteId = SafeId(suite.GetProperty("id").GetString());
        int frequency = suite.GetProperty("tick_frequency").GetInt32();
        ulong seed = suite.GetProperty("seed").GetUInt64(); long maximumTicks = suite.GetProperty("maximum_ticks").GetInt64();
        if (frequency <= 0 || maximumTicks <= 0) throw new ArgumentException("Positive tick frequency/budget required.");
        var definitions = new SkiModelDefinitions(ReadData); var runs = new List<Run>(); var ids = new HashSet<string>(StringComparer.Ordinal);
        string conditionsJson = ReadData("race_snow_neutral_v1.json");
        foreach (var scenario in suite.GetProperty("scenarios").EnumerateArray())
        {
            string id = SafeId(scenario.GetProperty("id").GetString());
            if (!ids.Add(id)) throw new ArgumentException("Duplicate time-trial scenario.");
            string trackId = SafeId(scenario.GetProperty("track_id").GetString());
            string rawTrack = File.ReadAllText(Path.Combine(tracks, trackId + ".json")); sources["tracks/" + trackId + ".json"] = rawTrack;
            var track = TrackDefinition.FromJson(rawTrack);
            if (track.Id != trackId) throw new ArgumentException("Track file identity mismatch.");
            string loopId = scenario.GetProperty("loop_id").GetString();
            var requirements = CompetitionRouteRequirements.TimeTrial(id + "_profile", track.GetLoop(loopId).Length, scenario.GetProperty("lap_count").GetInt32());
            var plan = RaceRoutePlan.TimeTrial(id + "_route", trackId, loopId, requirements);
            sources[id + "_profile.json"] = requirements.ToJson(); sources[id + "_route.json"] = plan.ToJson();
            string athleteId = SafeId(scenario.GetProperty("athlete_profile_id").GetString());
            var athlete = TimeTrialAthlete.FromJson(ReadData(athleteId + ".json"));
            if (athlete.Id != athleteId) throw new ArgumentException("Athlete file identity mismatch.");
            string profile = scenario.GetProperty("snow_profile_id").GetString(); definitions.Snow.GetProfile(profile);
            // Authoring adapter maps every surface binding in the baseline condition snapshot to this scenario profile.
            using var conditionsDocument = JsonDocument.Parse(conditionsJson);
            var bindings = new List<object>();
            foreach (var binding in conditionsDocument.RootElement.GetProperty("surfaces").EnumerateArray())
                bindings.Add(new { surface_id = binding.GetProperty("surface_id").GetString(), profile_id = profile });
            var conditions = RaceSnowConditions.FromJson(JsonSerializer.Serialize(new { schema_version = 1, id = id + "_snow", surfaces = bindings }));
            sources[id + "_snow.json"] = conditions.ToJson();
            var events = new RaceEventLog(1000);
            var session = new TimeTrialSession(new SimulationConfig(1, suiteId, frequency, seed), definitions, athlete,
                track, new RaceRoute(track, plan, requirements), conditions, scenario.GetProperty("ski_id").GetString(), events);
            var input = new TimeTrialInput(new SimulationInput(new SimulationTick(1), session.AthleteId, scenario.GetProperty("effort_control").GetInt32()),
                scenario.GetProperty("form").GetDouble(), scenario.GetProperty("line_quality").GetDouble(), scenario.GetProperty("headwind_mps").GetDouble(),
                scenario.GetProperty("tuck_requested").GetBoolean(), scenario.GetProperty("sprint_requested").GetBoolean());
            if (Math.Abs(input.HeadwindMetersPerSecond) > definitions.Physics.MaximumHeadwindMetersPerSecond) throw new ArgumentException("Scenario wind exceeds physical envelope.");
            runs.Add(new Run { Id = id, Scenario = scenario, Events = events, Session = session, Input = input });
        }
        if (runs.Count == 0) throw new ArgumentException("Time-trial suite is empty.");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new { schema_version = 1, id = suiteId + "_results", status = "incomplete" }), new UTF8Encoding(false));
        var results = new List<object>();
        foreach (var run in runs)
        {
            var session = run.Session; var input = run.Input; string csvPath = Path.Combine(output, run.Id + ".csv");
            double minReserve = session.PhysiologyState.AnaerobicReserve, maxHr = session.PhysiologyState.HeartRateBpm, maxSpeed = 0;
            using (var writer = new StreamWriter(csvPath, false, new UTF8Encoding(false)))
            {
                writer.Write(TimeTrialCsv.Header + "\n");
                while (!session.HasFinished && session.Clock.Tick.Value < maximumTicks)
                {
                    var command = new TimeTrialInput(new SimulationInput(session.Clock.Tick.Next(), session.AthleteId, input.Effort.RequestedEffort),
                        input.Form, input.LineQuality, input.HeadwindMetersPerSecond, input.TuckRequested, input.SprintRequested);
                    var frame = session.Step(command); TimeTrialCsv.WriteFrame(writer, frame);
                    minReserve = Math.Min(minReserve, frame.Physiology.State.AnaerobicReserve);
                    maxHr = Math.Max(maxHr, frame.Physiology.State.HeartRateBpm); maxSpeed = Math.Max(maxSpeed, frame.Forces.State.SpeedMetersPerSecond);
                }
            }
            if (!session.HasFinished) throw new InvalidOperationException("Time-trial tick budget exhausted: " + run.Id);
            var eventRows = new List<object>();
            for (int i = 0; i < run.Events.Count; i++)
            {
                var e = run.Events[i]; eventRows.Add(new { tick = e.Tick.Value, tick_frequency = e.TickFrequency, athlete_id = e.AthleteId.Value, event_type = e.EventType.ToString(), event_type_id = (int)e.EventType, payload = e.Payload });
            }
            string eventsJson = JsonSerializer.Serialize(eventRows, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(Path.Combine(output, run.Id + "-events.json"), eventsJson, new UTF8Encoding(false));
            var splits = new List<object>();
            for (int i = 0; i < session.CompletedLaps; i++) { var t = session.GetLapSplit(i); splits.Add(new { lap = i + 1, whole_ticks = t.WholeTicks, fraction_parts = t.FractionParts, seconds = t.Seconds }); }
            results.Add(new { id = run.Id, scenario = run.Scenario, finish_whole_ticks = session.FinishTime.WholeTicks,
                finish_fraction_parts = session.FinishTime.FractionParts, finish_seconds = session.FinishTime.Seconds,
                final_distance_nm = session.Motion.DistanceNanometers, final_speed_mps = session.Motion.SpeedMetersPerSecond,
                final_hr_bpm = session.PhysiologyState.HeartRateBpm, final_reserve = session.PhysiologyState.AnaerobicReserve,
                final_fatigue = session.PhysiologyState.MuscularFatigue, min_reserve = minReserve, max_hr_bpm = maxHr, max_speed_mps = maxSpeed,
                trace_sha256 = Hash(File.ReadAllBytes(csvPath)), events_sha256 = Hash(Encoding.UTF8.GetBytes(eventsJson)), splits });
            Console.WriteLine(FormattableString.Invariant($"{run.Id}: {session.FinishTime.Seconds:F6}s; {session.Clock.Tick.Value} ticks"));
        }
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            string path = Path.Combine(output, "inputs", source.Key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, source.Value, new UTF8Encoding(false));
            hashes[source.Key] = Hash(Encoding.UTF8.GetBytes(source.Value));
        }
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new { schema_version = 1, id = suiteId + "_results", status = "passed", tick_frequency = frequency, seed, input_sha256 = hashes, scenarios = results }, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        return 0;
    }
    private static string SafeId(string id) => id != null && Regex.IsMatch(id, "^[a-z0-9_]+$") ? id : throw new ArgumentException("Safe stable fixture ID required.");
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}

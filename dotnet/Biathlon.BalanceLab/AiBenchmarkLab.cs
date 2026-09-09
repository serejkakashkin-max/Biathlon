using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;

internal static class AiBenchmarkLab
{
    // Benchmark workload, not gameplay tuning. Matched seed and identical capabilities
    // isolate the two policy levels; these synthetic races are not population estimates.
    private const int Count = 30, Frequency = 20, MaximumTicks = 120000, WarmupTicks = 100;
    private const ulong Seed = 123;
    private static readonly JsonSerializerOptions Json = new JsonSerializerOptions { WriteIndented = true };
    private static void Write(string folder, string name, object value) => File.WriteAllText(Path.Combine(folder, name), JsonSerializer.Serialize(value, Json));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    public static int Execute(string data, string tracks, string output)
    {
        Directory.CreateDirectory(output);
        Write(output, "summary.json", new { schema_version = 1, id = "ai_benchmark_v1", status = "incomplete" });
        var summaries = new List<object>();
        foreach (string track in new[] { "synthetic_flat_v1", "synthetic_rolling_v1" })
        foreach (string level in new[] { "expert", "developing" })
        {
            string id = track + "_" + level + "_123", folder = Path.Combine(output, id);
            Directory.CreateDirectory(folder);
            var counts = new AiDecisionCounts(); var events = new RaceEventLog(200000);
            var runner = Create(data, tracks, track, level, events, counts); var field = runner.Field;
            File.WriteAllText(Path.Combine(folder, "context.json"), runner.ContextJson);
            var travel = new long[Count]; var phase = new int[Count];
            var phaseTicks = new long[Count, 4]; // staging, shooting, moving on course, stationary on course
            var intentTicks = new long[Count, 8]; var switches = new int[Count]; var previousIntent = new int[Count];
            Array.Fill(previousIntent, -1);
            var stepMilliseconds = new List<double>(MaximumTicks); long allocated = 0;
            while (!field.IsComplete && field.Tick.Value < MaximumTicks)
            {
                for (int i = 0; i < Count; i++)
                {
                    var actor = field.GetAthlete(i); travel[i] = actor.Motion.DistanceNanometers;
                    phase[i] = actor.HasFinished ? -1 : !field.IsOnCourse(i) ? 0 : actor.ActiveShooting != null ? 1 : 2;
                }
                long bytes = GC.GetAllocatedBytesForCurrentThread(), start = Stopwatch.GetTimestamp();
                runner.Step();
                long elapsed = Stopwatch.GetTimestamp() - start, allocation = GC.GetAllocatedBytesForCurrentThread() - bytes;
                if (field.Tick.Value > WarmupTicks) { stepMilliseconds.Add(elapsed * 1000.0 / Stopwatch.Frequency); allocated += allocation; }
                for (int i = 0; i < Count; i++)
                {
                    int p = phase[i]; if (p < 0) continue;
                    if (p == 2 && travel[i] == field.GetAthlete(i).Motion.DistanceNanometers) p = 3;
                    phaseTicks[i, p]++;
                    if (p < 2) continue;
                    int intent = (int)runner.GetLastIntent(i).Kind; intentTicks[i, intent]++;
                    if (previousIntent[i] >= 0 && previousIntent[i] != intent) switches[i]++;
                    previousIntent[i] = intent;
                }
            }
            if (!field.IsComplete) throw new InvalidOperationException(id + " exceeded the completion bound.");
            if (counts.Tactical != runner.TacticalDecisions || counts.Shooting != runner.ShootingDecisions)
                throw new InvalidOperationException("Decision accounting differs from producer.");
            string result = field.Result.ToJson(); File.WriteAllText(Path.Combine(folder, "result.json"), result);
            var rows = new List<object>(); var finishes = new double[Count]; var penaltyCounts = new double[Count];
            var sprintStarts = new long[Count]; var shots = new long[Count];
            var indices = new Dictionary<string, int>(); for (int i = 0; i < Count; i++) indices.Add(field.GetAthlete(i).AthleteId.Value, i);
            for (int e = 0; e < events.Count; e++)
            {
                var entry = events[e]; if (!indices.TryGetValue(entry.AthleteId.Value, out int i)) continue;
                if (entry.EventType == RaceEventType.FinalSprintStarted) sprintStarts[i]++;
                if (entry.EventType == RaceEventType.ShotAccepted) shots[i]++;
            }
            for (int i = 0; i < Count; i++)
            {
                var actor = field.GetAthlete(i); var durations = new long[4]; var intents = new long[8];
                for (int p = 0; p < 4; p++) durations[p] = phaseTicks[i, p];
                for (int p = 0; p < 8; p++) intents[p] = intentTicks[i, p];
                if (durations.Sum() != actor.Tick.Value || intents.Sum() != durations[2] + durations[3])
                    throw new InvalidOperationException("Athlete time accounting mismatch.");
                finishes[i] = actor.Lifecycle.ElapsedFinishTime.Seconds; penaltyCounts[i] = actor.TotalPenaltyLoops;
                rows.Add(new { athlete_id = actor.AthleteId.Value, personality = i % 2 == 0 ? "attacker" : "patient",
                    finish_seconds = finishes[i], penalties = penaltyCounts[i], accepted_shots = shots[i],
                    phase_ticks = durations, on_course_intent_ticks = intents, intent_switches = switches[i], sprint_starts = sprintStarts[i],
                    final_hr_bpm = actor.PhysiologyState.HeartRateBpm, final_fatigue = actor.PhysiologyState.MuscularFatigue,
                    final_reserve = actor.PhysiologyState.AnaerobicReserve });
            }
            var effortHistogram = new List<object>();
            for (int i = 0; i <= 1000; i++) if (counts.EffortCount(i) > 0) effortHistogram.Add(new { effort = i, decisions = counts.EffortCount(i) });
            var intentCounts = new long[8]; for (int i = 0; i < 8; i++) intentCounts[i] = counts.IntentCount(i);
            double totalMs = stepMilliseconds.Sum();
            var summary = new { schema_version = 1, id, track, difficulty = level, seed = Seed, athletes = Count,
                ticks = field.Tick.Value, tick_frequency = Frequency, result_sha256 = Hash(result), context_sha256 = Hash(runner.ContextJson),
                tactical_decisions = counts.Tactical, shooting_decisions = counts.Shooting, tactical_intent_counts = intentCounts,
                finish_seconds = Distribution(finishes), penalty_loops = Distribution(penaltyCounts),
                sprint_starts = sprintStarts.Sum(), accepted_shots = shots.Sum(), precision_fallback_ticks = field.PrecisionFallbackTicks,
                performance = new { runtime = Environment.Version.ToString(), os = Environment.OSVersion.ToString(), processor_count = Environment.ProcessorCount,
                    excluded_initial_ticks = WarmupTicks, measured_ticks = stepMilliseconds.Count, step_ms = Distribution(stepMilliseconds.ToArray()),
                    realtime_factor = stepMilliseconds.Count * 1000.0 / Frequency / totalMs, allocated_bytes = allocated,
                    allocation_scope = "Step only, includes later bout/controller initialization and event-log growth; main thread" },
                rows, tactical_effort_histogram = effortHistogram };
            Write(folder, "metrics.json", summary); summaries.Add(summary);
            Console.WriteLine(id + ": " + field.Tick.Value + " ticks; " + Hash(result) + "; sprint starts=" + sprintStarts.Sum());
        }
        Write(output, "summary.json", new { schema_version = 1, id = "ai_benchmark_v1", status = "completed",
            intent_order = Enum.GetNames(typeof(TacticalIntentKind)), phase_order = new[] { "staging", "shooting", "moving_on_course", "stationary_on_course" },
            sampling = "Phase at tick start; intent from executed step; finishing tick counted whole. Tactical samples have difficulty-specific cadence. Percentiles use nearest rank.",
            performance_scope = "Headless Step with counting sink and retained events; excludes setup, observer sampling and file IO. Not Unity rendering performance.",
            scenarios = summaries });
        return 0;
    }

    private static object Distribution(double[] values)
    {
        var sorted = (double[])values.Clone(); Array.Sort(sorted);
        double Rank(double p) => sorted[Math.Max(0, (int)Math.Ceiling(p * sorted.Length) - 1)];
        return new { count = sorted.Length, min = sorted[0], median = Rank(.5), p95 = Rank(.95), max = sorted[sorted.Length - 1], mean = sorted.Average() };
    }

    public static int QueueDiagnostics(string data, string tracks, string output)
    {
        Directory.CreateDirectory(output);
        Write(output, "summary.json", new { schema_version = 1, id = "ai_queue_diagnostics_v1", status = "incomplete" });
        var rows = new List<object>();
        foreach (var workload in new[] { (1, 2), (2, 2), (6, 2), (30, 2), (30, 30) })
        {
            int count = workload.Item1, interval = workload.Item2;
            string id = "flat_expert_123_" + count + "_interval_" + interval;
            var events = new RaceEventLog(200000);
            var runner = Create(data, tracks, "synthetic_flat_v1", "expert", events, new AiDecisionCounts(), count, interval);
            var field = runner.Field; var previous = new long[count]; var active = new bool[count];
            var locations = new Dictionary<string, long>(); long stationary = 0, moving = 0, sprints = 0, changes = 0;
            while (!field.IsComplete && field.Tick.Value < MaximumTicks)
            {
                for (int i = 0; i < count; i++)
                { previous[i] = field.GetAthlete(i).Motion.DistanceNanometers; active[i] = field.IsOnCourse(i) && field.GetAthlete(i).ActiveShooting == null; }
                runner.Step();
                for (int i = 0; i < count; i++) if (active[i])
                {
                    if (field.GetAthlete(i).Motion.DistanceNanometers > previous[i]) moving++;
                    else
                    {
                        stationary++; var point = field.GetPhysicalPoint(i);
                        string location = point.LoopId + ":" + point.LocalNanometers / 10000000000L * 10;
                        locations.TryGetValue(location, out long value); locations[location] = value + 1;
                    }
                }
            }
            if (!field.IsComplete) throw new InvalidOperationException(id + " exceeded completion bound.");
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i].EventType == RaceEventType.FinalSprintStarted) sprints++;
                if (events[i].EventType == RaceEventType.AiIntentChanged) changes++;
            }
            File.WriteAllText(Path.Combine(output, id + "-context.json"), runner.ContextJson);
            File.WriteAllText(Path.Combine(output, id + "-result.json"), field.Result.ToJson());
            rows.Add(new { id, count, start_interval_seconds = interval, ticks = field.Tick.Value,
                stationary_on_course_ticks = stationary, moving_on_course_ticks = moving, sprint_starts = sprints, intent_changes = changes,
                result_sha256 = Hash(field.Result.ToJson()), context_sha256 = Hash(runner.ContextJson),
                stationary_locations = locations.OrderByDescending(x => x.Value).ThenBy(x => x.Key, StringComparer.Ordinal).ToArray() });
            Console.WriteLine(id + ": stationary=" + stationary + ", moving=" + moving + ", sprint starts=" + sprints);
        }
        Write(output, "summary.json", new { schema_version = 1, id = "ai_queue_diagnostics_v1", status = "completed",
            location_bins = "Physical loop id and start of a 10-metre bin; intervals [start,start+10). Tick-start on-course, excluding shooting/staging/finished.", scenarios = rows });
        return 0;
    }

    private static PackAiRaceRunner Create(string data, string tracks, string trackId, string level, RaceEventLog events, IAiDecisionSink sink, int count = Count, int interval = 2)
    {
        string Read(string name) => File.ReadAllText(Path.Combine(data, name));
        string Fixture(string name) => File.ReadAllText(Path.Combine(tracks, name));
        var profile = CompetitionProfile.FromJson(Regex.Replace(Read("sprint_test_v1.json"), "(\"start_interval_s\"\\s*:\\s*)[0-9.]+", "${1}" + interval));
        var track = TrackDefinition.FromJson(Fixture(trackId + ".json"));
        var requirements = profile.ToRouteRequirements();
        var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", requirements), requirements);
        var ski = TimeTrialAthlete.FromJson(Read("time_trial_athlete_v1.json"));
        var shot = ShootingAthleteDefinition.FromJson(Read("shooting_athlete_v1.json"));
        var entrants = new SprintEntrant[count]; var profiles = new AiPersonalityDefinition[count];
        for (int i = 0; i < count; i++)
        {
            string id = "athlete_" + i;
            entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id, id)), ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id, id)), "ski_neutral");
            profiles[i] = AiPersonalityDefinition.FromJson(Read("ai_personality_" + (i % 2 == 0 ? "attacker" : "patient") + "_v1.json"));
        }
        var junction = PhysicalJunctionDefinition.FromJson(Fixture("synthetic_flat_junctions_v1.json").Replace("synthetic_flat_v1", trackId), track);
        var connection = PhysicalConnectionDefinition.FromJson(Fixture("synthetic_flat_connections_v1.json"), junction);
        var field = new PackSprintFieldSession(new SimulationConfig(1, "sprint_field_v1", Frequency, Seed), profile,
            new SkiModelDefinitions(Read), new ShootingModelDefinitions(Read), track, route,
            RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json")), ResultTimingDefinition.FromJson(Read("result_timing_v1.json")),
            entrants, new SimulationTick(1), "sprint_result_v1", OccupancyDefinition.FromJson(Read("occupancy_provisional_v1.json")),
            DraftDefinition.FromJson(Read("draft_provisional_v1.json")), OvertakeDefinition.FromJson(Read("overtake_provisional_v1.json")), junction, connection, events);
        return new PackAiRaceRunner(field, Read, profiles, AiDifficultyDefinition.FromJson(Read("ai_difficulty_" + level + "_v1.json")), Seed, events, sink);
    }
}

using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;

namespace Biathlon.AI
{
    /// <summary>Matched policy counterfactual: only the declared maximum drag reduction changes.
    /// Occupancy, physiology, range geometry and AI remain the real shared race systems.</summary>
    public static class DraftCalibrationRunner
    {
        public static BalanceMetrics Run(Func<string,string> read, Func<string,string> fixture, ulong seed,
            int count, int frequency, int maximumTicks, bool enabled)
        {
            if (read == null || fixture == null || maximumTicks < 1) throw new ArgumentException("Readers and positive tick budget required.");
            string Read(string name)
            {
                string json = read(name);
                if (!enabled && name == "draft_provisional_v1.json")
                    json = Regex.Replace(json, "(\"maximum_drag_reduction\"\\s*:\\s*)[0-9.]+", "${1}0");
                return json;
            }
            var events = new BalanceEventAudit(5);
            var runner = ChampionshipRaceFactory.Create(Read, fixture, CompetitionFormat.Sprint, events, new AiDecisionBuffer(8),
                count: count, frequency: frequency, seed: seed);
            double draftSeconds = 0, maximumReduction = 0;
            while (!runner.Field.IsComplete)
            {
                if (runner.Field.Tick.Value >= maximumTicks) throw new InvalidOperationException("Draft experiment exceeded tick budget.");
                runner.Step();
                for (int i = 0; i < count; i++)
                {
                    var draft = runner.Field.GetDraft(i);
                    if (draft.DragReduction > 0) draftSeconds += 1.0 / frequency;
                    maximumReduction = Math.Max(maximumReduction, draft.DragReduction);
                }
            }
            double total = 0;
            for (int i = 0; i < count; i++) total += runner.Field.GetAthlete(i).Lifecycle.ElapsedFinishTime.Seconds;
            var row = new BalanceMetrics();
            row.Values["schema_version"] = 1; row.Values["id"] = enabled ? "draft_on" : "draft_off";
            row.Values["seed"] = seed.ToString(System.Globalization.CultureInfo.InvariantCulture);
            row.Values["mean_finish_seconds"] = total / count;
            row.Values["draft_athlete_seconds"] = draftSeconds; row.Values["maximum_drag_reduction"] = maximumReduction;
            row.Values["context_sha256"] = BalanceRegression.Hash(runner.ContextJson);
            row.Values["event_fnv64"] = events.Hash.ToString("x16");
            return row;
        }
    }
}

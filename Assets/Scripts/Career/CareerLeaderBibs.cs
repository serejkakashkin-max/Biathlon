using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Career
{
    [Flags] public enum CareerLeaderBib { None = 0, Total = 1, Discipline = 2 }

    /// <summary>Pre-race World Cup leaders from registered results, never roster order or athlete ability.
    /// IBU 2026 15.7 and 15.8.5: points, then counts of first places, second places, etc.; genuine ties remain tied.</summary>
    public sealed class CareerLeaderBibs
    {
        private sealed class Score
        {
            public int Points;
            public readonly int[] Placings;
            public Score(int count) { Placings = new int[count]; }
        }
        private readonly Dictionary<string, CareerLeaderBib> bibs = new Dictionary<string, CareerLeaderBib>(StringComparer.Ordinal);
        public CareerLeaderBib ForAthlete(string athleteId)
        {
            if (athleteId == null) throw new ArgumentNullException(nameof(athleteId));
            return bibs.TryGetValue(athleteId, out var value) ? value : CareerLeaderBib.None;
        }
        public static CareerLeaderBibs Capture(CareerSave save, CareerCalendar calendar, CareerScoringDefinition scoring, CareerCalendarEvent nextEvent)
        {
            if (save == null || calendar == null || scoring == null || nextEvent == null) throw new ArgumentNullException();
            if (save.CalendarId != calendar.Id) throw new ArgumentException("Leader calendar must match the saved career.");
            bool found = false;
            for (int i = 0; i < calendar.Count; i++) if (calendar.Get(i).Id == nextEvent.Id && calendar.Get(i).Week == nextEvent.Week && calendar.Get(i).Format == nextEvent.Format) { nextEvent = calendar.Get(i); found = true; break; }
            if (!found) throw new ArgumentException("Leader event must belong to the saved calendar.");
            var result = new CareerLeaderBibs();
            // Regional/junior races must not inherit a World Cup classification. No invented preseason defending champion.
            if (calendar.Circuit != CareerCircuit.WorldCup) return result;
            var total = new Dictionary<string, Score>(StringComparer.Ordinal);
            var discipline = new Dictionary<string, Score>(StringComparer.Ordinal);
            for (int i = 0; i < save.RaceCount; i++)
            {
                var race = save.Race(i); CareerCalendarEvent definition = null;
                for (int e = 0; e < calendar.Count; e++) if (calendar.Get(e).Id == race.CalendarEventId) { definition = calendar.Get(e); break; }
                if (definition == null || definition.Week >= nextEvent.Week) continue;
                Add(total, race, scoring, save.AthleteCount);
                if (race.Format == nextEvent.Format) Add(discipline, race, scoring, save.AthleteCount);
            }
            result.MarkLeaders(total, CareerLeaderBib.Total);
            result.MarkLeaders(discipline, CareerLeaderBib.Discipline);
            return result;
        }
        private static void Add(Dictionary<string, Score> scores, CareerRaceRecord race, CareerScoringDefinition scoring, int count)
        {
            int rank = 1; long prior = -1;
            for (int i = 0; i < race.Count; i++)
            {
                var finish = race.Get(i);
                if (finish.RankTimeMilliseconds != prior) rank = i + 1;
                prior = finish.RankTimeMilliseconds;
                if (!scores.TryGetValue(finish.AthleteId, out var row)) scores.Add(finish.AthleteId, row = new Score(count));
                row.Points = checked(row.Points + scoring.Points(rank, race.Format)); row.Placings[rank - 1]++;
            }
        }
        private void MarkLeaders(Dictionary<string, Score> scores, CareerLeaderBib role)
        {
            Score best = null;
            foreach (var row in scores.Values) if (row.Points > 0 && (best == null || Compare(row, best) > 0)) best = row;
            if (best == null) return;
            foreach (var row in scores)
                if (Compare(row.Value, best) == 0) bibs[row.Key] = ForAthlete(row.Key) | role;
        }
        private static int Compare(Score a, Score b)
        {
            int value = a.Points.CompareTo(b.Points);
            if (value != 0) return value;
            for (int rank = 0; rank < a.Placings.Length; rank++) { value = a.Placings[rank].CompareTo(b.Placings[rank]); if (value != 0) return value; }
            return 0;
        }
    }
}

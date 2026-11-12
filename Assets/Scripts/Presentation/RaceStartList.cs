using System;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Presentation
{
    public readonly struct RaceStartRow
    {
        public string AthleteId { get; }
        public string Name { get; }
        public string Nation { get; }
        public int Bib { get; }
        public int? GridRow { get; }
        public double StartOffsetSeconds { get; }
        public bool IsPlayer { get; }

        internal RaceStartRow(CareerAthlete athlete, int bib, int? gridRow, double startOffsetSeconds, bool isPlayer)
        {
            AthleteId = athlete.Id;
            Name = athlete.Name;
            Nation = athlete.Nation;
            Bib = bib;
            GridRow = gridRow;
            StartOffsetSeconds = startOffsetSeconds;
            IsPlayer = isPlayer;
        }
    }

    /// <summary>Read-only projection of the roster and schedule of the loaded field.</summary>
    public sealed class RaceStartList
    {
        private readonly RaceStartRow[] rows;
        public CompetitionProfile Profile { get; }
        public int Count => rows.Length;
        public int PlayerIndex { get; }
        public RaceStartRow Row(int index) => rows[index];

        public RaceStartList(CareerSave roster, PackSprintFieldSession field)
        {
            if (roster == null || field == null) throw new ArgumentNullException();
            Profile = field.Profile;
            rows = new RaceStartRow[field.Count];
            int playerIndex = -1;
            long firstStart = field.GetAthlete(0).ScheduledStart.Value;
            for (int index = 0; index < rows.Length; index++)
            {
                var entry = field.GetAthlete(index);
                string id = entry.AthleteId.Value;
                var athlete = roster.FindWorldAthlete(id) ?? throw new ArgumentException("Missing start-list athlete: " + id);
                bool isPlayer = id == roster.PlayerId;
                if (isPlayer) playerIndex = index;
                int? gridRow = field.StartGrid == null ? (int?)null : field.StartGrid.Get(index).Row + 1;
                double offset = (entry.ScheduledStart.Value - firstStart) / (double)field.TickFrequency;
                rows[index] = new RaceStartRow(athlete, index + 1, gridRow, offset, isPlayer);
            }
            if (playerIndex < 0) throw new ArgumentException("The start list must contain the career player.");
            PlayerIndex = playerIndex;
        }
    }
}

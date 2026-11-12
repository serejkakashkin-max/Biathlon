using System;
using System.Collections.Generic;
using Biathlon.Career;

namespace Biathlon.Presentation
{
    public sealed class RaceResultsRow
    {
        public CareerFinish Finish { get; }
        public string Name { get; }
        public string Nation { get; }
        public int Rank { get; }
        public int Points { get; }
        public long GapMilliseconds { get; }
        public bool IsPlayer { get; }
        internal RaceResultsRow(CareerFinish finish,CareerAthlete athlete,int rank,int points,long leader,bool player)
        {Finish=finish;Name=athlete.Name;Nation=athlete.Nation;Rank=rank;Points=points;GapMilliseconds=finish.RankTimeMilliseconds-leader;IsPlayer=player;}
    }

    public readonly struct RaceStandingRow
    {
        public string AthleteId { get; }
        public string Name { get; }
        public string Nation { get; }
        public int Rank { get; }
        public int Points { get; }
        public int RacePoints { get; }
        public bool IsPlayer { get; }

        internal RaceStandingRow(CareerAthlete athlete, int rank, int points, int racePoints, bool isPlayer)
        {
            AthleteId = athlete.Id;
            Name = athlete.Name;
            Nation = athlete.Nation;
            Rank = rank;
            Points = points;
            RacePoints = racePoints;
            IsPlayer = isPlayer;
        }
    }

    /// <summary>Results and scoring projection for the completed season, including its final week.
    /// Uses a detached saved roster and the same standings accounting as Career; changes no source state.</summary>
    public sealed class RaceResultsSummary
    {
        private readonly RaceResultsRow[] rows;
        public CareerRaceRecord Record { get; }
        public int Season { get; }
        public RaceResultsRow Player { get; }
        public int PointsBefore { get; }
        public int PointsAfter { get; }
        public int? StandingBefore { get; }
        public int StandingAfter { get; }
        public IReadOnlyList<RaceStandingRow> OverallStandings { get; }
        public IReadOnlyList<RaceStandingRow> DisciplineStandings { get; }
        public int Count=>rows.Length;
        public RaceResultsRow Row(int index)=>rows[index];
        public RaceResultsSummary(CareerSave source,CareerRaceRecord record,CareerScoringDefinition scoring)
        {
            if(source==null||record==null||scoring==null)throw new ArgumentNullException();
            Record=record;Season=source.Season;
            var prior=CareerStandings.Table(source,scoring);var projection=CareerSave.FromJson(source.ToJson());
            CareerStandings.Register(projection,record);var after=CareerStandings.Table(projection,scoring);
            int classified = 0;
            foreach (var standing in prior)
            {
                if (standing.Races == 0) continue;
                classified++;
                if (standing.AthleteId != source.PlayerId) continue;
                PointsBefore = standing.Points;
                StandingBefore = classified;
                break;
            }
            rows=new RaceResultsRow[record.Count];
            var pointsEarned = new Dictionary<string,int>(StringComparer.Ordinal);
            for(int i=0;i<rows.Length;i++)
            {
                var finish=record.Get(i);CareerAthlete athlete=null;
                for(int a=0;a<source.AthleteCount;a++)if(source.Athlete(a).Id==finish.AthleteId){athlete=source.Athlete(a);break;}
                int place=CareerStandings.ResultRank(record,i,scoring);
                rows[i]=new RaceResultsRow(finish,athlete,place,scoring.Points(place,record.Format),record.Get(0).RankTimeMilliseconds,finish.AthleteId==source.PlayerId);
                pointsEarned.Add(finish.AthleteId, rows[i].Points);
                if(rows[i].IsPlayer)Player=rows[i];
            }
            if(Player==null)throw new ArgumentException("Results must contain the career player.");
            OverallStandings = BuildStandings(source, after, pointsEarned);
            DisciplineStandings = BuildStandings(source, CareerStandings.Table(projection, scoring, record.Format), pointsEarned);
            foreach (var standing in OverallStandings)
            {
                if (!standing.IsPlayer) continue;
                PointsAfter = standing.Points;
                StandingAfter = standing.Rank;
                break;
            }
        }

        private static IReadOnlyList<RaceStandingRow> BuildStandings(CareerSave source, CareerStanding[] standings,
            Dictionary<string,int> pointsEarned)
        {
            var ranked = new List<RaceStandingRow>(standings.Length);
            foreach (var standing in standings)
            {
                if (standing.Races == 0) continue;
                var athlete = source.FindWorldAthlete(standing.AthleteId);
                pointsEarned.TryGetValue(standing.AthleteId, out int racePoints);
                ranked.Add(new RaceStandingRow(athlete, ranked.Count + 1, standing.Points, racePoints, athlete.Id == source.PlayerId));
            }
            return ranked.AsReadOnly();
        }
    }
}

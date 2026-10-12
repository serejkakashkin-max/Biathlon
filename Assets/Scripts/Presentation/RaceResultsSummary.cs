using System;
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
        public int Count=>rows.Length;
        public RaceResultsRow Row(int index)=>rows[index];
        public RaceResultsSummary(CareerSave source,CareerRaceRecord record,CareerScoringDefinition scoring)
        {
            if(source==null||record==null||scoring==null)throw new ArgumentNullException();
            Record=record;Season=source.Season;
            var prior=CareerStandings.Table(source,scoring);var projection=CareerSave.FromJson(source.ToJson());
            CareerStandings.Register(projection,record);var after=CareerStandings.Table(projection,scoring);
            for(int i=0;i<prior.Length;i++)if(prior[i].AthleteId==source.PlayerId){PointsBefore=prior[i].Points;if(source.RaceCount>0)StandingBefore=i+1;}
            for(int i=0;i<after.Length;i++)if(after[i].AthleteId==source.PlayerId){PointsAfter=after[i].Points;StandingAfter=i+1;}
            rows=new RaceResultsRow[record.Count];
            for(int i=0;i<rows.Length;i++)
            {
                var finish=record.Get(i);CareerAthlete athlete=null;
                for(int a=0;a<source.AthleteCount;a++)if(source.Athlete(a).Id==finish.AthleteId){athlete=source.Athlete(a);break;}
                int place=CareerStandings.ResultRank(record,i,scoring);
                rows[i]=new RaceResultsRow(finish,athlete,place,scoring.Points(place,record.Format),record.Get(0).RankTimeMilliseconds,finish.AthleteId==source.PlayerId);
                if(rows[i].IsPlayer)Player=rows[i];
            }
            if(Player==null)throw new ArgumentException("Results must contain the career player.");
        }
    }
}

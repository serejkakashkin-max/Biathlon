using System;
using System.Collections.Generic;
using Biathlon.Career;

namespace Biathlon.Presentation
{
    public enum ObservedDevelopmentTrend { Unestablished, Improving, Stable, Declining }
    public enum CareerRosterSort { Overall, Points, Age, Name }
    public readonly struct AthleteRatingObservation
    {
        public int Season {get;}
        public int Week {get;}
        public int Age {get;}
        public int Overall {get;}
        public bool Current {get;}
        public AthleteRatingObservation(int season,int week,int age,int overall,bool current)
        {Season=season;Week=week;Age=age;Overall=overall;Current=current;}
    }

    /// <summary>Read-only observed ability and progress. Never reveals hidden ceilings or forecasts a guaranteed peak.</summary>
    public sealed class AthleteCareerSummary
    {
        private readonly int[] values=new int[13],deltas=new int[13],groupDeltas=new int[5];
        private readonly AthleteRatingObservation[] observations;
        public CareerAthlete Athlete {get;}
        public CareerCircuit Circuit {get;}
        public bool InActiveField {get;}
        public BiathlonRatings Ratings {get;}
        public int OverallDelta {get;}
        public int PeakOverall {get;}
        public int PeakAge {get;}
        public int PeakSeason {get;}
        public int? StrongestSeason {get;}
        public int? SeasonPoints {get;}
        public int? SeasonRank {get;}
        public bool HasDevelopmentBaseline=>Athlete.DevelopmentCount>0;
        public ObservedDevelopmentTrend Trend {get;}
        public int ObservationCount=>observations.Length;
        public AthleteRatingObservation Observation(int index)=>observations[index];
        public int Ability(CareerStat stat)=>values[(int)stat];
        public int AbilityDelta(CareerStat stat)=>deltas[(int)stat];
        public int GroupDelta(BiathlonRating group)
        {if(group<BiathlonRating.Ski||group>BiathlonRating.Mental)throw new ArgumentOutOfRangeException(nameof(group));return groupDeltas[(int)group-1];}

        public AthleteCareerSummary(CareerSave save,CareerDefinitions definitions,string athleteId):this(save,definitions,athleteId,null) { }
        private AthleteCareerSummary(CareerSave save,CareerDefinitions definitions,string athleteId,CareerStanding[] standings)
        {
            if(save==null||definitions==null||athleteId==null)throw new ArgumentNullException();
            if(save.CalendarId!=definitions.Calendar.Id)throw new ArgumentException("Matching saved career calendar required.");
            Athlete=save.FindWorldAthlete(athleteId)??throw new ArgumentException("Unknown living career athlete.",nameof(athleteId));
            Circuit=definitions.Calendar.Circuit;
            for(int i=0;i<save.AthleteCount;i++)if(save.Athlete(i).Id==athleteId){InActiveField=true;break;}
            if(!InActiveField)for(int i=0;i<save.PoolCount;i++)if(save.Pool(i).Athlete.Id==athleteId){Circuit=save.Pool(i).Circuit;break;}
            Ratings=BiathlonRatingEngine.Calculate(Athlete,definitions.Ratings);
            var baseline=new int[13];
            for(int i=0;i<13;i++)
            {
                baseline[i]=Athlete.SeasonBaselineStat((CareerStat)i);values[i]=BiathlonRatingEngine.StatRating(Athlete.Stat((CareerStat)i));
                deltas[i]=values[i]-BiathlonRatingEngine.StatRating(baseline[i]);
            }
            OverallDelta=Ratings.Overall-BiathlonRatingEngine.CalculateStats(baseline,definitions.Ratings);
            for(int i=0;i<5;i++)groupDeltas[i]=BiathlonRatingEngine.Calculate(Athlete,definitions.Ratings,(BiathlonRating)(i+1))-BiathlonRatingEngine.CalculateStats(baseline,definitions.Ratings,(BiathlonRating)(i+1));
            var points=new List<AthleteRatingObservation>();int bestSeasonOverall=-1;
            for(int i=0;i<Athlete.DevelopmentCount;i++)
            {
                var row=Athlete.Development(i);points.Add(new AthleteRatingObservation(row.Season,row.Week,row.Age,row.Overall,false));
                if(row.Kind==CareerDevelopmentObservation.SeasonEnd&&row.Overall>bestSeasonOverall){bestSeasonOverall=row.Overall;StrongestSeason=row.Season;}
            }
            // Current state is an observation of now, not a fabricated pre-migration career.
            if(points.Count>0&&points[points.Count-1].Season==save.Season&&points[points.Count-1].Week==save.Week&&points[points.Count-1].Overall==Ratings.Overall)
                points.RemoveAt(points.Count-1);
            points.Add(new AthleteRatingObservation(save.Season,save.Week,Athlete.Age,Ratings.Overall,true));observations=points.ToArray();
            PeakOverall=Math.Max(Athlete.PeakOverall,Ratings.Overall);
            if(Athlete.PeakOverall>=Ratings.Overall&&Athlete.PeakOverall>0){PeakAge=Athlete.PeakAge;PeakSeason=Athlete.PeakSeason;}
            else{PeakAge=Athlete.Age;PeakSeason=save.Season;}
            int observedChange=Athlete.DevelopmentCount<2?0:Ratings.Overall-Athlete.Development(Math.Max(0,Athlete.DevelopmentCount-2)).Overall;
            Trend=Athlete.DevelopmentCount<2?ObservedDevelopmentTrend.Unestablished:observedChange>0?ObservedDevelopmentTrend.Improving:observedChange<0?ObservedDevelopmentTrend.Declining:ObservedDevelopmentTrend.Stable;
            if(InActiveField&&save.RaceCount>0)
            {
                var table=standings??CareerStandings.Table(save,definitions.Scoring);
                for(int i=0;i<table.Length;i++)if(table[i].AthleteId==athleteId){SeasonPoints=table[i].Points;SeasonRank=i+1;break;}
            }
        }

        public static AthleteCareerSummary[] Search(CareerSave save,CareerDefinitions definitions,bool activeOnly,string nation,string name,CareerRosterSort sort,CareerCircuit? circuit=null)
        {
            if(save==null||definitions==null)throw new ArgumentNullException();
            if(save.CalendarId!=definitions.Calendar.Id)throw new ArgumentException("Matching saved career calendar required.");
            if(sort<CareerRosterSort.Overall||sort>CareerRosterSort.Name)throw new ArgumentOutOfRangeException(nameof(sort));
            if(circuit.HasValue&&(circuit<CareerCircuit.Open||circuit>CareerCircuit.WorldCup))throw new ArgumentOutOfRangeException(nameof(circuit));
            var rows=new List<AthleteCareerSummary>();
            var standings=save.RaceCount>0?CareerStandings.Table(save,definitions.Scoring):null;
            for(int i=0;i<(activeOnly?save.AthleteCount:save.WorldAthleteCount);i++)
            {
                var a=save.WorldAthlete(i);if(nation!=null&&a.Nation!=nation||!string.IsNullOrWhiteSpace(name)&&a.Name.IndexOf(name.Trim(),StringComparison.OrdinalIgnoreCase)<0)continue;
                var athleteCircuit=i<save.AthleteCount?definitions.Calendar.Circuit:save.Pool(i-save.AthleteCount).Circuit;
                if(circuit.HasValue&&circuit.Value!=athleteCircuit)continue;
                rows.Add(new AthleteCareerSummary(save,definitions,a.Id,standings));
            }
            rows.Sort((a,b)=>
            {
                int c=sort==CareerRosterSort.Points?(b.SeasonPoints??-1).CompareTo(a.SeasonPoints??-1):sort==CareerRosterSort.Age?a.Athlete.Age.CompareTo(b.Athlete.Age):sort==CareerRosterSort.Name?StringComparer.OrdinalIgnoreCase.Compare(a.Athlete.Name,b.Athlete.Name):b.Ratings.Overall.CompareTo(a.Ratings.Overall);
                return c!=0?c:StringComparer.Ordinal.Compare(a.Athlete.Id,b.Athlete.Id);
            });return rows.ToArray();
        }
    }
}

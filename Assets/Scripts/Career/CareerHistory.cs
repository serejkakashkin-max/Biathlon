using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
namespace Biathlon.Career
{
    [DataContract] public sealed class CareerHistoryDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="retained_seasons",IsRequired=true)] public int RetainedSeasons { get; private set; }
        [DataMember(Name="close_finish_ms",IsRequired=true)] public int CloseFinishMilliseconds { get; private set; }
        [DataMember(Name="standings_gap_points",IsRequired=true)] public int StandingsGapPoints { get; private set; }
        public static CareerHistoryDefinition FromJson(string json)
        {var d=CareerJson.Read<CareerHistoryDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid history definition.");CareerUnits.Id(d.Id);CareerUnits.Range(d.RetainedSeasons,1,100,"retained seasons");CareerUnits.Range(d.CloseFinishMilliseconds,0,86400000,"close finish");CareerUnits.Range(d.StandingsGapPoints,0,1000000,"standings gap");return d;}
    }
    [DataContract] public sealed class CareerHistoryRow
    {
        [DataMember(Name="athlete_id",IsRequired=true)] public string AthleteId { get; private set; }
        [DataMember(Name="name",IsRequired=true)] public string Name { get; private set; }
        [DataMember(Name="points",IsRequired=true)] public int Points { get; private set; }
        [DataMember(Name="shared_races",IsRequired=true)] public int SharedRaces { get; private set; }
        [DataMember(Name="close_finishes",IsRequired=true)] public int CloseFinishes { get; private set; }
        [DataMember(Name="shared_podiums",IsRequired=true)] public int SharedPodiums { get; private set; }
        [DataMember(Name="standings_battle",IsRequired=true)] public bool StandingsBattle { get; private set; }
        internal CareerHistoryRow(CareerAthlete a,int points,int shared,int close,int podiums,bool battle){AthleteId=a.Id;Name=a.Name;Points=points;SharedRaces=shared;CloseFinishes=close;SharedPodiums=podiums;StandingsBattle=battle;}
        internal void Validate(){CareerUnits.Id(AthleteId);CareerUnits.Text(Name,100,"historical name");CareerUnits.Range(Points,0,366000000,"historical points");CareerUnits.Range(SharedRaces,0,366,"shared races");CareerUnits.Range(CloseFinishes,0,SharedRaces,"close finishes");CareerUnits.Range(SharedPodiums,0,SharedRaces,"shared podiums");}
    }
    [DataContract] public sealed class CareerSeasonHistory
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="season",IsRequired=true)] public int Season { get; private set; }
        [DataMember(Name="race_count",IsRequired=true)] public int RaceCount { get; private set; }
        [DataMember(Name="standings",IsRequired=true)] private CareerHistoryRow[] rows { get; set; }
        public int Count=>rows.Length;public CareerHistoryRow Get(int index)=>rows[index];
        internal CareerSeasonHistory(CareerSave save,CareerHistoryRow[] entries){SchemaVersion=1;Id=save.SeasonId+"_history";Season=save.Season;RaceCount=save.RaceCount;rows=entries;Validate();}
        internal void Validate()
        {
            if(SchemaVersion!=1||rows==null||rows.Length<1||rows.Length>1000)throw new SerializationException("Invalid season history.");CareerUnits.Id(Id);CareerUnits.Range(Season,1,10000,"historical season");CareerUnits.Range(RaceCount,0,366,"historical races");
            var ids=new HashSet<string>(StringComparer.Ordinal);int points=int.MaxValue;foreach(var row in rows){if(row==null)throw new SerializationException("Null history row.");row.Validate();if(!ids.Add(row.AthleteId)||row.Points>points||row.SharedRaces>RaceCount)throw new SerializationException("Invalid historical order/count.");points=row.Points;}
        }
    }
    public static class CareerHistory
    {
        public static CareerSeasonHistory Summarize(CareerSave save,CareerScoringDefinition scoring,CareerHistoryDefinition definition)
        {
            if(save==null||scoring==null||definition==null)throw new ArgumentNullException();save.Validate();var standings=CareerStandings.Table(save,scoring);int playerPoints=0,playerRaces=0;
            foreach(var row in standings)if(row.AthleteId==save.PlayerId){playerPoints=row.Points;playerRaces=row.Races;}
            var athletes=new Dictionary<string,CareerAthlete>(StringComparer.Ordinal);for(int i=0;i<save.AthleteCount;i++)athletes.Add(save.Athlete(i).Id,save.Athlete(i));
            var entries=new CareerHistoryRow[standings.Length];
            for(int i=0;i<standings.Length;i++)
            {
                var row=standings[i];int shared=0,close=0,podiums=0;
                if(row.AthleteId!=save.PlayerId)for(int raceIndex=0;raceIndex<save.RaceCount;raceIndex++)
                {
                    var race=save.Race(raceIndex);int player=-1,opponent=-1;
                    for(int rank=0;rank<race.Count;rank++){if(race.Get(rank).AthleteId==save.PlayerId)player=rank;if(race.Get(rank).AthleteId==row.AthleteId)opponent=rank;}
                    if(player<0||opponent<0)continue;shared++;
                    if(Math.Abs(race.Get(player).RankTimeMilliseconds-race.Get(opponent).RankTimeMilliseconds)<=definition.CloseFinishMilliseconds)close++;
                    if(player<3&&opponent<3)podiums++;
                }
                entries[i]=new CareerHistoryRow(athletes[row.AthleteId],row.Points,shared,close,podiums,row.AthleteId!=save.PlayerId&&playerRaces>0&&row.Races>0&&Math.Abs(row.Points-playerPoints)<=definition.StandingsGapPoints);
            }
            return new CareerSeasonHistory(save,entries);
        }
        public static CareerSave SealSeason(CareerSave source,CareerCalendar calendar,CareerScoringDefinition scoring,CareerHistoryDefinition definition)
        {
            if(source==null||calendar==null||definition==null)throw new ArgumentNullException();source.Validate();
            if(source.CalendarId!=calendar.Id||source.Week!=calendar.WeeksPerSeason||source.Season>=10000)throw new InvalidOperationException("Season is not ready to seal.");
            var summary=Summarize(source,scoring,definition);var save=CareerSave.FromJson(source.ToJson());save.ArchiveSeason(summary,definition.RetainedSeasons);save.Season++;save.SeasonId=save.Id+"_season_"+save.Season.ToString(System.Globalization.CultureInfo.InvariantCulture);save.Week=1;save.Validate();return save;
        }
    }
}

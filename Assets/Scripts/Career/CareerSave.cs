using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerSave
    {
        public const int CurrentVersion=2;
        private CareerSave() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="save_version",IsRequired=true)] public int SaveVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="world_id",IsRequired=true)] public string WorldId { get; private set; }
        [DataMember(Name="season_id",IsRequired=true)] public string SeasonId { get; internal set; }
        [DataMember(Name="rules_id",IsRequired=true)] public string RulesId { get; private set; }
        [DataMember(Name="calendar_id",IsRequired=true)] public string CalendarId { get; private set; }
        [DataMember(Name="season",IsRequired=true)] public int Season { get; internal set; }
        [DataMember(Name="week",IsRequired=true)] public int Week { get; internal set; }
        [DataMember(Name="rng_state",IsRequired=true)] public ulong RandomState { get; internal set; }
        [DataMember(Name="next_athlete_ordinal",IsRequired=true)] public long NextAthleteOrdinal { get; internal set; }
        [DataMember(Name="player_id",IsRequired=true)] public string PlayerId { get; private set; }
        [DataMember(Name="athletes",IsRequired=true)] private CareerAthlete[] athletes { get; set; }
        [DataMember(Name="season_results",EmitDefaultValue=false)] private CareerRaceRecord[] seasonResults { get; set; }
        public int RaceCount=>seasonResults?.Length??0;public CareerRaceRecord Race(int index)=>seasonResults[index];
        internal void AppendRace(CareerRaceRecord record){var rows=new CareerRaceRecord[RaceCount+1];if(seasonResults!=null)Array.Copy(seasonResults,rows,RaceCount);rows[rows.Length-1]=record;seasonResults=rows;}
        public int AthleteCount=>athletes.Length;public CareerAthlete Athlete(int index)=>athletes[index];
        internal CareerAthlete[] Athletes { get=>athletes;set=>athletes=value; }
        internal static CareerSave Create(string id,string rules,string calendar,CareerAthlete[] roster,string playerId,ulong rngState,long nextOrdinal)
        {
            var s=new CareerSave{SchemaVersion=1,SaveVersion=CurrentVersion,Id=id,WorldId=id+"_world",SeasonId=id+"_season_1",RulesId=rules,CalendarId=calendar,
                Season=1,Week=1,RandomState=rngState,NextAthleteOrdinal=nextOrdinal,PlayerId=playerId,athletes=roster};s.Validate();return s;
        }
        public string ToJson(){Validate();return CareerJson.Write(this);}
        public static CareerSave FromJson(string json)
        {
            var s=CareerJson.Read<CareerSave>(json);
            if(s==null||s.SchemaVersion!=1||s.SaveVersion<1||s.SaveVersion>CurrentVersion)throw new SerializationException("Unsupported career save version.");
            if(s.SaveVersion==1)
            {
                // v1 did not persist confidence separately; migrate from its existing morale state.
                if(s.athletes!=null)foreach(var a in s.athletes)if(a!=null&&a.Confidence<0)a.Confidence=a.Morale;
                s.SaveVersion=CurrentVersion;
            }
            s.Validate();return s;
        }
        internal void Validate()
        {
            if(SchemaVersion!=1||SaveVersion!=CurrentVersion)throw new SerializationException("Invalid career save schema/version.");
            foreach(string id in new[]{Id,WorldId,SeasonId,RulesId,CalendarId,PlayerId})CareerUnits.Id(id);
            if(WorldId!=Id+"_world"||SeasonId!=Id+"_season_"+Season.ToString(System.Globalization.CultureInfo.InvariantCulture))throw new SerializationException("World/season identity mismatch.");
            CareerUnits.Range(Season,1,10000,"season");CareerUnits.Range(Week,1,366,"week");
            if(athletes==null||athletes.Length<1||athletes.Length>1000||NextAthleteOrdinal<athletes.Length)throw new SerializationException("Invalid career roster/counter.");
            var ids=new HashSet<string>(StringComparer.Ordinal);bool player=false;
            foreach(var a in athletes)
            {
                if(a==null)throw new SerializationException("Null athlete.");a.Validate();if(!ids.Add(a.Id))throw new SerializationException("Duplicate athlete ID.");player|=a.Id==PlayerId;
                string prefix=Id+"_athlete_";
                if(!a.Id.StartsWith(prefix,StringComparison.Ordinal)||!long.TryParse(a.Id.Substring(prefix.Length),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out long ordinal)||ordinal>=NextAthleteOrdinal)
                    throw new SerializationException("Athlete ordinal is inconsistent with the allocation counter.");
            }
            if(!player)throw new SerializationException("Player is absent from roster.");
            var events=new HashSet<string>(StringComparer.Ordinal);var records=new HashSet<string>(StringComparer.Ordinal);
            if(RaceCount>366)throw new SerializationException("Too many season results.");
            for(int i=0;i<RaceCount;i++)
            {
                var record=seasonResults[i];if(record==null)throw new SerializationException("Null season result.");record.Validate();
                if(record.SeasonId!=SeasonId||!events.Add(record.CalendarEventId)||!records.Add(record.Id))throw new SerializationException("Invalid season event history.");
                for(int j=0;j<record.Count;j++)if(!ids.Contains(record.Get(j).AthleteId))throw new SerializationException("Unknown result athlete.");
            }
        }
    }
}

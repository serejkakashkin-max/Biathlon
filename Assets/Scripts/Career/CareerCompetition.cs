using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerFinish
    {
        [DataMember(Name="athlete_id",IsRequired=true)] public string AthleteId { get; private set; }
        [DataMember(Name="rank_time_ms",IsRequired=true)] public long RankTimeMilliseconds { get; private set; }
        [DataMember(Name="elapsed_ms",IsRequired=true)] public long ElapsedMilliseconds { get; private set; }
        [DataMember(Name="penalties",IsRequired=true)] public int Penalties { get; private set; }
        [DataMember(Name="overtakes",IsRequired=true)] public int Overtakes { get; private set; }
        public CareerFinish(string athleteId,long rankTimeMilliseconds,long elapsedMilliseconds,int penalties=0,int overtakes=0)
        {AthleteId=athleteId;RankTimeMilliseconds=rankTimeMilliseconds;ElapsedMilliseconds=elapsedMilliseconds;Penalties=penalties;Overtakes=overtakes;Validate();}
        internal void Validate(){CareerUnits.Id(AthleteId);if(RankTimeMilliseconds<1||ElapsedMilliseconds<1||RankTimeMilliseconds>86400000||ElapsedMilliseconds>86400000||Penalties<0||Penalties>100||Overtakes<0)throw new SerializationException("Invalid career finish.");}
    }
    [DataContract] public sealed class CareerRaceRecord
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="season_id",IsRequired=true)] public string SeasonId { get; private set; }
        [DataMember(Name="calendar_event_id",IsRequired=true)] public string CalendarEventId { get; private set; }
        [DataMember(Name="format",IsRequired=true)] public CompetitionFormat Format { get; private set; }
        [DataMember(Name="seed",IsRequired=true)] public ulong Seed { get; private set; }
        [DataMember(Name="authority_sha256",IsRequired=true)] public string AuthoritySha256 { get; private set; }
        [DataMember(Name="authority_result_json",EmitDefaultValue=false)] public string AuthorityResultJson { get; private set; }
        [DataMember(Name="finishes",IsRequired=true)] private CareerFinish[] finishes { get; set; }
        public int Count=>finishes.Length;public CareerFinish Get(int index)=>finishes[index];
        public CareerRaceRecord(string id,string seasonId,string calendarEventId,CompetitionFormat format,ulong seed,string authoritySha256,IReadOnlyList<CareerFinish> results,string authorityResultJson=null)
        {
            AuthorityResultJson=authorityResultJson;SchemaVersion=1;Id=id;SeasonId=seasonId;CalendarEventId=calendarEventId;Format=format;Seed=seed;AuthoritySha256=authoritySha256;
            if(results==null)throw new ArgumentNullException(nameof(results));finishes=new CareerFinish[results.Count];for(int i=0;i<results.Count;i++)finishes[i]=results[i];Validate();
        }
        internal void Validate()
        {
            if(SchemaVersion!=1)throw new SerializationException("Unsupported race record schema.");CareerUnits.Id(Id);CareerUnits.Id(SeasonId);CareerUnits.Id(CalendarEventId);
            if(Format<CompetitionFormat.Sprint||Format>CompetitionFormat.MassStart||finishes==null||finishes.Length<1||finishes.Length>1000)throw new SerializationException("Invalid career result.");
            if(AuthorityResultJson!=null&&AuthorityResultJson.Length>4000000)throw new SerializationException("Authority payload too large.");
            if(AuthoritySha256==null||AuthoritySha256.Length!=64)throw new SerializationException("Missing authority fingerprint.");foreach(char c in AuthoritySha256)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))throw new SerializationException("Invalid authority fingerprint.");
            var ids=new HashSet<string>(StringComparer.Ordinal);long previous=0;
            foreach(var finish in finishes){if(finish==null)throw new SerializationException("Null finish.");finish.Validate();if(!ids.Add(finish.AthleteId)||finish.RankTimeMilliseconds<previous)throw new SerializationException("Duplicate or unordered finish.");previous=finish.RankTimeMilliseconds;}
        }
        public string ToJson()=>CareerJson.Write(this);
    }
    [DataContract] public sealed class CareerScoringDefinition
    {
        private CareerScoringDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="rank_points",IsRequired=true)] private int[] points { get; set; }
        public int Points(int rank){if(rank<1)throw new ArgumentOutOfRangeException(nameof(rank));return rank>points.Length?0:points[rank-1];}
        public static CareerScoringDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerScoringDefinition>(json);if(d==null||d.SchemaVersion!=1||d.points==null||d.points.Length<1||d.points.Length>1000)throw new SerializationException("Invalid scoring schema.");CareerUnits.Id(d.Id);
            int previous=int.MaxValue;foreach(int value in d.points){CareerUnits.Range(value,0,1000000,"points");if(value>previous)throw new SerializationException("Points must not increase with worse rank.");previous=value;}return d;
        }
    }
    public sealed class CareerStanding
    {
        public string AthleteId { get; } public int Points { get; internal set; } public int Wins { get; internal set; }
        public int Podiums { get; internal set; } public int Races { get; internal set; } public long TotalMilliseconds { get; internal set; }
        internal CareerStanding(string id){AthleteId=id;}
    }
    public static class CareerStandings
    {
        public static void Register(CareerSave save,CareerRaceRecord record)
        {
            if(save==null||record==null)throw new ArgumentNullException();record.Validate();
            if(record.SeasonId!=save.SeasonId||save.RaceCount>=366)throw new ArgumentException("Result season/budget mismatch.");
            for(int i=0;i<save.RaceCount;i++)if(save.Race(i).Id==record.Id||save.Race(i).CalendarEventId==record.CalendarEventId)throw new ArgumentException("Event already registered.");
            var known=new HashSet<string>(StringComparer.Ordinal);for(int i=0;i<save.AthleteCount;i++)known.Add(save.Athlete(i).Id);
            for(int i=0;i<record.Count;i++)if(!known.Contains(record.Get(i).AthleteId))throw new ArgumentException("Result references an unknown athlete.");
            save.AppendRace(record);
        }
        public static CareerStanding[] Table(CareerSave save,CareerScoringDefinition scoring,CompetitionFormat? format=null)
        {
            if(save==null||scoring==null)throw new ArgumentNullException();if(format.HasValue&&(format<CompetitionFormat.Sprint||format>CompetitionFormat.MassStart))throw new ArgumentException("Unknown format.");
            var rows=new Dictionary<string,CareerStanding>(StringComparer.Ordinal);for(int i=0;i<save.AthleteCount;i++)rows.Add(save.Athlete(i).Id,new CareerStanding(save.Athlete(i).Id));
            for(int i=0;i<save.RaceCount;i++)
            {
                var race=save.Race(i);if(format.HasValue&&race.Format!=format.Value)continue;
                for(int rank=0;rank<race.Count;rank++){var finish=race.Get(rank);var row=rows[finish.AthleteId];row.Points=checked(row.Points+scoring.Points(rank+1));row.Races++;if(rank==0)row.Wins++;if(rank<3)row.Podiums++;row.TotalMilliseconds=checked(row.TotalMilliseconds+finish.ElapsedMilliseconds);}
            }
            var result=new List<CareerStanding>(rows.Values);result.Sort((a,b)=>{int c=b.Points.CompareTo(a.Points);if(c==0)c=b.Wins.CompareTo(a.Wins);if(c==0)c=b.Podiums.CompareTo(a.Podiums);if(c==0)c=a.TotalMilliseconds.CompareTo(b.TotalMilliseconds);return c!=0?c:StringComparer.Ordinal.Compare(a.AthleteId,b.AthleteId);});return result.ToArray();
        }
    }
}

using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    public enum RosterRightsStatus { Unreviewed, Approved, Rejected }
    [DataContract] public sealed class RosterMediaRights
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="resource_id",IsRequired=true)] public string ResourceId {get;private set;}
        [DataMember(Name="sha256",IsRequired=true)] public string Sha256 {get;private set;}
        [DataMember(Name="review_reference",IsRequired=true)] public string ReviewReference {get;private set;}
        [DataMember(Name="status",IsRequired=true)] public RosterRightsStatus Status {get;private set;}
        internal void Validate()
        {
            CareerUnits.Id(Id);CareerUnits.Id(ResourceId);CareerUnits.Text(ReviewReference,1000,"media review reference");CareerUnits.Range((int)Status,0,2,"media rights status");
            if(Sha256==null||Sha256.Length!=64)throw new SerializationException("Missing media fingerprint.");
            foreach(char c in Sha256)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))throw new SerializationException("Invalid media fingerprint.");
        }
    }
    [DataContract] public sealed class RealRosterAthlete
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="name",IsRequired=true)] public string Name {get;private set;}
        [DataMember(Name="nation",IsRequired=true)] public string Nation {get;private set;}
        [DataMember(Name="birth_year",IsRequired=true)] public int BirthYear {get;private set;}
        [DataMember(Name="start_profile_id",IsRequired=true)] public string StartProfileId {get;private set;}
        [DataMember(Name="stats",IsRequired=true)] private int[] stats {get;set;}
        [DataMember(Name="appearance",EmitDefaultValue=false)] public AppearanceProfile Appearance {get;private set;}
        [DataMember(Name="identity_review_reference",IsRequired=true)] public string IdentityReviewReference {get;private set;}
        [DataMember(Name="identity_rights_status",IsRequired=true)] public RosterRightsStatus IdentityRightsStatus {get;private set;}
        public int Stat(CareerStat stat){CareerUnits.Range((int)stat,0,12,"roster stat");return stats[(int)stat];}
        public int Overall(BiathlonRatingDefinition ratings)=>BiathlonRatingEngine.CalculateStats(stats,ratings);
        internal void Validate(int season)
        {
            CareerUnits.Id(Id);CareerUnits.Text(Name,100,"licensed athlete name");CareerUnits.Id(Nation);CareerUnits.Id(StartProfileId);
            CareerUnits.Range(BirthYear,season-70,season-16,"athlete birth year");
            if(StartProfileId!="junior_novice"&&StartProfileId!="senior_debut"&&StartProfileId!="world_cup_debut")throw new SerializationException("Unknown roster circuit.");
            if(StartProfileId=="junior_novice"&&season-BirthYear>20)throw new SerializationException("Adult athlete cannot be recast as a junior.");
            if(stats==null||stats.Length!=13)throw new SerializationException("Roster requires13 actual abilities, not an independent OVR.");foreach(int s in stats)CareerUnits.Range(s,0,100000,"licensed athlete ability");
            Appearance?.Validate();CareerUnits.Text(IdentityReviewReference,1000,"identity review reference");CareerUnits.Range((int)IdentityRightsStatus,0,2,"identity rights status");
        }
    }
    /// <summary>Optional data/rights contract. No real people or media are bundled by default.
    /// Approved is a recorded external review decision, not automatic legal verification.</summary>
    [DataContract] public sealed class RealAthleteRosterPack
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="season_year",IsRequired=true)] public int SeasonYear {get;private set;}
        [DataMember(Name="statistics_version",IsRequired=true)] public string StatisticsVersion {get;private set;}
        [DataMember(Name="statistics_date",IsRequired=true)] public string StatisticsDate {get;private set;}
        [DataMember(Name="statistics_sources",IsRequired=true)] private string[] sources {get;set;}
        [DataMember(Name="review_reference",IsRequired=true)] public string ReviewReference {get;private set;}
        [DataMember(Name="distribution_rights_status",IsRequired=true)] public RosterRightsStatus RightsStatus {get;private set;}
        [DataMember(Name="athletes",IsRequired=true)] private RealRosterAthlete[] athletes {get;set;}
        [DataMember(Name="media",IsRequired=true)] private RosterMediaRights[] media {get;set;}
        public int Count=>athletes.Length;public RealRosterAthlete Get(int index)=>athletes[index];
        public int SourceCount=>sources.Length;public string Source(int index)=>sources[index];
        public int MediaCount=>media.Length;public RosterMediaRights Media(int index)=>media[index];
        public string ToJson()=>CareerJson.Write(this);
        public static RealAthleteRosterPack FromJson(string json)
        {
            var p=CareerJson.Read<RealAthleteRosterPack>(json);
            if(p==null||p.SchemaVersion!=1)throw new SerializationException("Unsupported real-athlete roster pack.");CareerUnits.Id(p.Id);CareerUnits.Id(p.StatisticsVersion);
            CareerUnits.Range(p.SeasonYear,2020,2200,"roster season");
            if(!DateTime.TryParseExact(p.StatisticsDate,"yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out _))throw new SerializationException("Exact statistics date required.");
            CareerUnits.Text(p.ReviewReference,1000,"roster rights review reference");CareerUnits.Range((int)p.RightsStatus,0,2,"roster rights status");
            if(p.sources==null||p.sources.Length<1||p.sources.Length>30||p.athletes==null||p.athletes.Length<1||p.athletes.Length>1000||p.media==null||p.media.Length>3000)throw new SerializationException("Invalid roster pack budgets.");
            foreach(string source in p.sources)if(!Uri.TryCreate(source,UriKind.Absolute,out var uri)||uri.Scheme!="https"||!string.IsNullOrEmpty(uri.UserInfo))throw new SerializationException("Public HTTPS statistics source required.");
            for(int i=0;i<p.Count;i++){if(p.athletes[i]==null)throw new SerializationException("Null roster athlete.");p.athletes[i].Validate(p.SeasonYear);for(int j=0;j<i;j++)if(p.athletes[i].Id==p.athletes[j].Id)throw new SerializationException("Duplicate external athlete ID.");}
            for(int i=0;i<p.MediaCount;i++){if(p.media[i]==null)throw new SerializationException("Null media rights.");p.media[i].Validate();for(int j=0;j<i;j++)if(p.media[i].Id==p.media[j].Id||p.media[i].ResourceId==p.media[j].ResourceId)throw new SerializationException("Duplicate roster media.");}
            return p;
        }
        public void ValidateForDistribution()
        {
            if(RightsStatus!=RosterRightsStatus.Approved)throw new InvalidOperationException("Roster distribution requires a recorded rights approval.");
            foreach(var a in athletes)if(a.IdentityRightsStatus!=RosterRightsStatus.Approved)throw new InvalidOperationException("Athlete name/likeness approval is missing.");
            foreach(var item in media)if(item.Status!=RosterRightsStatus.Approved)throw new InvalidOperationException("Media approval is missing.");
        }
    }
}

using System;
using System.Globalization;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    /// <summary>Immutable saved future adaptation, independent of readiness and race RNG.</summary>
    [DataContract] public sealed class CareerGrowthProfile
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="policy_id",IsRequired=true)] public string PolicyId {get;private set;}
        [DataMember(Name="style_id",IsRequired=true)] public string StyleId {get;private set;}
        [DataMember(Name="talent_band_id",IsRequired=true)] public string TalentBandId {get;private set;}
        [DataMember(Name="ceilings",IsRequired=true)] private int[] ceilings {get;set;}
        [DataMember(Name="responses",IsRequired=true)] private int[] responses {get;set;}
        [DataMember(Name="age_shift",IsRequired=true)] public int AgeShift {get;private set;}
        [DataMember(Name="decline_response",IsRequired=true)] public int DeclineResponse {get;private set;}
        [DataMember(Name="retirement_age",IsRequired=true)] public int RetirementAge {get;private set;}
        [DataMember(Name="stagnation_16_79",IsRequired=true)] private ulong stagnationLower {get;set;}
        [DataMember(Name="stagnation_80_120",IsRequired=true)] private ulong stagnationUpper {get;set;}
        public int Ceiling(CareerGrowthGroup group){CareerUnits.Range((int)group,0,4,"growth group");return ceilings[(int)group];}
        public int Response(CareerGrowthGroup group){CareerUnits.Range((int)group,0,4,"growth group");return responses[(int)group];}
        public bool IsStagnating(int age)
        {CareerUnits.Range(age,1,120,"stagnation age");return age<16?false:age<80?(stagnationLower&(1UL<<(age-16)))!=0:(stagnationUpper&(1UL<<(age-80)))!=0;}
        internal void Validate()
        {
            if(SchemaVersion!=1||Id!="individual_growth_v1")throw new SerializationException("Invalid growth profile version.");
            CareerUnits.Id(PolicyId);CareerUnits.Id(StyleId);CareerUnits.Id(TalentBandId);
            if(ceilings==null||responses==null||ceilings.Length!=5||responses.Length!=5)throw new SerializationException("Five individual growth groups required.");
            foreach(int n in ceilings)CareerUnits.Range(n,1000,100000,"individual ceiling");foreach(int n in responses)CareerUnits.Range(n,1000,25000,"individual response");
            CareerUnits.Range(AgeShift,-5,5,"individual age shift");CareerUnits.Range(DeclineResponse,1000,20000,"individual decline");CareerUnits.Range(RetirementAge,30,60,"individual retirement");
            if((stagnationUpper>>41)!=0)throw new SerializationException("Stagnation mask exceeds supported ages.");
        }
        internal static CareerGrowthProfile Generate(CareerGrowthDefinition d,int baseline,IRandomSource random)
        {
            var band=d.DrawBand(random);var style=d.DrawStyle(random);int talent=CareerFactory.Draw(random,band.Minimum,band.Maximum);
            var p=new CareerGrowthProfile{SchemaVersion=1,Id="individual_growth_v1",PolicyId=d.Id,StyleId=style.Id,TalentBandId=band.Id,ceilings=new int[5],responses=new int[5],
                AgeShift=CareerFactory.Draw(random,style.AgeShiftMin,style.AgeShiftMax),DeclineResponse=CareerFactory.Draw(random,d.DeclineResponseMin,d.DeclineResponseMax),RetirementAge=CareerFactory.Draw(random,style.RetirementMin,style.RetirementMax)};
            for(int i=0;i<5;i++)
            {
                p.ceilings[i]=Math.Min(100000,Math.Max(baseline+d.AbilityMargin,talent+style.Offset(i)+CareerFactory.Draw(random,-d.CeilingVariation,d.CeilingVariation)));
                p.responses[i]=Math.Max(1000,Math.Min(25000,style.Response(i)+CareerFactory.Draw(random,-d.ResponseVariation,d.ResponseVariation)));
            }
            for(int age=16;age<=120;age++)if(CareerFactory.Draw(random,1,10000)<=style.StagnationProbability)
            {if(age<80)p.stagnationLower|=1UL<<(age-16);else p.stagnationUpper|=1UL<<(age-80);}
            p.Validate();return p;
        }
        public string ToJson()=>CareerJson.Write(this);
    }
    public enum CareerDevelopmentObservation { Baseline, SeasonEnd }
    [DataContract] public sealed class CareerDevelopmentRecord
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="season",IsRequired=true)] public int Season {get;private set;}
        [DataMember(Name="week",IsRequired=true)] public int Week {get;private set;}
        [DataMember(Name="age",IsRequired=true)] public int Age {get;private set;}
        [DataMember(Name="kind",IsRequired=true)] public CareerDevelopmentObservation Kind {get;private set;}
        [DataMember(Name="rating_policy_id",IsRequired=true)] public string RatingPolicyId {get;private set;}
        [DataMember(Name="ovr",IsRequired=true)] public int Overall {get;private set;}
        [DataMember(Name="stats",IsRequired=true)] private int[] stats {get;set;}
        public int Stat(CareerStat stat){CareerUnits.Range((int)stat,0,12,"observed stat");return stats[(int)stat];}
        internal static CareerDevelopmentRecord Capture(CareerAthlete a,int season,int week,CareerDevelopmentObservation kind,BiathlonRatingDefinition ratings)
        {
            var r=new CareerDevelopmentRecord{SchemaVersion=1,Id=a.Id+"_dev_"+season.ToString(CultureInfo.InvariantCulture)+"_"+week.ToString(CultureInfo.InvariantCulture)+"_"+(int)kind,
                Season=season,Week=week,Age=a.Age,Kind=kind,RatingPolicyId=ratings.Id,Overall=BiathlonRatingEngine.Calculate(a,ratings,BiathlonRating.Overall),stats=new int[13]};
            for(int i=0;i<13;i++)r.stats[i]=a.Stat((CareerStat)i);r.Validate();return r;
        }
        internal void Validate()
        {
            if(SchemaVersion!=1||stats==null||stats.Length!=13)throw new SerializationException("Invalid development observation.");
            CareerUnits.Id(Id);CareerUnits.Id(RatingPolicyId);CareerUnits.Range(Season,1,10000,"observed season");CareerUnits.Range(Week,1,366,"observed week");CareerUnits.Range(Age,1,120,"observed age");CareerUnits.Range(Overall,1,99,"observed OVR");CareerUnits.Range((int)Kind,0,1,"observation kind");
            foreach(int n in stats)CareerUnits.Range(n,0,100000,"observed stat");
        }
    }
    public sealed partial class CareerAthlete
    {
        [DataMember(Name="growth_profile",EmitDefaultValue=false)] public CareerGrowthProfile Growth {get;internal set;}
        [DataMember(Name="development_history",EmitDefaultValue=false)] private CareerDevelopmentRecord[] development {get;set;}
        [DataMember(Name="peak_ovr",EmitDefaultValue=false)] public int PeakOverall {get;private set;}
        [DataMember(Name="peak_age",EmitDefaultValue=false)] public int PeakAge {get;private set;}
        [DataMember(Name="peak_season",EmitDefaultValue=false)] public int PeakSeason {get;private set;}
        public int DevelopmentCount=>development?.Length??0;
        public CareerDevelopmentRecord Development(int index)=>development[index];
        public int SeasonBaselineStat(CareerStat stat)=>DevelopmentCount==0?Stat(stat):development[DevelopmentCount-1].Stat(stat);
        internal void ObservePeak(int season,BiathlonRatingDefinition ratings)
        {
            int overall=BiathlonRatingEngine.Calculate(this,ratings,BiathlonRating.Overall);
            if(overall>PeakOverall){PeakOverall=overall;PeakAge=Age;PeakSeason=season;}
        }
        internal void AppendDevelopment(CareerDevelopmentRecord record,int limit,BiathlonRatingDefinition ratings)
        {
            int kept=Math.Min(DevelopmentCount,limit-1);var rows=new CareerDevelopmentRecord[kept+1];if(kept>0)Array.Copy(development,DevelopmentCount-kept,rows,0,kept);
            rows[kept]=record;development=rows;ObservePeak(record.Season,ratings);
        }
        private void ValidateDevelopment()
        {
            if(BackgroundWeeks<0)throw new SerializationException("Negative background weeks.");
            Growth?.Validate();if(DevelopmentCount>100)throw new SerializationException("Development history budget exceeded.");
            if((Growth==null)!=(DevelopmentCount==0))throw new SerializationException("Growth and baseline must be initialized together.");
            int season=0,week=0;CareerDevelopmentObservation kind=CareerDevelopmentObservation.Baseline;
            for(int i=0;i<DevelopmentCount;i++)
            {
                var row=development[i];if(row==null)throw new SerializationException("Null development observation.");row.Validate();
                string expected=Id+"_dev_"+row.Season.ToString(CultureInfo.InvariantCulture)+"_"+row.Week.ToString(CultureInfo.InvariantCulture)+"_"+(int)row.Kind;
                if(row.Id!=expected||row.Season<season||(row.Season==season&&(row.Week<week||(row.Week==week&&row.Kind<=kind))))throw new SerializationException("Invalid development identity/order.");
                if(row.Overall>PeakOverall)throw new SerializationException("Observed OVR exceeds recorded peak.");season=row.Season;week=row.Week;kind=row.Kind;
            }
            if(Growth!=null){CareerUnits.Range(PeakOverall,1,99,"peak OVR");CareerUnits.Range(PeakAge,1,120,"peak age");CareerUnits.Range(PeakSeason,1,10000,"peak season");}
            else if(PeakOverall!=0||PeakAge!=0||PeakSeason!=0)throw new SerializationException("Peak without observed development.");
        }
    }
    public static class CareerDevelopment
    {
        /// <summary>Build all metadata before publication. Existing stats, results and main RNG stay untouched.</summary>
        public static int InitializeWorld(CareerSave save,CareerGrowthDefinition d,BiathlonRatingDefinition ratings,Func<ulong,IRandomSource> restoreRandom,int? ownBaseline=null)
        {
            if(save==null||d==null||ratings==null||restoreRandom==null)throw new ArgumentNullException();save.Validate();
            if(ownBaseline.HasValue)CareerUnits.Range(ownBaseline.Value,0,100000,"immutable creation baseline");
            var profiles=new CareerGrowthProfile[save.WorldAthleteCount];var observations=new CareerDevelopmentRecord[save.WorldAthleteCount];int initialized=0;
            for(int i=0;i<save.WorldAthleteCount;i++)
            {
                var a=save.WorldAthlete(i);if(a.Growth!=null){d.RequireProfile(a);continue;}
                int baseline=0;for(int stat=0;stat<13;stat++)baseline+=a.Stat((CareerStat)stat);baseline/=13;
                if(a.Id==save.PlayerId&&ownBaseline.HasValue)baseline=ownBaseline.Value;
                string prefix=save.Id+"_athlete_";long ordinal=long.Parse(a.Id.Substring(prefix.Length),CultureInfo.InvariantCulture);
                ulong seed=unchecked(save.RandomState+d.SubstreamSalt+(ulong)ordinal*d.SubstreamStride);var random=restoreRandom(seed);
                if(random==null||random.State!=seed)throw new ArgumentException("Development substream checkpoint mismatch.");
                profiles[i]=CareerGrowthProfile.Generate(d,baseline,random);
                observations[i]=CareerDevelopmentRecord.Capture(a,save.Season,save.Week,CareerDevelopmentObservation.Baseline,ratings);initialized++;
            }
            for(int i=0;i<profiles.Length;i++)if(profiles[i]!=null)
            {save.WorldAthlete(i).Growth=profiles[i];save.WorldAthlete(i).AppendDevelopment(observations[i],d.HistoryLimit,ratings);}
            save.Validate();return initialized;
        }
        public static void SealSeason(CareerSave save,CareerGrowthDefinition d,BiathlonRatingDefinition ratings)
        {
            if(save==null||d==null||ratings==null)throw new ArgumentNullException();save.Validate();
            for(int i=0;i<save.WorldAthleteCount;i++)
            {
                var a=save.WorldAthlete(i);d.RequireProfile(a);
                if(a.Development(a.DevelopmentCount-1).Kind==CareerDevelopmentObservation.SeasonEnd&&a.Development(a.DevelopmentCount-1).Season==save.Season)throw new InvalidOperationException("Season development already observed.");
            }
            for(int i=0;i<save.WorldAthleteCount;i++)
            {var a=save.WorldAthlete(i);a.AppendDevelopment(CareerDevelopmentRecord.Capture(a,save.Season,save.Week,CareerDevelopmentObservation.SeasonEnd,ratings),d.HistoryLimit,ratings);}
        }
    }
}

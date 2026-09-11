using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class BalancePacingCase
    {
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="track_file",IsRequired=true)] public string TrackFile { get; private set; }
        [DataMember(Name="effort",IsRequired=true)] public int Effort { get; private set; }
        [DataMember(Name="final_effort",IsRequired=true)] public int FinalEffort { get; private set; }
        [DataMember(Name="recovery_effort",IsRequired=true)] public int RecoveryEffort { get; private set; }
        [DataMember(Name="recovery_distance_m",IsRequired=true)] public double RecoveryDistance { get; private set; }
        internal void Validate()
        {
            BalanceBatchDefinition.Plain(Id); BalanceBatchDefinition.Plain(TrackFile);
            AthleteState.ValidateEffort(Effort); AthleteState.ValidateEffort(FinalEffort); AthleteState.ValidateEffort(RecoveryEffort);
            TrackValidation.Finite(RecoveryDistance,0,1000,nameof(RecoveryDistance));
        }
        public string ToJson()=>RouteJson.Write(this);
    }
    [DataContract]
    public sealed class BalanceBatchDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="tick_frequency",IsRequired=true)] public int Frequency { get; private set; }
        [DataMember(Name="maximum_ticks",IsRequired=true)] public int MaximumTicks { get; private set; }
        [DataMember(Name="seed",IsRequired=true)] public ulong Seed { get; private set; }
        [DataMember(Name="iterations",IsRequired=true)] public int Iterations { get; private set; }
        [DataMember(Name="profile_file",IsRequired=true)] public string ProfileFile { get; private set; }
        [DataMember(Name="ski_athlete_file",IsRequired=true)] public string SkiAthleteFile { get; private set; }
        [DataMember(Name="shooting_athlete_file",IsRequired=true)] public string ShootingAthleteFile { get; private set; }
        [DataMember(Name="cases",IsRequired=true)] private BalancePacingCase[] cases { get; set; }
        public int Count=>cases.Length;
        public int RunCount=>checked(Count*Iterations);
        public BalancePacingCase Get(int index)=>cases[index];
        public ulong SeedAt(int iteration)
        { if(iteration<0||iteration>=Iterations)throw new ArgumentOutOfRangeException(nameof(iteration)); return checked(Seed+(ulong)iteration); }
        internal static void Plain(string value)
        {
            TrackValidation.Id(value);
            foreach(char c in value) if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='_'||c=='-'||c=='.'))throw new SerializationException("Plain ASCII ID/file required.");
            if(value=="."||value=="..")throw new SerializationException("Relative path not allowed.");
        }
        public static BalanceBatchDefinition FromJson(string json)
        {
            var v=RouteJson.Read<BalanceBatchDefinition>(json);
            if(v==null||v.SchemaVersion!=1||v.Frequency<10||v.Frequency>1000||v.MaximumTicks<1||v.MaximumTicks>1000000||v.Iterations<1||v.Iterations>100000||v.cases==null||v.Count<1||v.Count>100)
                throw new SerializationException("Invalid bounded batch definition.");
            Plain(v.Id);Plain(v.ProfileFile);Plain(v.SkiAthleteFile);Plain(v.ShootingAthleteFile);
            v.SeedAt(v.Iterations-1);
            for(int i=0;i<v.Count;i++)
            { if(v.cases[i]==null)throw new SerializationException("Null case.");v.cases[i].Validate();for(int j=0;j<i;j++)if(v.cases[i].Id==v.cases[j].Id)throw new SerializationException("Duplicate case."); }
            return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

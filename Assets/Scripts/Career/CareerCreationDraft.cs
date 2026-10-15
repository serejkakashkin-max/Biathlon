using System;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    public enum CareerCreationMode { CreateOwn, ExistingAthlete }

    [DataContract] public sealed class CareerCreationTier
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="baseline_stat",IsRequired=true)] public int BaselineStat {get;private set;}
        [DataMember(Name="minimum_stat",IsRequired=true)] public int MinimumStat {get;private set;}
        [DataMember(Name="maximum_stat",IsRequired=true)] public int MaximumStat {get;private set;}
        [DataMember(Name="minimum_ovr",IsRequired=true)] public int MinimumOvr {get;private set;}
        [DataMember(Name="maximum_ovr",IsRequired=true)] public int MaximumOvr {get;private set;}
        [DataMember(Name="budget",IsRequired=true)] public int Budget {get;private set;}
        [DataMember(Name="maximum_increase",IsRequired=true)] public int MaximumIncrease {get;private set;}
        [DataMember(Name="maximum_decrease",IsRequired=true)] public int MaximumDecrease {get;private set;}
        internal void Validate()
        {
            CareerUnits.Id(Id);CareerUnits.Range(MinimumStat,0,100000,"creation stat floor");
            CareerUnits.Range(BaselineStat,MinimumStat,100000,"creation baseline");CareerUnits.Range(MaximumStat,BaselineStat,100000,"creation stat ceiling");
            CareerUnits.Range(MinimumOvr,1,99,"creation OVR floor");CareerUnits.Range(MaximumOvr,MinimumOvr,99,"creation OVR ceiling");
            CareerUnits.Range(Budget,0,1000,"creation budget");CareerUnits.Range(MaximumIncrease,0,30,"specialization ceiling");CareerUnits.Range(MaximumDecrease,0,30,"specialization floor");
        }
    }
    [DataContract] public sealed class CareerCreationPreset
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="allocation",IsRequired=true)] private int[] allocation {get;set;}
        public int Allocation(CareerStat stat)=>allocation[(int)stat];
        internal int[] Copy()=>(int[])allocation.Clone();
        internal void Validate()
        {
            CareerUnits.Id(Id);if(allocation==null||allocation.Length!=CareerUnits.StatCount)throw new SerializationException("Expected 13 preset allocations.");
            foreach(int point in allocation)CareerUnits.Range(point,-30,30,"preset allocation");
        }
    }
    [DataContract] public sealed class CareerOriginStory
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="minimum_age",IsRequired=true)] public int MinimumAge {get;private set;}
        [DataMember(Name="maximum_age",IsRequired=true)] public int MaximumAge {get;private set;}
        [DataMember(Name="preset_id",IsRequired=true)] public string PresetId {get;private set;}
        public bool Allows(int age)=>age>=MinimumAge&&age<=MaximumAge;
        internal void Validate(){CareerUnits.Id(Id);CareerUnits.Id(PresetId);CareerUnits.Range(MinimumAge,16,60,"origin minimum age");CareerUnits.Range(MaximumAge,MinimumAge,60,"origin maximum age");}
    }
    [DataContract] public sealed class CareerCreationDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="stat_step",IsRequired=true)] public int StatStep {get;private set;}
        [DataMember(Name="expensive_after",IsRequired=true)] public int ExpensiveAfter {get;private set;}
        [DataMember(Name="extra_step_cost",IsRequired=true)] public int ExtraStepCost {get;private set;}
        [DataMember(Name="refund_per_step",IsRequired=true)] public int RefundPerStep {get;private set;}
        [DataMember(Name="costs",IsRequired=true)] private int[] costs {get;set;}
        [DataMember(Name="tiers",IsRequired=true)] private CareerCreationTier[] tiers {get;set;}
        [DataMember(Name="presets",IsRequired=true)] private CareerCreationPreset[] presets {get;set;}
        [DataMember(Name="origins",IsRequired=true)] private CareerOriginStory[] origins {get;set;}
        public int TierCount=>tiers.Length;public CareerCreationTier Tier(int index)=>tiers[index];
        public CareerCreationTier Tier(string id){foreach(var p in tiers)if(p.Id==id)return p;throw new ArgumentException("Unknown creation tier.");}
        public int PresetCount=>presets.Length;public CareerCreationPreset Preset(int index)=>presets[index];
        public CareerCreationPreset Preset(string id){foreach(var p in presets)if(p.Id==id)return p;throw new ArgumentException("Unknown specialization.");}
        public int OriginCount=>origins.Length;public CareerOriginStory Origin(int index)=>origins[index];
        public CareerOriginStory Origin(string id){foreach(var p in origins)if(p.Id==id)return p;throw new ArgumentException("Unknown origin.");}
        public int Cost(CareerStat stat,int allocation)
        {
            CareerUnits.Range((int)stat,0,12,"creation stat");CareerUnits.Range(allocation,-30,30,"allocation");
            if(allocation<0)return allocation*RefundPerStep;
            int expensive=Math.Max(0,allocation-ExpensiveAfter);
            return allocation*costs[(int)stat]+ExtraStepCost*expensive*(expensive+1)/2;
        }
        public string ToJson()=>CareerJson.Write(this);
        public static CareerCreationDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerCreationDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Unsupported creation definition.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.StatStep,1,1000,"creation step");CareerUnits.Range(d.ExpensiveAfter,1,30,"convex cost threshold");
            CareerUnits.Range(d.ExtraStepCost,1,100,"convex cost");CareerUnits.Range(d.RefundPerStep,0,10,"allocation refund");
            if(d.costs==null||d.costs.Length!=13)throw new SerializationException("Expected 13 stat costs.");
            foreach(int cost in d.costs)CareerUnits.Range(cost,Math.Max(1,d.RefundPerStep),100,"stat cost");
            if(d.tiers==null||d.tiers.Length!=3||d.presets==null||d.presets.Length<6||d.presets.Length>20||d.origins==null||d.origins.Length<1||d.origins.Length>30)throw new SerializationException("Missing creation choices.");
            for(int i=0;i<d.tiers.Length;i++){if(d.tiers[i]==null)throw new SerializationException("Null tier.");d.tiers[i].Validate();for(int j=0;j<i;j++)if(d.tiers[i].Id==d.tiers[j].Id)throw new SerializationException("Duplicate tier.");}
            for(int i=0;i<d.presets.Length;i++){if(d.presets[i]==null)throw new SerializationException("Null preset.");d.presets[i].Validate();for(int j=0;j<i;j++)if(d.presets[i].Id==d.presets[j].Id)throw new SerializationException("Duplicate preset.");}
            for(int i=0;i<d.origins.Length;i++){if(d.origins[i]==null)throw new SerializationException("Null origin.");d.origins[i].Validate();d.Preset(d.origins[i].PresetId);for(int j=0;j<i;j++)if(d.origins[i].Id==d.origins[j].Id)throw new SerializationException("Duplicate origin.");}
            return d;
        }
    }

    /// <summary>Detached settings. Preview/navigation neither creates a save nor consumes RNG.</summary>
    public sealed class CareerCreationDraft
    {
        private readonly CareerCreationDefinition policy;
        private readonly CareerStartDefinition starts;
        private readonly BiathlonRatingDefinition ratings;
        private readonly CareerRosterDefinition roster;
        private int[] allocation=new int[CareerUnits.StatCount];
        public CareerCreationMode Mode {get;private set;}=CareerCreationMode.CreateOwn;
        public string StartProfileId {get;private set;}
        public string SpecializationId {get;private set;}="balanced";
        public string OriginStoryId {get;private set;}="newcomer";
        public string Name {get;set;}="";
        public string Nation {get;set;}
        public int Age {get;private set;}
        public ulong DraftSeed {get;}
        public string PolicyId=>policy.Id;
        public CareerCreationTier Tier=>policy.Tier(StartProfileId);
        public int BudgetSpent {get{int sum=0;for(int i=0;i<13;i++)sum+=policy.Cost((CareerStat)i,allocation[i]);return sum;}}
        public int BudgetRemaining=>Tier.Budget-BudgetSpent;
        public int Overall=>BiathlonRatingEngine.CalculateStats(Stats(),ratings);
        public int Allocation(CareerStat stat){CareerUnits.Range((int)stat,0,12,"allocation stat");return allocation[(int)stat];}
        public int Stat(CareerStat stat){CareerUnits.Range((int)stat,0,12,"allocation stat");return Tier.BaselineStat+allocation[(int)stat]*policy.StatStep;}
        public CareerCreationDraft(CareerCreationDefinition policy,CareerStartDefinition starts,BiathlonRatingDefinition ratings,CareerRosterDefinition roster,ulong seed)
        {
            this.policy=policy??throw new ArgumentNullException(nameof(policy));this.starts=starts??throw new ArgumentNullException(nameof(starts));
            this.ratings=ratings??throw new ArgumentNullException(nameof(ratings));this.roster=roster??throw new ArgumentNullException(nameof(roster));
            DraftSeed=seed;Nation=roster.Nation(0);SelectTier(policy.Tier(0).Id);
        }
        public void SelectTier(string id)
        {
            var tier=policy.Tier(id);var profile=starts.Get(id);var next=policy.Preset(SpecializationId).Copy();ValidateAllocation(tier,next);
            StartProfileId=id;Age=Math.Max(profile.MinimumAge,Math.Min(profile.MaximumAge,Age==0?profile.DefaultAge:Age));allocation=next;
            if(!policy.Origin(OriginStoryId).Allows(Age))OriginStoryId="newcomer";
        }
        public void SetAge(int age)
        {
            var p=starts.Get(StartProfileId);CareerUnits.Range(age,p.MinimumAge,p.MaximumAge,"creation age");Age=age;
            if(!policy.Origin(OriginStoryId).Allows(age))OriginStoryId="newcomer";
        }
        public void SelectPreset(string id)
        {var next=policy.Preset(id).Copy();ValidateAllocation(Tier,next);allocation=next;SpecializationId=id;}
        public void SelectOrigin(string id)
        {
            var origin=policy.Origin(id);if(!origin.Allows(Age))throw new ArgumentException("Origin is incompatible with age.");
            SelectPreset(origin.PresetId);OriginStoryId=id;
        }
        public void SetAllocation(CareerStat stat,int points)
        {
            CareerUnits.Range((int)stat,0,12,"allocation stat");var next=(int[])allocation.Clone();next[(int)stat]=points;
            ValidateAllocation(Tier,next);allocation=next;
        }
        public void ResetAllocation(){var next=new int[13];ValidateAllocation(Tier,next);allocation=next;SpecializationId="balanced";}
        public void Validate()
        {
            CareerUnits.Text(Name?.Trim(),100,"player name");if(!roster.HasNation(Nation))throw new ArgumentException("Unsupported nationality.");
            var p=starts.Get(StartProfileId);CareerUnits.Range(Age,p.MinimumAge,p.MaximumAge,"creation age");
            if(!policy.Origin(OriginStoryId).Allows(Age))throw new ArgumentException("Incompatible origin.");
            ValidateAllocation(Tier,allocation);
        }
        private int[] Stats(){var stats=new int[13];for(int i=0;i<13;i++)stats[i]=Stat((CareerStat)i);return stats;}
        private void ValidateAllocation(CareerCreationTier tier,int[] points)
        {
            int cost=0;var stats=new int[13];
            for(int i=0;i<13;i++)
            {
                CareerUnits.Range(points[i],-tier.MaximumDecrease,tier.MaximumIncrease,"specialization limit");
                stats[i]=checked(tier.BaselineStat+points[i]*policy.StatStep);CareerUnits.Range(stats[i],tier.MinimumStat,tier.MaximumStat,"starting stat");
                cost+=policy.Cost((CareerStat)i,points[i]);
            }
            if(cost>tier.Budget)throw new ArgumentException("Starting allocation exceeds budget.");
            int overall=BiathlonRatingEngine.CalculateStats(stats,ratings);CareerUnits.Range(overall,tier.MinimumOvr,tier.MaximumOvr,"starting OVR");
        }
        public CareerSave Confirm(string careerId,CareerCalendar calendar,Func<ulong,IRandomSource> restoreRandom)
        {
            Validate();if(restoreRandom==null)throw new ArgumentNullException(nameof(restoreRandom));
            var random=restoreRandom(DraftSeed);if(random==null||random.State!=DraftSeed)throw new ArgumentException("Draft random checkpoint mismatch.");
            var save=CareerFactory.Create(roster,calendar,careerId,Name.Trim(),Nation,"player_appearance",random,starts,StartProfileId,Age);
            var player=save.Athlete(0);
            for(int i=0;i<13;i++)player.SetStat((CareerStat)i,Stat((CareerStat)i));
            // Starting ability is distinct from future potential; a ceiling cannot erase earned ability.
            int maximum=0;for(int i=0;i<13;i++)maximum=Math.Max(maximum,player.Stat((CareerStat)i));
            player.Potential=Math.Max(player.Potential,maximum);save.Validate();return save;
        }
    }
}

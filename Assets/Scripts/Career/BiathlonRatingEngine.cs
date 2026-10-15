using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    public enum BiathlonRating { Overall, Ski, Shooting, Physical, Technique, Mental }

    [DataContract] public sealed class CareerRatingWeight
    {
        [DataMember(Name="stat",IsRequired=true)] public CareerStat Stat {get;private set;}
        [DataMember(Name="overall",IsRequired=true)] public int Overall {get;private set;}
        [DataMember(Name="ski",IsRequired=true)] public int Ski {get;private set;}
        [DataMember(Name="shooting",IsRequired=true)] public int Shooting {get;private set;}
        [DataMember(Name="physical",IsRequired=true)] public int Physical {get;private set;}
        [DataMember(Name="technique",IsRequired=true)] public int Technique {get;private set;}
        [DataMember(Name="mental",IsRequired=true)] public int Mental {get;private set;}
        internal int Weight(BiathlonRating kind)
        {
            switch(kind)
            {
                case BiathlonRating.Overall:return Overall;
                case BiathlonRating.Ski:return Ski;
                case BiathlonRating.Shooting:return Shooting;
                case BiathlonRating.Physical:return Physical;
                case BiathlonRating.Technique:return Technique;
                case BiathlonRating.Mental:return Mental;
                default:throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }
    }

    [DataContract] public sealed class BiathlonRatingDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="weights",IsRequired=true)] private CareerRatingWeight[] weights {get;set;}
        internal CareerRatingWeight Get(int index)=>weights[index];
        public string ToJson()=>CareerJson.Write(this);
        public static BiathlonRatingDefinition FromJson(string json)
        {
            var d=CareerJson.Read<BiathlonRatingDefinition>(json);
            if(d==null||d.SchemaVersion!=1||d.weights==null||d.weights.Length!=CareerUnits.StatCount)
                throw new SerializationException("Expected rating weights for all 13 career stats.");
            CareerUnits.Id(d.Id);
            // DataContractJsonSerializer does not invoke field initializers.
            d.InitializeTotals();return d;
        }
        private int[] validatedTotals;
        private void InitializeTotals()
        {
            validatedTotals=new int[6];var seen=new bool[CareerUnits.StatCount];
            foreach(var row in weights)
            {
                if(row==null)throw new SerializationException("Null rating weight.");
                CareerUnits.Range((int)row.Stat,0,CareerUnits.StatCount-1,"rating stat");
                if(seen[(int)row.Stat])throw new SerializationException("Duplicate rating stat.");
                seen[(int)row.Stat]=true;
                for(int group=0;group<6;group++)
                {
                    int weight=row.Weight((BiathlonRating)group);CareerUnits.Range(weight,0,10000,"rating weight");
                    validatedTotals[group]+=weight;
                }
                if(row.Overall==0)throw new SerializationException("Every career stat must contribute to OVR.");
            }
            foreach(int sum in validatedTotals)if(sum==0)throw new SerializationException("Empty rating group.");
        }
        internal int Sum(BiathlonRating kind)=>validatedTotals[(int)kind];
    }

    public readonly struct BiathlonRatings
    {
        public int Overall {get;}
        public int Ski {get;}
        public int Shooting {get;}
        public int Physical {get;}
        public int Technique {get;}
        public int Mental {get;}
        internal BiathlonRatings(CareerAthlete a,BiathlonRatingDefinition d)
        {
            Overall=BiathlonRatingEngine.Calculate(a,d,BiathlonRating.Overall);
            Ski=BiathlonRatingEngine.Calculate(a,d,BiathlonRating.Ski);
            Shooting=BiathlonRatingEngine.Calculate(a,d,BiathlonRating.Shooting);
            Physical=BiathlonRatingEngine.Calculate(a,d,BiathlonRating.Physical);
            Technique=BiathlonRatingEngine.Calculate(a,d,BiathlonRating.Technique);
            Mental=BiathlonRatingEngine.Calculate(a,d,BiathlonRating.Mental);
        }
    }

    /// <summary>Read-only permanent-ability projection. Never used as race power or readiness.</summary>
    public static class BiathlonRatingEngine
    {
        public static BiathlonRatings Calculate(CareerAthlete athlete,BiathlonRatingDefinition definition)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();
            return new BiathlonRatings(athlete,definition);
        }
        public static int Calculate(CareerAthlete athlete,BiathlonRatingDefinition definition,BiathlonRating kind)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();
            if(kind<BiathlonRating.Overall||kind>BiathlonRating.Mental)throw new ArgumentOutOfRangeException(nameof(kind));
            long weighted=0;
            for(int i=0;i<CareerUnits.StatCount;i++)
            {var row=definition.Get(i);weighted+=(long)athlete.Stat(row.Stat)*row.Weight(kind);}
            return Normalize(weighted,definition.Sum(kind));
        }
        public static int StatRating(int stat)
        {CareerUnits.Range(stat,0,CareerUnits.MaximumStat,"rated attribute");return Normalize(stat,1);}
        public static int CalculateStats(IReadOnlyList<int> stats,BiathlonRatingDefinition definition,BiathlonRating kind=BiathlonRating.Overall)
        {
            if(stats==null||definition==null)throw new ArgumentNullException();
            if(stats.Count!=CareerUnits.StatCount)throw new ArgumentException("Expected 13 permanent attributes.",nameof(stats));
            if(kind<BiathlonRating.Overall||kind>BiathlonRating.Mental)throw new ArgumentOutOfRangeException(nameof(kind));
            long weighted=0;
            for(int i=0;i<CareerUnits.StatCount;i++)
            {
                var row=definition.Get(i);int value=stats[(int)row.Stat];
                CareerUnits.Range(value,0,CareerUnits.MaximumStat,"rated attribute");weighted+=(long)value*row.Weight(kind);
            }
            return Normalize(weighted,definition.Sum(kind));
        }
        private static int Normalize(long weighted,int weight)
        {
            long denominator=(long)weight*CareerUnits.MaximumStat;
            long numerator=weighted*98+denominator;
            long whole=numerator/denominator,remainder=numerator%denominator;
            if(remainder*2>denominator||remainder*2==denominator&&(whole&1)!=0)whole++;
            return checked((int)whole);
        }
    }
}

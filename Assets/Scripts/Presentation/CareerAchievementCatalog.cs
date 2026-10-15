using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Career;

namespace Biathlon.Presentation
{
    public enum CareerAchievementMetric { Races, Wins, PeakOverall }
    [DataContract] public sealed class CareerAchievement
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="ru",IsRequired=true)] public string Russian {get;private set;}
        [DataMember(Name="en",IsRequired=true)] public string English {get;private set;}
        [DataMember(Name="metric",IsRequired=true)] public CareerAchievementMetric Metric {get;private set;}
        [DataMember(Name="target",IsRequired=true)] public int Target {get;private set;}
        public string Title(string locale)=>locale=="ru"?Russian:locale=="en"?English:throw new ArgumentException("Unsupported achievement locale.");
        public int Value(CareerAthlete athlete,BiathlonRatingDefinition ratings)
        {
            if(athlete==null||ratings==null)throw new ArgumentNullException();
            return Metric==CareerAchievementMetric.Races?athlete.Races:Metric==CareerAchievementMetric.Wins?athlete.Wins:Math.Max(athlete.PeakOverall,BiathlonRatingEngine.Calculate(athlete,ratings).Overall);
        }
        public bool Earned(CareerAthlete athlete,BiathlonRatingDefinition ratings)=>Value(athlete,ratings)>=Target;
    }
    /// <summary>Observed milestones only. No rewards, save mutation, invented podiums or retroactive race results.</summary>
    [DataContract] public sealed class CareerAchievementCatalog
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="entries",IsRequired=true)] private CareerAchievement[] entries {get;set;}
        public int Count=>entries.Length;
        public CareerAchievement Get(int index)=>entries[index];
        public static CareerAchievementCatalog FromJson(string json)
        {
            if(json==null)throw new ArgumentNullException(nameof(json));CareerAchievementCatalog d;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))d=(CareerAchievementCatalog)new DataContractJsonSerializer(typeof(CareerAchievementCatalog)).ReadObject(stream);
            if(d==null||d.SchemaVersion!=1||!StableId(d.Id)||d.entries==null||d.entries.Length<1||d.entries.Length>24)throw new SerializationException("Invalid career achievement catalog.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var row in d.entries)
            {
                if(row==null||!StableId(row.Id)||!ids.Add(row.Id)||string.IsNullOrWhiteSpace(row.Russian)||string.IsNullOrWhiteSpace(row.English)||row.Russian.Length>100||row.English.Length>100||
                    row.Metric<CareerAchievementMetric.Races||row.Metric>CareerAchievementMetric.PeakOverall||row.Target<1||row.Target>(row.Metric==CareerAchievementMetric.PeakOverall?99:100000))throw new SerializationException("Invalid career achievement.");
            }return d;
        }
        private static bool StableId(string id)
        {if(string.IsNullOrEmpty(id)||id.Length>128)return false;foreach(char c in id)if(!(c>='a'&&c<='z'||c>='0'&&c<='9'||c=='_'))return false;return true;}
    }
}

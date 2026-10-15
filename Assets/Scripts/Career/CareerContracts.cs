using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;

namespace Biathlon.Career
{
    public enum CareerStat { Endurance, AnaerobicCapacity, Recovery, Strength, SkiTechnique, Climbing, Downhill, Cornering, BreathControl, ProneStability, StandingStability, ShootingSpeed, Composure }
    public enum CareerTier { Regional, National, International }
    public static class CareerUnits
    {
        public const int StatCount=13, StatScale=1000, MaximumStat=100000, Basis=10000;
        internal static void Id(string value)
        {if(string.IsNullOrEmpty(value)||value.Length>128)throw new SerializationException("Invalid career ID.");foreach(char c in value)if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='_'||c=='-'))throw new SerializationException("Invalid career ID.");}
        internal static void Range(int value,int min,int max,string name){if(value<min||value>max)throw new SerializationException("Invalid "+name+".");}
        internal static void Text(string value,int maximum,string name){if(string.IsNullOrWhiteSpace(value)||value.Length>maximum||value.IndexOfAny(new[]{'\r','\n','\0'})>=0)throw new SerializationException("Invalid "+name+".");}
        internal static int ClampBasis(int value)=>Math.Max(0,Math.Min(Basis,value));
        internal static int Round(decimal value)=>checked((int)Math.Round(value,0,MidpointRounding.ToEven));
    }
    internal static class CareerJson
    {
        internal static T Read<T>(string text){if(text==null)throw new ArgumentNullException(nameof(text));using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(text.TrimStart('\uFEFF'))))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);}
        internal static string Write<T>(T value){using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value);return Encoding.UTF8.GetString(stream.ToArray());}}
    }
    [DataContract] public sealed class CareerAthlete
    {
        private CareerAthlete() { }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; internal set; }
        [DataMember(Name="name",IsRequired=true)] public string Name { get; internal set; }
        [DataMember(Name="nation",IsRequired=true)] public string Nation { get; internal set; }
        [DataMember(Name="appearance_id",IsRequired=true)] public string AppearanceId { get; internal set; }
        [DataMember(Name="appearance_profile",EmitDefaultValue=false)] public AppearanceProfile Appearance {get;internal set;}
        [DataMember(Name="personality_id",IsRequired=true)] public string PersonalityId { get; internal set; }
        [DataMember(Name="age_years",IsRequired=true)] public int Age { get; internal set; }
        [DataMember(Name="potential",IsRequired=true)] public int Potential { get; internal set; }
        [DataMember(Name="stats",IsRequired=true)] private int[] stats { get; set; }
        [DataMember(Name="fatigue",IsRequired=true)] public int Fatigue { get; internal set; }
        [DataMember(Name="form",IsRequired=true)] public int Form { get; internal set; }
        [DataMember(Name="morale",IsRequired=true)] public int Morale { get; internal set; }
        [DataMember(Name="confidence")] public int Confidence { get; internal set; }
        [DataMember(Name="health",IsRequired=true)] public int Health { get; internal set; }
        [DataMember(Name="health_risk",IsRequired=true)] public int HealthRisk { get; internal set; }
        [DataMember(Name="tier",IsRequired=true)] public CareerTier Tier { get; internal set; }
        [DataMember(Name="last_progression_season",EmitDefaultValue=false)] public int LastProgressionSeason { get; internal set; }
        [DataMember(Name="pending_camp_quality",EmitDefaultValue=false)] public int PendingCampQuality { get; internal set; }
        [DataMember(Name="money_minor",IsRequired=true)] public long Money { get; internal set; }
        [DataMember(Name="inventory",IsRequired=true)] private string[] inventory { get; set; }
        [DataMember(Name="selected_ski",IsRequired=true)] public string SelectedSki { get; internal set; }
        [DataMember(Name="training_sessions",IsRequired=true)] public int TrainingSessions { get; internal set; }
        [DataMember(Name="races",IsRequired=true)] public int Races { get; internal set; }
        [DataMember(Name="wins",IsRequired=true)] public int Wins { get; internal set; }
        [DataMember(Name="lifetime_earnings_minor",IsRequired=true)] public long LifetimeEarnings { get; internal set; }
        public int Stat(CareerStat stat){CareerUnits.Range((int)stat,0,CareerUnits.StatCount-1,"stat index");return stats[(int)stat];}
        public int InventoryCount=>inventory.Length;public string InventoryItem(int index)=>inventory[index];
        internal void SetStat(CareerStat stat,int value)=>stats[(int)stat]=value;
        internal void AddEquipment(string id){var items=new string[inventory.Length+1];Array.Copy(inventory,items,inventory.Length);items[items.Length-1]=id;inventory=items;}
        [OnDeserializing] private void BeforeRead(StreamingContext context){Confidence=-1;}
        internal static CareerAthlete Create(string id,string name,string nation,string appearance,string personality,int age,int potential,int[] attributes,int initialState,long money)
        {
            var a=new CareerAthlete{Id=id,Name=name,Nation=nation,AppearanceId=appearance,Appearance=AppearanceProfile.Legacy(id),PersonalityId=personality,Age=age,Potential=potential,stats=(int[])attributes.Clone(),
                Form=initialState,Morale=initialState,Confidence=initialState,Health=CareerUnits.Basis,Money=money,inventory=new[]{"ski_neutral"},SelectedSki="ski_neutral"};a.Validate();return a;
        }
        internal void Validate()
        {
            CareerUnits.Id(Id);CareerUnits.Text(Name,100,"athlete name");CareerUnits.Id(Nation);CareerUnits.Id(AppearanceId);CareerUnits.Id(PersonalityId);
            if(Appearance==null)throw new SerializationException("Missing appearance profile.");Appearance.Validate();
            CareerUnits.Range(Age,1,120,"age");CareerUnits.Range(Potential,1,CareerUnits.MaximumStat,"potential");
            if(stats==null||stats.Length!=CareerUnits.StatCount)throw new SerializationException("Expected all career attributes.");
            foreach(int stat in stats)CareerUnits.Range(stat,0,CareerUnits.MaximumStat,"attribute");
            foreach(int state in new[]{Fatigue,Form,Morale,Confidence,Health,HealthRisk})CareerUnits.Range(state,0,CareerUnits.Basis,"temporary state");
            CareerUnits.Range(LastProgressionSeason,0,10000,"progression season");CareerUnits.Range(PendingCampQuality,0,12000,"pending camp quality");CareerUnits.Range((int)Tier,0,2,"tier");if(Money<0||LifetimeEarnings<0||TrainingSessions<0||Races<0||Wins<0||Wins>Races)throw new SerializationException("Invalid athlete counters.");
            if(inventory==null||inventory.Length<1||inventory.Length>100)throw new SerializationException("Invalid inventory.");
            var ids=new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);foreach(string item in inventory){CareerUnits.Id(item);if(!ids.Add(item))throw new SerializationException("Duplicate equipment.");}
            if(!ids.Contains(SelectedSki))throw new SerializationException("Selected skis are not owned.");
        }
        public string ToJson()=>CareerJson.Write(this);
    }
}

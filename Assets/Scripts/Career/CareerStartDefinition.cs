using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    public enum CareerCircuit { Open, Junior, Development, WorldCup }
    [DataContract] public sealed class CareerStartProfile
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="calendar_resource",IsRequired=true)] public string CalendarResource {get;private set;}
        [DataMember(Name="minimum_age",IsRequired=true)] public int MinimumAge {get;private set;}
        [DataMember(Name="maximum_age",IsRequired=true)] public int MaximumAge {get;private set;}
        [DataMember(Name="default_age",IsRequired=true)] public int DefaultAge {get;private set;}
        [DataMember(Name="player_skill_min",IsRequired=true)] public int PlayerSkillMin {get;private set;}
        [DataMember(Name="player_skill_max",IsRequired=true)] public int PlayerSkillMax {get;private set;}
        [DataMember(Name="peer_skill_min",IsRequired=true)] public int PeerSkillMin {get;private set;}
        [DataMember(Name="peer_skill_max",IsRequired=true)] public int PeerSkillMax {get;private set;}
        [DataMember(Name="peer_stat_spread",IsRequired=true)] public int PeerStatSpread {get;private set;}
        [DataMember(Name="peer_age_min",IsRequired=true)] public int PeerAgeMin {get;private set;}
        [DataMember(Name="peer_age_max",IsRequired=true)] public int PeerAgeMax {get;private set;}
        [DataMember(Name="player_tier",IsRequired=true)] public CareerTier PlayerTier {get;private set;}
        internal void Validate()
        {
            CareerUnits.Id(Id);if(string.IsNullOrWhiteSpace(CalendarResource)||!CalendarResource.EndsWith(".json",StringComparison.Ordinal)||CalendarResource.IndexOfAny(new[]{'/','\\'})>=0)throw new SerializationException("Invalid start calendar resource.");
            CareerUnits.Range(MinimumAge,16,40,"start minimum age");CareerUnits.Range(MaximumAge,MinimumAge,40,"start maximum age");CareerUnits.Range(DefaultAge,MinimumAge,MaximumAge,"start default age");
            CareerUnits.Range(PeerAgeMin,16,60,"peer minimum age");CareerUnits.Range(PeerAgeMax,PeerAgeMin,60,"peer maximum age");
            CareerUnits.Range(PlayerSkillMin,0,CareerUnits.MaximumStat,"start skill");CareerUnits.Range(PlayerSkillMax,PlayerSkillMin,CareerUnits.MaximumStat,"start skill");
            CareerUnits.Range(PeerSkillMin,0,CareerUnits.MaximumStat,"peer skill");CareerUnits.Range(PeerSkillMax,PeerSkillMin,CareerUnits.MaximumStat,"peer skill");CareerUnits.Range(PeerStatSpread,0,20000,"peer specialization");CareerUnits.Range((int)PlayerTier,0,2,"start tier");
        }
    }
    [DataContract] public sealed class CareerStartDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="first_season_year",IsRequired=true)] public int FirstSeasonYear {get;private set;}
        [DataMember(Name="profiles",IsRequired=true)] private CareerStartProfile[] profiles {get;set;}
        public int Count=>profiles.Length;public CareerStartProfile Get(int index)=>profiles[index];
        public CareerStartProfile Get(string id){foreach(var p in profiles)if(p.Id==id)return p;throw new ArgumentException("Unknown career start profile.",nameof(id));}
        public static CareerStartDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerStartDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid career start schema.");CareerUnits.Id(d.Id);CareerUnits.Range(d.FirstSeasonYear,2020,2200,"first season year");
            if(d.profiles==null||d.profiles.Length<1||d.profiles.Length>10)throw new SerializationException("Start profiles required.");
            for(int i=0;i<d.Count;i++){d.profiles[i]?.Validate();if(d.profiles[i]==null)throw new SerializationException("Null start profile.");for(int j=0;j<i;j++)if(d.profiles[i].Id==d.profiles[j].Id)throw new SerializationException("Duplicate start profile.");}return d;
        }
    }
}

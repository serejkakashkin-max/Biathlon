using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerRosterDefinition
    {
        private CareerRosterDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="population",IsRequired=true)] public int Population { get; private set; }
        [DataMember(Name="player_age",IsRequired=true)] public int PlayerAge { get; private set; }
        [DataMember(Name="player_skill_min",IsRequired=true)] public int PlayerSkillMin { get; private set; }
        [DataMember(Name="player_skill_max",IsRequired=true)] public int PlayerSkillMax { get; private set; }
        [DataMember(Name="ai_age_min",IsRequired=true)] public int AiAgeMin { get; private set; }
        [DataMember(Name="ai_age_max",IsRequired=true)] public int AiAgeMax { get; private set; }
        [DataMember(Name="ai_skill_min",IsRequired=true)] public int AiSkillMin { get; private set; }
        [DataMember(Name="ai_skill_max",IsRequired=true)] public int AiSkillMax { get; private set; }
        [DataMember(Name="potential_min",IsRequired=true)] public int PotentialMin { get; private set; }
        [DataMember(Name="potential_max",IsRequired=true)] public int PotentialMax { get; private set; }
        [DataMember(Name="initial_state",IsRequired=true)] public int InitialState { get; private set; }
        [DataMember(Name="starting_money_minor",IsRequired=true)] public long StartingMoney { get; private set; }
        [DataMember(Name="nations",IsRequired=true)] private string[] nations { get; set; }
        [DataMember(Name="given_names",IsRequired=true)] private string[] givenNames { get; set; }
        [DataMember(Name="family_names",IsRequired=true)] private string[] familyNames { get; set; }
        [DataMember(Name="personalities",IsRequired=true)] private string[] personalities { get; set; }
        public int NationCount=>nations.Length;public string Nation(int i)=>nations[i];
        public int GivenNameCount=>givenNames.Length;public string GivenName(int i)=>givenNames[i];
        public int FamilyNameCount=>familyNames.Length;public string FamilyName(int i)=>familyNames[i];
        public int PersonalityCount=>personalities.Length;public string Personality(int i)=>personalities[i];
        public bool HasNation(string nation)=>Array.IndexOf(nations,nation)>=0;
        public string ToJson()=>CareerJson.Write(this);
        public static CareerRosterDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerRosterDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid roster schema.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.Population,2,1000,"population");CareerUnits.Range(d.PlayerAge,16,60,"player age");CareerUnits.Range(d.AiAgeMin,16,60,"AI age");CareerUnits.Range(d.AiAgeMax,d.AiAgeMin,60,"AI age");
            CareerUnits.Range(d.PlayerSkillMin,0,CareerUnits.MaximumStat,"player skill");CareerUnits.Range(d.PlayerSkillMax,d.PlayerSkillMin,CareerUnits.MaximumStat,"player skill");
            CareerUnits.Range(d.AiSkillMin,0,CareerUnits.MaximumStat,"AI skill");CareerUnits.Range(d.AiSkillMax,d.AiSkillMin,CareerUnits.MaximumStat,"AI skill");
            CareerUnits.Range(d.PotentialMin,Math.Max(1,d.PlayerSkillMax),CareerUnits.MaximumStat,"potential");CareerUnits.Range(d.PotentialMax,d.PotentialMin,CareerUnits.MaximumStat,"potential");
            CareerUnits.Range(d.InitialState,0,CareerUnits.Basis,"initial state");if(d.StartingMoney<0)throw new SerializationException("Negative starting money.");
            foreach(var list in new[]{d.nations,d.givenNames,d.familyNames,d.personalities})if(list==null||list.Length==0||list.Length>10000)throw new SerializationException("Empty/oversized roster dictionary.");
            foreach(string value in d.nations)CareerUnits.Id(value);foreach(string value in d.personalities)CareerUnits.Id(value);
            foreach(string value in d.givenNames)CareerUnits.Text(value,40,"given name");foreach(string value in d.familyNames)CareerUnits.Text(value,40,"family name");return d;
        }
    }
}

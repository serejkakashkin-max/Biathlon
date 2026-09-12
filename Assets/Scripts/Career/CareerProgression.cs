using System;
using System.Runtime.Serialization;
namespace Biathlon.Career
{
    [DataContract] public sealed class CareerProgressionDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="minimum_races",IsRequired=true)] public int MinimumRaces { get; private set; }
        [DataMember(Name="national_max_rank",IsRequired=true)] public int NationalMaximumRank { get; private set; }
        [DataMember(Name="international_max_rank",IsRequired=true)] public int InternationalMaximumRank { get; private set; }
        public static CareerProgressionDefinition FromJson(string json)
        {var d=CareerJson.Read<CareerProgressionDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid progression.");CareerUnits.Id(d.Id);CareerUnits.Range(d.MinimumRaces,1,366,"minimum races");CareerUnits.Range(d.NationalMaximumRank,1,1000,"national rank");CareerUnits.Range(d.InternationalMaximumRank,1,d.NationalMaximumRank,"international rank");return d;}
    }
    public static class CareerProgression
    {
        public static CareerSave ApplySeason(CareerSave source,CareerCalendar calendar,CareerScoringDefinition scoring,CareerProgressionDefinition definition)
        {
            if(source==null||calendar==null||scoring==null||definition==null)throw new ArgumentNullException();source.Validate();
            if(source.CalendarId!=calendar.Id||source.Week!=calendar.WeeksPerSeason)throw new InvalidOperationException("Progression is evaluated at season end.");
            var save=CareerSave.FromJson(source.ToJson());var table=CareerStandings.Table(save,scoring);
            for(int rank=0;rank<table.Length;rank++)for(int i=0;i<save.AthleteCount;i++)
            {
                var a=save.Athlete(i);if(a.Id!=table[rank].AthleteId||a.LastProgressionSeason==save.Season)continue;
                if(table[rank].Races>=definition.MinimumRaces)
                {
                    if(a.Tier==CareerTier.Regional&&rank+1<=definition.NationalMaximumRank)a.Tier=CareerTier.National;
                    else if(a.Tier==CareerTier.National&&rank+1<=definition.InternationalMaximumRank)a.Tier=CareerTier.International;
                }
                a.LastProgressionSeason=save.Season;
            }
            save.Validate();return save;
        }
    }
}

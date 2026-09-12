using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerFormDefinition
    {
        private CareerFormDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="rest_target",IsRequired=true)] public int RestTarget { get; private set; }
        [DataMember(Name="productive_target",IsRequired=true)] public int ProductiveTarget { get; private set; }
        [DataMember(Name="taper_target",IsRequired=true)] public int TaperTarget { get; private set; }
        [DataMember(Name="overload_fatigue",IsRequired=true)] public int OverloadFatigue { get; private set; }
        [DataMember(Name="maximum_fatigue_penalty",IsRequired=true)] public int MaximumFatiguePenalty { get; private set; }
        [DataMember(Name="weekly_response",IsRequired=true)] public int WeeklyResponse { get; private set; }
        [DataMember(Name="minimum_race_form",IsRequired=true)] public int MinimumRaceForm { get; private set; }
        [DataMember(Name="maximum_race_form",IsRequired=true)] public int MaximumRaceForm { get; private set; }
        public static CareerFormDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerFormDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid form definition.");CareerUnits.Id(d.Id);
            foreach(int v in new[]{d.RestTarget,d.ProductiveTarget,d.TaperTarget,d.MaximumFatiguePenalty,d.WeeklyResponse,d.MinimumRaceForm})CareerUnits.Range(v,0,CareerUnits.Basis,"form coefficient");
            CareerUnits.Range(d.OverloadFatigue,0,CareerUnits.Basis-1,"overload threshold");CareerUnits.Range(d.MaximumRaceForm,d.MinimumRaceForm,11000,"race form");return d;
        }
    }
    public readonly struct CareerFormFrame
    {
        public int Before { get; } public int After { get; } public int Target { get; }
        internal CareerFormFrame(int before,int after,int target){Before=before;After=after;Target=target;}
    }
    public static class CareerForm
    {
        public static CareerFormFrame Advance(CareerAthlete athlete,CareerFormDefinition definition,bool productiveTraining,bool taper)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            // Taper is a choice to reduce work before an event, not a permanent skill increase.
            int target=taper?definition.TaperTarget:productiveTraining?definition.ProductiveTarget:definition.RestTarget;
            int excess=Math.Max(0,athlete.Fatigue-definition.OverloadFatigue);
            target-=CareerUnits.Round((decimal)definition.MaximumFatiguePenalty*excess/(CareerUnits.Basis-definition.OverloadFatigue));
            target=CareerUnits.ClampBasis(CareerUnits.Round((decimal)target*athlete.Health/CareerUnits.Basis));
            int before=athlete.Form;athlete.Form=CareerUnits.ClampBasis(before+CareerUnits.Round((decimal)(target-before)*definition.WeeklyResponse/CareerUnits.Basis));
            return new CareerFormFrame(before,athlete.Form,target);
        }
        public static double RaceMultiplier(CareerAthlete athlete,CareerFormDefinition definition)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            return (definition.MinimumRaceForm+(double)(definition.MaximumRaceForm-definition.MinimumRaceForm)*athlete.Form/CareerUnits.Basis)/CareerUnits.Basis;
        }
    }
}

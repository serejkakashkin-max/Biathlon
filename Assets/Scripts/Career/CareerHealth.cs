using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerHealthDefinition
    {
        private CareerHealthDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="fatigue_threshold",IsRequired=true)] public int FatigueThreshold { get; private set; }
        [DataMember(Name="maximum_fatigue_risk",IsRequired=true)] public int MaximumFatigueRisk { get; private set; }
        [DataMember(Name="maximum_age_risk",IsRequired=true)] public int MaximumAgeRisk { get; private set; }
        [DataMember(Name="age_start",IsRequired=true)] public int AgeStart { get; private set; }
        [DataMember(Name="age_full",IsRequired=true)] public int AgeFull { get; private set; }
        [DataMember(Name="maximum_risk",IsRequired=true)] public int MaximumRisk { get; private set; }
        public static CareerHealthDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerHealthDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid health definition.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.FatigueThreshold,0,CareerUnits.Basis-1,"fatigue threshold");foreach(int v in new[]{d.MaximumFatigueRisk,d.MaximumAgeRisk,d.MaximumRisk})CareerUnits.Range(v,0,CareerUnits.Basis,"risk");
            CareerUnits.Range(d.AgeStart,1,119,"risk age");CareerUnits.Range(d.AgeFull,d.AgeStart+1,120,"risk age");return d;
        }
    }
    public readonly struct CareerHealthFrame
    {
        public int SessionRisk { get; } public int FatigueRisk { get; } public int AgeRisk { get; } public int TotalRisk { get; }
        internal CareerHealthFrame(int session,int fatigue,int age,int total){SessionRisk=session;FatigueRisk=fatigue;AgeRisk=age;TotalRisk=total;}
    }
    public static class CareerHealth
    {
        public static CareerHealthFrame Evaluate(CareerAthlete athlete,CareerHealthDefinition definition,int sessionRisk)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();CareerUnits.Range(sessionRisk,0,CareerUnits.Basis,"session risk");
            int fatigue=CareerUnits.Round((decimal)definition.MaximumFatigueRisk*Math.Max(0,athlete.Fatigue-definition.FatigueThreshold)/(CareerUnits.Basis-definition.FatigueThreshold));
            int age=CareerUnits.Round((decimal)definition.MaximumAgeRisk*Math.Min(definition.AgeFull-definition.AgeStart,Math.Max(0,athlete.Age-definition.AgeStart))/(definition.AgeFull-definition.AgeStart));
            int total=Math.Min(definition.MaximumRisk,sessionRisk+fatigue+age);return new CareerHealthFrame(sessionRisk,fatigue,age,total);
        }
        public static CareerHealthFrame RecordRisk(CareerAthlete athlete,CareerHealthDefinition definition,int sessionRisk)
        {
            var frame=Evaluate(athlete,definition,sessionRisk);athlete.HealthRisk=frame.TotalRisk;
            // M9 owner gate: do not turn a risk indicator into injury, absence or permanent damage.
            return frame;
        }
    }
}

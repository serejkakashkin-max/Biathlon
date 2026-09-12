using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerAgePoint
    {
        private CareerAgePoint() { }
        [DataMember(Name="age",IsRequired=true)] public int Age { get; private set; }
        [DataMember(Name="training_factor",IsRequired=true)] public int TrainingFactor { get; private set; }
        [DataMember(Name="annual_decline",IsRequired=true)] public int AnnualDecline { get; private set; }
    }
    [DataContract] public sealed class CareerAdaptationDefinition
    {
        private CareerAdaptationDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="minimum_recovery_factor",IsRequired=true)] public int MinimumRecoveryFactor { get; private set; }
        [DataMember(Name="maximum_quality",IsRequired=true)] public int MaximumQuality { get; private set; }
        [DataMember(Name="age_curve",IsRequired=true)] private CareerAgePoint[] ageCurve { get; set; }
        public int TrainingFactor(int age)=>Interpolate(age,false);
        public int AnnualDecline(int age)=>Interpolate(age,true);
        private int Interpolate(int age,bool decline)
        {
            CareerUnits.Range(age,1,120,"age");
            int Value(CareerAgePoint p)=>decline?p.AnnualDecline:p.TrainingFactor;
            if(age<=ageCurve[0].Age)return Value(ageCurve[0]);
            for(int i=1;i<ageCurve.Length;i++)if(age<=ageCurve[i].Age)
            {var a=ageCurve[i-1];var b=ageCurve[i];return CareerUnits.Round(Value(a)+(decimal)(Value(b)-Value(a))*(age-a.Age)/(b.Age-a.Age));}
            return Value(ageCurve[ageCurve.Length-1]);
        }
        public string ToJson()=>CareerJson.Write(this);
        public static CareerAdaptationDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerAdaptationDefinition>(json);if(d==null||d.SchemaVersion!=1||d.ageCurve==null||d.ageCurve.Length<2||d.ageCurve.Length>120)throw new SerializationException("Invalid adaptation definition.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.MinimumRecoveryFactor,0,CareerUnits.Basis,"recovery floor");CareerUnits.Range(d.MaximumQuality,CareerUnits.Basis,2*CareerUnits.Basis,"quality cap");
            int previous=0;foreach(var p in d.ageCurve){if(p==null)throw new SerializationException("Null age knot.");CareerUnits.Range(p.Age,previous+1,120,"ordered age knot");CareerUnits.Range(p.TrainingFactor,0,2*CareerUnits.Basis,"age factor");CareerUnits.Range(p.AnnualDecline,0,CareerUnits.MaximumStat,"annual decline");previous=p.Age;}return d;
        }
    }
    public readonly struct CareerAdaptationFrame
    {
        public int TotalGain { get; } public int Quality { get; } public int AgeFactor { get; } public int RecoveryFactor { get; }
        internal CareerAdaptationFrame(int gain,int quality,int age,int recovery){TotalGain=gain;Quality=quality;AgeFactor=age;RecoveryFactor=recovery;}
    }
    public static class CareerAdaptation
    {
        public static CareerAdaptationFrame Apply(CareerAthlete athlete,CareerTrainingSession session,CareerAdaptationDefinition definition,int quality)
        {
            if(athlete==null||session==null||definition==null)throw new ArgumentNullException();athlete.Validate();CareerUnits.Range(quality,0,definition.MaximumQuality,"training quality");
            int age=definition.TrainingFactor(athlete.Age);
            int recovery=Math.Max(definition.MinimumRecoveryFactor,CareerUnits.Basis-athlete.Fatigue);
            var gains=new int[CareerUnits.StatCount];int total=0;
            for(int i=0;i<gains.Length;i++)
            {
                int current=athlete.Stat((CareerStat)i);int room=Math.Max(0,athlete.Potential-current);
                decimal gain=(decimal)session.Gain((CareerStat)i)*quality/CareerUnits.Basis*age/CareerUnits.Basis*recovery/CareerUnits.Basis;
                gain*=athlete.Potential/(decimal)CareerUnits.MaximumStat;
                gain*=room/(decimal)athlete.Potential;
                gains[i]=Math.Min(room,CareerUnits.Round(gain));total=checked(total+gains[i]);
            }
            // Compute/validate the entire update before mutating any attribute.
            for(int i=0;i<gains.Length;i++)athlete.SetStat((CareerStat)i,athlete.Stat((CareerStat)i)+gains[i]);
            return new CareerAdaptationFrame(total,quality,age,recovery);
        }
    }
}

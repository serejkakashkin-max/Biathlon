using System;
using System.Runtime.Serialization;
namespace Biathlon.Career
{
    [DataContract] public sealed class CareerMentalDefinition
    {
        private CareerMentalDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="race_morale_change",IsRequired=true)] public int RaceMoraleChange { get; private set; }
        [DataMember(Name="race_confidence_change",IsRequired=true)] public int RaceConfidenceChange { get; private set; }
        [DataMember(Name="training_morale_gain",IsRequired=true)] public int TrainingMoraleGain { get; private set; }
        [DataMember(Name="training_confidence_gain",IsRequired=true)] public int TrainingConfidenceGain { get; private set; }
        [DataMember(Name="baseline",IsRequired=true)] public int Baseline { get; private set; }
        [DataMember(Name="weekly_reversion",IsRequired=true)] public int WeeklyReversion { get; private set; }
        [DataMember(Name="maximum_pressure",IsRequired=true)] public int MaximumPressure { get; private set; }
        public static CareerMentalDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerMentalDefinition>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid mental definition.");CareerUnits.Id(d.Id);
            foreach(int v in new[]{d.RaceMoraleChange,d.RaceConfidenceChange,d.TrainingMoraleGain,d.TrainingConfidenceGain,d.Baseline,d.WeeklyReversion,d.MaximumPressure})CareerUnits.Range(v,0,CareerUnits.Basis,"mental coefficient");return d;
        }
    }
    public static class CareerMental
    {
        public static void Race(CareerAthlete athlete,CareerMentalDefinition definition,int rank,int expectedRank,int fieldCount)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();CareerUnits.Range(fieldCount,1,1000,"field count");CareerUnits.Range(rank,1,fieldCount,"rank");CareerUnits.Range(expectedRank,1,fieldCount,"expected rank");
            decimal performance=(decimal)(expectedRank-rank)/Math.Max(1,fieldCount-1);
            athlete.Morale=CareerUnits.ClampBasis(athlete.Morale+CareerUnits.Round(performance*definition.RaceMoraleChange));
            athlete.Confidence=CareerUnits.ClampBasis(athlete.Confidence+CareerUnits.Round(performance*definition.RaceConfidenceChange));
        }
        public static void Training(CareerAthlete athlete,CareerMentalDefinition definition,bool productive)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            if(productive){athlete.Morale=CareerUnits.ClampBasis(athlete.Morale+definition.TrainingMoraleGain);athlete.Confidence=CareerUnits.ClampBasis(athlete.Confidence+definition.TrainingConfidenceGain);}
        }
        public static void Rest(CareerAthlete athlete,CareerMentalDefinition definition)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            athlete.Morale=CareerUnits.ClampBasis(athlete.Morale+CareerUnits.Round((decimal)(definition.Baseline-athlete.Morale)*definition.WeeklyReversion/CareerUnits.Basis));
            athlete.Confidence=CareerUnits.ClampBasis(athlete.Confidence+CareerUnits.Round((decimal)(definition.Baseline-athlete.Confidence)*definition.WeeklyReversion/CareerUnits.Basis));
        }
        public static double Pressure(CareerAthlete athlete,CareerMentalDefinition definition)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();
            return (2.0*CareerUnits.Basis-athlete.Morale-athlete.Confidence)/(2*CareerUnits.Basis)*definition.MaximumPressure/CareerUnits.Basis;
        }
    }
}

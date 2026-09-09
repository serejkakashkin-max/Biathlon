using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class OvertakeDecisionDefinition
    {
        private OvertakeDecisionDefinition() { }
        [DataMember(Name="schema_version", IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id", IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="minimum_reserve", IsRequired=true)] public double MinimumReserve { get; private set; }
        [DataMember(Name="maximum_fatigue", IsRequired=true)] public double MaximumFatigue { get; private set; }
        [DataMember(Name="minimum_race_value", IsRequired=true)] public double MinimumRaceValue { get; private set; }
        [DataMember(Name="maximum_abs_grade", IsRequired=true)] public double MaximumAbsGrade { get; private set; }
        [DataMember(Name="maximum_abs_curvature_per_m", IsRequired=true)] public double MaximumAbsCurvature { get; private set; }
        [DataMember(Name="minimum_closing_speed_mps", IsRequired=true)] public double MinimumClosingSpeed { get; private set; }
        [DataMember(Name="maximum_pass_seconds", IsRequired=true)] public double MaximumPassSeconds { get; private set; }
        public static OvertakeDecisionDefinition FromJson(string json)
        {
            var v=RouteJson.Read<OvertakeDecisionDefinition>(json);
            if(v==null || v.SchemaVersion!=1) throw new SerializationException("Unsupported overtake decision schema.");
            TrackValidation.Id(v.Id); TrackValidation.Finite(v.MinimumReserve,0,1,nameof(MinimumReserve));
            TrackValidation.Finite(v.MaximumFatigue,0,1,nameof(MaximumFatigue)); TrackValidation.Finite(v.MinimumRaceValue,0,1,nameof(MinimumRaceValue));
            TrackValidation.Finite(v.MaximumAbsGrade,0,1,nameof(MaximumAbsGrade)); TrackValidation.Finite(v.MaximumAbsCurvature,0,1,nameof(MaximumAbsCurvature));
            TrackValidation.Finite(v.MinimumClosingSpeed,0.000001,1000,nameof(MinimumClosingSpeed));
            TrackValidation.Finite(v.MaximumPassSeconds,0.000001,120,nameof(MaximumPassSeconds)); return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

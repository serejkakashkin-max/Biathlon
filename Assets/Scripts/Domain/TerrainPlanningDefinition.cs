using System;
using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class TerrainPlanningDefinition
    {
        private TerrainPlanningDefinition() { }
        [DataMember(Name="schema_version", IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id", IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="minimum_preview_m", IsRequired=true)] public double MinimumPreviewMeters { get; private set; }
        [DataMember(Name="maximum_preview_m", IsRequired=true)] public double MaximumPreviewMeters { get; private set; }
        [DataMember(Name="full_climb_grade", IsRequired=true)] public double FullClimbGrade { get; private set; }
        [DataMember(Name="full_turn_curvature_per_m", IsRequired=true)] public double FullTurnCurvature { get; private set; }
        [DataMember(Name="climb_effort", IsRequired=true)] public int ClimbEffort { get; private set; }
        [DataMember(Name="turn_effort_reduction", IsRequired=true)] public int TurnEffortReduction { get; private set; }
        public static TerrainPlanningDefinition FromJson(string json)
        {
            var v=RouteJson.Read<TerrainPlanningDefinition>(json);
            if(v==null || v.SchemaVersion!=1) throw new SerializationException("Unsupported terrain planning schema.");
            TrackValidation.Id(v.Id); TrackValidation.Finite(v.MinimumPreviewMeters,0.001,10000,nameof(MinimumPreviewMeters));
            TrackValidation.Finite(v.MaximumPreviewMeters,v.MinimumPreviewMeters,10000,nameof(MaximumPreviewMeters));
            TrackValidation.Finite(v.FullClimbGrade,0.000001,1,nameof(FullClimbGrade));
            TrackValidation.Finite(v.FullTurnCurvature,0.000001,1,nameof(FullTurnCurvature));
            AthleteState.ValidateEffort(v.ClimbEffort); AthleteState.ValidateEffort(v.TurnEffortReduction); return v;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

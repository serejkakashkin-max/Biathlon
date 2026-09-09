using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class AiDifficultyDefinition
    {
        private AiDifficultyDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="decision_hz",IsRequired=true)] public int DecisionHz { get; private set; }
        [DataMember(Name="planning_horizon_seconds",IsRequired=true)] public double HorizonSeconds { get; private set; }
        [DataMember(Name="reaction_seconds",IsRequired=true)] public double ReactionSeconds { get; private set; }
        [DataMember(Name="tracking_error_rad",IsRequired=true)] public double TrackingError { get; private set; }
        [DataMember(Name="pace_control_error",IsRequired=true)] public int PaceControlError { get; private set; }
        public static AiDifficultyDefinition FromJson(string json)
        {
            var v=RouteJson.Read<AiDifficultyDefinition>(json);
            if(v==null || v.SchemaVersion!=1) throw new SerializationException("Unsupported difficulty schema.");
            TrackValidation.Id(v.Id);
            if(v.DecisionHz<2 || v.DecisionHz>5) throw new ArgumentException("Decision cadence must be 2–5 Hz.");
            TrackValidation.Finite(v.HorizonSeconds,0.01,120,nameof(HorizonSeconds));
            TrackValidation.Finite(v.ReactionSeconds,0.01,2,nameof(ReactionSeconds));
            TrackValidation.Finite(v.TrackingError,0,0.005,nameof(TrackingError));
            AthleteState.ValidateEffort(v.PaceControlError); return v;
        }
        public TacticalCadenceDefinition Cadence()=>TacticalCadenceDefinition.ForDifficulty(this);
        public PaceControllerDefinition Apply(PaceControllerDefinition baseline)
        { if(baseline==null) throw new ArgumentNullException(nameof(baseline)); return baseline.WithDifficulty(this); }
        public ShootingAiDefinition Apply(ShootingAiDefinition baseline)
        { if(baseline==null) throw new ArgumentNullException(nameof(baseline)); return baseline.WithDifficulty(this); }
        public string ToJson()=>RouteJson.Write(this);
    }
}

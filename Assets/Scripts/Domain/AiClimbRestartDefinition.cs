using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Observable AI restart decisions; no additional physical capability.</summary>
    [DataContract] public sealed class AiClimbRestartDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="minimum_grade",IsRequired=true)] public double MinimumGrade {get;private set;}
        [DataMember(Name="trigger_below_speed_mps",IsRequired=true)] public double TriggerBelowSpeed {get;private set;}
        [DataMember(Name="release_above_speed_mps",IsRequired=true)] public double ReleaseAboveSpeed {get;private set;}
        [DataMember(Name="stalled_seconds",IsRequired=true)] public double StalledSeconds {get;private set;}
        [DataMember(Name="restart_effort",IsRequired=true)] public int RestartEffort {get;private set;}
        [DataMember(Name="recovery_effort",IsRequired=true)] public int RecoveryEffort {get;private set;}
        [DataMember(Name="recover_below_reserve",IsRequired=true)] public double RecoverBelowReserve {get;private set;}
        [DataMember(Name="restart_above_reserve",IsRequired=true)] public double RestartAboveReserve {get;private set;}
        internal void Validate()
        {
            if(SchemaVersion!=1)throw new SerializationException("Unsupported climb restart policy.");TrackValidation.Id(Id);
            TrackValidation.Finite(MinimumGrade,.001,.5,nameof(MinimumGrade));
            TrackValidation.Finite(TriggerBelowSpeed,.01,3,nameof(TriggerBelowSpeed));
            TrackValidation.Finite(ReleaseAboveSpeed,TriggerBelowSpeed,10,nameof(ReleaseAboveSpeed));
            TrackValidation.Finite(StalledSeconds,.05,30,nameof(StalledSeconds));
            AthleteState.ValidateEffort(RestartEffort);AthleteState.ValidateEffort(RecoveryEffort);
            if(RestartEffort<=RecoveryEffort)throw new SerializationException("Restart must request more effort than recovery.");
            TrackValidation.Finite(RecoverBelowReserve,0,1,nameof(RecoverBelowReserve));
            TrackValidation.Finite(RestartAboveReserve,RecoverBelowReserve,1,nameof(RestartAboveReserve));
            if(RestartAboveReserve==RecoverBelowReserve||ReleaseAboveSpeed==TriggerBelowSpeed)throw new SerializationException("Restart hysteresis must have distinct thresholds.");
        }
    }
}

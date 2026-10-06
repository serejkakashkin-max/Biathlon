using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Decision quality for v2 world-heading AI. Physical yaw/grip limits
    /// still come from the shared manual ski solver.</summary>
    [DataContract]
    public sealed class ManualAiSteeringDefinition
    {
        private ManualAiSteeringDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="lookahead_base_m",IsRequired=true)] public double LookaheadBaseMeters {get;private set;}
        [DataMember(Name="lookahead_speed_seconds",IsRequired=true)] public double LookaheadSpeedSeconds {get;private set;}
        [DataMember(Name="lookahead_max_m",IsRequired=true)] public double LookaheadMaximumMeters {get;private set;}
        [DataMember(Name="heading_response_seconds",IsRequired=true)] public double HeadingResponseSeconds {get;private set;}
        [DataMember(Name="yaw_rate_feedback",IsRequired=true)] public double YawRateFeedback {get;private set;}
        public static ManualAiSteeringDefinition FromJson(string json)
        {
            var value=RouteJson.Read<ManualAiSteeringDefinition>(json);
            if(value==null||value.SchemaVersion!=1)throw new SerializationException("Invalid manual AI steering schema.");
            TrackValidation.Id(value.Id);
            TrackValidation.Finite(value.LookaheadBaseMeters,.1,200,nameof(LookaheadBaseMeters));
            TrackValidation.Finite(value.LookaheadSpeedSeconds,0,10,nameof(LookaheadSpeedSeconds));
            TrackValidation.Finite(value.LookaheadMaximumMeters,value.LookaheadBaseMeters,200,nameof(LookaheadMaximumMeters));
            TrackValidation.Finite(value.HeadingResponseSeconds,.05,10,nameof(HeadingResponseSeconds));
            TrackValidation.Finite(value.YawRateFeedback,0,2,nameof(YawRateFeedback));
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

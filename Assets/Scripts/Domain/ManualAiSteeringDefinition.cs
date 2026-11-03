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
        [DataMember(Name="range_approach_minimum_lateral_m",EmitDefaultValue=false)] public double RangeApproachMinimumLateral {get;private set;}
        [DataMember(Name="range_transit_soft_contacts",EmitDefaultValue=false)] public bool RangeTransitSoftContacts {get;private set;}
        [DataMember(Name="private_departure_lookahead_m",EmitDefaultValue=false)] public double PrivateDepartureLookahead {get;private set;}
        [DataMember(Name="brake_for_steering_demand",EmitDefaultValue=false)] public bool BrakeForSteeringDemand {get;private set;}
        [DataMember(Name="recover_behind_penalty_target",EmitDefaultValue=false)] public bool RecoverBehindPenaltyTarget {get;private set;}
        [DataMember(Name="recover_low_speed_course_steering",EmitDefaultValue=false)] public bool RecoverLowSpeedCourseSteering {get;private set;}
        [DataMember(Name="bend_speed_safety_factor",EmitDefaultValue=false)] public double BendSpeedSafetyFactor {get;private set;}
        [DataMember(Name="climb_restart",EmitDefaultValue=false)] public AiClimbRestartDefinition ClimbRestart {get;private set;}
        [DataMember(Name="penalty_yaw_tracking_error_radps",EmitDefaultValue=false)] public double PenaltyYawTrackingError {get;private set;}
        [DataMember(Name="moving_overtake_minimum_advantage_mps",EmitDefaultValue=false)] public double MovingOvertakeMinimumAdvantage {get;private set;}
        [DataMember(Name="moving_overtake_minimum_reserve",EmitDefaultValue=false)] public double MovingOvertakeMinimumReserve {get;private set;}
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
            TrackValidation.Finite(value.RangeApproachMinimumLateral,0,3,nameof(RangeApproachMinimumLateral));
            TrackValidation.Finite(value.PrivateDepartureLookahead,0,200,nameof(PrivateDepartureLookahead));
            TrackValidation.Finite(value.BendSpeedSafetyFactor,0,1,nameof(BendSpeedSafetyFactor));
            if(value.BendSpeedSafetyFactor>0&&value.BendSpeedSafetyFactor<.5)throw new SerializationException("AI bend speed margin must be at least 0.5.");
            value.ClimbRestart?.Validate();
            TrackValidation.Finite(value.PenaltyYawTrackingError,0,10,nameof(PenaltyYawTrackingError));
            TrackValidation.Finite(value.MovingOvertakeMinimumAdvantage,0,5,nameof(MovingOvertakeMinimumAdvantage));
            TrackValidation.Finite(value.MovingOvertakeMinimumReserve,0,1,nameof(MovingOvertakeMinimumReserve));
            return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

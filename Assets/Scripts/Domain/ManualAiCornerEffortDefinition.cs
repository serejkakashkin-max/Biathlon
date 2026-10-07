using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>AI decision cap before an observed course bend. This does not
    /// change the athlete's power capability or the shared ski force solver.</summary>
    [DataContract]
    public sealed class ManualAiCornerEffortDefinition
    {
        private ManualAiCornerEffortDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="bend_lookahead_m",IsRequired=true)] public double BendLookaheadMeters {get;private set;}
        [DataMember(Name="bend_angle_threshold_rad",IsRequired=true)] public double BendAngleThresholdRadians {get;private set;}
        [DataMember(Name="bend_power_cap_w",IsRequired=true)] public double BendPowerCapWatts {get;private set;}

        public static ManualAiCornerEffortDefinition FromJson(string json)
        {
            var result=RouteJson.Read<ManualAiCornerEffortDefinition>(json);
            if(result==null||result.SchemaVersion!=1)
                throw new SerializationException("Unsupported manual AI corner effort schema.");
            TrackValidation.Id(result.Id);
            TrackValidation.Finite(result.BendLookaheadMeters,.1,200,nameof(BendLookaheadMeters));
            TrackValidation.Finite(result.BendAngleThresholdRadians,.001,Math.PI,nameof(BendAngleThresholdRadians));
            TrackValidation.Finite(result.BendPowerCapWatts,0,PhysiologyNumbers.MaximumMagnitude,nameof(BendPowerCapWatts));
            return result;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ManualAiTrafficDefinition
    {
        private ManualAiTrafficDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="obstruction_lookahead_m",IsRequired=true)] public double ObstructionLookaheadMeters {get;private set;}
        [DataMember(Name="pass_line_clearance_m",IsRequired=true)] public double PassLineClearanceMeters {get;private set;}
        [DataMember(Name="prefer_right_on_tie",IsRequired=true)] public bool PreferRightOnTie {get;private set;}
        public static ManualAiTrafficDefinition FromJson(string json)
        {
            var result=RouteJson.Read<ManualAiTrafficDefinition>(json);
            if(result==null||result.SchemaVersion!=1)
                throw new SerializationException("Unsupported manual AI traffic schema.");
            TrackValidation.Id(result.Id);
            TrackValidation.Finite(result.ObstructionLookaheadMeters,.1,200,nameof(ObstructionLookaheadMeters));
            TrackValidation.Finite(result.PassLineClearanceMeters,0,100,nameof(PassLineClearanceMeters));
            return result;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

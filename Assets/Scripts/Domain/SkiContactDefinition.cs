using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SkiContactDefinition
    {
        private SkiContactDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="restitution",IsRequired=true)] public double Restitution {get;private set;}
        [DataMember(Name="sliding_friction",IsRequired=true)] public double SlidingFriction {get;private set;}
        [DataMember(Name="contact_tolerance_m",IsRequired=true)] public double ContactTolerance {get;private set;}
        public static SkiContactDefinition FromJson(string json)
        {
            var d=RouteJson.Read<SkiContactDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid ski contact schema.");
            TrackValidation.Id(d.Id);
            TrackValidation.Finite(d.Restitution,0,1,nameof(Restitution));
            TrackValidation.Finite(d.SlidingFriction,0,1,nameof(SlidingFriction));
            TrackValidation.Finite(d.ContactTolerance,PhysiologyNumbers.MinimumPositive,.05,nameof(ContactTolerance));
            return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

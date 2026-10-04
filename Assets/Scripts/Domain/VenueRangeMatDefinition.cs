using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Optional physical mat footprint. The path anchor remains the accepted
    /// athlete stop; the firing datum is its explicit forward offset. No aim tuning.</summary>
    [DataContract]
    public sealed class VenueRangeMatDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="width_m",IsRequired=true)] public double Width { get; private set; }
        [DataMember(Name="length_m",IsRequired=true)] public double Length { get; private set; }
        [DataMember(Name="thickness_m",IsRequired=true)] public double Thickness { get; private set; }
        [DataMember(Name="athlete_stop_depth_m",IsRequired=true)] public double AthleteStopDepth { get; private set; }
        [DataMember(Name="prone_support",EmitDefaultValue=false)] public VenueProneSupportDefinition ProneSupport { get; private set; }
        public double CentreForwardFromStop=>AthleteStopDepth-Length*.5;

        internal void Validate()
        {
            if(SchemaVersion!=1)throw new SerializationException("Unsupported range mat schema.");
            TrackValidation.Id(Id);
            TrackValidation.Finite(Width,.1,10,"mat width");
            TrackValidation.Finite(Length,.1,10,"mat length");
            TrackValidation.Finite(Thickness,.001,.1,"mat thickness");
            TrackValidation.Finite(AthleteStopDepth,0,Length,"athlete stop depth");
            if(ProneSupport!=null)
            {
                ProneSupport.Validate();
                if(!ContainsSupports(0,0,ShootingStance.Prone))
                    throw new ArgumentException("The authored prone supports must fit at the paid mat stop.");
            }
        }
        public void Validate(OccupancyDefinition body)
        {
            if(body==null)throw new ArgumentNullException(nameof(body));
            Validate();
            if(Width<body.BodyWidth||Length<body.BodyLength||
                AthleteStopDepth<body.BodyLength*.5||AthleteStopDepth>Length-body.BodyLength*.5)
                throw new ArgumentException("The unchanged athlete body must fit at the authored mat stop.");
        }
        /// <summary>Body containment in the final shooting heading. Coordinates are measured
        /// from the accepted path stop, with forward along the explicit range direction.</summary>
        public bool ContainsBody(double lateralFromStop,double forwardFromStop,OccupancyDefinition body)
        {
            if(body==null)throw new ArgumentNullException(nameof(body));
            if(!double.IsFinite(lateralFromStop)||!double.IsFinite(forwardFromStop))
                throw new ArgumentException("Finite accepted body position required.");
            return Math.Abs(lateralFromStop)+body.BodyWidth*.5<=Width*.5&&
                Math.Abs(forwardFromStop-CentreForwardFromStop)+body.BodyLength*.5<=Length*.5;
        }
        /// <summary>Additional authored support footprint; it never substitutes for the body predicate.</summary>
        public bool ContainsBodyAndSupports(double lateralFromStop,double forwardFromStop,OccupancyDefinition body,ShootingStance stance)
            =>ContainsBody(lateralFromStop,forwardFromStop,body)&&ContainsSupports(lateralFromStop,forwardFromStop,stance);
        public bool ContainsSupports(double lateralFromStop,double forwardFromStop,ShootingStance stance)
        {
            if(stance!=ShootingStance.Prone&&stance!=ShootingStance.Standing)throw new ArgumentOutOfRangeException(nameof(stance));
            if(!double.IsFinite(lateralFromStop)||!double.IsFinite(forwardFromStop))throw new ArgumentException("Finite accepted support position required.");
            if(stance!=ShootingStance.Prone||ProneSupport==null)return true;
            var p=ProneSupport;double rear=CentreForwardFromStop-Length*.5,front=CentreForwardFromStop+Length*.5;
            return lateralFromStop+p.LateralMinimum-p.Clearance>=-Width*.5&&
                lateralFromStop+p.LateralMaximum+p.Clearance<=Width*.5&&
                forwardFromStop+p.ForwardMinimum-p.Clearance>=rear&&
                forwardFromStop+p.ForwardMaximum+p.Clearance<=front;
        }
        public static VenueRangeMatDefinition FromJson(string json)
        {
            var value=RouteJson.Read<VenueRangeMatDefinition>(json);
            if(value==null)throw new SerializationException("Range mat definition required.");
            value.Validate();return value;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
    /// <summary>Measured final prone torso/elbow/boot support extent in the final shooting heading.
    /// Gloves and rifle are excluded; the unchanged collision body is independently mandatory.</summary>
    [DataContract]
    public sealed class VenueProneSupportDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="lateral_min_m",IsRequired=true)] public double LateralMinimum { get; private set; }
        [DataMember(Name="lateral_max_m",IsRequired=true)] public double LateralMaximum { get; private set; }
        [DataMember(Name="forward_min_m",IsRequired=true)] public double ForwardMinimum { get; private set; }
        [DataMember(Name="forward_max_m",IsRequired=true)] public double ForwardMaximum { get; private set; }
        [DataMember(Name="clearance_m",IsRequired=true)] public double Clearance { get; private set; }
        internal void Validate()
        {
            if(SchemaVersion!=1)throw new SerializationException("Unsupported prone support schema.");
            TrackValidation.Id(Id);
            TrackValidation.Finite(LateralMinimum,-10,0,"prone lateral minimum");
            TrackValidation.Finite(LateralMaximum,0,10,"prone lateral maximum");
            TrackValidation.Finite(ForwardMinimum,-10,0,"prone forward minimum");
            TrackValidation.Finite(ForwardMaximum,0,10,"prone forward maximum");
            TrackValidation.Finite(Clearance,0,.1,"prone support clearance");
            if(LateralMinimum>=LateralMaximum||ForwardMinimum>=ForwardMaximum)
                throw new ArgumentException("Ordered nonempty prone support extents required.");
        }
    }

}

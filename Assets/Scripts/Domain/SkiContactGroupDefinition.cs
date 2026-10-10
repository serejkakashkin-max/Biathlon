using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class SkiContactGroupDefinition
    {
        private SkiContactGroupDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="maximum_passes",IsRequired=true)] public int MaximumPasses {get;private set;}
        [DataMember(Name="closing_speed_tolerance_mps",IsRequired=true)] public double ClosingSpeedTolerance {get;private set;}
        [DataMember(Name="impact_closing_speed_tolerance_mps",EmitDefaultValue=false)] private double? ImpactTolerance {get;set;}
        public double ImpactClosingSpeedTolerance=>ImpactTolerance??ClosingSpeedTolerance;
        [DataMember(Name="support_skin_fraction",EmitDefaultValue=false)] public double SupportSkinFraction {get;private set;}
        [DataMember(Name="support_recovery_speed_mps",EmitDefaultValue=false)] public double SupportRecoverySpeed {get;private set;}
        [DataMember(Name="minimum_force_piece_seconds",EmitDefaultValue=false)] public double MinimumForcePieceSeconds {get;private set;}
        public static SkiContactGroupDefinition FromJson(string json)
        {
            var d=RouteJson.Read<SkiContactGroupDefinition>(json);
            if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid ski contact group schema.");
            TrackValidation.Id(d.Id);
            if(d.MaximumPasses<1||d.MaximumPasses>4096)throw new SerializationException("Invalid contact group pass limit.");
            TrackValidation.Finite(d.ClosingSpeedTolerance,1e-9,.01,nameof(ClosingSpeedTolerance));
            TrackValidation.Finite(d.ImpactClosingSpeedTolerance,1e-9,d.ClosingSpeedTolerance,nameof(ImpactClosingSpeedTolerance));
            TrackValidation.Finite(d.SupportSkinFraction,0,.5,nameof(SupportSkinFraction));
            TrackValidation.Finite(d.SupportRecoverySpeed,0,.01,nameof(SupportRecoverySpeed));
            TrackValidation.Finite(d.MinimumForcePieceSeconds,0,.001,nameof(MinimumForcePieceSeconds));
            return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

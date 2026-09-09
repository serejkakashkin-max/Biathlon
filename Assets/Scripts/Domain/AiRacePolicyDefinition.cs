using System.Runtime.Serialization;
namespace Biathlon.Domain
{
    [DataContract] public sealed class AiRacePolicyDefinition
    {
        private AiRacePolicyDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="physical_radius_m",IsRequired=true)] public double PhysicalRadius { get; private set; }
        [DataMember(Name="preview_stride_m",IsRequired=true)] public double PreviewStride { get; private set; }
        [DataMember(Name="preview_samples",IsRequired=true)] public int PreviewSamples { get; private set; }
        [DataMember(Name="attack_opportunity",IsRequired=true)] public double AttackOpportunity { get; private set; }
        [DataMember(Name="attack_minimum_reserve",IsRequired=true)] public double AttackMinimumReserve { get; private set; }
        [DataMember(Name="final_sprint_distance_m",IsRequired=true)] public double FinalSprintDistance { get; private set; }
        [DataMember(Name="shooting_time_urgency",IsRequired=true)] public double ShootingTimeUrgency { get; private set; }
        public static AiRacePolicyDefinition FromJson(string json)
        {
            var d=RouteJson.Read<AiRacePolicyDefinition>(json);
            if(d==null || d.SchemaVersion!=1) throw new SerializationException("Unsupported AI race policy.");
            TrackValidation.Id(d.Id); TrackValidation.Finite(d.PhysicalRadius,0.001,1000,nameof(PhysicalRadius));
            TrackValidation.Finite(d.PreviewStride,0.001,1000,nameof(PreviewStride));
            if(d.PreviewSamples<2 || d.PreviewSamples>128) throw new SerializationException("Bounded route preview required.");
            TrackValidation.Finite(d.AttackOpportunity,0,1,nameof(AttackOpportunity)); TrackValidation.Finite(d.AttackMinimumReserve,0,1,nameof(AttackMinimumReserve));
            TrackValidation.Finite(d.FinalSprintDistance,0.001,10000,nameof(FinalSprintDistance)); TrackValidation.Finite(d.ShootingTimeUrgency,0,1,nameof(ShootingTimeUrgency)); return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

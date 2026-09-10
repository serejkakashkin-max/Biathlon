using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class PursuitTacticsDefinition
    {
        private PursuitTacticsDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="full_chase_gap_s",IsRequired=true)] public double FullChaseGap { get; private set; }
        [DataMember(Name="defense_radius_m",IsRequired=true)] public double DefenseRadius { get; private set; }
        [DataMember(Name="chase_preference",IsRequired=true)] public double ChasePreference { get; private set; }
        [DataMember(Name="defense_preference",IsRequired=true)] public double DefensePreference { get; private set; }
        [DataMember(Name="group_preference",IsRequired=true)] public double GroupPreference { get; private set; }
        [DataMember(Name="chase_urgency",IsRequired=true)] public double ChaseUrgency { get; private set; }
        [DataMember(Name="last_range_urgency",IsRequired=true)] public double LastRangeUrgency { get; private set; }
        [DataMember(Name="leader_caution",IsRequired=true)] public double LeaderCaution { get; private set; }
        public static PursuitTacticsDefinition FromJson(string json)
        {
            var d=RouteJson.Read<PursuitTacticsDefinition>(json);
            if(d==null || d.SchemaVersion!=1) throw new SerializationException("Unsupported Pursuit tactics schema.");
            TrackValidation.Id(d.Id); TrackValidation.Finite(d.FullChaseGap,0.001,3600,nameof(FullChaseGap));
            TrackValidation.Finite(d.DefenseRadius,0.001,1000,nameof(DefenseRadius));
            TrackValidation.Finite(d.ChasePreference,0,1,nameof(ChasePreference)); TrackValidation.Finite(d.DefensePreference,0,1,nameof(DefensePreference));
            TrackValidation.Finite(d.GroupPreference,0,1,nameof(GroupPreference)); TrackValidation.Finite(d.ChaseUrgency,0,1,nameof(ChaseUrgency));
            TrackValidation.Finite(d.LastRangeUrgency,0,1,nameof(LastRangeUrgency)); TrackValidation.Finite(d.LeaderCaution,0,1,nameof(LeaderCaution));
            return d;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

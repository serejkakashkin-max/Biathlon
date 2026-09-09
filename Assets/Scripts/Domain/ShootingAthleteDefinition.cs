using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingAthleteDefinition
    {
        private ShootingAthleteDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "breath_control", IsRequired = true)] public int BreathControl { get; private set; }
        [DataMember(Name = "prone_stability", IsRequired = true)] public int ProneStability { get; private set; }
        [DataMember(Name = "standing_stability", IsRequired = true)] public int StandingStability { get; private set; }
        [DataMember(Name = "shooting_speed", IsRequired = true)] public int ShootingSpeed { get; private set; }
        [DataMember(Name = "composure", IsRequired = true)] public int Composure { get; private set; }
        public static ShootingAthleteDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingAthleteDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported shooting athlete schema.");
            TrackValidation.Id(v.Id);
            foreach (int stat in new[] { v.BreathControl, v.ProneStability, v.StandingStability, v.ShootingSpeed, v.Composure })
                if (stat < 0 || stat > 100) throw new SerializationException("Shooting stats must be 0..100.");
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

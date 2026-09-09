using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class ShootingDefinition
    {
        private ShootingDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "rounds", IsRequired = true)] public int Rounds { get; private set; }
        [DataMember(Name = "setup_seconds", IsRequired = true)] public double SetupSeconds { get; private set; }
        [DataMember(Name = "bolt_seconds", IsRequired = true)] public double BoltSeconds { get; private set; }
        [DataMember(Name = "exit_seconds", IsRequired = true)] public double ExitSeconds { get; private set; }
        public static ShootingDefinition FromJson(string json)
        {
            var v = RouteJson.Read<ShootingDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported shooting lifecycle schema.");
            TrackValidation.Id(v.Id);
            if (v.Rounds < 1 || v.Rounds > 100) throw new SerializationException("Invalid bout ammunition count.");
            TrackValidation.Finite(v.SetupSeconds, 0.001, 60, nameof(SetupSeconds));
            TrackValidation.Finite(v.BoltSeconds, 0.001, 60, nameof(BoltSeconds));
            TrackValidation.Finite(v.ExitSeconds, 0.001, 60, nameof(ExitSeconds));
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
    public readonly struct ShootingLaneAssignment
    {
        public string RangeId { get; }
        public string LaneId { get; }
        public int Ordinal { get; }
        public bool IsValid => RangeId != null && LaneId != null && Ordinal > 0;
        public ShootingLaneAssignment(string rangeId, string laneId, int ordinal)
        {
            TrackValidation.Id(rangeId); TrackValidation.Id(laneId);
            if (ordinal <= 0) throw new System.ArgumentOutOfRangeException(nameof(ordinal));
            RangeId = rangeId; LaneId = laneId; Ordinal = ordinal;
        }
    }
}

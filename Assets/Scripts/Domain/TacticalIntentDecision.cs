using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    public enum TacticalIntentKind
    { Recover = 0, Hold = 1, Follow = 2, Draft = 3, Overtake = 4, Attack = 5, FinalSprint = 6, PreRangeRecovery = 7 }

    /// <summary>Decision valid on [DecidedAt, ExpiresAt). It requests a tactic, never a physical bonus.
    /// Id identifies the decision stream; (Id, AthleteId, DecidedAt) identifies a recorded decision.</summary>
    public readonly struct TacticalIntentDecision
    {
        public string Id { get; }
        public AthleteId AthleteId { get; }
        public TacticalIntentKind Kind { get; }
        public AthleteId TargetId { get; }
        public SimulationTick DecidedAt { get; }
        public SimulationTick ExpiresAt { get; }
        public bool IsValid => !string.IsNullOrEmpty(Id) && AthleteId.IsValid && ExpiresAt.Value > DecidedAt.Value;
        public TacticalIntentDecision(string id, AthleteId athlete, TacticalIntentKind kind, SimulationTick decidedAt,
            SimulationTick expiresAt, AthleteId target = default)
        {
            TrackValidation.Id(id);
            if (!athlete.IsValid || kind < TacticalIntentKind.Recover || kind > TacticalIntentKind.PreRangeRecovery ||
                expiresAt.Value <= decidedAt.Value) throw new ArgumentException("Valid actor, intent and positive lifetime required.");
            bool targeted = RequiresTarget(kind);
            if (targeted != target.IsValid || (targeted && target.Equals(athlete)))
                throw new ArgumentException("Follow/Draft/Overtake require another athlete; other intents are untargeted.");
            Id = id; AthleteId = athlete; Kind = kind; TargetId = target; DecidedAt = decidedAt; ExpiresAt = expiresAt;
        }
        public static bool RequiresTarget(TacticalIntentKind kind) =>
            kind == TacticalIntentKind.Follow || kind == TacticalIntentKind.Draft || kind == TacticalIntentKind.Overtake;
        public bool IsCurrentAt(SimulationTick tick) => IsValid && tick.Value >= DecidedAt.Value && tick.Value < ExpiresAt.Value;
        public void ValidateFor(AthleteId athlete, SimulationTick tick)
        {
            if (!AthleteId.Equals(athlete) || !IsCurrentAt(tick)) throw new ArgumentException("Intent is missing, stale or belongs to another athlete.");
        }
        public string ToJson()
        {
            if (!IsValid) throw new InvalidOperationException("Cannot serialize a default intent.");
            return RouteJson.Write(new Payload { Schema = 1, Id = Id, Athlete = AthleteId.Value, Intent = (int)Kind,
                Target = TargetId.IsValid ? TargetId.Value : null, Tick = DecidedAt.Value, Until = ExpiresAt.Value });
        }
        public static TacticalIntentDecision FromJson(string json)
        {
            var data = RouteJson.Read<Payload>(json);
            if (data == null || data.Schema != 1) throw new SerializationException("Unsupported intent schema.");
            return new TacticalIntentDecision(data.Id, new AthleteId(data.Athlete), (TacticalIntentKind)data.Intent,
                new SimulationTick(data.Tick), new SimulationTick(data.Until), data.Target == null ? default : new AthleteId(data.Target));
        }
        [DataContract] private sealed class Payload
        {
            [DataMember(Name = "schema_version", IsRequired = true)] public int Schema { get; set; }
            [DataMember(Name = "id", IsRequired = true)] public string Id { get; set; }
            [DataMember(Name = "athlete_id", IsRequired = true)] public string Athlete { get; set; }
            [DataMember(Name = "intent", IsRequired = true)] public int Intent { get; set; }
            [DataMember(Name = "target_id", IsRequired = true)] public string Target { get; set; }
            [DataMember(Name = "decided_tick", IsRequired = true)] public long Tick { get; set; }
            [DataMember(Name = "expires_tick", IsRequired = true)] public long Until { get; set; }
        }
    }
}

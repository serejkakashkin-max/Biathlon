using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    public enum TacticalUtilityFeature
    { RecoveryNeed = 0, Reserve = 1, Fatigue = 2, RangeProximity = 3, DraftOpportunity = 4, OvertakeOpportunity = 5, AttackOpportunity = 6, FinalSprintOpportunity = 7 }
    [DataContract] public sealed class TacticalUtilityRow
    {
        private TacticalUtilityRow() { }
        [DataMember(Name = "intent", IsRequired = true)] public TacticalIntentKind Intent { get; private set; }
        [DataMember(Name = "bias", IsRequired = true)] public double Bias { get; private set; }
        [DataMember(Name = "feature_weights", IsRequired = true)] private double[] Weights { get; set; }
        [DataMember(Name = "personality_weight", IsRequired = true)] public double PersonalityWeight { get; private set; }
        public double Weight(TacticalUtilityFeature feature)
        {
            int i = (int)feature; if (i < 0 || i >= 8) throw new ArgumentOutOfRangeException(nameof(feature)); return Weights[i];
        }
        internal void Validate(int index)
        {
            if ((int)Intent != index || Weights == null || Weights.Length != 8)
                throw new ArgumentException("Eight ordered intents and eight feature weights required.");
            TrackValidation.Finite(Bias, -100, 100, nameof(Bias));
            TrackValidation.Finite(PersonalityWeight, -100, 100, nameof(PersonalityWeight));
            for (int i = 0; i < Weights.Length; i++) TrackValidation.Finite(Weights[i], -100, 100, nameof(Weights));
        }
    }
    [DataContract] public sealed class TacticalUtilityDefinition
    {
        private TacticalUtilityDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "intents", IsRequired = true)] private TacticalUtilityRow[] Rows { get; set; }
        public TacticalUtilityRow Get(TacticalIntentKind kind)
        { int i = (int)kind; if (i < 0 || i >= 8) throw new ArgumentOutOfRangeException(nameof(kind)); return Rows[i]; }
        public static TacticalUtilityDefinition FromJson(string json)
        {
            var v = RouteJson.Read<TacticalUtilityDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported tactical utility schema.");
            TrackValidation.Id(v.Id);
            if (v.Rows == null || v.Rows.Length != 8) throw new ArgumentException("Complete tactical utility rows required.");
            for (int i = 0; i < v.Rows.Length; i++)
            { if (v.Rows[i] == null) throw new ArgumentException("Null utility row."); v.Rows[i].Validate(i); }
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}

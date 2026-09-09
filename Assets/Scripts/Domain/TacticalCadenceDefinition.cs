using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class TacticalCadenceDefinition
    {
        private TacticalCadenceDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "decision_hz", IsRequired = true)] public int DecisionHz { get; private set; }
        [DataMember(Name = "planning_horizon_seconds", IsRequired = true)] public double PlanningHorizonSeconds { get; private set; }
        public static TacticalCadenceDefinition FromJson(string json)
        {
            var v = RouteJson.Read<TacticalCadenceDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported tactical cadence schema.");
            TrackValidation.Id(v.Id);
            if (v.DecisionHz < 2 || v.DecisionHz > 5) throw new ArgumentException("Tactical cadence must be 2–5 Hz.");
            TrackValidation.Finite(v.PlanningHorizonSeconds, 0.01, 120, nameof(PlanningHorizonSeconds)); return v;
        }
        public string ToJson() => RouteJson.Write(this);
        internal static TacticalCadenceDefinition ForDifficulty(AiDifficultyDefinition d)=>new TacticalCadenceDefinition
        { SchemaVersion=1,Id=d.Id+"_cadence",DecisionHz=d.DecisionHz,PlanningHorizonSeconds=d.HorizonSeconds };
    }
}

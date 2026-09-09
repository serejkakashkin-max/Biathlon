using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class PaceControllerDefinition
    {
        private PaceControllerDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "intent_effort", IsRequired = true)] private int[] Efforts { get; set; }
        [DataMember(Name = "response_effort_per_second", IsRequired = true)] public double ResponsePerSecond { get; private set; }
        [DataMember(Name = "control_error_effort", IsRequired = true)] public int ControlError { get; private set; }
        public int Effort(TacticalIntentKind kind)
        { int i = (int)kind; if (i < 0 || i >= 8) throw new ArgumentOutOfRangeException(nameof(kind)); return Efforts[i]; }
        public static PaceControllerDefinition FromJson(string json)
        {
            var v = RouteJson.Read<PaceControllerDefinition>(json);
            if (v == null || v.SchemaVersion != 1) throw new SerializationException("Unsupported pace controller schema.");
            TrackValidation.Id(v.Id);
            if (v.Efforts == null || v.Efforts.Length != 8) throw new ArgumentException("Eight effort targets required.");
            for (int i = 0; i < 8; i++) AthleteState.ValidateEffort(v.Efforts[i]);
            TrackValidation.Finite(v.ResponsePerSecond, 0.01, 10000, nameof(ResponsePerSecond));
            if (v.ControlError < 0 || v.ControlError > 1000) throw new ArgumentException("Invalid effort error bound.");
            return v;
        }
        public string ToJson() => RouteJson.Write(this);
        internal PaceControllerDefinition WithDifficulty(AiDifficultyDefinition d)=>new PaceControllerDefinition
        { SchemaVersion=SchemaVersion,Id=d.Id+"_pace",Efforts=(int[])Efforts.Clone(),ResponsePerSecond=ResponsePerSecond,ControlError=d.PaceControlError };
    }
}

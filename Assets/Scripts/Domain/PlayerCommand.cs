using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    public enum EffortPreset { Analog = 0, Recovery = 1, Race = 2, Attack = 3, FinalSprint = 4 }

    /// <summary>Authoritative intent for one target simulation tick; no presentation/frame-time fields.</summary>
    public readonly struct PlayerCommand
    {
        public const int CurrentSchemaVersion = 1;
        public const string SchemaId = "biathlon_player_command_v1";
        public const int LineIntentUnits = 1000;
        public SimulationTick Tick { get; }
        public AthleteId AthleteId { get; }
        public int AnalogEffort { get; }
        public int LineIntent { get; }
        public EffortPreset Preset { get; }
        public bool TuckHeld { get; }
        public bool OvertakeRequested { get; }
        public bool CancelOvertake { get; }
        public string TechniqueId { get; }
        public bool IsValid => Tick.Value > 0 && AthleteId.IsValid;
        public PlayerCommand(SimulationTick tick, AthleteId athleteId, int analogEffort, int lineIntent,
            EffortPreset preset = EffortPreset.Analog, bool tuckHeld = false, bool overtakeRequested = false,
            bool cancelOvertake = false, string techniqueId = null)
        {
            if (tick.Value <= 0 || !athleteId.IsValid) throw new ArgumentException("Command requires an athlete and positive target tick.");
            AthleteState.ValidateEffort(analogEffort);
            if (lineIntent < -LineIntentUnits || lineIntent > LineIntentUnits) throw new ArgumentOutOfRangeException(nameof(lineIntent));
            if (preset < EffortPreset.Analog || preset > EffortPreset.FinalSprint) throw new ArgumentOutOfRangeException(nameof(preset));
            if (overtakeRequested && cancelOvertake) throw new ArgumentException("A command cannot request and cancel overtaking together.");
            if (techniqueId != null && string.IsNullOrWhiteSpace(techniqueId)) throw new ArgumentException("Technique ID must be null (automatic) or nonempty.");
            Tick = tick; AthleteId = athleteId; AnalogEffort = analogEffort; LineIntent = lineIntent; Preset = preset;
            TuckHeld = tuckHeld; OvertakeRequested = overtakeRequested; CancelOvertake = cancelOvertake; TechniqueId = techniqueId;
        }
        public string ToJson()
        {
            if (!IsValid) throw new InvalidOperationException("Cannot serialize a default command.");
            return RouteJson.Write(new CommandJson {
                SchemaVersion = CurrentSchemaVersion, Id = SchemaId, TargetTick = Tick.Value, AthleteId = AthleteId.Value,
                AnalogEffort = AnalogEffort, LineIntent = LineIntent, Preset = (int)Preset,
                TuckHeld = TuckHeld, OvertakeRequested = OvertakeRequested, CancelOvertake = CancelOvertake, TechniqueId = TechniqueId
            });
        }
        public static PlayerCommand FromJson(string json)
        {
            var value = RouteJson.Read<CommandJson>(json);
            if (value == null || value.SchemaVersion != CurrentSchemaVersion || value.Id != SchemaId)
                throw new SerializationException("Unsupported player command schema identity/version.");
            return new PlayerCommand(new SimulationTick(value.TargetTick), new AthleteId(value.AthleteId), value.AnalogEffort,
                value.LineIntent, (EffortPreset)value.Preset, value.TuckHeld, value.OvertakeRequested, value.CancelOvertake, value.TechniqueId);
        }
        [DataContract]
        private sealed class CommandJson
        {
            [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion;
            [DataMember(Name = "id", IsRequired = true)] public string Id;
            [DataMember(Name = "target_tick", IsRequired = true)] public long TargetTick;
            [DataMember(Name = "athlete_id", IsRequired = true)] public string AthleteId;
            [DataMember(Name = "analog_effort", IsRequired = true)] public int AnalogEffort;
            [DataMember(Name = "line_intent", IsRequired = true)] public int LineIntent;
            [DataMember(Name = "effort_preset", IsRequired = true)] public int Preset;
            [DataMember(Name = "tuck_held", IsRequired = true)] public bool TuckHeld;
            [DataMember(Name = "overtake_requested", IsRequired = true)] public bool OvertakeRequested;
            [DataMember(Name = "cancel_overtake", IsRequired = true)] public bool CancelOvertake;
            [DataMember(Name = "technique_id", IsRequired = true)] public string TechniqueId;
        }
    }
}

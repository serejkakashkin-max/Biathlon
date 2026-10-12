using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Explicit v2 player intent. Steering requests signed yaw, never a lateral target.</summary>
    public readonly struct ManualSkiCommand
    {
        public const int CurrentSchemaVersion = 2;
        public const string SchemaId = "biathlon_player_command_v2";
        public const int SteeringUnits = 1000;
        private readonly PlayerCommand effort;
        public SimulationTick Tick => effort.Tick;
        public AthleteId AthleteId => effort.AthleteId;
        public int AnalogEffort => effort.AnalogEffort;
        public int SteeringInput { get; }
        public double BrakeFraction { get; }
        public EffortPreset Preset => effort.Preset;
        public bool TuckHeld => effort.TuckHeld;
        public bool OvertakeRequested => effort.OvertakeRequested;
        public bool CancelOvertake => effort.CancelOvertake;
        public string TechniqueId => effort.TechniqueId;
        public bool IsValid => effort.IsValid;

        public ManualSkiCommand(SimulationTick tick, AthleteId athleteId, int analogEffort, int steeringInput,
            EffortPreset preset = EffortPreset.Analog, bool tuckHeld = false, bool overtakeRequested = false,
            bool cancelOvertake = false, string techniqueId = null, double brakeFraction = 0)
        {
            if (steeringInput < -SteeringUnits || steeringInput > SteeringUnits)
                throw new ArgumentOutOfRangeException(nameof(steeringInput));
            // Reuse unchanged effort/action validation. This value is not sent to legacy steering.
            effort = new PlayerCommand(tick, athleteId, analogEffort, 0, preset, tuckHeld,
                overtakeRequested, cancelOvertake, techniqueId);
            SteeringInput = steeringInput;
            BrakeFraction = PhysiologyNumbers.RequireRange(brakeFraction,0,1,nameof(brakeFraction));
        }

        /// <summary>For the existing effort evaluator only; its zero line is not a steering command.</summary>
        public PlayerCommand ToEffortCommand()
        {
            if (!IsValid) throw new InvalidOperationException("Cannot adapt a default manual command.");
            return effort;
        }

        public string ToJson()
        {
            if (!IsValid) throw new InvalidOperationException("Cannot serialize a default manual command.");
            return RouteJson.Write(new CommandJson {
                SchemaVersion = CurrentSchemaVersion, Id = SchemaId, TargetTick = Tick.Value,
                AthleteId = AthleteId.Value, AnalogEffort = AnalogEffort, SteeringInput = SteeringInput,
                Preset = (int)Preset, TuckHeld = TuckHeld, OvertakeRequested = OvertakeRequested,
                CancelOvertake = CancelOvertake, TechniqueId = TechniqueId, BrakeFraction=BrakeFraction
            });
        }

        public static ManualSkiCommand FromJson(string json)
        {
            var value = RouteJson.Read<CommandJson>(json);
            if (value == null || value.SchemaVersion != CurrentSchemaVersion || value.Id != SchemaId)
                throw new SerializationException("Unsupported manual player command schema identity/version.");
            return new ManualSkiCommand(new SimulationTick(value.TargetTick), new AthleteId(value.AthleteId),
                value.AnalogEffort, value.SteeringInput, (EffortPreset)value.Preset, value.TuckHeld,
                value.OvertakeRequested, value.CancelOvertake, value.TechniqueId,value.BrakeFraction);
        }

        [DataContract]
        private sealed class CommandJson
        {
            [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion;
            [DataMember(Name = "id", IsRequired = true)] public string Id;
            [DataMember(Name = "target_tick", IsRequired = true)] public long TargetTick;
            [DataMember(Name = "athlete_id", IsRequired = true)] public string AthleteId;
            [DataMember(Name = "analog_effort", IsRequired = true)] public int AnalogEffort;
            [DataMember(Name = "steering_input", IsRequired = true)] public int SteeringInput;
            [DataMember(Name = "effort_preset", IsRequired = true)] public int Preset;
            [DataMember(Name = "tuck_held", IsRequired = true)] public bool TuckHeld;
            [DataMember(Name = "overtake_requested", IsRequired = true)] public bool OvertakeRequested;
            [DataMember(Name = "cancel_overtake", IsRequired = true)] public bool CancelOvertake;
            [DataMember(Name = "technique_id", IsRequired = true)] public string TechniqueId;
            [DataMember(Name = "brake_fraction", EmitDefaultValue=false)] public double BrakeFraction;
        }
    }
}

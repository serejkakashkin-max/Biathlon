using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    public sealed class ShootingCommandTape
    {
        private readonly ShootingSessionInput[] inputs;
        public string Id { get; } public string ContextJson { get; } public AthleteId AthleteId { get; }
        public int Count { get; private set; }
        public SimulationTick InitialTick { get; }
        public ShootingCommandTape(string id, string contextJson, AthleteId athleteId, int capacity, SimulationTick initialTick = default)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(contextJson) || !athleteId.IsValid || capacity <= 0)
                throw new ArgumentException("Invalid shooting recording identity/capacity.");
            Id = id; ContextJson = contextJson; AthleteId = athleteId; inputs = new ShootingSessionInput[capacity];
            InitialTick = initialTick;
        }
        public ShootingSessionInput Get(int index) => index >= 0 && index < Count ? inputs[index] : throw new ArgumentOutOfRangeException(nameof(index));
        private void ValidateSession(ShootingSession session)
        {
            if (session == null || !session.AthleteId.Equals(AthleteId) || session.InitialTick.Value != InitialTick.Value || !string.Equals(session.ContextJson, ContextJson, StringComparison.Ordinal))
                throw new ArgumentException("Shooting recording context differs from session.");
        }
        private void ValidateAppend(ShootingSessionInput input)
        {
            if (Count >= inputs.Length || !input.Aim.AthleteId.Equals(AthleteId) || input.Aim.Tick.Value != checked(InitialTick.Value + Count + 1L))
                throw new ArgumentException("Shooting recording requires capacity and consecutive matching commands.");
        }
        public ShootingSessionFrame StepAndRecord(ShootingSession session, ShootingSessionInput input)
        {
            ValidateSession(session); ValidateAppend(input);
            if (session.Tick.Value != checked(InitialTick.Value + Count)) throw new ArgumentException("Recording cursor differs from session.");
            var frame = session.Step(input); inputs[Count++] = input; return frame;
        }
        public ShootingSessionFrame ReplayNext(ShootingSession session)
        {
            ValidateSession(session);
            long cursor = session.Tick.Value - InitialTick.Value;
            if (cursor < 0 || cursor >= Count) throw new InvalidOperationException("No recorded shooting command remains.");
            return session.Step(inputs[(int)cursor]);
        }
        public string ToJson()
        {
            var rows = new InputRow[Count]; for (int i = 0; i < Count; i++) rows[i] = InputRow.From(inputs[i]);
            var dto = new TapeJson { SchemaVersion = InitialTick.Value == 0 ? 1 : 2, InitialTick = InitialTick.Value == 0 ? (long?)null : InitialTick.Value,
                Id = Id, Context = ContextJson, Athlete = AthleteId.Value, Inputs = rows };
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(TapeJson)).WriteObject(stream, dto); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        public static ShootingCommandTape FromJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            TapeJson dto;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json))) dto = (TapeJson)new DataContractJsonSerializer(typeof(TapeJson)).ReadObject(stream);
            if (dto == null || (dto.SchemaVersion != 1 && dto.SchemaVersion != 2) || dto.Inputs == null ||
                (dto.SchemaVersion == 1 && dto.InitialTick.HasValue) || (dto.SchemaVersion == 2 && (!dto.InitialTick.HasValue || dto.InitialTick.Value <= 0)))
                throw new SerializationException("Unsupported shooting recording schema/origin.");
            var tape = new ShootingCommandTape(dto.Id, dto.Context, new AthleteId(dto.Athlete), Math.Max(1, dto.Inputs.Length), new SimulationTick(dto.InitialTick ?? 0));
            foreach (var row in dto.Inputs)
            {
                if (row == null) throw new SerializationException("Null shooting command.");
                var input = row.ToInput(tape.AthleteId); tape.ValidateAppend(input); tape.inputs[tape.Count++] = input;
            }
            return tape;
        }
        [DataContract] private sealed class TapeJson
        {
            [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion;
            [DataMember(Name = "initial_tick", EmitDefaultValue = false)] public long? InitialTick;
            [DataMember(Name = "id", IsRequired = true)] public string Id;
            [DataMember(Name = "context_json", IsRequired = true)] public string Context;
            [DataMember(Name = "athlete_id", IsRequired = true)] public string Athlete;
            [DataMember(Name = "inputs", IsRequired = true)] public InputRow[] Inputs;
        }
        [DataContract] private sealed class InputRow
        {
            [DataMember(Name = "tick", IsRequired = true)] public long Tick;
            [DataMember(Name = "x", IsRequired = true)] public int X;
            [DataMember(Name = "y", IsRequired = true)] public int Y;
            [DataMember(Name = "fine", IsRequired = true)] public bool Fine;
            [DataMember(Name = "action", IsRequired = true)] public int Action;
            [DataMember(Name = "range_id", IsRequired = true)] public string Range;
            [DataMember(Name = "lane_id", IsRequired = true)] public string Lane;
            [DataMember(Name = "lane_ordinal", IsRequired = true)] public int LaneOrdinal;
            [DataMember(Name = "hold", IsRequired = true)] public bool Hold;
            [DataMember(Name = "hr_bpm", IsRequired = true)] public double Hr;
            [DataMember(Name = "fatigue", IsRequired = true)] public double Fatigue;
            [DataMember(Name = "arm_fatigue", IsRequired = true)] public double Arm;
            [DataMember(Name = "wind_east_mps", IsRequired = true)] public double East;
            [DataMember(Name = "wind_north_mps", IsRequired = true)] public double North;
            [DataMember(Name = "heading_rad", IsRequired = true)] public double Heading;
            [DataMember(Name = "exposure", IsRequired = true)] public double Exposure;
            [DataMember(Name = "race_importance", IsRequired = true)] public double Race;
            [DataMember(Name = "position_importance", IsRequired = true)] public double Position;
            [DataMember(Name = "rival_proximity", IsRequired = true)] public double Rivals;
            [DataMember(Name = "confidence", IsRequired = true)] public double Confidence;
            [DataMember(Name = "final_standing", IsRequired = true)] public bool Final;
            [DataMember(Name = "first_podium", IsRequired = true)] public bool Podium;
            public static InputRow From(ShootingSessionInput v) => new InputRow {
                Tick = v.Aim.Tick.Value, X = v.Aim.Horizontal, Y = v.Aim.Vertical, Fine = v.Aim.FineControl, Action = (int)v.Action,
                Range = v.Lane.RangeId, Lane = v.Lane.LaneId, LaneOrdinal = v.Lane.Ordinal, Hold = v.HoldBreath,
                Hr = v.HeartRateBpm, Fatigue = v.Fatigue, Arm = v.ArmFatigue, East = v.Wind.EastMetersPerSecond, North = v.Wind.NorthMetersPerSecond,
                Heading = v.HeadingRadians, Exposure = v.Exposure, Race = v.Pressure.RaceImportance, Position = v.Pressure.PositionImportance,
                Rivals = v.Pressure.RivalProximity, Confidence = v.Pressure.Confidence, Final = v.Pressure.FinalStanding, Podium = v.Pressure.FirstPodium };
            public ShootingSessionInput ToInput(AthleteId athlete)
            {
                if (Action < 0 || Action > (int)ShootingAction.Trigger) throw new SerializationException("Unknown shooting action.");
                PhysiologyNumbers.RequireRange(Hr, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(Hr));
                PhysiologyNumbers.RequireRange(Fatigue, 0, 1, nameof(Fatigue)); PhysiologyNumbers.RequireRange(Arm, 0, 1, nameof(Arm));
                PhysiologyNumbers.RequireRange(Exposure, 0, 1, nameof(Exposure)); PhysiologyNumbers.RequireRange(Heading, -2 * Math.PI, 2 * Math.PI, nameof(Heading));
                bool hasLane = Range != null || Lane != null || LaneOrdinal != 0;
                var assignment = hasLane ? new ShootingLaneAssignment(Range, Lane, LaneOrdinal) : default;
                if (((ShootingAction)Action == ShootingAction.AssignLane) != hasLane) throw new SerializationException("Lane/action mismatch.");
                return new ShootingSessionInput(new WeaponAimCommand(new SimulationTick(Tick), athlete, X, Y, Fine), (ShootingAction)Action, assignment,
                    Hold, Hr, Fatigue, Arm, new WindVector(East, North), Heading, Exposure, new ShootingPressureContext(Race, Position, Rivals, Confidence, Final, Podium));
            }
        }
    }
}

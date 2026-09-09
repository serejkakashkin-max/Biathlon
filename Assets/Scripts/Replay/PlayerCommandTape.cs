using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    public sealed class PlayerCommandTape
    {
        private readonly PlayerCommand[] commands;
        public string Id { get; }
        public string ContextJson { get; }
        public AthleteId AthleteId { get; }
        public int Count { get; private set; }
        public int Capacity => commands.Length;
        public PlayerCommandTape(string id, string contextJson, AthleteId athleteId, int capacity)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(contextJson) || !athleteId.IsValid || capacity <= 0)
                throw new ArgumentException("Valid tape ID/context/athlete/capacity required.");
            Id = id; ContextJson = contextJson; AthleteId = athleteId; commands = new PlayerCommand[capacity];
        }
        public PlayerCommand GetCommand(int index)
        {
            if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
            return commands[index];
        }
        public void ValidateAppend(PlayerCommand command)
        {
            if (Count == Capacity) throw new InvalidOperationException("Command tape capacity exhausted.");
            if (!command.IsValid || !command.AthleteId.Equals(AthleteId) || command.Tick.Value != (long)Count + 1)
                throw new ArgumentException("Tape commands must be consecutive from tick one for the recorded athlete.");
        }
        public void Append(PlayerCommand command) { ValidateAppend(command); commands[Count++] = command; }
        public PlayerTimeTrialFrame StepAndRecord(PlayerTimeTrialSession session, PlayerCommand command)
        {
            ValidateSession(session); ValidateAppend(command);
            if (session.Clock.Tick.Value != Count) throw new ArgumentException("Recorder and session tick differ.");
            var frame = session.Step(command); commands[Count++] = command; return frame;
        }
        public void ValidateSession(PlayerTimeTrialSession session)
        {
            if (session == null || !session.AthleteId.Equals(AthleteId) || !string.Equals(ContextJson, session.ContextJson, StringComparison.Ordinal))
                throw new ArgumentException("Replay context differs from the recorded definitions/settings.");
        }
        public PlayerTimeTrialFrame ReplayNext(PlayerTimeTrialSession session)
        {
            ValidateSession(session);
            if (session.Clock.Tick.Value >= Count) throw new InvalidOperationException("No recorded command for the next tick.");
            return session.Step(commands[checked((int)session.Clock.Tick.Value)]);
        }
        public string ToJson()
        {
            var rows = new string[Count]; for (int i = 0; i < Count; i++) rows[i] = commands[i].ToJson();
            var dto = new TapeJson { SchemaVersion = 1, Id = Id, Context = ContextJson, AthleteId = AthleteId.Value, Commands = rows };
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(TapeJson)).WriteObject(stream, dto);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public static PlayerCommandTape FromJson(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            TapeJson dto;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json))) dto = (TapeJson)new DataContractJsonSerializer(typeof(TapeJson)).ReadObject(stream);
            if (dto == null || dto.SchemaVersion != 1 || dto.Commands == null) throw new SerializationException("Invalid player tape schema.");
            var tape = new PlayerCommandTape(dto.Id, dto.Context, new AthleteId(dto.AthleteId), Math.Max(1, dto.Commands.Length));
            foreach (string command in dto.Commands) tape.Append(PlayerCommand.FromJson(command));
            return tape;
        }
        [DataContract]
        private sealed class TapeJson
        {
            [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion;
            [DataMember(Name = "id", IsRequired = true)] public string Id;
            [DataMember(Name = "context_json", IsRequired = true)] public string Context;
            [DataMember(Name = "athlete_id", IsRequired = true)] public string AthleteId;
            [DataMember(Name = "commands_json", IsRequired = true)] public string[] Commands;
        }
    }
}

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;

namespace Biathlon.Replay
{
    /// <summary>Persisted v2 device intent only. It cannot replay a race until a
    /// world-motion host supplies the matching authoritative context and state.</summary>
    public sealed class ManualSkiCommandTape
    {
        public const string Kind = "manual_ski_commands_v2";
        private readonly ManualSkiCommand[] commands;
        public string Id { get; }
        public string ContextJson { get; }
        public AthleteId AthleteId { get; }
        public int Count { get; private set; }
        public int Capacity => commands.Length;

        public ManualSkiCommandTape(string id,string contextJson,AthleteId athlete,int capacity)
        {
            if(string.IsNullOrWhiteSpace(id)||string.IsNullOrWhiteSpace(contextJson)||
                !athlete.IsValid||capacity<1)
                throw new ArgumentException("Valid manual tape identity, context, athlete and capacity required.");
            Id=id;ContextJson=contextJson;AthleteId=athlete;commands=new ManualSkiCommand[capacity];
        }

        public void ValidateContext(string contextJson,AthleteId athlete)
        {
            if(!athlete.Equals(AthleteId)||!string.Equals(contextJson,ContextJson,StringComparison.Ordinal))
                throw new ArgumentException("Manual replay context or athlete differs from the recording.");
        }
        public ManualSkiCommand GetCommand(int index)
        {
            if(index<0||index>=Count)throw new ArgumentOutOfRangeException(nameof(index));
            return commands[index];
        }
        public void Append(ManualSkiCommand command)
        {
            if(Count==Capacity)throw new InvalidOperationException("Manual command tape capacity exhausted.");
            if(!command.IsValid||!command.AthleteId.Equals(AthleteId)||command.Tick.Value!=(long)Count+1)
                throw new ArgumentException("Manual commands must be consecutive from tick one for the recorded athlete.");
            commands[Count++]=command;
        }
        public string ToJson()
        {
            var rows=new string[Count];
            for(int i=0;i<Count;i++)rows[i]=commands[i].ToJson();
            var dto=new TapeJson{SchemaVersion=2,Kind=Kind,Id=Id,Context=ContextJson,
                AthleteId=AthleteId.Value,Commands=rows};
            using(var stream=new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(TapeJson)).WriteObject(stream,dto);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public static ManualSkiCommandTape FromJson(string json)
        {
            if(json==null)throw new ArgumentNullException(nameof(json));
            TapeJson dto;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json)))
                dto=(TapeJson)new DataContractJsonSerializer(typeof(TapeJson)).ReadObject(stream);
            if(dto==null||dto.SchemaVersion!=2||dto.Kind!=Kind||dto.Commands==null)
                throw new SerializationException("Unsupported manual command tape schema.");
            var tape=new ManualSkiCommandTape(dto.Id,dto.Context,new AthleteId(dto.AthleteId),
                Math.Max(1,dto.Commands.Length));
            foreach(string row in dto.Commands)tape.Append(ManualSkiCommand.FromJson(row));
            return tape;
        }

        [DataContract]
        private sealed class TapeJson
        {
            [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion;
            [DataMember(Name="kind",IsRequired=true)] public string Kind;
            [DataMember(Name="id",IsRequired=true)] public string Id;
            [DataMember(Name="context_json",IsRequired=true)] public string Context;
            [DataMember(Name="athlete_id",IsRequired=true)] public string AthleteId;
            [DataMember(Name="commands_json",IsRequired=true)] public string[] Commands;
        }
    }
}

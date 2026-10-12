using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using Biathlon.Domain;

namespace Biathlon.Replay
{
    [DataContract] public sealed class RecordedRaceEvent
    {
        [DataMember(Name="tick",IsRequired=true)]public long Tick;
        [DataMember(Name="athlete_id",IsRequired=true)]public string AthleteId;
        [DataMember(Name="event_type",IsRequired=true)]public RaceEventType Type;
        [DataMember(Name="payload",IsRequired=true)]public int Payload;
        public RecordedRaceEvent(RaceEvent value){Tick=value.Tick.Value;AthleteId=value.AthleteId.Value;Type=value.EventType;Payload=value.Payload;}
    }
    [DataContract] public sealed class RaceReplayInfo
    {
        [DataMember(Name="schema_version",IsRequired=true)]public int SchemaVersion=1;
        [DataMember(Name="id",IsRequired=true)]public string Id;
        [DataMember(Name="command_version",IsRequired=true)]public int CommandVersion=1;
        [DataMember(Name="compression",IsRequired=true)]public string Compression="gzip";
        [DataMember(Name="recorded_utc",IsRequired=true)]public string RecordedUtc;
        [DataMember(Name="career_snapshot_json",IsRequired=true)]public string CareerSnapshotJson;
        [DataMember(Name="race_weather_json",EmitDefaultValue=false)]public string RaceWeatherJson;
        [DataMember(Name="start_order_version",EmitDefaultValue=false)]public int StartOrderVersion;
        [DataMember(Name="event_id",IsRequired=true)]public string EventId;
        [DataMember(Name="venue_id",IsRequired=true)]public string VenueId;
        [DataMember(Name="format",IsRequired=true)]public int Format;
        [DataMember(Name="seed",IsRequired=true)]public ulong Seed;
        [DataMember(Name="player_id",IsRequired=true)]public string PlayerId;
        [DataMember(Name="player_name",IsRequired=true)]public string PlayerName;
        [DataMember(Name="tick_frequency",IsRequired=true)]public int TickFrequency;
        [DataMember(Name="tick_count",IsRequired=true)]public long TickCount;
        [DataMember(Name="command_sha256",IsRequired=true)]public string CommandSha256;
        [DataMember(Name="result_sha256",IsRequired=true)]public string ResultSha256;
        [DataMember(Name="events",IsRequired=true)]public RecordedRaceEvent[] Events;
        [DataMember(Name="highlights",IsRequired=true)]public RaceHighlight[] Highlights;
        internal void Validate()
        {
            RaceReplayLibrary.ValidId(Id);
            if(SchemaVersion!=1||CommandVersion!=1||Compression!="gzip"||!DateTimeOffset.TryParse(RecordedUtc,out _)||string.IsNullOrWhiteSpace(CareerSnapshotJson)||
                string.IsNullOrWhiteSpace(EventId)||string.IsNullOrWhiteSpace(VenueId)||string.IsNullOrWhiteSpace(PlayerId)||string.IsNullOrWhiteSpace(PlayerName)||
                Format<0||Format>2||StartOrderVersion<0||StartOrderVersion>1||TickFrequency<1||TickFrequency>120||TickCount<1||TickCount>1000000||Events==null||Highlights==null)
                throw new SerializationException("Unsupported or incomplete replay metadata.");
            Hash(CommandSha256);Hash(ResultSha256);
            if(RaceWeatherJson!=null)RaceWeatherPreset.FromJson(RaceWeatherJson);
            long prior=0;foreach(var e in Events){if(e==null||e.Tick<prior||e.Tick>TickCount||string.IsNullOrWhiteSpace(e.AthleteId)||!Enum.IsDefined(typeof(RaceEventType),e.Type))throw new SerializationException("Invalid replay event.");prior=e.Tick;}
            foreach(var h in Highlights)h.Validate(TickCount);
        }
        private static void Hash(string s){if(s==null||s.Length!=64)throw new SerializationException("Invalid replay hash.");foreach(char c in s)if(c<'0'||c>'9'&&c<'a'||c>'f')throw new SerializationException("Invalid replay hash.");}
        public string ToJson(){Validate();using(var m=new MemoryStream()){new DataContractJsonSerializer(typeof(RaceReplayInfo)).WriteObject(m,this);return Encoding.UTF8.GetString(m.ToArray());}}
        public static RaceReplayInfo FromJson(string json){using(var m=new MemoryStream(Encoding.UTF8.GetBytes(json??throw new ArgumentNullException(nameof(json))))){var info=(RaceReplayInfo)new DataContractJsonSerializer(typeof(RaceReplayInfo)).ReadObject(m);if(info==null)throw new SerializationException("Missing replay.");info.Validate();return info;}}
    }
    /// <summary>Separate from career profiles. Partial/corrupt files are never offered as playable races.</summary>
    public sealed class RaceReplayLibrary
    {
        private readonly string folder;
        public RaceReplayLibrary(string directory){folder=Path.GetFullPath(directory??throw new ArgumentNullException(nameof(directory)));Directory.CreateDirectory(folder);}
        internal static void ValidId(string id)
        {if(string.IsNullOrWhiteSpace(id)||id.Length>200)throw new ArgumentException("Replay identity required.");foreach(char c in id)if(!char.IsLetterOrDigit(c)&&c!='_'&&c!='-')throw new ArgumentException("Invalid replay identity.");}
        private string PathFor(string id,string extension){ValidId(id);return Path.Combine(folder,id+extension);}
        public string PendingPath(string id)=>PathFor(id,".pending");
        public string CommandsPath(string id)=>PathFor(id,".brp");
        public Stream CreatePending(string id)
            =>new BufferedStream(new GZipStream(new FileStream(PendingPath(id),FileMode.CreateNew,FileAccess.Write,FileShare.Read,65536,FileOptions.SequentialScan),CompressionLevel.Fastest),65536);
        public static string HashFile(string path){using(var f=File.OpenRead(path))using(var sha=SHA256.Create()){var hash=sha.ComputeHash(f);var b=new StringBuilder(64);foreach(byte v in hash)b.Append(v.ToString("x2"));return b.ToString();}}
        public void Publish(RaceReplayInfo info)
        {
            if(info==null)throw new ArgumentNullException(nameof(info));info.CommandSha256=HashFile(PendingPath(info.Id));string json=info.ToJson();
            string path=PathFor(info.Id,".json"),temp=PathFor(info.Id,".json.tmp");
            if(File.Exists(path)||File.Exists(CommandsPath(info.Id)))throw new IOException("Replay already exists.");
            try{File.WriteAllText(temp,json,new UTF8Encoding(false));File.Move(PendingPath(info.Id),CommandsPath(info.Id));File.Move(temp,path);}
            finally{if(File.Exists(temp))File.Delete(temp);}
        }
        public RaceReplayInfo[] List()
        {
            var list=new List<RaceReplayInfo>();foreach(string path in Directory.GetFiles(folder,"*.json"))
                try{var info=RaceReplayInfo.FromJson(File.ReadAllText(path));if(Path.GetFileNameWithoutExtension(path)==info.Id&&File.Exists(CommandsPath(info.Id)))list.Add(info);}
                catch(Exception e)when(e is IOException||e is SerializationException||e is ArgumentException){ }
            list.Sort((a,b)=>string.CompareOrdinal(b.RecordedUtc,a.RecordedUtc));return list.ToArray();
        }
        public Stream OpenVerified(RaceReplayInfo info)
        {info.Validate();string path=CommandsPath(info.Id);if(HashFile(path)!=info.CommandSha256)throw new InvalidDataException("Replay commands are damaged.");return new BufferedStream(new GZipStream(new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.SequentialScan),CompressionMode.Decompress),65536);}
        public void DiscardPending(string id){string path=PendingPath(id);if(File.Exists(path))File.Delete(path);}
    }
}

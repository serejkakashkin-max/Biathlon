using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Career;

namespace Biathlon.Presentation
{
    [DataContract]public sealed class ReliabilityPolicy
    {
        [DataMember(Name="schema_version",IsRequired=true)]public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)]public string Id {get;private set;}
        [DataMember(Name="diagnostic_event_capacity",IsRequired=true)]public int EventCapacity {get;private set;}
        [DataMember(Name="maximum_bundle_bytes",IsRequired=true)]public int MaximumBundleBytes {get;private set;}
        public static ReliabilityPolicy FromJson(string json)
        {
            var p=DiagnosticJson.Read<ReliabilityPolicy>(json);
            if(p==null||p.SchemaVersion!=1||string.IsNullOrWhiteSpace(p.Id)||p.EventCapacity<8||p.EventCapacity>256||p.MaximumBundleBytes<16384||p.MaximumBundleBytes>1048576)throw new SerializationException("Invalid reliability policy.");return p;
        }
    }
    public enum DiagnosticIssue
    {SaveReadFailed,SaveBackupRecovered,SaveCheckpointRecovered,SaveWriteFailed,UnsupportedSaveVersion,MissingContent,InvalidConfiguration,ReplayMismatch,SimulationFault,RuntimeError,DeviceOutputUnavailable,SettingsRecovered}
    [DataContract]public struct DiagnosticEvent
    {
        [DataMember(Name="code",IsRequired=true)]public string Code {get;private set;}
        [DataMember(Name="utc_unix_ms",IsRequired=true)]public long UtcUnixMilliseconds {get;private set;}
        [DataMember(Name="race_tick",IsRequired=true)]public long RaceTick {get;private set;}
        public DiagnosticEvent(DiagnosticIssue issue,long utcUnixMilliseconds,long raceTick=-1)
        {if(!Enum.IsDefined(typeof(DiagnosticIssue),issue)||utcUnixMilliseconds<0||raceTick< -1)throw new ArgumentException("Invalid diagnostic event.");Code=issue.ToString();UtcUnixMilliseconds=utcUnixMilliseconds;RaceTick=raceTick;}
    }
    /// <summary>Bounded allowlisted event codes, never arbitrary exception/log text.</summary>
    public sealed class ReliabilityEventBuffer
    {
        private readonly DiagnosticEvent[] rows;private int next,count;
        public ReliabilityEventBuffer(ReliabilityPolicy policy){rows=new DiagnosticEvent[(policy??throw new ArgumentNullException(nameof(policy))).EventCapacity];}
        public void Record(DiagnosticIssue issue,long utcUnixMilliseconds,long tick=-1){rows[next]=new DiagnosticEvent(issue,utcUnixMilliseconds,tick);next=(next+1)%rows.Length;count=Math.Min(rows.Length,count+1);}
        public DiagnosticEvent[] Snapshot(){var copy=new DiagnosticEvent[count];for(int i=0;i<count;i++)copy[i]=rows[(next-count+i+rows.Length)%rows.Length];return copy;}
    }
    [DataContract]public sealed class DiagnosticConfiguration
    {
        [DataMember(Name="unity_version")]public string UnityVersion {get;private set;}
        [DataMember(Name="product_version")]public string ProductVersion {get;private set;}
        [DataMember(Name="locale")]public string Locale {get;private set;}
        [DataMember(Name="text_scale_index")]public int TextScaleIndex {get;private set;}
        [DataMember(Name="high_contrast")]public bool HighContrast {get;private set;}
        [DataMember(Name="reduced_motion")]public bool ReducedMotion {get;private set;}
        [DataMember(Name="graphics_tier")]public string GraphicsTier {get;private set;}
        public DiagnosticConfiguration(string unityVersion,string productVersion,string locale,int textScaleIndex,bool highContrast,bool reducedMotion,string graphicsTier)
        {
            if(!Version(unityVersion)||!Version(productVersion)||locale!="ru"&&locale!="en"||textScaleIndex<0||textScaleIndex>2||graphicsTier!="high"&&graphicsTier!="balanced"&&graphicsTier!="low")throw new ArgumentException("Invalid public configuration metadata.");
            UnityVersion=unityVersion;ProductVersion=productVersion;Locale=locale;TextScaleIndex=textScaleIndex;HighContrast=highContrast;ReducedMotion=reducedMotion;GraphicsTier=graphicsTier;
        }
        private static bool Version(string value)
        {if(string.IsNullOrEmpty(value)||value.Length>32||value[0]<'0'||value[0]>'9')return false;foreach(char c in value)if(!(c>='0'&&c<='9'||c=='.'||c=='-'||c=='+'||c>='a'&&c<='z'||c>='A'&&c<='Z'))return false;return true;}
    }
    [DataContract]internal sealed class SaveDiagnostic
    {
        [DataMember(Name="available")]public bool Available;
        [DataMember(Name="schema_version")]public int Schema;
        [DataMember(Name="save_version")]public int SaveVersion;
        [DataMember(Name="season_number")]public int Season;
        [DataMember(Name="week")]public int Week;
        [DataMember(Name="active_athletes")]public int ActiveAthletes;
        [DataMember(Name="world_athletes")]public int WorldAthletes;
        [DataMember(Name="saved_races")]public int Races;
        internal static SaveDiagnostic From(CareerSave save)=>save==null?new SaveDiagnostic():new SaveDiagnostic{Available=true,Schema=save.SchemaVersion,SaveVersion=save.SaveVersion,Season=save.Season,Week=save.Week,ActiveAthletes=save.AthleteCount,WorldAthletes=save.WorldAthleteCount,Races=save.RaceCount};
    }
    [DataContract]internal sealed class DiagnosticManifest
    {
        [DataMember(Name="schema_version")]public int Schema=1;
        [DataMember(Name="id")]public string Id="biathlon_local_diagnostic_v1";
        [DataMember(Name="privacy")]public string Privacy="Allowlisted public configuration, anonymous save structure and bounded issue codes. No raw logs, saves, names, IDs, paths or credentials.";
        [DataMember(Name="utc_unix_ms")]public long Timestamp;
    }
    internal static class DiagnosticJson
    {
        internal static T Read<T>(string json){using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json??throw new ArgumentNullException(nameof(json)))))return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);}
        internal static string Write<T>(T value){using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value);return Encoding.UTF8.GetString(stream.ToArray());}}
    }
    public static class DiagnosticBundle
    {
        public static string Export(string directory,ReliabilityPolicy policy,DiagnosticConfiguration config,CareerSave save,DiagnosticEvent[] events,long utcUnixMilliseconds)
        {
            if(string.IsNullOrWhiteSpace(directory)||policy==null||config==null||events==null||events.Length>policy.EventCapacity||utcUnixMilliseconds<0)throw new ArgumentException("Invalid diagnostic export.");
            foreach(var item in events){if(!Enum.TryParse(item.Code,out DiagnosticIssue parsed)||!Enum.IsDefined(typeof(DiagnosticIssue),parsed)||item.UtcUnixMilliseconds<0||item.RaceTick< -1)throw new ArgumentException("Only allowlisted diagnostic codes are exportable.");}
            var payloads=new[]{DiagnosticJson.Write(new DiagnosticManifest{Timestamp=utcUnixMilliseconds}),DiagnosticJson.Write(config),DiagnosticJson.Write(SaveDiagnostic.From(save)),DiagnosticJson.Write(events)};
            string[] names={"manifest.json","configuration.json","save-structure.json","events.json"};long bytes=0;foreach(string payload in payloads)bytes+=Encoding.UTF8.GetByteCount(payload);if(bytes>policy.MaximumBundleBytes)throw new IOException("Diagnostic size limit exceeded.");
            Directory.CreateDirectory(directory);string target=Path.Combine(Path.GetFullPath(directory),"biathlon-report-"+utcUnixMilliseconds+"-"+Guid.NewGuid().ToString("N")+".zip"),temporary=target+".tmp";
            try
            {
                using(var file=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {using(var archive=new ZipArchive(file,ZipArchiveMode.Create,true))for(int i=0;i<names.Length;i++)using(var writer=new StreamWriter(archive.CreateEntry(names[i]).Open(),new UTF8Encoding(false)))writer.Write(payloads[i]);file.Flush(true);}
                using(var file=File.OpenRead(temporary))using(var archive=new ZipArchive(file,ZipArchiveMode.Read))
                {if(archive.Entries.Count!=names.Length)throw new IOException("Diagnostic verification failed.");for(int i=0;i<names.Length;i++)using(var reader=new StreamReader(archive.GetEntry(names[i]).Open(),Encoding.UTF8))if(reader.ReadToEnd()!=payloads[i])throw new IOException("Diagnostic verification failed.");}
                File.Move(temporary,target);return target;
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
    }
}

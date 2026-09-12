using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class TuningLockEntry
    {
        private TuningLockEntry() { }
        [DataMember(Name="path",IsRequired=true)] public string Path { get; private set; }
        [DataMember(Name="sha256",IsRequired=true)] public string Sha256 { get; private set; }
        internal void Validate()
        {
            if(string.IsNullOrEmpty(Path)||!(Path.StartsWith("Assets/Resources/Biathlon/",StringComparison.Ordinal)||Path.StartsWith("tests/fixtures/",StringComparison.Ordinal)))throw new SerializationException("Lock path is outside the declared data roots.");
            foreach(string segment in Path.Split('/'))
            {if(segment.Length==0||segment=="."||segment=="..")throw new SerializationException("Invalid lock path.");foreach(char c in segment)if(!((c>='a'&&c<='z')||(c>='A'&&c<='Z')||(c>='0'&&c<='9')||c=='_'||c=='-'||c=='.'))throw new SerializationException("Invalid lock path character.");}
            if(Sha256==null||Sha256.Length!=64)throw new SerializationException("Invalid lock digest.");
            foreach(char c in Sha256)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))throw new SerializationException("Invalid lock digest.");
        }
    }
    [DataContract] public sealed class BalanceTuningLock
    {
        private BalanceTuningLock() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="status",IsRequired=true)] public string Status { get; private set; }
        [DataMember(Name="entries",IsRequired=true)] private TuningLockEntry[] entries { get; set; }
        public int Count=>entries.Length;public TuningLockEntry Get(int index)=>entries[index];
        public string ToJson()=>RouteJson.Write(this);
        public static BalanceTuningLock FromJson(string json)
        {
            var d=RouteJson.Read<BalanceTuningLock>(json);
            if(d==null||d.SchemaVersion!=1||d.Status!="locked_provisional"||d.entries==null||d.entries.Length<1||d.entries.Length>10000)throw new SerializationException("Invalid tuning lock schema/status.");
            TrackValidation.Id(d.Id);var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(var entry in d.entries){if(entry==null)throw new SerializationException("Null lock entry.");entry.Validate();if(!paths.Add(entry.Path))throw new SerializationException("Duplicate lock path.");}return d;
        }
    }
}

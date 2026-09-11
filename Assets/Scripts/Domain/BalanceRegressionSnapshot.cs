using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract] public sealed class BalanceRegressionRecord
    {
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="sha256",IsRequired=true)] public string Sha256 { get; private set; }
    }
    [DataContract] public sealed class BalanceTargetWindow
    {
        [DataMember(Name="case_id",IsRequired=true)] public string CaseId { get; private set; }
        [DataMember(Name="metric",IsRequired=true)] public string Metric { get; private set; }
        [DataMember(Name="mean_min",IsRequired=true)] public double Minimum { get; private set; }
        [DataMember(Name="mean_max",IsRequired=true)] public double Maximum { get; private set; }
    }
    [DataContract] public sealed class BalanceRegressionSnapshot
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="suite_sha256",IsRequired=true)] public string SuiteSha256 { get; private set; }
        [DataMember(Name="target_status",IsRequired=true)] public string TargetStatus { get; private set; }
        [DataMember(Name="records",IsRequired=true)] private BalanceRegressionRecord[] records { get; set; }
        [DataMember(Name="windows",IsRequired=true)] private BalanceTargetWindow[] windows { get; set; }
        public int RecordCount=>records.Length;public int WindowCount=>windows.Length;
        public BalanceRegressionRecord GetRecord(int i)=>records[i];public BalanceTargetWindow GetWindow(int i)=>windows[i];
        private static void Digest(string x){if(x==null||x.Length!=64)throw new SerializationException("SHA256 required.");foreach(char c in x)if(!((c>='0'&&c<='9')||(c>='a'&&c<='f')))throw new SerializationException("Lowercase SHA256 required.");}
        public static BalanceRegressionSnapshot FromJson(string json)
        {
            var s=RouteJson.Read<BalanceRegressionSnapshot>(json);
            if(s==null||s.SchemaVersion!=1||s.records==null||s.RecordCount<1||s.windows==null||s.WindowCount<1||s.TargetStatus!="provisional_observation")throw new SerializationException("Versioned provisional regression snapshot required.");
            BalanceBatchDefinition.Plain(s.Id);Digest(s.SuiteSha256);
            for(int i=0;i<s.RecordCount;i++){var r=s.records[i];if(r==null)throw new SerializationException("Null record.");BalanceBatchDefinition.Plain(r.Id);Digest(r.Sha256);for(int j=0;j<i;j++)if(s.records[j].Id==r.Id)throw new SerializationException("Duplicate snapshot record.");}
            for(int i=0;i<s.WindowCount;i++){var w=s.windows[i];if(w==null)throw new SerializationException("Null window.");BalanceBatchDefinition.Plain(w.CaseId);BalanceBatchDefinition.Plain(w.Metric);TrackValidation.Finite(w.Minimum,-1e6,1e6,"minimum");TrackValidation.Finite(w.Maximum,w.Minimum,1e6,"maximum");for(int j=0;j<i;j++)if(s.windows[j].CaseId==w.CaseId&&s.windows[j].Metric==w.Metric)throw new SerializationException("Duplicate target window.");}
            return s;
        }
        public string ToJson()=>RouteJson.Write(this);
    }
}

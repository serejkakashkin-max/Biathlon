using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Biathlon.AI;
using Biathlon.Domain;

internal static class BatchLab
{
    private static readonly JsonSerializerOptions Json=new JsonSerializerOptions{WriteIndented=true};
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    private static void Write(string path,object value)=>File.WriteAllText(path,JsonSerializer.Serialize(value,Json));
    public static int Execute(string suiteFile,string data,string tracks,string output,int workers,string snapshotFile=null)
    {
        if(workers<1||workers>16)throw new ArgumentException("Workers must be 1..16.");
        var suite=BalanceBatchDefinition.FromJson(File.ReadAllText(suiteFile));
        var snapshot=snapshotFile==null?null:BalanceRegressionSnapshot.FromJson(File.ReadAllText(snapshotFile));
        Directory.CreateDirectory(output);
        Write(Path.Combine(output,"status.json"),new{schema_version=1,id=suite.Id,status="incomplete",expected=suite.RunCount});
        var definitions=Directory.GetFiles(data,"*.json").ToDictionary(Path.GetFileName,File.ReadAllText,StringComparer.Ordinal);
        var courses=suiteFiles();
        Dictionary<string,TrackDefinition> suiteFiles()=>Enumerable.Range(0,suite.Count).Select(i=>suite.Get(i).TrackFile).Distinct().ToDictionary(x=>x,x=>TrackDefinition.FromJson(File.ReadAllText(Path.Combine(tracks,x))),StringComparer.Ordinal);
        string Read(string name)=>definitions[name];
        File.WriteAllText(Path.Combine(output,"suite.json"),suite.ToJson());
        Write(Path.Combine(output,"inputs.json"),new{schema_version=1,id="batch_inputs_v1",definitions=definitions.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>Hash(x.Value)),tracks=courses.OrderBy(x=>x.Key).ToDictionary(x=>x.Key,x=>Hash(x.Value.ToJson()))});
        var rows=new List<BalanceMetrics>(suite.RunCount);var timer=System.Diagnostics.Stopwatch.StartNew();
        using(var jsonl=new StreamWriter(Path.Combine(output,"races.jsonl"),false,new UTF8Encoding(false)))
        {
            // Bounded independent jobs; output order never depends on worker completion order.
            for(int offset=0;offset<suite.RunCount;offset+=workers)
            {
                int count=Math.Min(workers,suite.RunCount-offset);var chunk=new BalanceMetrics[count];int first=offset;
                Parallel.For(0,count,new ParallelOptions{MaxDegreeOfParallelism=workers},i=>
                {
                    int index=first+i,caseIndex=index%suite.Count,iteration=index/suite.Count;
                    var profile=CompetitionProfile.FromJson(Read(suite.ProfileFile));var events=new BalanceEventAudit(profile.ShotsPerBout);
                    var runner=new BalanceRaceRunner(suite,caseIndex,iteration,Read,courses[suite.Get(caseIndex).TrackFile],events);
                    var m=BalanceMeasurement.Run(runner,suite,caseIndex,iteration,events);m.Values["context_sha256"]=Hash(runner.ContextJson);
                    if(iteration==0)File.WriteAllText(Path.Combine(output,suite.Get(caseIndex).Id+"-context.json"),runner.ContextJson);
                    chunk[i]=m;
                });
                foreach(var row in chunk){jsonl.WriteLine(row.ToJson());rows.Add(row);}jsonl.Flush();
                if(rows.Count%100<workers)Console.WriteLine("Completed "+rows.Count+"/"+suite.RunCount);
            }
        }
        var columns=rows[0].Values.Keys.OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        using(var csv=new StreamWriter(Path.Combine(output,"races.csv"),false,new UTF8Encoding(false)))
        {csv.WriteLine(string.Join(",",columns));foreach(var r in rows)csv.WriteLine(string.Join(",",columns.Select(k=>Cell(r.Values[k]))));}
        Report(rows,output,suite);
        if(snapshot!=null)
        {
            var diff=BalanceRegression.Compare(suite,snapshot,rows);
            Write(Path.Combine(output,"regression.json"),new{schema_version=1,id=snapshot.Id,status=diff.Length==0?"passed":"failed",diff});
            if(diff.Length>0)throw new InvalidOperationException("Regression differs; see regression.json: "+string.Join("; ",diff));
        }
        Write(Path.Combine(output,"status.json"),new{schema_version=1,id=suite.Id,status="completed",completed=rows.Count,wall_seconds=timer.Elapsed.TotalSeconds,workers,runtime=System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,metrics_sha256=Hash(File.ReadAllText(Path.Combine(output,"races.jsonl")))});
        return 0;
    }
    private static string Cell(object value)=>value==null?"":"\""+Convert.ToString(value,CultureInfo.InvariantCulture).Replace("\"","\"\"")+"\"";
    private static void Report(List<BalanceMetrics> rows,string output,BalanceBatchDefinition suite)
    {
        string[] metrics={"finish_seconds","ski_seconds","range_seconds","penalty_seconds","hit_rate","penalties","avg_hr_bpm","range_entry_hr_bpm","fatigue_finish","anaerobic_min","final_lap_speed_mps"};
        var summaries=new List<object>();var md=new StringBuilder("# Batch distributions\n\nPopulation standard deviation; linear (n-1) percentiles. Solo Sprint; no group-benefit claim.\n\n| Case | n | Mean seconds | P05 | P95 | Hit rate | Fatigue |\n|---|---:|---:|---:|---:|---:|---:|\n");
        foreach(var g in rows.GroupBy(r=>(string)r.Values["case_id"]))
        {
            var d=metrics.ToDictionary(k=>k,k=>new BalanceDistribution(g.Select(r=>r.Number(k))));
            var penalties=g.GroupBy(r=>r.Number("penalties")).OrderBy(x=>x.Key).ToDictionary(x=>x.Key.ToString(CultureInfo.InvariantCulture),x=>x.Count());
            summaries.Add(new{case_id=g.Key,metrics=d,penalty_histogram=penalties,finish_fatigue_correlation=BalanceDistribution.Correlation(g.Select(x=>x.Number("finish_seconds")).ToArray(),g.Select(x=>x.Number("fatigue_finish")).ToArray())});
            var t=d["finish_seconds"];md.AppendFormat(CultureInfo.InvariantCulture,"| {0} | {1} | {2:F3} | {3:F3} | {4:F3} | {5:F4} | {6:F4} |\n",g.Key,t.Count,t.Mean,t.P05,t.P95,d["hit_rate"].Mean,d["fatigue_finish"].Mean);
        }
        Write(Path.Combine(output,"distributions.json"),new{schema_version=1,id="batch_distributions_v1",groups=summaries});
        var pairs=new List<BalancePairedComparison>();
        for(int i=0;i<suite.Count;i++)
        {
            int first=i;for(int j=0;j<i;j++)if(suite.Get(j).TrackFile==suite.Get(i).TrackFile){first=j;break;}
            if(first!=i)pairs.Add(new BalancePairedComparison(suite,rows,suite.Get(first).Id,suite.Get(i).Id));
        }
        Write(Path.Combine(output,"paired.json"),new{schema_version=1,id="batch_paired_v1",pairs});
        md.Append("\nPaired savings relative to the first case on each identical track (positive = changed faster):\n\n| Baseline | Changed | Mean saved s | P05 | P95 | Wins |\n|---|---|---:|---:|---:|---:|\n");
        foreach(var p in pairs)md.AppendFormat(CultureInfo.InvariantCulture,"| {0} | {1} | {2:F3} | {3:F3} | {4:F3} | {5}/{6} |\n",p.Baseline,p.Changed,p.SecondsSaved.Mean,p.SecondsSaved.P05,p.SecondsSaved.P95,p.ChangedWins,p.SecondsSaved.Count);
        File.WriteAllText(Path.Combine(output,"report.md"),md.ToString());
    }
}

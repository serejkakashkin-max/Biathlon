using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Biathlon.AI;
using Biathlon.Domain;

internal static class TuningLab
{
    public static int Execute(string definitionFile,string suiteFile,string data,string output)
    {
        var definition=BoundedTuningDefinition.FromJson(File.ReadAllText(definitionFile));
        var suite=ShootingBalanceSuite.FromJson(File.ReadAllText(suiteFile));
        var sources=new SortedDictionary<string,string>(StringComparer.Ordinal);
        string Read(string name){string raw=File.ReadAllText(Path.Combine(data,name));sources[name]=BalanceRegression.Hash(raw);return raw;}
        var evaluator=new ShootingTuningEvaluator(suite,Read);
        Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"status.json"),"{\"status\":\"incomplete\"}");
        var evaluations=new List<object>();
        var proposal=BoundedTuner.Search(definition,values=>{var metrics=evaluator.Evaluate(values);evaluations.Add(new{values,metrics});return metrics;});
        var changes=new List<object>();for(int i=0;i<definition.ParameterCount;i++){var p=definition.Parameter(i);changes.Add(new{coefficient=p.Name,old_value=p.Initial,proposed_value=proposal.Values[p.Name]});}
        var options=new JsonSerializerOptions{WriteIndented=true};
        File.WriteAllText(Path.Combine(output,"evaluations.json"),JsonSerializer.Serialize(evaluations,options));
        File.WriteAllText(Path.Combine(output,"proposal.json"),JsonSerializer.Serialize(new{schema_version=1,id=definition.Id+"_proposal",status=proposal.TargetsMet?"proposal_targets_met":"targets_unmet",applied=false,
            evaluations=proposal.Evaluations,changes,metrics=proposal.Metrics,window_violation=proposal.WindowViolation,center_distance=proposal.CenterDistance,
            definition_sha256=BalanceRegression.Hash(definition.ToJson()),suite_sha256=BalanceRegression.Hash(suite.ToJson()),sources},options));
        File.WriteAllText(Path.Combine(output,"status.json"),JsonSerializer.Serialize(new{status=proposal.TargetsMet?"PASS":"FAIL",applied=false}));
        Console.WriteLine("Bounded tuning: "+proposal.Evaluations+" evaluations; targets met="+proposal.TargetsMet+"; proposal only.");
        return proposal.TargetsMet?0:1;
    }
}

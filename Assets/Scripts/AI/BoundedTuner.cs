using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public sealed class TuningProposal
    {
        public IReadOnlyDictionary<string,double> Values { get; }
        public IReadOnlyDictionary<string,double> Metrics { get; }
        public double WindowViolation { get; } public double CenterDistance { get; }
        public bool TargetsMet=>WindowViolation==0;public int Evaluations { get; }
        internal TuningProposal(Dictionary<string,double> values,Dictionary<string,double> metrics,double violation,double center,int evaluations)
        {Values=new ReadOnlyDictionary<string,double>(values);Metrics=new ReadOnlyDictionary<string,double>(metrics);WindowViolation=violation;CenterDistance=center;Evaluations=evaluations;}
    }
    /// <summary>Finite, deterministic, proposal-only search. Never writes or applies tuning.</summary>
    public static class BoundedTuner
    {
        public static TuningProposal Search(BoundedTuningDefinition definition,Func<IReadOnlyDictionary<string,double>,IReadOnlyDictionary<string,double>> evaluate)
        {
            if(definition==null||evaluate==null)throw new ArgumentNullException();
            Dictionary<string,double> bestValues=null,bestMetrics=null;double bestViolation=double.PositiveInfinity,bestCenter=double.PositiveInfinity;
            for(int candidate=0;candidate<definition.CandidateCount;candidate++)
            {
                int index=candidate;var values=new Dictionary<string,double>(StringComparer.Ordinal);
                for(int i=definition.ParameterCount-1;i>=0;i--){var p=definition.Parameter(i);values.Add(p.Name,p.Get(index%p.Count));index/=p.Count;}
                var observed=evaluate(new ReadOnlyDictionary<string,double>(values));
                if(observed==null)throw new InvalidOperationException("Evaluator returned no metrics.");
                var metrics=new Dictionary<string,double>(StringComparer.Ordinal);double violation=0,center=0;
                for(int i=0;i<definition.TargetCount;i++)
                {
                    var target=definition.Target(i);
                    if(!observed.TryGetValue(target.Metric,out double value)||double.IsNaN(value)||double.IsInfinity(value))throw new InvalidOperationException("Missing/nonfinite target metric: "+target.Metric);
                    metrics.Add(target.Metric,value);double width=target.Maximum-target.Minimum;
                    violation+=Math.Max(0,Math.Max(target.Minimum-value,value-target.Maximum))/width;
                    center+=Math.Abs(value-(target.Minimum+target.Maximum)/2)/width;
                    if(double.IsInfinity(violation)||double.IsInfinity(center))throw new InvalidOperationException("Target score exceeds numeric bounds.");
                }
                // Window compliance first, normalized distance second, declared enumeration order last.
                if(violation<bestViolation||(violation==bestViolation&&center<bestCenter))
                {bestValues=values;bestMetrics=metrics;bestViolation=violation;bestCenter=center;}
            }
            return new TuningProposal(bestValues,bestMetrics,bestViolation,bestCenter,definition.CandidateCount);
        }
    }
}

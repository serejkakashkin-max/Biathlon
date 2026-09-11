using System;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.AI
{
    /// <summary>Positive finish savings favor Changed; other deltas are Changed minus Baseline.</summary>
    public sealed class BalancePairedComparison
    {
        public string Baseline { get; } public string Changed { get; }
        public BalanceDistribution SecondsSaved { get; }
        public BalanceDistribution RangeHrDelta { get; }
        public BalanceDistribution FatigueDelta { get; }
        public BalanceDistribution FinalSpeedDelta { get; }
        public int ChangedWins { get; }
        public BalancePairedComparison(BalanceBatchDefinition suite,IReadOnlyList<BalanceMetrics> rows,string baseline,string changed)
        {
            if(suite==null||rows==null||baseline==changed)throw new ArgumentException("Distinct paired cases required.");
            BalancePacingCase a=null,b=null;for(int i=0;i<suite.Count;i++){if(suite.Get(i).Id==baseline)a=suite.Get(i);if(suite.Get(i).Id==changed)b=suite.Get(i);}
            if(a==null||b==null||a.TrackFile!=b.TrackFile)throw new ArgumentException("Cases must share authored track, athlete/profile and batch inputs.");
            var left=Collect(suite,rows,baseline);var right=Collect(suite,rows,changed);
            var time=new double[suite.Iterations];var hr=new double[time.Length];var fatigue=new double[time.Length];var speed=new double[time.Length];
            for(int i=0;i<time.Length;i++)
            {
                time[i]=BalanceDistribution.PairedBenefit(left[i].Number("finish_seconds"),right[i].Number("finish_seconds"));
                hr[i]=right[i].Number("range_entry_hr_bpm")-left[i].Number("range_entry_hr_bpm");fatigue[i]=right[i].Number("fatigue_finish")-left[i].Number("fatigue_finish");
                speed[i]=right[i].Number("final_lap_speed_mps")-left[i].Number("final_lap_speed_mps");if(time[i]>0)ChangedWins++;
            }
            Baseline=baseline;Changed=changed;SecondsSaved=new BalanceDistribution(time);RangeHrDelta=new BalanceDistribution(hr);FatigueDelta=new BalanceDistribution(fatigue);FinalSpeedDelta=new BalanceDistribution(speed);
        }
        private static BalanceMetrics[] Collect(BalanceBatchDefinition suite,IReadOnlyList<BalanceMetrics> rows,string id)
        {
            var result=new BalanceMetrics[suite.Iterations];int count=0;
            foreach(var row in rows)if((string)row.Values["case_id"]==id)
            {
                double raw=row.Number("iteration");if(raw<0||raw>=result.Length||raw!=Math.Truncate(raw))throw new ArgumentException("Invalid iteration.");int i=(int)raw;
                if(result[i]!=null||(string)row.Values["seed"]!=suite.SeedAt(i).ToString(CultureInfo.InvariantCulture))throw new ArgumentException("Duplicate or unmatched seed.");
                result[i]=row;count++;
            }
            if(count!=result.Length)throw new ArgumentException("Complete paired seed sets required.");return result;
        }
    }
}

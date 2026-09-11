using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>Constant-memory event audit. Replay events are observed, not suppressed or synthesized.</summary>
    public sealed class BalanceEventAudit : IRaceEventSink
    {
        private readonly int shotsPerBout;
        public int Shots { get; private set; } public int HitsProne { get; private set; } public int HitsStanding { get; private set; }
        public int Penalties { get; private set; } public int Overtakes { get; private set; }
        public ulong Hash { get; private set; }=14695981039346656037UL;
        public BalanceEventAudit(int shotsPerBout) { if(shotsPerBout<1)throw new ArgumentOutOfRangeException();this.shotsPerBout=shotsPerBout; }
        private void Add(long value){unchecked{Hash=(Hash^(ulong)value)*1099511628211UL;}}
        public void Record(RaceEvent e)
        {
            Add(e.Tick.Value);Add(e.TickFrequency);Add((int)e.EventType);Add(e.Payload);foreach(char c in e.AthleteId.Value)Add(c);
            if(e.EventType==RaceEventType.ShotResolved){if(e.Payload>0){if(Shots<shotsPerBout)HitsProne++;else HitsStanding++;}Shots++;}
            if(e.EventType==RaceEventType.PenaltyLoopCompleted)Penalties++;
            if(e.EventType==RaceEventType.OvertakeCompleted)Overtakes++;
        }
    }
    public sealed class BalanceMetrics
    {
        public readonly Dictionary<string,object> Values=new Dictionary<string,object>(StringComparer.Ordinal);
        public double Number(string key)=>Convert.ToDouble(Values[key],CultureInfo.InvariantCulture);
        public string ToJson()
        {
            // Sorted pairs avoid runtime-dependent dictionary serialization order.
            var keys=new List<string>(Values.Keys);keys.Sort(StringComparer.Ordinal);var b=new StringBuilder("{");
            for(int i=0;i<keys.Count;i++)
            {
                if(i>0)b.Append(',');b.Append(Quote(keys[i])).Append(':');var v=Values[keys[i]];
                b.Append(v==null?"null":v is string?Quote((string)v):Convert.ToString(v,CultureInfo.InvariantCulture));
            }
            return b.Append('}').ToString();
        }
        private static string Quote(string value)
        {using(var s=new MemoryStream()){new DataContractJsonSerializer(typeof(string)).WriteObject(s,value);return Encoding.UTF8.GetString(s.ToArray());}}
    }
    public static class BalanceMeasurement
    {
        public static BalanceMetrics Run(BalanceRaceRunner runner,BalanceBatchDefinition suite,int caseIndex,int iteration,BalanceEventAudit events,System.Threading.CancellationToken cancellation=default)
        {
            double hr=0,maxHr=0,minReserve=1,rangeTime=0,penaltyTime=0,elapsed=0,entryHr=0,finalDistance=0,finalTime=0;int entries=0;
            while(!runner.Field.IsComplete)
            {
                cancellation.ThrowIfCancellationRequested();var a=runner.Field.GetAthlete(0);bool shooting=a.ActiveShooting!=null;
                var range=a.CurrentRangeMotion;bool penalty=range.HasValue&&range.Value.Phase==RangeTraversalPhase.PenaltyLoop;
                bool lastLap=a.Lifecycle.CompletedBouts==runner.Field.Profile.BoutCount&&!penalty;long oldDistance=a.Motion.DistanceNanometers;
                runner.Step();
                if(!shooting&&a.ActiveShooting!=null){entryHr+=a.PhysiologyState.HeartRateBpm;entries++;}
                // Exclude the start instant. Attribute boundary ticks to the state at interval start.
                double dt=runner.Field.Tick.Value<=a.ScheduledStart.Value?0:1.0/suite.Frequency;
                if(runner.Field.IsComplete)dt=a.Lifecycle.ElapsedFinishTime.Seconds-elapsed;
                if(dt<0||dt>1.0/suite.Frequency+1e-8)throw new InvalidOperationException("Invalid measurement interval.");
                elapsed+=dt;hr+=a.PhysiologyState.HeartRateBpm*dt;maxHr=Math.Max(maxHr,a.PhysiologyState.HeartRateBpm);minReserve=Math.Min(minReserve,a.PhysiologyState.AnaerobicReserve);
                if(shooting)rangeTime+=dt;else if(penalty)penaltyTime+=dt;
                if(lastLap&&!shooting){finalTime+=dt;finalDistance+=(a.Motion.DistanceNanometers-oldDistance)/(double)SkiState.NanometersPerMeter;}
            }
            var actor=runner.Field.GetAthlete(0);var profile=runner.Field.Profile;
            if(events.Shots!=profile.BoutCount*profile.ShotsPerBout||events.Penalties!=actor.TotalPenaltyLoops||entries!=profile.BoutCount)
                throw new InvalidOperationException("Incomplete batch shooting/penalty accounting.");
            var m=new BalanceMetrics();var v=m.Values;
            v["schema_version"]=1;v["id"]=suite.Get(caseIndex).Id+"_"+iteration.ToString(CultureInfo.InvariantCulture);v["case_id"]=suite.Get(caseIndex).Id;
            v["seed"]=suite.SeedAt(iteration).ToString(CultureInfo.InvariantCulture);v["iteration"]=iteration;v["track_file"]=suite.Get(caseIndex).TrackFile;
            v["finish_seconds"]=elapsed;v["ski_seconds"]=elapsed-rangeTime-penaltyTime;v["range_seconds"]=rangeTime;v["penalty_seconds"]=penaltyTime;
            v["hits_prone"]=events.HitsProne;v["hits_standing"]=events.HitsStanding;v["shots_prone"]=profile.ShotsPerBout;v["shots_standing"]=profile.ShotsPerBout;
            v["hit_rate"]=(events.HitsProne+events.HitsStanding)/(double)events.Shots;v["penalties"]=events.Penalties;
            v["avg_hr_bpm"]=hr/elapsed;v["max_hr_bpm"]=maxHr;v["range_entry_hr_bpm"]=entryHr/entries;
            v["anaerobic_min"]=minReserve;v["fatigue_finish"]=actor.PhysiologyState.MuscularFatigue;
            v["final_lap_speed_mps"]=finalDistance/finalTime;v["draft_seconds"]=0;v["overtakes"]=events.Overtakes;v["position"]=1;
            v["attack_value_seconds"]=null;v["draft_benefit_seconds"]=null;v["stat_marginal_seconds_per_point"]=null;
            v["effect_status"]="requires_matched_counterfactual";v["event_fnv64"]=events.Hash.ToString("x16");
            return m;
        }
    }
}

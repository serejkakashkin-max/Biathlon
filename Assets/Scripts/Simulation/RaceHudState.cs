using System;
using System.Text;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>Detached HUD values; provisional rank compares arrivals at the same checkpoint.</summary>
    public readonly struct RaceHudState
    {
        public int Lap { get; }
        public int LapCount { get; }
        public int NextRangeAfterLap { get; }
        public int Rank { get; }
        public int ComparedAthletes { get; }
        public int CheckpointOrdinal { get; }
        public double? GapSeconds { get; }
        public bool Final { get; }
        public string ShootingResults { get; }
        internal RaceHudState(PackSprintFieldSession field,int index)
        {
            var a=field.GetAthlete(index);var profile=field.Profile;
            LapCount=profile.LapCount;Lap=Math.Min(LapCount,a.Lifecycle.CompletedLaps+1);
            NextRangeAfterLap=a.ActiveBout>=0?profile.GetBout(a.ActiveBout).AfterLap:0;
            var text=new StringBuilder();for(int i=0;i<profile.BoutCount;i++)
            {
                if(i>0)text.Append(" | ");
                if(i>=a.Lifecycle.CompletedBouts){text.Append("-");continue;}
                int mask=a.GetBoutClosedMask(i),hits=0;for(int j=0;j<TargetGeometryDefinition.TargetCount;j++)if((mask&(1<<j))!=0)hits++;
                text.Append(hits).Append('/').Append(profile.ShotsPerBout);
            }
            ShootingResults=text.ToString();Final=field.IsComplete;Rank=0;ComparedAthletes=0;GapSeconds=null;CheckpointOrdinal=a.Checkpoints.Count-1;
            if(Final)
            {
                ComparedAthletes=field.Count;
                if(profile.Format==CompetitionFormat.Sprint)
                {
                    var result=field.Result;var timing=ResultTimingDefinition.FromJson(result.TimingDefinitionJson);
                    for(int i=0;i<result.Count;i++){var row=result.Get(i);if(row.AthleteId!=a.AthleteId.Value)continue;Rank=row.Rank;GapSeconds=row.GapUnits.HasValue?row.GapUnits.Value/(double)timing.UnitsPerSecond:(double?)null;break;}
                }
                else if(profile.Format==CompetitionFormat.Pursuit)
                {
                    var result=field.PursuitResult;for(int i=0;i<result.Count;i++){var row=result.Get(i);if(row.AthleteId!=a.AthleteId.Value)continue;Rank=row.Rank;GapSeconds=Seconds(row.GlobalFinish(field.TickFrequency))-Seconds(result.Get(0).GlobalFinish(field.TickFrequency));break;}
                }
                else
                {
                    var result=field.MassStartResult;for(int i=0;i<result.Count;i++){var row=result.Get(i);if(row.AthleteId!=a.AthleteId.Value)continue;Rank=row.Rank;GapSeconds=Seconds(row.GlobalFinish(field.TickFrequency))-Seconds(result.Get(0).GlobalFinish(field.TickFrequency));break;}
                }
            }
            else if(CheckpointOrdinal>=0)
            {
                Rank=1;var split=a.Checkpoints.Get(CheckpointOrdinal);var time=profile.Format==CompetitionFormat.Sprint?split.ElapsedTime:split.GlobalTime;
                for(int i=0;i<field.Count;i++)
                {
                    var other=field.GetAthlete(i);if(other.Checkpoints.Count<=CheckpointOrdinal)continue;ComparedAthletes++;
                    var candidate=other.Checkpoints.Get(CheckpointOrdinal);var t=profile.Format==CompetitionFormat.Sprint?candidate.ElapsedTime:candidate.GlobalTime;if(t.CompareTo(time)<0)Rank++;
                }
                GapSeconds=field.Timing.GapSeconds(a.AthleteId,CheckpointOrdinal);
            }
        }
        private static double Seconds(FinishTime time)=>(time.WholeTicks+time.FractionParts/(double)FinishTime.PartsPerTick)/time.TickFrequency;
    }
}

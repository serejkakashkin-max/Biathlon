using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Presentation
{
    public readonly struct CheckpointStanding
    {
        public string AthleteId {get;}
        public int Rank {get;}
        public double Seconds {get;}
        public double GapSeconds {get;}
        internal CheckpointStanding(string id,int rank,double seconds,double gap)
        {AthleteId=id;Rank=rank;Seconds=seconds;GapSeconds=gap;}
    }
    /// <summary>One timing line only. Later interval starters may change this provisional table.</summary>
    public sealed class CheckpointStandings
    {
        private readonly CheckpointStanding[] rows;
        public int Count=>rows.Length;
        public int Ordinal {get;}
        public string CheckpointId {get;}
        public string LapId {get;}
        public CheckpointStanding Get(int index)=>rows[index];

        public const int BroadcastSlots = 3;

        public int BroadcastPageStart(long elapsedPages, double approachProgress)
        {
            if (Count <= BroadcastSlots) return 0;

            int pages = Count - BroadcastSlots + 1;
            int distancePage = Math.Min(pages - 1, (int)(Math.Clamp(approachProgress, 0, 1) * pages));
            int page = (int)(Math.Max(elapsedPages, distancePage) % pages);
            return page;
        }

        public int BroadcastResultStart(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Count) throw new ArgumentOutOfRangeException(nameof(playerIndex));
            return Math.Clamp(playerIndex - 1, 0, Math.Max(0, Count - BroadcastSlots));
        }

        public CheckpointStandings(ReadOnlySpan<CheckpointSplit> arrivals,CheckpointClock clock,FinishTime? through=null)
        {
            if(clock!=CheckpointClock.Elapsed&&clock!=CheckpointClock.Global)throw new ArgumentOutOfRangeException(nameof(clock));
            var splits=new List<CheckpointSplit>();Ordinal=-1;
            foreach(var split in arrivals)
            {
                if(!split.IsValid)continue;
                if(Ordinal<0){Ordinal=split.Ordinal;CheckpointId=split.CheckpointId;LapId=split.LapId;}
                if(split.Ordinal!=Ordinal||split.CheckpointId!=CheckpointId||split.LapId!=LapId)throw new ArgumentException("Compare arrivals at the same timing line.");
                // A later arrival can be inside the same tick as the player.
                if(through.HasValue&&split.GlobalTime.CompareTo(through.Value)>0)continue;
                splits.Add(split);
            }
            FinishTime Time(CheckpointSplit s)=>clock==CheckpointClock.Elapsed?s.ElapsedTime:s.GlobalTime;
            splits.Sort((a,b)=>{int order=Time(a).CompareTo(Time(b));return order!=0?order:string.CompareOrdinal(a.Athlete.Value,b.Athlete.Value);});
            rows=new CheckpointStanding[splits.Count];int rank=1;
            for(int i=0;i<rows.Length;i++)
            {if(i>0&&Time(splits[i]).CompareTo(Time(splits[i-1]))!=0)rank=i+1;double seconds=Time(splits[i]).Seconds;
                rows[i]=new CheckpointStanding(splits[i].Athlete.Value,rank,seconds,seconds-Time(splits[0]).Seconds);}
        }
        public static CheckpointStandings Capture(PackSprintFieldSession field,int ordinal,FinishTime? through=null)
        {
            if(field==null)throw new ArgumentNullException(nameof(field));
            var arrivals=new CheckpointSplit[field.Count];
            for(int i=0;i<field.Count;i++)if(ordinal>=0&&field.GetAthlete(i).Checkpoints.Count>ordinal)arrivals[i]=field.GetAthlete(i).Checkpoints.Get(ordinal);
            return new CheckpointStandings(arrivals,field.Profile.Format==CompetitionFormat.Sprint?CheckpointClock.Elapsed:CheckpointClock.Global,through);
        }
    }
}

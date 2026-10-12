using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Replay
{
    public enum RaceHighlightKind{LeadChange,FinalRange,Overtake,CloseFinish,KeyMiss,Finish}
    [DataContract] public sealed class RaceHighlight
    {
        [DataMember(Name="tick",IsRequired=true)]public long Tick;
        [DataMember(Name="athlete_id",IsRequired=true)]public string AthleteId;
        [DataMember(Name="kind",IsRequired=true)]public RaceHighlightKind Kind;
        public RaceHighlight(long tick,string athlete,RaceHighlightKind kind){Tick=tick;AthleteId=athlete;Kind=kind;}
        internal void Validate(long maximum){if(Tick<1||Tick>maximum||string.IsNullOrWhiteSpace(AthleteId)||!Enum.IsDefined(typeof(RaceHighlightKind),Kind))throw new SerializationException("Invalid replay highlight.");}
    }
    /// <summary>Observed events/checkpoint leaders only. No invented drama or influence on outcomes.</summary>
    public sealed class RaceHighlightDetector:IRaceEventSink
    {
        private readonly List<RecordedRaceEvent> events=new List<RecordedRaceEvent>(16384);
        private readonly List<RaceHighlight> highlights=new List<RaceHighlight>(1024);
        private readonly Dictionary<string,int> bouts=new Dictionary<string,int>(StringComparer.Ordinal);
        private readonly int finalBout,frequency;
        private readonly int cooldownTicks;
        private readonly double closeFinishSeconds;
        private readonly bool interval;
        private string leader;
        private long firstFinish=-1;
        public RaceHighlightDetector(int boutCount,int tickFrequency,double closeFinishSeconds,int repeatCooldownSeconds,bool intervalStart=false)
        {if(boutCount<1||tickFrequency<1||closeFinishSeconds<0||repeatCooldownSeconds<0)throw new ArgumentException("Highlight policy required.");finalBout=boutCount-1;frequency=tickFrequency;this.closeFinishSeconds=closeFinishSeconds;cooldownTicks=repeatCooldownSeconds*tickFrequency;interval=intervalStart;}
        public void Record(RaceEvent e)
        {
            if(e.EventType==RaceEventType.CheckpointCrossed||e.EventType==RaceEventType.CompetitionStarted||e.EventType==RaceEventType.CompetitionPhaseChanged||
                e.EventType==RaceEventType.ShootingMatCaptured||e.EventType==RaceEventType.ShootingBoutCompleted||e.EventType==RaceEventType.ShotResolved||
                e.EventType==RaceEventType.OvertakeCompleted||e.EventType==RaceEventType.RaceFinished||e.EventType==RaceEventType.PenaltyLoopCompleted)
                events.Add(new RecordedRaceEvent(e));
            int bout=0;bouts.TryGetValue(e.AthleteId.Value,out bout);
            if(e.EventType==RaceEventType.ShootingMatCaptured&&bout==finalBout)Add(e.Tick.Value,e.AthleteId.Value,RaceHighlightKind.FinalRange);
            else if(e.EventType==RaceEventType.ShootingBoutCompleted)bouts[e.AthleteId.Value]=bout+1;
            else if(e.EventType==RaceEventType.ShotResolved&&e.Payload<=0&&bout==finalBout)Add(e.Tick.Value,e.AthleteId.Value,RaceHighlightKind.KeyMiss);
            else if(e.EventType==RaceEventType.OvertakeCompleted)Add(e.Tick.Value,e.AthleteId.Value,RaceHighlightKind.Overtake);
            else if(e.EventType==RaceEventType.RaceFinished)
            {
                Add(e.Tick.Value,e.AthleteId.Value,RaceHighlightKind.Finish);
                if(firstFinish<0)firstFinish=e.Tick.Value;else if(!interval&&(e.Tick.Value-firstFinish)/(double)frequency<=closeFinishSeconds)Add(e.Tick.Value,e.AthleteId.Value,RaceHighlightKind.CloseFinish);
            }
        }
        public void ObserveLeader(long tick,string athlete)
        {if(tick<1||string.IsNullOrWhiteSpace(athlete))return;if(leader!=null&&leader!=athlete)Add(tick,athlete,RaceHighlightKind.LeadChange);leader=athlete;}
        public void ObserveRegisteredFinish(string athlete,double gapSeconds)
        {
            if(gapSeconds<0||gapSeconds>closeFinishSeconds)return;
            for(int i=0;i<events.Count;i++)if(events[i].AthleteId==athlete&&events[i].Type==RaceEventType.RaceFinished){Add(events[i].Tick,athlete,RaceHighlightKind.CloseFinish);return;}
        }
        private void Add(long tick,string athlete,RaceHighlightKind kind)
        {
            for(int i=highlights.Count-1;i>=0;i--){var h=highlights[i];if(h.Tick<tick-cooldownTicks)break;if(h.AthleteId==athlete&&h.Kind==kind)return;}
            highlights.Add(new RaceHighlight(tick,athlete,kind));
        }
        public RecordedRaceEvent[] Events()=>events.ToArray();
        public RaceHighlight[] Highlights(){var result=highlights.ToArray();Array.Sort(result,(a,b)=>a.Tick.CompareTo(b.Tick));return result;}
    }
}

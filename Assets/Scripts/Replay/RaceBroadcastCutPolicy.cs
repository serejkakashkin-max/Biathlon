using System;
namespace Biathlon.Replay
{
    /// <summary>Holds a readable shot during dense events; only a higher-priority moment interrupts it.</summary>
    public sealed class RaceBroadcastCutPolicy
    {
        private readonly long holdTicks;
        private int priority;
        public long UntilTick {get;private set;}
        public string ShotId {get;private set;}="track";
        public RaceBroadcastCutPolicy(int frequency,double holdSeconds)
        {if(frequency<1||!double.IsFinite(holdSeconds)||holdSeconds<=0)throw new ArgumentException("Positive shot duration required.");holdTicks=Math.Max(1,(long)Math.Round(frequency*holdSeconds));}
        public bool TrySelect(RaceHighlight moment)
        {
            if(moment==null)throw new ArgumentNullException(nameof(moment));
            int next=moment.Kind==RaceHighlightKind.CloseFinish?4:moment.Kind==RaceHighlightKind.Finish?3:moment.Kind==RaceHighlightKind.FinalRange||moment.Kind==RaceHighlightKind.KeyMiss?2:1;
            if(moment.Tick<UntilTick&&next<=priority)return false;
            priority=next;UntilTick=moment.Tick+holdTicks;ShotId=next>=3?"finish":next==2?"range":"wide";return true;
        }
        public void Reset(){priority=0;UntilTick=0;ShotId="track";}
    }
}

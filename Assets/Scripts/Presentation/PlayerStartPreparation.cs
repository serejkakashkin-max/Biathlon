using System;
using Biathlon.Domain;
namespace Biathlon.Presentation
{
    /// <summary>Local preparation time and a personal countdown on the accepted race clock.
    /// A skip target is a boundary only: the caller must execute every intervening race tick.</summary>
    public sealed class PlayerStartPreparation
    {
        private readonly RaceStartCountdown hold;
        private readonly int frequency;
        public long StartTick {get;}
        public long SkipTargetTick {get;}
        public double CountdownSeconds {get;}
        public bool HoldsRace=>hold!=null&&!hold.Ready;
        public PlayerStartPreparation(RaceStartDefinition definition,int tickFrequency,long scheduledStartTick)
        {
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            if(tickFrequency<=0||scheduledStartTick<1)throw new ArgumentOutOfRangeException();
            double ticks=definition.CountdownSeconds*tickFrequency;
            if(ticks!=Math.Truncate(ticks))throw new ArgumentException("Countdown must fit the race tick frequency exactly.");
            frequency=tickFrequency;StartTick=scheduledStartTick;CountdownSeconds=definition.CountdownSeconds;
            SkipTargetTick=Math.Max(0,StartTick-(long)ticks);
            // If the player starts immediately (or within three seconds), prepare
            // before the first gun without altering any registered start gaps.
            double seconds=Math.Max(0,CountdownSeconds-(StartTick-1)/(double)frequency);
            if(seconds>0)hold=new RaceStartCountdown(seconds);
        }
        public void AdvanceHold(double seconds)
        {if(hold==null)throw new InvalidOperationException("This start has no local hold.");hold.Advance(seconds);}
        public double Remaining(long acceptedTick)
        {
            if(acceptedTick<0)throw new ArgumentOutOfRangeException(nameof(acceptedTick));
            if(acceptedTick>=StartTick)return 0;
            return (HoldsRace?hold.Remaining:0)+Math.Max(0,StartTick-Math.Max(1,acceptedTick))/(double)frequency;
        }
        public double CountdownRemaining(long acceptedTick)
        {double remaining=Remaining(acceptedTick);return remaining<=CountdownSeconds?remaining:0;}
        public bool CanSkip(long acceptedTick)=>!HoldsRace&&acceptedTick>=0&&acceptedTick<SkipTargetTick&&Remaining(acceptedTick)>CountdownSeconds;
    }
}

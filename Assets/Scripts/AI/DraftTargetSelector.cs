using System;
using Biathlon.Domain;
using Biathlon.Simulation;
namespace Biathlon.AI
{
    public enum DraftSelectionReason { NoWake=0, Acquired=1, Retained=2, HoldDuration=3, ImprovementTooSmall=4, Switched=5, LostTarget=6 }
    public readonly struct DraftSelectionFrame
    {
        public SimulationTick Tick { get; }
        public DraftOutput Selected { get; }
        public DraftOutput BestAvailable { get; }
        public DraftSelectionReason Reason { get; }
        internal DraftSelectionFrame(SimulationTick tick,DraftOutput selected,DraftOutput best,DraftSelectionReason reason)
        { Tick=tick; Selected=selected; BestAvailable=best; Reason=reason; }
    }
    public sealed class DraftTargetSelector
    {
        private readonly DraftSelectionDefinition definition;
        private readonly DraftingSystem drafting;
        private readonly AthleteId athlete;
        private readonly long holdTicks;
        private long selectedAt;
        public SimulationTick Tick { get; private set; }
        public DraftSelectionFrame LastFrame { get; private set; }
        public DraftTargetSelector(DraftSelectionDefinition definition,DraftDefinition draft,AthleteId athlete,int frequency,SimulationTick initialTick=default)
        {
            this.definition=definition??throw new ArgumentNullException(nameof(definition)); drafting=new DraftingSystem(draft);
            if(!athlete.IsValid || frequency<=0 || frequency>10000) throw new ArgumentException("Invalid draft selector actor/clock.");
            this.athlete=athlete; holdTicks=checked((long)Math.Ceiling((decimal)definition.MinimumHoldSeconds*frequency)); Tick=initialTick;
        }
        public DraftSelectionFrame Evaluate(SimulationTick tick,int athleteIndex,ReadOnlySpan<SkiPlacement> placements,TrackSample track,
            double headwind=0,double crosswind=0)
        {
            if(tick.Value<=Tick.Value || athleteIndex<0 || athleteIndex>=placements.Length || !placements[athleteIndex].AthleteId.Equals(athlete))
                throw new ArgumentException("Increasing observation tick and matching physical actor required.");
            for(int i=0;i<placements.Length;i++)
            {
                if(!placements[i].IsValid) throw new ArgumentException("Invalid physical placement.");
                for(int j=0;j<i;j++) if(placements[i].AthleteId.Equals(placements[j].AthleteId)) throw new ArgumentException("Duplicate physical athlete.");
            }
            var self=placements[athleteIndex]; var best=drafting.Evaluate(self,placements,track,headwind,crosswind);
            var retained=default(DraftOutput); var oldId=LastFrame.Selected.LeaderId;
            for(int i=0;i<placements.Length;i++) if(placements[i].AthleteId.Equals(oldId))
                retained=drafting.Evaluate(self,placements.Slice(i,1),track,headwind,crosswind);
            var selected=best; DraftSelectionReason reason;
            if(best.Strength<=0) reason=oldId.IsValid?DraftSelectionReason.LostTarget:DraftSelectionReason.NoWake;
            else if(retained.Strength<=0) reason=DraftSelectionReason.Acquired;
            else if(best.LeaderId.Equals(oldId)) reason=DraftSelectionReason.Retained;
            else if(tick.Value-selectedAt<holdTicks) { selected=retained; reason=DraftSelectionReason.HoldDuration; }
            else if(best.Strength<=retained.Strength+definition.SwitchStrengthMargin) { selected=retained; reason=DraftSelectionReason.ImprovementTooSmall; }
            else reason=DraftSelectionReason.Switched;
            long since=selected.LeaderId.Equals(oldId)?selectedAt:tick.Value;
            var frame=new DraftSelectionFrame(tick,selected,best,reason);
            Tick=tick; selectedAt=since; LastFrame=frame; return frame;
        }
    }
}

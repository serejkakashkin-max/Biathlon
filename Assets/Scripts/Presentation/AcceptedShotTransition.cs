namespace Biathlon.Presentation
{
    /// <summary>Read-only single-shot delta, including final-shot mat release. No geometry or RNG.</summary>
    public static class AcceptedShotTransition
    {
        public static RangeShotMark Result(in RangeBroadcastObservation before,in RangeBroadcastObservation after)
        {
            if(!before.Active||before.AthleteId!=after.AthleteId||string.IsNullOrEmpty(before.AthleteId))return RangeShotMark.Pending;
            int shots,mask;
            if(after.Active&&after.CompletedBouts==before.CompletedBouts){shots=after.Shots;mask=after.Mask;}
            else if(!after.Active&&after.CompletedBouts==before.CompletedBouts+1){shots=5;mask=after.LastCompletedMask;}
            else return RangeShotMark.Pending;
            if(before.Shots<0||before.Shots>=5||shots-before.Shots!=1||before.Mask<0||before.Mask>31||mask<0||mask>31||
                (before.Mask&~mask)!=0||RangeBroadcastRow.Hits(before.Mask)>before.Shots)return RangeShotMark.Pending;
            int added=RangeBroadcastRow.Hits(mask)-RangeBroadcastRow.Hits(before.Mask);
            return added==1?RangeShotMark.Hit:added==0?RangeShotMark.Miss:RangeShotMark.Pending;
        }
    }
}

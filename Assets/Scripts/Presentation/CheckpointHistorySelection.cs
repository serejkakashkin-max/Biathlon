using System;

namespace Biathlon.Presentation
{
    /// <summary>Presentation selection only; timings remain in the accepted checkpoint trackers.</summary>
    public sealed class CheckpointHistorySelection
    {
        public int Latest {get;private set;}=-1;
        public int Selected {get;private set;}=-1;
        public bool FollowsLatest {get;private set;}=true;
        private long tick=-1;
        public void Observe(long acceptedTick,int latestOrdinal)
        {
            if(acceptedTick<0||latestOrdinal< -1)throw new ArgumentOutOfRangeException();
            if(acceptedTick<tick||latestOrdinal<Latest)Reset();
            tick=acceptedTick;Latest=latestOrdinal;
            if(FollowsLatest)Selected=Latest;
        }
        public bool Move(int direction)
        {
            if(direction!= -1&&direction!=1)throw new ArgumentOutOfRangeException(nameof(direction));
            if(Latest<0)return false;
            int next=Math.Max(0,Math.Min(Latest,Selected+direction));
            if(next==Selected)return false;
            Selected=next;FollowsLatest=Selected==Latest;return true;
        }
        public void FollowLatest(){FollowsLatest=true;Selected=Latest;}
        public void Reset(){tick=-1;Latest=Selected=-1;FollowsLatest=true;}
    }
}

using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public sealed partial class ManualScopedWorldRoster
    {
        private long[] startReleaseTicks,pendingStartReleaseTicks;
        private bool[] startReleaseHeld,pendingStartReleaseHeld;
        private int[] startReleaseOrder,startReleaseChanges;
        private long startReleasePreviewTick=-1;
        public bool HasStartReleaseGate {get;private set;}
        private void BindStartReleaseGate(PackSprintFieldSession field)
        {
            HasStartReleaseGate=field.StartContinuationVersion>=3&&field.StartGrid==null;
            if(!HasStartReleaseGate)return;
            startReleaseTicks=new long[Count];pendingStartReleaseTicks=new long[Count];
            startReleaseHeld=new bool[Count];pendingStartReleaseHeld=new bool[Count];
            startReleaseOrder=new int[Count];startReleaseChanges=new int[Count];
            for(int i=0;i<Count;i++)
            {
                startReleaseTicks[i]=pendingStartReleaseTicks[i]=-1;int at=i;
                while(at>0&&scheduledPresenceStarts[startReleaseOrder[at-1]]>scheduledPresenceStarts[i])
                {startReleaseOrder[at]=startReleaseOrder[at-1];at--;}
                startReleaseOrder[at]=i;
            }
            ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,
                "scheduled_start_physical_release_v1;official_clock_unchanged;stationary_staging_until_actual_spawn_body_fits;stable_scheduled_then_field_order;no_pose_snap_or_contact_exclusion_after_release;accepted_tick_atomic_release_and_wait_events");
        }
        // One pure preview of the next scheduled release set. Scratch decisions
        // cannot escape on an aborted/rejected tick; Commit publishes them.
        private void PreviewStartReleases(SimulationTick tick)
        {
            if(!HasStartReleaseGate||startReleasePreviewTick==tick.Value)return;
            Array.Copy(startReleaseTicks,pendingStartReleaseTicks,Count);Array.Clear(pendingStartReleaseHeld,0,Count);
            for(int k=0;k<Count;k++)
            {
                int i=startReleaseOrder[k];if(tick.Value<=scheduledPresenceStarts[i]||pendingStartReleaseTicks[i]>=0)continue;
                var a=accepted[i];bool clear=true;
                for(int j=0;j<Count;j++)
                {
                    if(i==j||pendingStartReleaseTicks[j]<0||!HasScheduledPhysicalPresence(j,tick))continue;
                    var b=accepted[j];
                    var gap=ownedContactGeometry.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,default,1),new ManualSkiContactBody(b.Athlete,b.Pose,default,1));
                    if(gap.Status==SkiContactPreviewStatus.Penetrating||gap.GapMeters< -ownedContactTolerance){clear=false;break;}
                }
                if(clear)pendingStartReleaseTicks[i]=tick.Value;else pendingStartReleaseHeld[i]=true;
            }
            startReleasePreviewTick=tick.Value;
        }
        public bool IsStartReleaseHeld(int index,SimulationTick tick)
        {
            if(index<0||index>=Count||tick.Value!=Tick.Value+1)throw new ArgumentOutOfRangeException(nameof(index));
            if(!HasStartReleaseGate)return false;
            PreviewStartReleases(tick);return pendingStartReleaseHeld[index];
        }
        public bool IsStartReleaseHeld(int index)=>HasStartReleaseGate&&startReleaseHeld[index];
        public long GetStartReleaseTick(int index)=>HasStartReleaseGate?startReleaseTicks[index]:-1;
        public int GetStartReleaseChange(int index)=>HasStartReleaseGate?startReleaseChanges[index]:0;
        public int GetStartReleaseWaitTicks(int index)=>HasStartReleaseGate?checked((int)Math.Max(0,
            (startReleaseTicks[index]>=0?startReleaseTicks[index]:Tick.Value+1)-scheduledPresenceStarts[index]-1)):0;
        private void CommitStartReleases(SimulationTick tick)
        {
            if(!HasStartReleaseGate)return;
            if(startReleasePreviewTick!=tick.Value)throw new InvalidOperationException("Missing attested scheduled-start release preview.");
            for(int i=0;i<Count;i++)
            {
                startReleaseChanges[i]=startReleaseTicks[i]<0&&pendingStartReleaseTicks[i]>=0?2:
                    pendingStartReleaseHeld[i]&&!startReleaseHeld[i]?1:0;
                startReleaseTicks[i]=pendingStartReleaseTicks[i];startReleaseHeld[i]=pendingStartReleaseHeld[i];
            }
        }
    }
}

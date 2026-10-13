using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class ProductionVenueHost
    {
        public bool RequestSkipStartWait()
        {
            if(!CanSkipStartWait)return false;
            ski.ReleaseHeldInput();shot.ReleaseHeldInput();
            frameClock=new FixedTickFrameClock(frameBudget,Runner.Field.TickFrequency);
            StartWaitFastForwarding=true;return true;
        }
        public void AdvanceStartWaitFrame()
        {
            if(!StartWaitFastForwarding||HasSimulationFault)return;
            frameWork.Restart();batchFrame=true;int completed=0;
            try
            {
                for(int i=0;i<startDefinition.SkipMaximumSteps&&Runner.Field.Tick.Value<startPreparation.SkipTargetTick;i++)
                {
                    StepDeviceTick();completed++;
                    if(frameWork.Elapsed.TotalMilliseconds>=startDefinition.SkipWorkBudgetMilliseconds)break;
                }
                if(Runner.Field.Tick.Value==startPreparation.SkipTargetTick)
                {
                    StartWaitFastForwarding=false;
                    frameClock=new FixedTickFrameClock(frameBudget,Runner.Field.TickFrequency);
                }
            }
            catch(System.Exception failure){HasSimulationFault=true;StartWaitFastForwarding=false;Debug.LogException(failure,this);}
            finally{batchFrame=false;LastSimulationFrameMilliseconds=frameWork.Elapsed.TotalMilliseconds;}
            // The journal still observes every accepted tick; rendering is coalesced.
            if(completed>0)
            {
                // A coalesced batch is a visual discontinuity. Do not interpolate
                // backwards across its skipped interval on the next normal frame.
                if(!StartWaitFastForwarding)Feed.ResetRace();
                Feed.Publish(RacePresentationSnapshot.Capture(Runner.Field));Feed.SetInterpolationAlpha(1);PublishPresentation(false);
            }
        }
    }
}

using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class ProductionVenueHost
    {
        public bool FastFinish {get;private set;}
        private RaceReviewSettings finishPolicy;
        public void RequestFastFinish()
        {
            if(Runner==null||!Runner.Field.GetAthlete(PlayerIndex).HasFinished)throw new InvalidOperationException("Finish your race before simulating the remaining field.");
            finishPolicy=finishPolicy??RaceReviewSettings.Load();FastFinish=true;
        }
        private void StepFinishedPlayerTick()
        {
            Runner.StepRemainingField(PlayerIndex);
        }
        public void AdvanceFinishedFieldFrame()
        {
            if(!FastFinish||HasSimulationFault||Runner.Field.IsComplete)return;
            frameWork.Restart();
            try
            {
                for(int i=0;i<finishPolicy.finish_max_steps&&!Runner.Field.IsComplete;i++)
                {StepFinishedPlayerTick();if(frameWork.Elapsed.TotalMilliseconds>=finishPolicy.finish_work_budget_ms)break;}
                // One detached observation per frame; all authority ticks and replay commands remain intact.
                Feed.Publish(RacePresentationSnapshot.Capture(Runner.Field));Feed.SetInterpolationAlpha(1);PublishPresentation(false);
            }
            catch(Exception failure){HasSimulationFault=true;Debug.LogException(failure,this);}
            LastSimulationFrameMilliseconds=frameWork.Elapsed.TotalMilliseconds;
        }
        public void EnterSpectatorView(Camera view)
        {
            if(view==null||!Runner.Field.GetAthlete(PlayerIndex).HasFinished)throw new InvalidOperationException("A finished player and spectator camera are required.");
            range.enabled=false;range.AimCamera.enabled=false;GameplayHud.enabled=false;Guidance.enabled=false;
            ChaseCamera.enabled=false;ChaseCamera.GetComponent<AthleteChaseCamera>().enabled=false;
            var source=ChaseCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            var target=view.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            target.renderPostProcessing=source.renderPostProcessing;target.volumeLayerMask=source.volumeLayerMask;target.antialiasing=source.antialiasing;
            view.farClipPlane=ChaseCamera.farClipPlane;Proxies.SetViewCamera(view);Environment?.SetViewCamera(view);
        }
    }
}

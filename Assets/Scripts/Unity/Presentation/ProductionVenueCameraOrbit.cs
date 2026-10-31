using UnityEngine;
using UnityEngine.InputSystem;
namespace Biathlon.Unity.Presentation
{
    public sealed partial class ProductionVenueHost
    {
        private AthleteChaseCamera orbitCamera;
        private bool orbitMouseBlockedUntilRelease;
        public void CaptureCameraOrbit(Mouse mouse)
        {
            if(orbitCamera==null||Runner==null)return;
            var actor=Runner.Field.GetAthlete(PlayerIndex);bool held=mouse?.leftButton.isPressed==true;
            bool onCourse=Runner.Field.IsOnCourse(PlayerIndex);
            bool allowed=isActiveAndEnabled&&GameplayCursorCaptured&&!HasSimulationFault&&onCourse&&!actor.HasFinished&&actor.ActiveShooting==null&&!WaitingForPlayerStart&&
                ChaseCamera.enabled&&orbitCamera.enabled&&GameplayHud?.CheckpointTableVisible!=true&&GameplayHud?.BroadcastVisible!=true&&GameplayHud?.HelpVisible!=true;
            if(allowed&&Review!=null&&mouse!=null&&Review.ContainsScreenPointer(mouse.position.ReadValue()))allowed=false;
            if(!allowed){orbitMouseBlockedUntilRelease=held;orbitCamera.CancelOrbit();return;}
            if(!held)orbitMouseBlockedUntilRelease=false;
            orbitCamera.SetOrbitInput(held&&!orbitMouseBlockedUntilRelease,held&&!orbitMouseBlockedUntilRelease?mouse.delta.ReadValue():Vector2.zero);
        }
        private void CancelCameraOrbit(){orbitMouseBlockedUntilRelease=true;orbitCamera?.CancelOrbit();}
    }
}

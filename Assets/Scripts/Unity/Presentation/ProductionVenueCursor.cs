using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public sealed partial class ProductionVenueHost
    {
        private bool ownsGameplayCursor;
        public bool GameplayCursorCaptured {get;private set;}
        private void OnEnable(){RefreshGameplayCursor(Application.isFocused);}
        private void OnDisable(){CancelCameraOrbit();ReleaseGameplayCursor();}
        private void RefreshGameplayCursor(bool focused)
        {
            bool gameplay=isActiveAndEnabled&&Runner!=null&&!Runner.Field.GetAthlete(PlayerIndex).HasFinished;
            if(!gameplay){ReleaseGameplayCursor();return;}
            ownsGameplayCursor=true;ApplyGameplayCursor(focused);
        }
        private void ApplyGameplayCursor(bool captured)
        {
            GameplayCursorCaptured=captured;
            var mode=captured?CursorLockMode.Locked:CursorLockMode.None;
            if(Cursor.lockState!=mode)Cursor.lockState=mode;
            if(Cursor.visible==captured)Cursor.visible=!captured;
        }
        private void ReleaseGameplayCursor()
        {if(!ownsGameplayCursor)return;ownsGameplayCursor=false;ApplyGameplayCursor(false);}
        /// <summary>Shared focus boundary for cursor and held device intent.</summary>
        public void SetInputFocus(bool focused)
        {ski?.SetInputFocus(focused);shot?.SetInputFocus(focused);if(!focused)CancelCameraOrbit();RefreshGameplayCursor(focused);}
    }
}

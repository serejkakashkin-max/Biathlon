using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in zoom priority for broadcast views. Physical cull/VFX distances stay unchanged.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ProjectionAwareLod : MonoBehaviour
    {
        // Existing distance thresholds were authored for a 60-degree perspective view.
        public static float Scale(Camera camera)
        {
            if(camera==null||camera.orthographic||!camera.TryGetComponent<ProjectionAwareLod>(out var policy)||!policy.isActiveAndEnabled)return 1;
            return Mathf.Min(1,Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad*.5f)/Mathf.Tan(60*Mathf.Deg2Rad*.5f));
        }
    }
}

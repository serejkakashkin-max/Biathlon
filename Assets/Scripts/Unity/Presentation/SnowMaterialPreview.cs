using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public sealed class SnowMaterialPreview : MonoBehaviour
    {
        public Camera View;
        private void Start() => gameObject.AddComponent<RenderQualityPresentation>().Initialize(View);
        private void OnGUI()
        {
            GUI.Box(new Rect(16,16,420,48), "SNOW MATERIAL LAB  |  COLD / WET\nOriginal procedural PBR · world-space grain");
        }
    }
}

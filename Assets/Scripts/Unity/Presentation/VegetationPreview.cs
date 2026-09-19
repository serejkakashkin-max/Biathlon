using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed class VegetationPreview : MonoBehaviour
    {
        public Camera View;
        public string Tier="balanced";
        public bool Animate=true;
        public float FrozenTime=12;
        public VegetationPresentation Forest {get;private set;}
        private RenderQualityPresentation quality;
        public static Vector3[] Route => new[]{new Vector3(0,0,-80),new Vector3(0,0,80)};
        private void Start(){Forest=gameObject.AddComponent<VegetationPresentation>();Forest.Initialize(Route);quality=gameObject.AddComponent<RenderQualityPresentation>();quality.Initialize(View);}
        private void Update(){if(quality.TierId!=Tier)quality.Apply(Tier);Forest.Render(View,Tier,Animate?Time.time:FrozenTime);}
    }
}

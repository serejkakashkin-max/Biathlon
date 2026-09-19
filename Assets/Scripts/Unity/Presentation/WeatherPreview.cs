using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed class WeatherPreview : MonoBehaviour
    {
        public Camera View;public Light Key;
        public string Preset="overcast",Tier="balanced";
        public bool Animate=true;public float FrozenTime=12;public Vector2 Wind=new Vector2(2,.5f);
        public WeatherPresentation Weather {get;private set;}
        public VegetationPresentation Forest {get;private set;}
        private RenderQualityPresentation quality;
        private void Start(){quality=gameObject.AddComponent<RenderQualityPresentation>();quality.Initialize(View);Forest=gameObject.AddComponent<VegetationPresentation>();Forest.Initialize(VegetationPreview.Route);Weather=gameObject.AddComponent<WeatherPresentation>();Weather.Initialize(View,Key);}
        private void Update(){if(quality.TierId!=Tier)quality.Apply(Tier);float time=Animate?Time.time:FrozenTime;Weather.Render(Preset,Tier,time,Wind,0);Forest.Render(View,Tier,time,Wind);}
    }
}

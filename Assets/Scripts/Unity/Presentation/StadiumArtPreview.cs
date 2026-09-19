using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed class StadiumArtPreview : MonoBehaviour
    {
        public Camera View;public Light Key;public string Tier="balanced",WeatherPreset="clear";
        private RenderQualityPresentation quality;private WeatherPresentation weather;
        private void Start(){quality=gameObject.AddComponent<RenderQualityPresentation>();quality.Initialize(View);weather=gameObject.AddComponent<WeatherPresentation>();weather.Initialize(View,Key);}
        private void Update(){if(quality.TierId!=Tier)quality.Apply(Tier);weather.Render(WeatherPreset,Tier,12,new Vector2(1,.3f),0);}
    }
}

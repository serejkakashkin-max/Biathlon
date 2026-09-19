using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class WeatherTests
    {
        [Test] public void SnowSamplingIsBoundedRewindableAndUsesExplicitWind()
        {
            var d=WeatherVisualDefinition.Load();var center=new Vector3(123,3,-81);var wind=new Vector2(2,-1);
            for(int i=0;i<d.max_particles;i++){var a=d.Sample(i,12,center,wind);d.Sample(i,19,center,wind);Assert.That(d.Sample(i,12,center,wind),Is.EqualTo(a));Assert.That(Mathf.Abs(a.x-center.x),Is.LessThanOrEqualTo(d.volume_width_m/2));Assert.That(Mathf.Abs(a.z-center.z),Is.LessThanOrEqualTo(d.volume_width_m/2));Assert.That(Mathf.Abs(a.y-center.y),Is.LessThanOrEqualTo(d.volume_height_m/2));}
            var p=d.Sample(0,0,Vector3.zero,wind);var q=d.Sample(0,.01,Vector3.zero,wind);Assert.That(q.x-p.x,Is.EqualTo(.02f).Within(.00001));Assert.That(q.z-p.z,Is.EqualTo(-.01f).Within(.00001));Assert.That(q.y-p.y,Is.EqualTo(-d.fall_speed_mps*.01f).Within(.00001));
            Assert.That(d.Count("low",0),Is.Zero);
        }
        [Test] public void PresetsAndDensityRejectInvalidValues()
        {
            var d=WeatherVisualDefinition.Load();Assert.That(d.Count("high",1),Is.EqualTo(d.max_particles));Assert.That(d.Count("low",1),Is.LessThan(d.Count("balanced",1)));Assert.That(d.Count("balanced",d.Get("clear").snowfall),Is.Zero);
            Assert.Throws<ArgumentException>(()=>d.Sample(0,double.NaN,Vector3.zero,Vector2.zero));Assert.Throws<ArgumentException>(()=>d.Sample(0,0,Vector3.zero,new Vector2(float.NaN,0)));Assert.Throws<ArgumentException>(()=>d.Get("unknown"));
            var json=Resources.Load<TextAsset>("Biathlon/weather_visual_v1").text;Assert.Throws<ArgumentException>(()=>WeatherVisualDefinition.FromJson(json.Replace("1600","10000")));
        }
        [Test] public void WeatherLeaseRestoresSceneCameraLightAndRejectsConcurrentOwnership()
        {
            var go=new GameObject("weather test");var other=new GameObject("other weather");var camGo=new GameObject("weather camera");var lightGo=new GameObject("weather light");
            var oldSky=RenderSettings.skybox;var oldFog=RenderSettings.fog;var oldMode=RenderSettings.fogMode;var oldStart=RenderSettings.fogStartDistance;var oldEnd=RenderSettings.fogEndDistance;var oldAmbient=RenderSettings.ambientSkyColor;
            try
            {
                var cam=camGo.AddComponent<Camera>();cam.enabled=false;cam.transform.position=new Vector3(0,2,-10);cam.fieldOfView=53;cam.clearFlags=CameraClearFlags.SolidColor;var background=cam.backgroundColor;
                var light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.23f;light.shadowStrength=.77f;var lightColor=light.color;
                var weather=go.AddComponent<WeatherPresentation>();weather.Initialize(cam,light);var second=other.AddComponent<WeatherPresentation>();Assert.Throws<InvalidOperationException>(()=>second.Initialize(cam,light));
                weather.Render("snowfall","high",12,new Vector2(2,1),0);Assert.That(weather.SubmittedFlakes,Is.InRange(100,1600));Assert.That(cam.fieldOfView,Is.EqualTo(53));Assert.That(cam.transform.position,Is.EqualTo(new Vector3(0,2,-10)));Assert.That(RenderSettings.skybox,Is.Not.SameAs(oldSky));
                var fog=RenderSettings.fogEndDistance;Assert.Throws<ArgumentException>(()=>weather.Render("clear","high",double.NaN,Vector2.zero,0));Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(fog));
                weather.Render("clear","low",12,Vector2.zero,0);Assert.That(weather.SubmittedFlakes,Is.Zero);
                weather.enabled=false;Assert.That(RenderSettings.skybox,Is.SameAs(oldSky));Assert.That(RenderSettings.fog,Is.EqualTo(oldFog));Assert.That(RenderSettings.fogMode,Is.EqualTo(oldMode));Assert.That(RenderSettings.fogStartDistance,Is.EqualTo(oldStart));Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(oldEnd));Assert.That(RenderSettings.ambientSkyColor,Is.EqualTo(oldAmbient));Assert.That(light.intensity,Is.EqualTo(1.23f));Assert.That(light.color,Is.EqualTo(lightColor));Assert.That(light.shadowStrength,Is.EqualTo(.77f));Assert.That(cam.clearFlags,Is.EqualTo(CameraClearFlags.SolidColor));Assert.That(cam.backgroundColor,Is.EqualTo(background));
                weather.enabled=true;weather.Render("overcast","balanced",0,Vector2.zero,0);Assert.That(weather.PresetId,Is.EqualTo("overcast"));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(other);Object.DestroyImmediate(camGo);Object.DestroyImmediate(lightGo);}
        }
        [Test] public void WarmedSnowfallMeshAndSceneApplicationAllocateNoManagedMemory()
        {
            var go=new GameObject("weather allocation");var camGo=new GameObject("weather camera");var lightGo=new GameObject("weather light");
            try{var cam=camGo.AddComponent<Camera>();cam.enabled=false;cam.transform.position=new Vector3(0,2,0);var key=lightGo.AddComponent<Light>();key.type=LightType.Directional;var weather=go.AddComponent<WeatherPresentation>();weather.Initialize(cam,key);for(int i=0;i<30;i++)weather.Render("snowfall","high",i,Vector2.right,0);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)weather.Render("snowfall","high",i,Vector2.right,0);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);}
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(camGo);Object.DestroyImmediate(lightGo);}
        }
    }
}

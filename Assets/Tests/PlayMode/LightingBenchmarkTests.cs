using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class LightingBenchmarkTests
    {
        [Test] public void FourLightingProfilesValidateSolarAnglesAndColors()
        {
            var d=LightingVisualDefinition.Load();Assert.That(d.presets.Length,Is.EqualTo(4));Assert.That(d.Get("dawn").sun_elevation,Is.LessThan(d.Get("clear_cold").sun_elevation));Assert.That(d.Get("overcast").recommended_weather,Is.EqualTo("overcast"));
            Assert.Throws<ArgumentException>(()=>d.Get("unknown"));d.Get("dawn").sun_azimuth=float.NaN;Assert.Throws<ArgumentException>(()=>LightingVisualDefinition.FromJson(JsonUtility.ToJson(d)));
        }
        [Test] public void SingleWeatherOwnerComposesLightingWithoutChangingCameraAndRestoresOrientation()
        {
            var go=new GameObject("lighting");var camGo=new GameObject("camera");var sunGo=new GameObject("sun");
            var fog=RenderSettings.fogColor;var ambient=RenderSettings.ambientSkyColor;
            try
            {
                var c=camGo.AddComponent<Camera>();c.enabled=false;c.transform.position=new Vector3(1,2,3);c.transform.rotation=Quaternion.Euler(5,10,0);c.fieldOfView=52;var cameraRotation=c.transform.rotation;
                var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(32,18,7);var initial=sun.transform.rotation;sun.intensity=1.27f;var tint=sun.color;
                var owner=go.AddComponent<WeatherPresentation>();owner.Initialize(c,sun);var lighting=LightingVisualDefinition.Load();var weather=WeatherVisualDefinition.Load();
                foreach(var profile in lighting.presets)
                {
                    owner.Render(profile.recommended_weather,"balanced",12,Vector2.zero,0,profile);Assert.That(Quaternion.Angle(sun.transform.rotation,Quaternion.Euler(profile.sun_elevation,profile.sun_azimuth,0)),Is.LessThan(.001f));
                    Assert.That(sun.intensity,Is.EqualTo(weather.Get(profile.recommended_weather).light_intensity*profile.sun_gain).Within(.00001));Assert.That(c.transform.position,Is.EqualTo(new Vector3(1,2,3)));Assert.That(c.transform.rotation,Is.EqualTo(cameraRotation));Assert.That(c.fieldOfView,Is.EqualTo(52));
                }
                var before=sun.transform.rotation;var invalid=lighting.Get("dawn");invalid.sun_gain=float.NaN;Assert.Throws<ArgumentException>(()=>owner.Render("clear","high",12,Vector2.zero,0,invalid));Assert.That(sun.transform.rotation,Is.EqualTo(before));
                owner.Render("clear","high",12,Vector2.zero,0);Assert.That(Quaternion.Angle(sun.transform.rotation,initial),Is.LessThan(.001f));
                owner.enabled=false;Assert.That(Quaternion.Angle(sun.transform.rotation,initial),Is.LessThan(.001f));Assert.That(sun.intensity,Is.EqualTo(1.27f));Assert.That(sun.color,Is.EqualTo(tint));Assert.That(RenderSettings.fogColor,Is.EqualTo(fog));Assert.That(RenderSettings.ambientSkyColor,Is.EqualTo(ambient));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(camGo);Object.DestroyImmediate(sunGo);}
        }
        [Test] public void WarmedLightingApplicationAllocatesNothingAndRewindsExactly()
        {
            var go=new GameObject("lighting");var camGo=new GameObject("camera");var sunGo=new GameObject("sun");
            try
            {
                var c=camGo.AddComponent<Camera>();c.enabled=false;var sun=sunGo.AddComponent<Light>();sun.type=LightType.Directional;var owner=go.AddComponent<WeatherPresentation>();owner.Initialize(c,sun);var d=LightingVisualDefinition.Load();var dawn=d.Get("dawn");
                owner.Render("clear","high",12,Vector2.zero,0,dawn);var first=sun.transform.rotation;var firstFog=RenderSettings.fogColor;owner.Render("clear","high",12,Vector2.zero,0,d.Get("late_day"));owner.Render("clear","high",12,Vector2.zero,0,dawn);Assert.That(sun.transform.rotation,Is.EqualTo(first));Assert.That(RenderSettings.fogColor,Is.EqualTo(firstFog));
                for(int i=0;i<20;i++)owner.Render("clear","high",12,Vector2.zero,0,dawn);long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)owner.Render("clear","high",12,Vector2.zero,0,dawn);Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(camGo);Object.DestroyImmediate(sunGo);}
        }
    }
}

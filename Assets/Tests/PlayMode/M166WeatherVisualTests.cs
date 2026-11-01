using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class M166WeatherVisualTests
    {
        [Test] public void AuthoritativeFrameGraduallyChangesTheSingleAtmosphereLeaseAndRestoresGlobals()
        {
            var root=new GameObject("M166 weather frame acceptance");var camera=root.AddComponent<Camera>();camera.transform.position=new Vector3(0,2,0);var lightObject=new GameObject("Explicit weather light");lightObject.transform.SetParent(root.transform);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;
            var weather=root.AddComponent<WeatherPresentation>();bool fog=RenderSettings.fog;var sky=RenderSettings.skybox;
            try
            {
                string Read(string n)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(n));var policy=RaceWeatherPolicy.FromJson(Read("race_weather_policy_v1.json"));var timeline=policy.CreateScenario(RaceWeatherCatalog.FromJson(Read("career_weather_v1.json")).Get(1),"snow_arrives");
                weather.Initialize(camera,light);weather.RaceObservation=timeline.Sample(0);weather.Render("clear","high",0,Vector2.zero,0);Assert.That(weather.SubmittedFlakes,Is.Zero);float intensity=light.intensity;
                weather.RaceObservation=timeline.Sample(300);weather.Render("clear","high",300,new Vector2(1,1),0);Assert.That(light.intensity,Is.LessThan(intensity));Assert.That(weather.SubmittedFlakes,Is.GreaterThan(0));
                weather.RaceObservation=timeline.Sample(1200);weather.Render("clear","high",1200,new Vector2(1,1),0);Assert.That(weather.VisualAtmosphereId,Is.EqualTo("snowfall"));
                var other=root.AddComponent<WeatherPresentation>();Assert.Throws<InvalidOperationException>(()=>other.Initialize(camera,light));Object.DestroyImmediate(other);
            }
            finally{weather.Shutdown();Object.DestroyImmediate(root);}
            Assert.That(RenderSettings.fog,Is.EqualTo(fog));Assert.That(RenderSettings.skybox,Is.SameAs(sky));
        }
    }
}

using System;
using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2LightingTests
    {
        private static LightingVisualDefinition Definition()=>LightingVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/g2_lighting_visual_v1").text);
        [Test] public void BenchmarkProfilesAndAuditedSkyAreBoundedWithoutReplacingGameplayDefaults()
        {
            var d=Definition();Assert.That(d.id,Is.EqualTo("g2_lighting_visual_v1"));Assert.That(LightingVisualDefinition.Load().Get("clear_cold").ambient_gain,Is.EqualTo(1));
            Assert.That(LightingVisualDefinition.Load().Get("clear_cold").ambient_equator_fraction,Is.EqualTo(.65f));Assert.That(LightingVisualDefinition.Load().Get("clear_cold").ambient_ground_fraction,Is.EqualTo(.35f));
            var mat=Resources.Load<Material>("Biathlon/Weather/G2Lighting/BenchmarkSky");Assert.That(mat,Is.Not.Null);Assert.That(mat.shader.name,Is.EqualTo("Biathlon/BenchmarkSky"));Assert.That(mat.shader.isSupported,Is.True);
            var texture=mat.GetTexture("_Panorama");Assert.That(texture.width,Is.EqualTo(2048));Assert.That(texture.height,Is.EqualTo(1024));Assert.That(mat.GetFloat("_PanoramaExposure"),Is.InRange(.1f,1f));
            foreach(var p in d.presets)p.Validate();Assert.That(d.Get("clear_cold").ambient_gain,Is.GreaterThan(LightingVisualDefinition.Load().Get("clear_cold").ambient_gain));
            d.Get("clear_cold").ambient_gain=float.NaN;Assert.Throws<ArgumentException>(()=>LightingVisualDefinition.FromJson(JsonUtility.ToJson(d)));
        }
        [Test] public void CustomSkyLeaseTracksKeyPreservesTemplateAndRestoresSceneWithZeroAllocation()
        {
            var go=new GameObject("weather");var cameraGo=new GameObject("camera");var sunGo=new GameObject("sun");var old=RenderSettings.skybox;
            try
            {
                var c=cameraGo.AddComponent<Camera>();c.enabled=false;var key=sunGo.AddComponent<Light>();key.type=LightType.Directional;var w=go.AddComponent<WeatherPresentation>();
                var template=Resources.Load<Material>("Biathlon/Weather/G2Lighting/BenchmarkSky");var original=template.GetVector("_SunDirection");var sky=template.GetColor("_SkyColor");w.Initialize(c,key,template);var d=Definition();
                foreach(var preset in d.presets){w.Render(preset.recommended_weather,"high",8,Vector2.zero,0,preset);Assert.That(RenderSettings.skybox,Is.Not.SameAs(template));Assert.That(Vector3.Distance(RenderSettings.skybox.GetVector("_SunDirection"),-key.transform.forward),Is.LessThan(.00001f));Assert.That(RenderSettings.ambientGroundColor.r,Is.EqualTo(RenderSettings.ambientSkyColor.r*preset.ambient_ground_fraction).Within(.00001f));}
                var clear=d.Get("clear_cold");for(int i=0;i<30;i++)w.Render("clear","high",8,Vector2.zero,0,clear);long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)w.Render("clear","high",8,Vector2.zero,0,clear);Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
                Assert.That(template.GetVector("_SunDirection"),Is.EqualTo(original));Assert.That(template.GetColor("_SkyColor"),Is.EqualTo(sky));w.enabled=false;Assert.That(RenderSettings.skybox,Is.SameAs(old));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(cameraGo);Object.DestroyImmediate(sunGo);}
        }
        [UnityTest] public IEnumerator IntegratedLightingWeatherAndCrowdReadabilityCaptures()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                VisualBenchmarkPresentation p=null;foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent(out VisualBenchmarkPresentation found))p=found;Assert.That(p,Is.Not.Null);p.enabled=false;p.ShowControls=false;p.Tier="high";
                var folder=Path.GetFullPath("artifacts/g2-lighting/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))
                {
                    var probeLog=new System.Text.StringBuilder();
                    foreach(var light in Definition().presets)
                    {
                        p.LightingPreset=light.id;p.WeatherPreset=light.recommended_weather;
                        foreach(var shot in new[]{"athlete","range","approach","crowd_zoom"}){p.Shot=shot;p.Seek(8);for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,light.id+"-"+shot+".png"));}
                        probeLog.AppendLine(light.id+" ambient="+RenderSettings.ambientSkyColor+" probe="+RenderSettings.ambientProbe[0,0]+","+RenderSettings.ambientProbe[1,0]+","+RenderSettings.ambientProbe[2,0]);
                    }
                    File.WriteAllText(Path.Combine(folder,"ambient-probe.txt"),probeLog.ToString());
                    p.LightingPreset="overcast";p.WeatherPreset="snowfall";p.Shot="athlete";p.Seek(8);for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,"snowfall-athlete.png"));
                    p.LightingPreset="clear_cold";p.WeatherPreset="clear";p.Seek(8);p.View.transform.position=p.Seating.transform.TransformPoint(new Vector3(-3,4,-5));p.View.transform.LookAt(p.Seating.transform.TransformPoint(new Vector3(0,3,2)));p.View.fieldOfView=35;
                    for(int i=0;i<3;i++){p.GetComponent<SpectatorCrowd>().Render(p.View,"high",8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,"crowd-near.png"));
                }
            }
            finally{if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}

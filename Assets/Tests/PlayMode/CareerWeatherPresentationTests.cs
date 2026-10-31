using System;
using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Replay;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class CareerWeatherPresentationTests
    {
        private GameObject root;
        private string folder;
        private static string Read(string file)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(file));
        private static RaceWeatherCatalog Catalog()=>RaceWeatherCatalog.FromJson(Read("career_weather_v1.json"));
        [TearDown] public void Cleanup()
        {
            if(root!=null)Object.DestroyImmediate(root);
            if(folder!=null&&Directory.Exists(folder)){foreach(var f in Directory.GetFiles(folder))File.Delete(f);Directory.Delete(folder,false);}
        }
        [TestCase(0),TestCase(1),TestCase(2),TestCase(3),TestCase(4),TestCase(5),TestCase(6),TestCase(9)]
        public void WeatherBindsLightingSnowMaterialAndActiveShootingCameraWithoutChangingSource(int index)
        {
            var fixture=new ProductionEnvironmentFixture();string before=fixture.Recipe.ToJson();var weather=Catalog().Get(index);
            var recipe=CareerWeatherPresentation.Bind(fixture.Recipe,weather);
            Assert.That(fixture.Recipe.ToJson(),Is.EqualTo(before));
            root=new GameObject("Weather binding integration");
            var chase=new GameObject("Chase",typeof(Camera)).GetComponent<Camera>();chase.transform.SetParent(root.transform,false);chase.enabled=false;chase.transform.position=new Vector3(0,5,-18);
            var aim=new GameObject("Aim",typeof(Camera)).GetComponent<Camera>();aim.transform.SetParent(root.transform,false);aim.enabled=false;aim.transform.position=new Vector3(0,1,-5);
            var oldClear=chase.clearFlags;var oldAimClear=aim.clearFlags;
            var e=root.AddComponent<ProductionVenueEnvironment>();e.Initialize(fixture.Plan,recipe,fixture.Read,root.AddComponent<RacePresentationFeed>(),chase,aim);
            e.Render(chase,"high",12);e.Render(aim,"high",12);
            Assert.That(e.WeatherPresetId,Is.EqualTo(weather.VisualPreset));
            Assert.That(aim.clearFlags,Is.EqualTo(CameraClearFlags.Skybox));Assert.That(chase.clearFlags,Is.EqualTo(oldClear));
            if(weather.VisualPreset=="snowfall"||weather.VisualPreset=="light_snow")Assert.That(e.SubmittedSnowflakes,Is.GreaterThan(0));else Assert.That(e.SubmittedSnowflakes,Is.Zero);
            var definitions=SnowMaterialDefinition.Load();SnowMaterialPreset preset=null;foreach(var p in definitions.presets)if(p.id==weather.SnowMaterial)preset=p;
            bool terrainChecked=false;
            foreach(var r in e.GetComponentsInChildren<MeshRenderer>())
                if(r.GetComponent<MeshFilter>()?.sharedMesh==e.TileMesh(0))
                {
                    var m=r.sharedMaterial;Assert.That(m.GetFloat("_Roughness"),Is.EqualTo(preset.roughness));
                    Assert.That(m.GetFloat("_TerrainBlend"),Is.EqualTo(1));
                    if(weather.SnowProfile=="fresh_soft")Assert.That(Vector4.Distance(m.GetColor("_PackedColor"),m.GetColor("_BaseColor")),Is.LessThan(.00001f));
                    if(weather.SnowProfile=="icy_fast")Assert.That(Vector4.Distance(m.GetColor("_PackedColor"),m.GetColor("_IceColor")),Is.LessThan(.00001f));
                    terrainChecked=true;
                }
            Assert.That(terrainChecked,Is.True);e.Shutdown();Assert.That(aim.clearFlags,Is.EqualTo(oldAimClear));
        }
        private sealed class Store:ICareerProfileStore
        {
            public string Json;
            public bool HasProfile=>Json!=null;
            public string ReadLatest()=>Json;
            public void WriteNew(string id,string value)=>Json=value;
            public void Replace(string id,string before,string value){Assert.That(Json,Is.EqualTo(before));Json=value;}
        }
        [UnityTest,Timeout(300000)] public IEnumerator ActualCareerLaunchUsesForecastAndSnowfallWithFullQuality()
        {
            folder=Path.Combine(Path.GetTempPath(),"biathlon-weather-"+Guid.NewGuid().ToString("N"));
            root=new GameObject("Weather live career review");var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;
            menu.Initialize(new Store(),new RaceReplayLibrary(folder));menu.Session.OpenNewCareer();menu.Session.CreateCareer("weather_live","Алекс","NOR",11);
            for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();menu.RefreshFrontEnd();yield return null;
            Assert.That(menu.Root.Q<Label>("pre_race_weather").text,Does.Contain("-6°C"));
            menu.Race.Launch(menu);for(int i=0;i<60&&menu.Race.Loading;i++)yield return null;
            var host=menu.Race.Host;Assert.That(host,Is.Not.Null,menu.LastMessage);Assert.That(host.Environment.WeatherPresetId,Is.EqualTo("snowfall"));
            for(int i=0;i<5;i++)yield return null;
            Assert.That(host.Environment.SubmittedSnowflakes,Is.GreaterThan(0));
            Assert.That(host.Runner.RaceWind.EastMetersPerSecond,Is.EqualTo(1.8));
            var target=new RenderTexture(1280,720,24);target.Create();var camera=host.ChaseCamera;camera.targetTexture=target;
            Texture2D pixels=null;var prior=RenderTexture.active;
            try
            {
                for(int i=0;i<3;i++)yield return null;
                RenderTexture.active=target;pixels=new Texture2D(1280,720,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();
                string evidence=Path.Combine(Application.dataPath,"../docs/quality/m14/weather-20261012");Directory.CreateDirectory(evidence);File.WriteAllBytes(Path.Combine(evidence,"career-snowfall.png"),pixels.EncodeToPNG());
            }
            finally{camera.targetTexture=null;RenderTexture.active=prior;if(pixels!=null)Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target);}
            menu.Race.Abandon();yield return null;
        }
    }
}

using System;
using System.Text.RegularExpressions;
using Biathlon.Replay;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class ProductionFogPresentationTests
    {
        private static readonly float[] InvalidScales={0,.99f,3.01f,float.NaN,float.PositiveInfinity,float.NegativeInfinity};

        [Test] public void LegacyEnvironmentJsonDefaultsToOneAndWoodlandOptsIntoExplicitScale()
        {
            var fixture=new ProductionEnvironmentFixture();var json=fixture.Recipe.ToJson();
            var legacy=Regex.Replace(json,",\"fog_distance_scale\":1(?:\\.0)?","");
            Assert.That(legacy,Does.Not.Contain("\"fog_distance_scale\""));
            var parsed=ProductionEnvironmentDefinition.FromJson(legacy);
            Assert.That(parsed.lighting.fog_distance_scale,Is.EqualTo(1));
            Assert.That(ProductionEnvironmentDefinition.FromJson(parsed.ToJson()).lighting.fog_distance_scale,Is.EqualTo(1));
            var actual=ProductionEnvironmentDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/Venues/woodland_basin_v1/environment").text);
            Assert.That(actual.lighting.fog_distance_scale,Is.EqualTo(2.7f));
            Assert.That(actual.lighting.weather_preset,Is.EqualTo("clear"));Assert.That(actual.lighting.preset_id,Is.EqualTo("clear_cold"));
        }

        [Test] public void InvalidDetachedEnvironmentScaleIsRejectedBeforeObjectsOrGlobalsChange()
        {
            foreach(float scale in InvalidScales)
            {
                var fixture=new ProductionEnvironmentFixture();fixture.Recipe.lighting.fog_distance_scale=scale;
                var root=new GameObject("Rejected atmosphere recipe");
                try
                {
                    var camera=root.AddComponent<Camera>();camera.enabled=false;var key=root.AddComponent<Light>();key.type=LightType.Directional;
                    var before=new State(camera,key);var view=root.AddComponent<ProductionVenueEnvironment>();var feed=root.AddComponent<RacePresentationFeed>();
                    Assert.Throws<ArgumentException>(()=>view.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera));
                    Assert.That(view.Initialized,Is.False);Assert.That(root.transform.childCount,Is.Zero);before.AssertEqual(new State(camera,key));
                }
                finally{Object.DestroyImmediate(root);}
            }
        }

        [Test] public void ScaleUsesEachActualPresetWithoutChangingWeatherLightSkyOrSnowAndNeverCompounds()
        {
            using(var fixture=new WeatherFixture())
            {
                var definition=WeatherVisualDefinition.Load();var lighting=LightingVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/g2_lighting_visual_v1").text).Get("clear_cold");
                foreach(var preset in definition.presets)
                {
                    fixture.Weather.Render(preset.id,"high",12,new Vector2(2,1),0,lighting);
                    var baseline=new State(fixture.Camera,fixture.Key);int flakes=fixture.Weather.SubmittedFlakes;
                    for(int frame=0;frame<4;frame++)
                    {
                        fixture.Weather.Render(preset.id,"high",12,new Vector2(2,1),0,lighting,2.7f);
                        Assert.That(RenderSettings.fogStartDistance,Is.EqualTo(preset.fog_start_m*2.7f).Within(1e-4));
                        Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(preset.fog_end_m*2.7f).Within(1e-4));
                        baseline.AssertEqual(new State(fixture.Camera,fixture.Key),false);
                        Assert.That(fixture.Weather.PresetId,Is.EqualTo(preset.id));Assert.That(fixture.Weather.SubmittedFlakes,Is.EqualTo(flakes));
                    }
                    fixture.Weather.Render(preset.id,"high",12,new Vector2(2,1),0,lighting);
                    baseline.AssertEqual(new State(fixture.Camera,fixture.Key));
                }
            }
        }

        [Test] public void InvalidRenderScaleLeavesAppliedAtmosphereAndLeaseUnchanged()
        {
            using(var fixture=new WeatherFixture())
            {
                fixture.Weather.Render("snowfall","high",12,Vector2.right,0,null,2.7f);
                var baseline=new State(fixture.Camera,fixture.Key);int flakes=fixture.Weather.SubmittedFlakes;
                foreach(float scale in InvalidScales)
                {
                    Assert.Throws<ArgumentException>(()=>fixture.Weather.Render("clear","low",0,Vector2.zero,0,null,scale));
                    baseline.AssertEqual(new State(fixture.Camera,fixture.Key));
                    Assert.That(fixture.Weather.PresetId,Is.EqualTo("snowfall"));Assert.That(fixture.Weather.SubmittedFlakes,Is.EqualTo(flakes));
                }
                fixture.Weather.Render("clear","high",0,Vector2.zero,0,null,3);
                Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(WeatherVisualDefinition.Load().Get("clear").fog_end_m*3));
            }
        }

        [Test] public void ScaledLeaseRestoresAllSceneCameraAndLightValuesAndCanBeReacquired()
        {
            using(var fixture=new WeatherFixture())
            {
                fixture.Weather.Render("clear","high",12,Vector2.right,0,null,2.7f);
                fixture.Weather.enabled=false;fixture.Baseline.AssertEqual(new State(fixture.Camera,fixture.Key));
                fixture.Weather.enabled=true;fixture.Weather.Render("overcast","balanced",8,Vector2.left,0,null,1.5f);
                fixture.Weather.enabled=false;fixture.Baseline.AssertEqual(new State(fixture.Camera,fixture.Key));
                fixture.Weather.enabled=true;fixture.Weather.Render("snowfall","low",4,Vector2.zero,0,null,2.7f);
            }
        }

        [Test] public void EnvironmentUsesDetachedFogRecipeAndDoesNotMutateTheRaceSnapshotOrEvents()
        {
            var fixture=new ProductionEnvironmentFixture();fixture.Recipe.lighting.fog_distance_scale=2.7f;
            var events=new RaceEventLog(6000);var field=fixture.Field(events);var snapshot=RacePresentationSnapshot.Capture(field);
            var root=new GameObject("Scaled production environment");var oldStart=RenderSettings.fogStartDistance;var oldEnd=RenderSettings.fogEndDistance;var oldSky=RenderSettings.skybox;
            try
            {
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,3,-20);
                var feed=root.AddComponent<RacePresentationFeed>();feed.Publish(snapshot);var view=root.AddComponent<ProductionVenueEnvironment>();
                view.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera);
                fixture.Recipe.lighting.fog_distance_scale=float.NaN;fixture.Recipe.lighting.weather_preset="snowfall";
                long tick=field.Tick.Value,route=field.GetAthlete(0).RouteNanometers,motion=field.GetAthlete(0).Motion.DistanceNanometers;int eventCount=events.Count;var point=field.GetPhysicalPoint(0);
                var weather=WeatherVisualDefinition.Load().Get("clear");
                for(int frame=0;frame<3;frame++)
                {
                    view.Render(camera,"high",frame);
                    Assert.That(RenderSettings.fogStartDistance,Is.EqualTo(weather.fog_start_m*2.7f).Within(1e-4));
                    Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(weather.fog_end_m*2.7f).Within(1e-4));
                }
                Assert.That(feed.Current,Is.SameAs(snapshot));Assert.That(field.Tick.Value,Is.EqualTo(tick));Assert.That(field.GetAthlete(0).RouteNanometers,Is.EqualTo(route));
                Assert.That(field.GetAthlete(0).Motion.DistanceNanometers,Is.EqualTo(motion));Assert.That(events.Count,Is.EqualTo(eventCount));Assert.That(field.GetPhysicalPoint(0),Is.EqualTo(point));
                view.Shutdown();Assert.That(RenderSettings.fogStartDistance,Is.EqualTo(oldStart));Assert.That(RenderSettings.fogEndDistance,Is.EqualTo(oldEnd));Assert.That(RenderSettings.skybox,Is.SameAs(oldSky));
            }
            finally{root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.DestroyImmediate(root);}
        }

        [Test] public void WarmScaledClearPresentationAllocatesNoManagedMemory()
        {
            using(var fixture=new WeatherFixture())
            {
                var lighting=LightingVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/g2_lighting_visual_v1").text).Get("clear_cold");
                for(int i=0;i<30;i++)fixture.Weather.Render("clear","high",i,Vector2.zero,0,lighting,2.7f);
                long before=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<100;i++)fixture.Weather.Render("clear","high",i,Vector2.zero,0,lighting,2.7f);
                long allocated=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(allocated,Is.Zero);
            }
        }

        private sealed class WeatherFixture:IDisposable
        {
            private readonly GameObject root=new GameObject("Scaled weather test");
            public readonly Camera Camera;public readonly Light Key;public readonly WeatherPresentation Weather;public readonly State Baseline;
            public WeatherFixture()
            {
                var cameraObject=new GameObject("Explicit fog camera");cameraObject.transform.SetParent(root.transform,false);
                Camera=cameraObject.AddComponent<Camera>();Camera.enabled=false;Camera.transform.position=new Vector3(0,3,-10);Camera.clearFlags=CameraClearFlags.SolidColor;
                var keyObject=new GameObject("Explicit fog directional light");keyObject.transform.SetParent(root.transform,false);
                Key=keyObject.AddComponent<Light>();Key.type=LightType.Directional;Key.intensity=1.23f;Key.shadowStrength=.77f;Key.transform.rotation=Quaternion.Euler(32,18,7);
                Baseline=new State(Camera,Key);Weather=root.AddComponent<WeatherPresentation>();Weather.Initialize(Camera,Key,Resources.Load<Material>("Biathlon/Weather/G2Lighting/BenchmarkSky"));
            }
            public void Dispose(){try{Weather.enabled=false;Baseline.AssertEqual(new State(Camera,Key));}finally{Object.DestroyImmediate(root);}}
        }

        private readonly struct State
        {
            private readonly bool fog;private readonly FogMode mode;private readonly float start,end,density,intensity,shadow,cloud,fieldOfView;
            private readonly Color fogColor,sky,equator,ground,keyColor,background,materialSky,materialHorizon;
            private readonly AmbientMode ambient;private readonly Material skybox;private readonly CameraClearFlags clear;private readonly Quaternion rotation,cameraRotation;private readonly Vector3 cameraPosition;private readonly Vector4 drift,sun;
            public State(Camera camera,Light key)
            {
                fog=RenderSettings.fog;mode=RenderSettings.fogMode;start=RenderSettings.fogStartDistance;end=RenderSettings.fogEndDistance;density=RenderSettings.fogDensity;fogColor=RenderSettings.fogColor;
                ambient=RenderSettings.ambientMode;sky=RenderSettings.ambientSkyColor;equator=RenderSettings.ambientEquatorColor;ground=RenderSettings.ambientGroundColor;skybox=RenderSettings.skybox;
                intensity=key.intensity;shadow=key.shadowStrength;keyColor=key.color;rotation=key.transform.rotation;clear=camera.clearFlags;background=camera.backgroundColor;
                cameraPosition=camera.transform.position;cameraRotation=camera.transform.rotation;fieldOfView=camera.fieldOfView;
                materialSky=ColorProperty(skybox,"_SkyColor");materialHorizon=ColorProperty(skybox,"_HorizonColor");cloud=skybox!=null&&skybox.HasProperty("_CloudCover")?skybox.GetFloat("_CloudCover"):0;
                drift=VectorProperty(skybox,"_Drift");sun=VectorProperty(skybox,"_SunDirection");
            }
            private static Color ColorProperty(Material material,string name)=>material!=null&&material.HasProperty(name)?material.GetColor(name):Color.clear;
            private static Vector4 VectorProperty(Material material,string name)=>material!=null&&material.HasProperty(name)?material.GetVector(name):Vector4.zero;
            public void AssertEqual(State actual,bool distances=true)
            {
                Assert.That(actual.fog,Is.EqualTo(fog));Assert.That(actual.mode,Is.EqualTo(mode));Assert.That(actual.density,Is.EqualTo(density));Assert.That(actual.fogColor,Is.EqualTo(fogColor));
                if(distances){Assert.That(actual.start,Is.EqualTo(start));Assert.That(actual.end,Is.EqualTo(end));}
                Assert.That(actual.ambient,Is.EqualTo(ambient));Assert.That(actual.sky,Is.EqualTo(sky));Assert.That(actual.equator,Is.EqualTo(equator));Assert.That(actual.ground,Is.EqualTo(ground));Assert.That(actual.skybox,Is.SameAs(skybox));
                Assert.That(actual.intensity,Is.EqualTo(intensity));Assert.That(actual.shadow,Is.EqualTo(shadow));Assert.That(actual.keyColor,Is.EqualTo(keyColor));Assert.That(actual.rotation,Is.EqualTo(rotation));
                Assert.That(actual.clear,Is.EqualTo(clear));Assert.That(actual.background,Is.EqualTo(background));Assert.That(actual.materialSky,Is.EqualTo(materialSky));Assert.That(actual.materialHorizon,Is.EqualTo(materialHorizon));
                Assert.That(actual.cameraPosition,Is.EqualTo(cameraPosition));Assert.That(actual.cameraRotation,Is.EqualTo(cameraRotation));Assert.That(actual.fieldOfView,Is.EqualTo(fieldOfView));
                Assert.That(actual.cloud,Is.EqualTo(cloud));Assert.That(actual.drift,Is.EqualTo(drift));Assert.That(actual.sun,Is.EqualTo(sun));
            }
        }
    }
}

using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class CourseFloodlightTests
    {
        private const string Prefix="Biathlon/Venues/woodland_basin_v1/";
        private static string Read(string id)=>ProductionVenueHost.ReadResource(id);
        private static ProductionEnvironmentDefinition Venue()=>ProductionEnvironmentDefinition.FromJson(Read(Prefix+"environment"));
        private static CourseFloodlightDefinition Definition()=>CourseFloodlightDefinition.FromJson(Read("Biathlon/course_floodlights_v1"));
        [Test] public void AllDevelopedLooksBindToCareerAndRecordedReplayWithoutMutatingSources()
        {
            var catalog=RaceWeatherCatalog.FromJson(Read("Biathlon/career_weather_v1"));var source=Venue();string before=source.ToJson();
            var weather=WeatherVisualDefinition.Load();var lighting=LightingVisualDefinition.FromJson(Read(source.lighting.resource));int evening=0;
            for(int i=0;i<catalog.Count;i++)
            {
                var original=catalog.Get(i);var snapshot=RaceWeatherPreset.FromJson(original.ToJson());var bound=CareerWeatherPresentation.Bind(source,snapshot);
                Assert.That(weather.Get(bound.lighting.weather_preset),Is.Not.Null);var light=lighting.Get(bound.lighting.preset_id);
                Assert.That(bound.lighting.preset_id,Is.EqualTo(original.LightingPreset));Assert.That(bound.lighting.weather_preset,Is.EqualTo(original.VisualPreset));
                Assert.That(bound.terrain.snow_surface_profile,Is.EqualTo(original.SnowProfile));
                if(light.floodlights_gain>0){evening++;Assert.That(original.LightingPreset,Is.EqualTo("evening"));}
                Assert.That(source.ToJson(),Is.EqualTo(before));
            }
            Assert.That(evening,Is.EqualTo(2));Assert.That(lighting.Get("clear_cold").floodlights_gain,Is.Zero);
        }
        [Test] public void LayoutStaysOutsideEveryCorridorHasContinuousPoolsAndRejectsInvalidBudgets()
        {
            var root=new GameObject("Static floodlight contract");
            try
            {
                var d=Definition();var venue=Venue();var lights=root.AddComponent<CourseFloodlightPresentation>();lights.Initialize(d,venue,p=>0);
                Assert.That(lights.MastCount,Is.InRange(20,d.max_masts));Assert.That(lights.RealtimeLightCount,Is.InRange(2,8));foreach(var fill in root.GetComponentsInChildren<Light>()){Assert.That(fill.enabled,Is.False);Assert.That(fill.shadows,Is.EqualTo(UnityEngine.LightShadows.None));}
                foreach(var anchor in lights.Anchors){Assert.That(CourseFloodlightLayout.Clearance(venue,anchor.Foot),Is.GreaterThanOrEqualTo(d.setback_m*.5f+d.foot_radius_m));Assert.That(anchor.Foot.y,Is.Zero);}
                float minimum=float.PositiveInfinity;
                foreach(var route in venue.route_corridors)for(int i=0;i<route.knots.Length-1;i++)for(int j=0;j<=4;j++)
                {var point=Vector3.Lerp(route.knots[i].Position,route.knots[i+1].Position,j/4f);minimum=Mathf.Min(minimum,lights.Illumination(point).maxColorComponent);}
                TestContext.WriteLine("masts="+lights.MastCount+" minimum_course_illumination="+minimum);
                Assert.That(minimum,Is.GreaterThan(.12f),"No unlit corridor intervals, including start, finish and penalty branches.");
                for(int i=0;i<20;i++)lights.Illumination(Vector3.zero);long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)lights.Illumination(Vector3.zero);Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
                string json=Read("Biathlon/course_floodlights_v1");Assert.Catch(()=>CourseFloodlightDefinition.FromJson(json.Replace("\"max_masts\":96","\"max_masts\":10000")));
                Assert.Catch(()=>CourseFloodlightDefinition.FromJson(json.Replace("\"height_m\":11","\"height_m\":0")));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest,Timeout(300000)] public IEnumerator ActualWoodlandDayFogSnowAndEveningRenderAndRestoreSceneWithoutAuthorityChanges()
        {
            var root=new GameObject("Actual Woodland weather and lighting");var beforeTexture=Shader.GetGlobalTexture("_VenueLightMap");float beforeGain=Shader.GetGlobalFloat("_VenueLightGain");
            try
            {
                var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Prefix+"manifest")),Read);var venue=CareerWeatherPresentation.Bind(Venue(),RaceWeatherCatalog.FromJson(Read("Biathlon/career_weather_v1")).Get(5));
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(plan,CompetitionProfile.FromJson(Read(Prefix+"mass_start_profile")),RaceRoutePlan.FromJson(Read(Prefix+"mass_start_route")),RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),count:2);
                host.InitializeEnvironment(venue);host.Environment.AutomaticRendering=false;host.GameplayHud.enabled=false;
                var e=host.Environment;var lights=e.Floodlights;Assert.That(lights,Is.Not.Null);Assert.That(lights.MastCount,Is.InRange(20,Definition().max_masts));
                e.Render(host.ChaseCamera,"high",12);Assert.That(lights.LampsOn,Is.True,"Actual forecast binding activates the evening lights without a review override.");
                foreach(var a in lights.Anchors)Assert.That(a.Foot.y,Is.EqualTo(e.SampleGround(a.Foot)).Within(.001f));
                float minimum=float.PositiveInfinity;foreach(var corridor in venue.route_corridors)for(int i=0;i<corridor.knots.Length-1;i++)for(int j=0;j<=4;j++)minimum=Mathf.Min(minimum,lights.Illumination(Vector3.Lerp(corridor.knots[i].Position,corridor.knots[i+1].Position,j/4f)).maxColorComponent);
                Assert.That(minimum,Is.GreaterThan(.12f),"Real-tree/coaches clearance may not leave an unlit course branch.");
                var camera=host.ChaseCamera;camera.GetComponent<AthleteChaseCamera>().enabled=false;camera.enabled=false;
                var tick=host.Runner.Field.Tick;var actor=host.Runner.Field.GetAthlete(host.PlayerIndex).LastFrame;
                var catalog=RaceWeatherCatalog.FromJson(Read("Biathlon/career_weather_v1"));
                string folder=Path.GetFullPath("artifacts/m16-4/weather-lights");Directory.CreateDirectory(folder);
                camera.transform.position=venue.range.origin-venue.range.forward*42+Vector3.up*10;camera.transform.LookAt(venue.range.origin+venue.range.forward*30);
                using(var target=new BenchmarkRenderTarget(1280,720))
                {
                    for(int i=0;i<catalog.Count;i++)
                    {
                        var w=catalog.Get(i);e.SetAtmosphereForReview(w.VisualPreset);e.SetLightingForReview(w.LightingPreset);
                        for(int frame=0;frame<3;frame++){e.Render(camera,"high",12);target.Render(camera);yield return null;}
                        Assert.That(lights.LampsOn,Is.EqualTo(w.LightingPreset=="evening"));
                        Assert.That(e.VisualAtmosphereId,Is.EqualTo(w.VisualPreset));
                        Assert.That(e.SubmittedSnowflakes>0,Is.EqualTo(w.VisualPreset=="snowfall"||w.VisualPreset=="light_snow"));
                        target.SavePng(Path.Combine(folder,w.Id+".png"));
                    }
                    var a=lights.Anchors[0];var forward=(a.Focus-a.Foot).normalized;
                    camera.transform.position=a.Foot+forward*9+Vector3.up*6;camera.transform.LookAt(a.Foot+Vector3.up*Definition().height_m);
                    e.SetAtmosphereForReview("clear");e.SetLightingForReview("evening");e.Render(camera,"high",12);target.Render(camera);yield return null;target.SavePng(Path.Combine(folder,"evening-lamp.png"));
                    // Read real white front-plate pixels across the whole bank, not
                    // merely the enabled state of lights. Keep scoring surfaces intact.
                    for(int lane=0;lane<venue.range.banks.Length;lane+=7)
                    {
                        var bank=venue.range.banks[lane].position;camera.transform.position=bank-venue.range.forward*10+Vector3.up*.5f;camera.transform.LookAt(bank);camera.fieldOfView=42;
                        for(int frame=0;frame<3;frame++){e.Render(camera,"high",12);target.Render(camera);yield return null;}
                        string path=Path.Combine(folder,"evening-target-"+lane+".png");target.SavePng(path);
                        var pixels=new Texture2D(2,2);
                        try
                        {
                            ImageConversion.LoadImage(pixels,File.ReadAllBytes(path));var uv=camera.WorldToViewportPoint(bank+Vector3.up*.12f);
                            var color=pixels.GetPixel(Mathf.RoundToInt(uv.x*pixels.width),Mathf.RoundToInt(uv.y*pixels.height));
                            Assert.That(color.grayscale,Is.GreaterThan(.4f),"White target plate must remain readable at evening lane "+lane);
                        }
                        finally{Object.DestroyImmediate(pixels);}
                    }
                }
                Assert.That(host.Runner.Field.Tick,Is.EqualTo(tick));Assert.That(host.Runner.Field.GetAthlete(host.PlayerIndex).LastFrame,Is.EqualTo(actor));
                e.SetLightingForReview("evening");e.Render(camera,"high",12);Assert.That(Shader.GetGlobalFloat("_VenueLightGain"),Is.EqualTo(1));
                for(int i=0;i<10;i++)lights.Render(camera,1);long allocated=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)lights.Render(camera,1);Assert.That(GC.GetAllocatedBytesForCurrentThread()-allocated,Is.Zero);
                File.WriteAllText(Path.Combine(folder,"illumination-budget.json"),"{\"masts\":"+lights.MastCount+",\"target_lights\":"+lights.RealtimeLightCount+",\"shadowed_local_lights\":0,\"fixture_instanced_batches\":2,\"lightmap_bytes\":"+(Definition().map_resolution*Definition().map_resolution*8)+",\"warm_render_allocations\":0,\"minimum_course_illumination\":"+minimum.ToString(System.Globalization.CultureInfo.InvariantCulture)+"}");
                e.enabled=false;Assert.That(Shader.GetGlobalFloat("_VenueLightGain"),Is.EqualTo(beforeGain));e.enabled=true;e.Render(camera,"high",12);Assert.That(lights.LampsOn,Is.True);
                e.Shutdown();Assert.That(Shader.GetGlobalTexture("_VenueLightMap"),Is.EqualTo(beforeTexture));Assert.That(Shader.GetGlobalFloat("_VenueLightGain"),Is.EqualTo(beforeGain));
            }
            finally{Object.DestroyImmediate(root);if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
        }
    }
}

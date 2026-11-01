using System;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class M165VenuePresentationTests
    {
        private const string Prefix="Biathlon/Venues/woodland_basin_v1/";
        private static string Read(string resource)=>ProductionVenueHost.ReadResource(resource);
        [TestCase("sprint"),TestCase("pursuit"),TestCase("mass_start")]
        public void EveryActualCheckpointUsesCumulativeOfficialDistanceAcrossLapsAndReplaySeek(string format)
        {
            var root=new GameObject("Cumulative distance labels");
            try
            {
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Prefix+"manifest")),Read);var profile=CompetitionProfile.FromJson(Read(Prefix+format+"_profile"));
                var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(Prefix+format+"_route")),profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
                var distance=RaceDistanceBinding.Create(profile,route);var view=root.AddComponent<CourseTimingPresentation>();view.Initialize(venue);view.Bind(profile,route,root.AddComponent<RacePresentationFeed>(),0,distance);
                for(int lap=1;lap<route.LapCount;lap++)Assert.That(distance.AtRouteMeters(route.GetLapStart(lap).Meters),Is.EqualTo(distance.TotalMeters*lap/route.LapCount).Within(.000001),"Each nominal lap must retain its official discipline share.");
                var layout=new CheckpointLayout(venue.Track,route);var cached=new System.Collections.Generic.List<string>();
                foreach(var index in Enumerable.Range(0,layout.Count))
                {
                    var occurrence=layout.Get(index);double meters=occurrence.RouteNanometers/(double)SkiState.NanometersPerMeter;
                    int station=Enumerable.Range(0,view.TimingLines).Single(i=>view.CheckpointId(i)==occurrence.CheckpointId);
                    view.RefreshDistance(meters);string expected=distance.Kilometers(meters,System.Globalization.CultureInfo.InvariantCulture)+" KM";
                    Assert.That(view.DistanceLabel(station),Is.EqualTo(expected));cached.Add(expected);
                    view.RefreshDistance(meters+.01);Assert.That(view.DistanceLabel(station),Is.EqualTo(expected),"Keep the just-passed number while the gantry remains visible.");
                }
                Assert.That(cached.Distinct().Count(),Is.GreaterThan(profile.LapCount));view.RefreshDistance(0);
                Assert.That(root.GetComponentsInChildren<TextMesh>().Any(t=>t.text=="TIMING"),Is.False);
                long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)view.RefreshDistance(0);Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
                string folder=Path.GetFullPath("artifacts/m16-5");Directory.CreateDirectory(folder);File.WriteAllLines(Path.Combine(folder,format+"-checkpoint-labels.txt"),cached);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest,Timeout(180000)] public IEnumerator EveningSpectatorsAndAdvertisingRespondToTheSameStaticFieldWithOriginalColours()
        {
            var root=new GameObject("Actual evening spectator and advert check");
            try
            {
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Prefix+"manifest")),Read);var recipe=CareerWeatherPresentation.Bind(ProductionEnvironmentDefinition.FromJson(Read(Prefix+"environment")),RaceWeatherCatalog.FromJson(Read("Biathlon/career_weather_v1")).Get(5));
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(Read(Prefix+"mass_start_profile")),RaceRoutePlan.FromJson(Read(Prefix+"mass_start_route")),RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),count:2);
                host.InitializeEnvironment(recipe);var e=host.Environment;e.AutomaticRendering=false;host.GameplayHud.enabled=false;
                var camera=host.ChaseCamera;camera.GetComponent<AthleteChaseCamera>().enabled=false;camera.enabled=false;
                var crowd=Enumerable.Range(0,e.CrowdCount).Select(e.Crowd).First(c=>c.Pose==SpectatorPose.Standing);var seating=crowd.GetComponentInChildren<SpectatorSeating>();var foot=seating.transform.TransformPoint(seating.CopyAnchors()[0]);
                camera.transform.position=foot+seating.transform.forward*6+Vector3.up*1.2f;camera.transform.LookAt(foot+Vector3.up*1.1f);camera.fieldOfView=40;
                string folder=Path.GetFullPath("artifacts/m16-5");Directory.CreateDirectory(folder);float[] luminance=new float[2];
                using(var target=new BenchmarkRenderTarget(640,480))for(int on=0;on<2;on++)
                {
                    for(int f=0;f<3;f++){e.Render(camera,"high",12);e.Floodlights.Render(camera,on);target.Render(camera);yield return null;}
                    string path=Path.Combine(folder,on==1?"crowd-evening-lit.png":"crowd-evening-unlit.png");target.SavePng(path);var image=new Texture2D(2,2);
                    try{ImageConversion.LoadImage(image,File.ReadAllBytes(path));float sum=0;for(int y=180;y<300;y++)for(int x=280;x<360;x++)sum+=image.GetPixel(x,y).grayscale;luminance[on]=sum/(80*120);}
                    finally{Object.DestroyImmediate(image);}
                }
                Assert.That(luminance[1],Is.GreaterThan(luminance[0]+.025f),"Actual spectators beside lit course must receive venue illumination.");
                var placement=recipe.mesh_assets.First(a=>a.material_resource=="Biathlon/Stadium/G2Event/EventTeal");var mesh=Resources.Load<Mesh>(placement.mesh_resource);var original=Resources.Load<Material>(placement.material_resource);
                var renderer=e.GetComponentsInChildren<MeshRenderer>(true).First(r=>{var filter=r.GetComponent<MeshFilter>();return filter!=null&&filter.sharedMesh==mesh&&r.sharedMaterial.shader.name=="Biathlon/Illuminated Venue Lit";});
                Assert.That(renderer.sharedMaterial.GetColor("_BaseColor"),Is.EqualTo(original.GetColor("_BaseColor")));Assert.That(renderer.sharedMaterial.GetTexture("_BaseMap"),Is.SameAs(original.GetTexture("_BaseMap")));Assert.That(original.shader.name,Is.EqualTo("Universal Render Pipeline/Lit"));
                Assert.That(e.Floodlights.RealtimeLightCount,Is.EqualTo(6));
            }
            finally{Object.DestroyImmediate(root);if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
        }
    }
}

using System.Collections;
using System.Linq;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class TimingPortalReadabilityTests
    {
        private GameObject root;
        private CourseTimingPresentation view;
        private RaceRoute route;
        private VenueAuthoringPlan venue;
        private int stadium;
        [SetUp] public void Setup()
        {
            string prefix="Biathlon/Venues/woodland_basin_v1/";
            venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_profile"));
            route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_route")),profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            root=new GameObject("Timing readability check");view=root.AddComponent<CourseTimingPresentation>();view.Initialize(venue);
            view.Bind(profile,route,root.AddComponent<RacePresentationFeed>(),0,RaceDistanceBinding.Create(profile,route));
            stadium=Enumerable.Range(0,view.TimingLines).Single(view.UsesExistingStadiumPortal);
        }
        [TearDown] public void Cleanup()=>Object.DestroyImmediate(root);

        [Test] public void SharedStartGantryIsBlankUntilItsFirstActualSplitAndAgainAfterSeekingToStart()
        {
            Assert.That(view.DistanceLabel(stadium),Is.Empty,"Starting under the shared gantry must not show a future lap-end distance.");
            var layout=new CheckpointLayout(venue.Track,route);
            double first=Enumerable.Range(0,layout.Count).Select(layout.Get).First(x=>x.CheckpointId==view.CheckpointId(stadium)).RouteNanometers/(double)SkiState.NanometersPerMeter;
            view.RefreshDistance(first);Assert.That(view.DistanceLabel(stadium),Is.Not.Empty);
            view.RefreshDistance(0);Assert.That(view.DistanceLabel(stadium),Is.Empty);
        }

        [UnityTest] public IEnumerator StadiumDigitsFitInsideActualHeaderInsteadOfHangingBelowIt()
        {
            var layout=new CheckpointLayout(venue.Track,route);
            double first=Enumerable.Range(0,layout.Count).Select(layout.Get).First(x=>x.CheckpointId==view.CheckpointId(stadium)).RouteNanometers/(double)SkiState.NanometersPerMeter;
            view.RefreshDistance(first);yield return null;
            var label=view.GateTransform(stadium).GetComponentInChildren<TextMesh>();
            var bounds=label.GetComponent<Renderer>().localBounds;
            float low=label.transform.localPosition.y+bounds.min.y,high=label.transform.localPosition.y+bounds.max.y;
            var mesh=Resources.Load<Mesh>("Biathlon/Stadium/FinishGantry_dark");
            var header=mesh.vertices.Where(p=>p.y>4f).ToArray();
            Assert.That(header,Is.Not.Empty);
            Assert.That(low,Is.GreaterThan(header.Min(p=>p.y)+.04f),"Digits must stay above the lower edge of the actual header.");
            Assert.That(high,Is.LessThan(header.Max(p=>p.y)-.04f),"Digits must stay under the snow cap.");
            Assert.That(label.transform.localPosition.z,Is.LessThan(header.Min(p=>p.z)-.005f),"Text is in front of every opaque header layer.");
            Assert.That(bounds.size.y,Is.InRange(.3f,.7f),"Readable digits fit the actual ninety-centimetre panel.");
            Assert.That(bounds.size.x,Is.LessThan(8),"Cumulative distances remain within the beam.");
        }

        [UnityTest] public IEnumerator ActualNativeGantryRendersBlankAtLaunchAndLegibleDigitsOnApproach()
        {
            var previous=QualitySettings.renderPipeline;
            try
            {
                QualitySettings.renderPipeline=Resources.Load<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>("Biathlon/Rendering/high");
                var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));
                foreach(var part in recipe.mesh_assets.Where(p=>p.id.StartsWith("timing_gantry_")))
                {
                    var obj=new GameObject(part.id,typeof(MeshFilter),typeof(MeshRenderer));obj.transform.SetParent(root.transform,false);
                    obj.transform.SetPositionAndRotation(part.position,Quaternion.Euler(0,part.yaw_deg,0));obj.layer=31;
                    obj.GetComponent<MeshFilter>().sharedMesh=Resources.Load<Mesh>(part.mesh_resource);
                    obj.GetComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>(part.material_resource);
                }
                foreach(var renderer in view.GetComponentsInChildren<Renderer>())renderer.gameObject.layer=31;
                var cameraObject=new GameObject("Portal arrival camera",typeof(Camera));cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.3f,.4f,.5f);camera.fieldOfView=55;
                var light=new GameObject("Portal daylight",typeof(Light));light.transform.SetParent(root.transform,false);
                light.GetComponent<Light>().type=LightType.Directional;light.GetComponent<Light>().intensity=1.3f;light.GetComponent<Light>().cullingMask=1<<31;
                light.transform.rotation=Quaternion.Euler(40,30,0);
                var layout=new CheckpointLayout(venue.Track,route);
                double first=Enumerable.Range(0,layout.Count).Select(layout.Get).First(x=>x.CheckpointId==view.CheckpointId(stadium)).RouteNanometers/(double)SkiState.NanometersPerMeter;
                string folder=Path.GetFullPath("docs/quality/m16-6/pulse");Directory.CreateDirectory(folder);
                var frame=view.GateTransform(stadium);
                using(var target=new BenchmarkRenderTarget(1280,720))
                foreach(int distance in new[]{25,12})
                {
                    camera.transform.position=frame.TransformPoint(new Vector3(0,2,-distance));camera.transform.LookAt(frame.TransformPoint(new Vector3(0,2,0)));
                    Texture2D blank=null,shown=null;
                    try
                    {
                        view.RefreshDistance(0);target.Render(camera);yield return null;yield return null;target.Render(camera);yield return null;
                        target.SavePng(Path.Combine(folder,"portal-blank-"+distance+"m.png"));
                        blank=new Texture2D(2,2);ImageConversion.LoadImage(blank,File.ReadAllBytes(Path.Combine(folder,"portal-blank-"+distance+"m.png")));
                        view.RefreshDistance(first);target.Render(camera);yield return null;yield return null;target.Render(camera);yield return null;
                        target.SavePng(Path.Combine(folder,"portal-distance-"+distance+"m.png"));
                        shown=new Texture2D(2,2);ImageConversion.LoadImage(shown,File.ReadAllBytes(Path.Combine(folder,"portal-distance-"+distance+"m.png")));
                        int brighter=0;for(int y=0;y<720;y++)for(int x=0;x<1280;x++)
                            if(shown.GetPixel(x,y).grayscale>blank.GetPixel(x,y).grayscale+.3f)brighter++;
                        Assert.That(brighter,Is.GreaterThan(100),"Depth-tested digits must be visible in the actual rendered portal, not hidden behind its beam.");
                    }
                    finally{if(blank!=null)Object.Destroy(blank);if(shown!=null)Object.Destroy(shown);}
                }
            }
            finally{QualitySettings.renderPipeline=previous;}
        }
    }
}

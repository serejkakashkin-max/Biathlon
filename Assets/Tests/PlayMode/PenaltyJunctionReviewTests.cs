using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class PenaltyJunctionReviewTests
    {
        [UnityTest, Timeout(180000)]
        public IEnumerator ActualPenaltyJunctionViews()
        {
            const string prefix = "Biathlon/Venues/woodland_basin_v1/";
            const string folder = "artifacts/m16-2/penalty-spur/";
            string phase = File.Exists(folder + "phase.txt") ? File.ReadAllText(folder + "phase.txt").Trim() : "after";
            Assert.That(phase == "before" || phase == "after", Is.True);
            string output = Path.GetFullPath(folder + phase); Directory.CreateDirectory(output);
            var plan = new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix + "manifest")), ProductionVenueHost.ReadResource);
            var profile = CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix + "mass_start_profile"));
            var route = new RaceRoute(plan.Track, RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix + "mass_start_route")), profile.ToRouteRequirements(), plan.Junctions, plan.Connections);
            var recipe = ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix + "environment"));
            var root = new GameObject("Penalty junction review"); BenchmarkRenderTarget target = null;
            try
            {
                var course = root.AddComponent<ProductionCoursePresentation>();
                course.Initialize(plan, route: route, sharedApron: plan.SharedApron, snowShoulders: ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(recipe.terrain.snow_shoulders_resource), plan));
                var cameraObject = new GameObject("Penalty junction camera"); cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.fieldOfView = 58; camera.farClipPlane = 650;
                var feed = root.AddComponent<RacePresentationFeed>(); var environment = root.AddComponent<ProductionVenueEnvironment>();
                environment.Initialize(plan, recipe, ProductionVenueHost.ReadResource, feed, camera, apron: course.Apron); environment.AutomaticRendering = false;
                course.JoinGround(environment, environment.TerrainGridSpacing, environment.TerrainGridOrigin);
                var quality = root.AddComponent<RenderQualityPresentation>(); quality.Initialize(camera); quality.Apply("high");
                target = new BenchmarkRenderTarget(1280, 720);
                camera.transform.position = new Vector3(110, 8, 24); camera.transform.LookAt(new Vector3(88.5f, 0, -24));
                yield return Capture("stadium");
                if (phase == "before")
                {
                    course.LoopRenderer("main").enabled = course.SkirtRenderer("main").enabled = false;
                    yield return Capture("no-main");
                    course.LoopRenderer("main").enabled = course.SkirtRenderer("main").enabled = true;
                    course.LoopRenderer("start_approach").enabled = course.SkirtRenderer("start_approach").enabled = false;
                    yield return Capture("no-start");
                    course.LoopRenderer("start_approach").enabled = course.SkirtRenderer("start_approach").enabled = true;
                }
                else
                {
                    camera.transform.position = new Vector3(98, 3.5f, 12); camera.transform.LookAt(new Vector3(88.5f, 0, -12));
                    yield return Capture("close");
                    camera.transform.position = new Vector3(69, 5, -4); camera.transform.LookAt(new Vector3(101, 0, -15));
                    yield return Capture("range-exit");
                }
                camera.transform.position = new Vector3(88.5f, 65, 0); camera.transform.LookAt(new Vector3(88.5f, 0, -20));
                yield return Capture("overview");
                Assert.That(course.GroundJoined, Is.True);
                IEnumerator Capture(string name)
                {
                    for (int i = 0; i < 5; i++) { environment.Render(camera, "high", 12); target.Render(camera); yield return null; }
                    target.SavePng(Path.Combine(output, name + ".png"));
                }
            }
            finally { target?.Dispose(); Object.DestroyImmediate(root); }
            yield return null;
        }
    }
}

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
    public sealed class VenueDistantContinuityTests
    {
        private const string Prefix="Biathlon/Venues/woodland_basin_v1/";
        private GameObject root;
        [TearDown]public void Cleanup(){if(root!=null){root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.DestroyImmediate(root);}}
        private Camera View()
        {var go=new GameObject("Visibility regression camera");go.transform.SetParent(root.transform,false);var c=go.AddComponent<Camera>();c.enabled=false;c.fieldOfView=60;c.aspect=16f/9;c.nearClipPlane=.1f;c.farClipPlane=1000;return c;}
        [TestCase("low")][TestCase("balanced")][TestCase("high")]
        public void ActualProductionGrandstandKeepsItsAudienceAcrossTheOldDistanceCut(string tier)
        {
            root=new GameObject("Actual distant venue regression");var camera=View();
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(Prefix+"manifest")),ProductionVenueHost.ReadResource);
            var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(Prefix+"environment"));
            var environment=root.AddComponent<ProductionVenueEnvironment>();environment.AutomaticRendering=false;
            environment.Initialize(venue,recipe,ProductionVenueHost.ReadResource,root.AddComponent<RacePresentationFeed>(),camera);
            var crowd=environment.Crowd(0);Assert.That(crowd.Pose,Is.EqualTo(SpectatorPose.Seated));
            var seating=crowd.GetComponentInChildren<SpectatorSeating>();var center=seating.transform.position+Vector3.up*3;
            camera.transform.position=center-seating.transform.forward*270;camera.transform.LookAt(center);
            crowd.Prepare(camera,tier,0);
            Assert.That(crowd.Visible,Is.EqualTo(crowd.Total),"A fully visible grandstand must not lose its audience simply because it is farther than110m.");
            Assert.That(crowd.Far,Is.EqualTo(crowd.Total));
            Assert.That(crowd.Silhouettes,Is.EqualTo(crowd.Total));
            Assert.That(crowd.ShadowSourceTriangles,Is.Zero,"Distant occupancy does not add detailed spectator shadows.");
            camera.transform.rotation*=Quaternion.Euler(0,180,0);crowd.Prepare(camera,tier,0);Assert.That(crowd.Visible,Is.Zero,"Only visible scenery needs submission.");
        }
        [TestCase("low")][TestCase("balanced")][TestCase("high")]
        public void ActualProductionForestKeepsVisibleFarSilhouettes(string tier)
        {
            root=new GameObject("Distant native fir/pine regression");var camera=View();camera.transform.position=new Vector3(0,3,0);camera.transform.LookAt(new Vector3(0,3,220));
            var forest=root.AddComponent<VegetationPresentation>();
            var instances=new[]{new VegetationInstance(new Vector3(0,0,210),1,0,.04f),new VegetationInstance(new Vector3(20,0,230),1,0,.8f),new VegetationInstance(new Vector3(70,0,240),1,0,.98f)};
            // The same explicit-species, fog-bound initialization path used by the production environment.
            var policy=ProductionVisibilityDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/venue_visibility_v1")).Bind(600);
            forest.Initialize(null,true,null,true,true,instances,new byte[]{0,1,0},600,policy);
            forest.Prepare(camera,tier,0,Vector2.zero);
            Assert.That(forest.NearCount+forest.MidCount+forest.FarCount,Is.EqualTo(3),"In-frustum native tree silhouettes must survive beyond the old120m cutoff on every graphics tier.");
            Assert.That(forest.ShadowSourceTriangles,Is.Zero);
        }
        [Test] public void CompleteCrowdKeepsDensityRanksWithBudgetedDetailAndCachedTransforms()
        {
            root=new GameObject("Crowd detail/cache qualification");var camera=View();camera.transform.position=new Vector3(0,3,-18);camera.transform.LookAt(new Vector3(0,2,2));
            var seating=root.AddComponent<SpectatorSeating>();seating.Configure(StadiumArtDefinition.Load().SeatAnchors());
            var crowd=root.AddComponent<SpectatorCrowd>();var policy=ProductionVisibilityDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/venue_visibility_v1")).Bind(600);crowd.Initialize(seating,true,false,policy);
            crowd.Prepare(camera,"high",12);Assert.That(crowd.Near,Is.EqualTo(crowd.Total));long high=crowd.SubmittedTriangles;
            crowd.Prepare(camera,"low",12);Assert.That(crowd.Visible,Is.EqualTo(crowd.Total));Assert.That(crowd.Near,Is.LessThan(crowd.Total));Assert.That(crowd.Far,Is.GreaterThan(0));Assert.That(crowd.SubmittedTriangles,Is.LessThan(high));
            for(int i=0;i<20;i++)crowd.Prepare(camera,"balanced",i);
            int builds=crowd.GeometryBuildCount;long bytes=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)crowd.Prepare(camera,"balanced",i);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);Assert.That(crowd.GeometryBuildCount,Is.EqualTo(builds));
            root.transform.position+=Vector3.up*.5f;crowd.Prepare(camera,"high",12);Assert.That(crowd.GeometryBuildCount,Is.EqualTo(builds+1));
        }
        [Test] public void WholeAudienceSupportAndCameraFarPlaneBoundTheFogWork()
        {
            root=new GameObject("Partial audience fog qualification");var camera=View();var seating=root.AddComponent<SpectatorSeating>();seating.Configure(new[]{Vector3.zero});
            var crowd=root.AddComponent<SpectatorCrowd>();var policy=ProductionVisibilityDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/venue_visibility_v1")).Bind(120);crowd.Initialize(seating,true,false,policy);
            camera.transform.position=new Vector3(0,1,-121);camera.transform.LookAt(new Vector3(0,1,0));crowd.Prepare(camera,"high",0);
            Assert.That(crowd.Visible,Is.EqualTo(1),"Retain the visible edge even if the seat pivot is beyond the fog plane.");
            camera.transform.position=new Vector3(0,1,-122);crowd.Prepare(camera,"high",0);Assert.That(crowd.Visible,Is.Zero);
            camera.orthographic=true;camera.orthographicSize=10;crowd.Prepare(camera,"high",0);Assert.That(crowd.Visible,Is.EqualTo(1),"An uncertified fog projection must retain its whole frustum.");
            camera.farClipPlane=50;crowd.Prepare(camera,"high",0);Assert.That(crowd.Visible,Is.Zero);
        }
        [Test] public void CompleteVisibilityPolicyIsVersionedBoundedAndDetached()
        {
            var json=ProductionVenueHost.ReadResource("Biathlon/venue_visibility_v1");var d=ProductionVisibilityDefinition.FromJson(json);var bound=d.Bind(600);d.id="changed";
            Assert.That(bound.Id,Is.EqualTo("winter_complete_scenery_visibility_v1"));Assert.That(bound.FogEndMeters,Is.EqualTo(600));
            Assert.Throws<ArgumentException>(()=>d.Bind(float.NaN));Assert.Throws<ArgumentException>(()=>d.Bind(0));
            Assert.Throws<ArgumentException>(()=>ProductionVisibilityDefinition.FromJson(json.Replace("\"schema_version\": 1","\"schema_version\": 2")));
            Assert.Throws<ArgumentException>(()=>ProductionVisibilityDefinition.FromJson(json.Replace("complete_fog_frustum_v1","unknown")));
        }
        [TestCase("SpectatorLOD")][TestCase("StandingSpectatorLOD")]
        public void DistantOriginalModelsKeepBodyColourHeightAndPoseWithOneSixthOfTheFarTriangles(string resource)
        {
            var old=Resources.Load<Mesh>("Biathlon/Crowd/"+resource+"1");var silhouette=Resources.Load<Mesh>("Biathlon/Crowd/"+resource+"2");
            Assert.That(silhouette,Is.Not.Null);Assert.That(silhouette.GetIndexCount(0)*6,Is.EqualTo(old.GetIndexCount(0)));
            Assert.That(silhouette.bounds.max.y,Is.EqualTo(old.bounds.max.y).Within(.0001));Assert.That(silhouette.bounds.min.y,Is.EqualTo(old.bounds.min.y).Within(.0001));
            Assert.That(silhouette.bounds.size.x,Is.EqualTo(old.bounds.size.x).Within(.0001));
            Assert.That(silhouette.colors.Length,Is.EqualTo(silhouette.vertexCount));Assert.That(silhouette.normals.Length,Is.EqualTo(silhouette.vertexCount));
            var pivots=new System.Collections.Generic.List<Vector4>();silhouette.GetUVs(0,pivots);Assert.That(pivots.Count,Is.EqualTo(silhouette.vertexCount));
            foreach(var p in pivots)Assert.That(p.w,Is.InRange(0,1));foreach(var v in silhouette.vertices)Assert.That(float.IsFinite(v.sqrMagnitude),Is.True);
        }
    }
}

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class RangeCoachesTests
    {
        private const string Prefix="Biathlon/Venues/woodland_basin_v1/";
        private static string Read(string name)=>Resources.Load<TextAsset>(name).text;
        private static ProductionEnvironmentDefinition Venue()=>ProductionEnvironmentDefinition.FromJson(Read(Prefix+"environment"));
        private static RangeCoachesDefinition Definition()=>RangeCoachesDefinition.FromJson(Read(Prefix+"range_coaches_v1"));
        [Test] public void EnclosureClearsAllAuthoredFormatAndMatCorridors()
        {
            var d=Definition();var e=Venue();var layout=d.Layout(e.range);
            Assert.That(layout.ClearsCourses(e.route_corridors,d.route_clearance_m),Is.True);
            Assert.That(layout.Top,Is.GreaterThanOrEqualTo(.3f));Assert.That(layout.Front-layout.Back,Is.GreaterThanOrEqualTo(2));
            Assert.That(d.coaches.Length,Is.EqualTo(12));Assert.That(new HashSet<string>(Array.ConvertAll(d.coaches,c=>c.nation)).Count,Is.EqualTo(6));
            Assert.That(new HashSet<string>(Array.ConvertAll(d.coaches,c=>c.face)).Count,Is.EqualTo(6));
            Assert.That(new HashSet<string>(Array.ConvertAll(d.coaches,c=>c.skin)).Count,Is.GreaterThanOrEqualTo(6));
        }
        [Test] public void PlacementFollowsTranslatedRotatedRangeAndRejectsCrossingSegment()
        {
            var d=Definition();var e=Venue();var first=d.Layout(e.range);var q=Quaternion.Euler(0,63,0);var offset=new Vector3(102,5,-33);
            e.range.origin=q*e.range.origin+offset;e.range.forward=q*e.range.forward;
            foreach(var bank in e.range.banks){bank.position=q*bank.position+offset;bank.mat_position=q*bank.mat_position+offset;}
            foreach(var path in e.route_corridors)foreach(var k in path.knots){var p=q*k.Position+offset;k.x=p.x;k.y=p.y;k.z=p.z;}
            var second=d.Layout(e.range,_=>5);
            for(int i=0;i<12;i++)Assert.That(Vector3.Distance(second.CoachPosition(i,12),q*first.CoachPosition(i,12)+offset),Is.LessThan(.0001f));
            Assert.That(second.ClearsCourses(e.route_corridors,3),Is.True);
            var crossing=new ProductionRouteCorridor{knots=new[]{new ProductionRouteKnot{x=-200,z=-18},new ProductionRouteKnot{x=200,z=-18}},widths_m=new[]{1f,1f}};
            Assert.That(first.ClearsCourses(new[]{crossing},3),Is.False,"Both endpoints can clear while a segment crosses the whole enclosure.");
            Assert.Throws<ArgumentException>(()=>d.Layout(e.range,_=>float.NaN));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)] public void InvalidDefinitionsFailBeforeArtCreation(int kind)
        {
            var d=Definition();if(kind==0)d.raised_m=.1f;if(kind==1)d.coaches[1].id=d.coaches[0].id;if(kind==2)d.coaches[0].face="missing";
            Assert.Throws<ArgumentException>(()=>RangeCoachesDefinition.FromJson(JsonUtility.ToJson(d)));
        }
        [UnityTest] public IEnumerator StaticAthleteDerivedArtAimsAtTargetsAndCleansUp()
        {
            var root=new GameObject("Static coach qualification");var view=root.AddComponent<RangeCoachesPresentation>();Mesh props=null;
            string source=JsonUtility.ToJson(Venue());var shared=new List<Mesh>();
            try
            {
                var e=Venue();view.Initialize(Definition(),e,_=>-.03f);yield return null;
                Assert.That(view.Count,Is.EqualTo(12));Assert.That(view.ScopeCount,Is.EqualTo(12));Assert.That(root.GetComponentsInChildren<Collider>(),Is.Empty);
                Assert.That(root.GetComponentsInChildren<Animator>(),Is.Empty);Assert.That(root.GetComponentsInChildren<SkinnedMeshRenderer>(),Is.Empty);
                Assert.That(root.GetComponentsInChildren<MonoBehaviour>().Length,Is.EqualTo(13),"Only the enclosure owner and twelve data-only eye anchors; no athlete frame scripts.");
                for(int i=0;i<12;i++)
                {
                    var art=view.Coach(i);var p=view.Layout.Local(art.transform.position);Assert.That(p.y,Is.EqualTo(view.Layout.Top).Within(.0001));
                    Assert.That(Vector3.Distance(view.ScopeEye(i),art.Eye),Is.EqualTo(.025f).Within(.0001));
                    Assert.That(Vector3.Dot(view.ScopeDirection(i),e.range.forward),Is.GreaterThan(.999f));
                    Assert.That(art.GetComponent<LODGroup>().GetLODs().Length,Is.EqualTo(3));
                    foreach(var filter in art.GetComponentsInChildren<MeshFilter>())
                    {Assert.That(filter.sharedMesh.subMeshCount,Is.EqualTo(1));Assert.That(filter.sharedMesh.blendShapeCount,Is.Zero);shared.Add(filter.sharedMesh);}
                }
                foreach(var filter in root.GetComponentsInChildren<MeshFilter>())if(filter.GetComponentInParent<RangeCoachArt>()==null)props=filter.sharedMesh;
                Assert.That(props,Is.Not.Null);Assert.That(JsonUtility.ToJson(e),Is.EqualTo(source));
                Assert.That(view.ClearsTree(new ProductionTree{species="fir",scale=1,position=view.Coach(0).transform.position}),Is.False);
                Assert.That(view.ClearsTree(new ProductionTree{species="pine",scale=1.5f,position=new Vector3(1000,0,1000)}),Is.True);
                view.Shutdown();yield return null;
                Assert.That(props==null,Is.True);foreach(var mesh in shared)Assert.That(mesh,Is.Not.Null,"Shutdown must not destroy shared baked assets.");
                Assert.That(view.Count,Is.Zero);Assert.That(root.transform.childCount,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator NativeHeroCoachFacesAndProductionPlacementViews()
        {
            const string output="artifacts/m16-4/coaches/views/";Directory.CreateDirectory(output);
            var root=new GameObject("Native coaches' venue review");BenchmarkRenderTarget target=null;
            try
            {
                var camera=new GameObject("Native coach camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.enabled=false;camera.fieldOfView=52;camera.nearClipPlane=.1f;camera.farClipPlane=800;
                var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(camera);quality.Apply("high");
                var e=Venue();var plan=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Prefix+"manifest")),Read);
                var binding=plan.Formats.Get(0);var route=new RaceRoute(plan.Track,binding.Route,binding.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
                var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route,sharedApron:plan.SharedApron,snowShoulders:ProductionSnowShoulderDefinition.FromJson(Read(e.terrain.snow_shoulders_resource),plan));
                var view=root.AddComponent<ProductionVenueEnvironment>();view.AutomaticRendering=false;view.Initialize(plan,e,Read,root.AddComponent<RacePresentationFeed>(),camera,apron:course.Apron);
                course.JoinGround(view,view.TerrainGridSpacing,view.TerrainGridOrigin);
                Assert.That(view.Coaches.Count,Is.EqualTo(12));Assert.That(view.Coaches.Layout.ClearsCourses(e.route_corridors,3),Is.True);
                for(int i=0;i<view.Dressing.Count;i++)Assert.That(view.Coaches.Layout.DistanceToFootprint(view.Dressing.Position(i)),Is.GreaterThan(3));
                target=new BenchmarkRenderTarget(1600,900);
                yield return Capture("overview",new Vector3(-55,11,-36),new Vector3(0,1,4));
                yield return Capture("coach-row",new Vector3(-20,2.8f,-11),new Vector3(-17,1.2f,-18.1f));
                foreach(int i in new[]{0,1,3,8})
                {
                    var coach=view.Coaches.Coach(i);var eye=coach.Eye;
                    yield return Capture("face-"+i,eye+new Vector3(-.65f,.12f,1.15f),eye-Vector3.up*.15f);
                }
                File.WriteAllText(output+"review.json","{\"schema_version\":1,\"id\":\"m16_4_coaches_native_views\",\"coaches\":"+view.Coaches.Count+",\"scopes\":"+view.Coaches.ScopeCount+",\"excluded_trees\":"+view.CoachClearanceExcludedTrees+",\"colliders\":"+root.GetComponentsInChildren<Collider>().Length+",\"skinned_renderers\":"+root.GetComponentsInChildren<SkinnedMeshRenderer>().Length+"}");
                IEnumerator Capture(string name,Vector3 position,Vector3 look)
                {
                    camera.transform.position=position;camera.transform.LookAt(look);
                    for(int i=0;i<5;i++){view.Render(camera,"high",12);target.Render(camera);yield return null;}
                    var path=Path.GetFullPath(output+name+".png");target.SavePng(path);var probe=new Texture2D(2,2);
                    try{ImageConversion.LoadImage(probe,File.ReadAllBytes(path));int nonblack=0;foreach(var pixel in probe.GetPixels32())if(pixel.r>12||pixel.g>12||pixel.b>12)nonblack++;Assert.That(nonblack,Is.GreaterThan(10000));}finally{Object.DestroyImmediate(probe);}
                }
            }
            finally{target?.Dispose();root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();Object.DestroyImmediate(root);}
            yield return null;
        }
    }
}

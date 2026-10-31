using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class FormatSnowCutGeometryTests
    {
        [Serializable]private sealed class MeshDump {public Vector3[] points;public int[] faces;}
        [TestCase("sprint",0f,false)][TestCase("sprint",45f,false)]
        [TestCase("pursuit",0f,false)][TestCase("pursuit",45f,false)]
        [TestCase("sprint",0f,true)][TestCase("pursuit",0f,true)]
        [TestCase("mass_start",0f,false)]
        public void ActualFormatJoinsKeepPackedBuffersAndWholeTerrainContact(string format,float yaw,bool runout)
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment"));
            string profileId=format=="mass_start"?"mass_start":format;
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+profileId+"_profile"));
            var route=new RaceRoute(plan.Track,RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+profileId+"_route")),profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            string trackBefore=plan.Track.ToJson(),visualBefore=plan.Visual.ToJson(),pathsBefore=plan.Paths.ToJson();
            var root=new GameObject("Actual format-cut contact fixture");root.transform.SetPositionAndRotation(new Vector3(200,70,-80),Quaternion.Euler(0,yaw,0));
            GameObject witnessObject=null;var owned=new List<Mesh>();
            try
            {
                var course=root.AddComponent<ProductionCoursePresentation>();
                course.Initialize(plan,route:route,sharedApron:plan.SharedApron,snowShoulders:ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(recipe.terrain.snow_shoulders_resource),plan),
                    finishRunout:runout?ManualFinishRunoutDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/manual_finish_runout_gameplay_v1")):null);
                var originals=new Dictionary<string,(Vector3[],int[],Vector3[],int[])>();
                for(int i=0;i<course.LoopCount;i++)
                {string id=course.LoopId(i);originals.Add(id,(course.NativeLoopMesh(id).vertices,course.NativeLoopMesh(id).triangles,course.LoopMesh(id).vertices,course.LoopMesh(id).triangles));}
                var cameraObject=new GameObject("Native contact camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                var environment=root.AddComponent<ProductionVenueEnvironment>();environment.Initialize(plan,recipe,ProductionVenueHost.ReadResource,root.AddComponent<RacePresentationFeed>(),camera,apron:course.Apron);environment.AutomaticRendering=false;
                course.JoinGround(environment,environment.TerrainGridSpacing,environment.TerrainGridOrigin);
                if(format=="sprint"&&yaw==0&&!runout)
                {
                    string output=Path.GetFullPath("artifacts/m16-2/snow-seams/geometry");Directory.CreateDirectory(output);
                    for(int loop=0;loop<course.LoopCount;loop++)
                    {
                        string id=course.LoopId(loop);var mesh=course.SkirtMesh(id);var p=mesh.vertices;var frame=course.SkirtRenderer(id).localToWorldMatrix;
                        for(int i=0;i<p.Length;i++)p[i]=frame.MultiplyPoint3x4(p[i]);
                        File.WriteAllText(Path.Combine(output,id+".json"),JsonUtility.ToJson(new MeshDump{points=p,faces=mesh.triangles}));
                        mesh=course.LoopMesh(id);p=mesh.vertices;frame=course.LoopRenderer(id).localToWorldMatrix;
                        for(int i=0;i<p.Length;i++)p[i]=frame.MultiplyPoint3x4(p[i]);
                        File.WriteAllText(Path.Combine(output,"packed_"+id+".json"),JsonUtility.ToJson(new MeshDump{points=p,faces=mesh.triangles}));
                    }
                }
                Assert.That(course.RoundedSnowCutCount,Is.GreaterThan(0),
                    "Actual selected format and replaced-range cuts receive rounded snow caps, including MassStart's unused bypass.");
                if(format=="sprint")
                {
                    var outside=new Vector3(226.96336f,0,-111.8641f);var inside=new Vector3(226.96336f,0,-112.4641f);
                    Assert.That(RoofCovers(course.LoopMesh("main"),course.LoopRenderer("main").localToWorldMatrix,inside),Is.True,"Control: negative Z lies inside the actual packed main ribbon.");
                    Assert.That(RoofCovers(course.SkirtMesh("main"),course.SkirtRenderer("main").localToWorldMatrix,inside),Is.False,"Decorative snow must not cover the paid main track.");
                    for(int loop=0;loop<course.LoopCount;loop++)Assert.That(RoofCovers(course.LoopMesh(loop),course.LoopRenderer(course.LoopId(loop)).localToWorldMatrix,outside),Is.False,"The visual regression witness must be outside every actual paid ribbon.");
                    Assert.That(RoofCovers(course.Apron.Top,course.ApronRenderer.localToWorldMatrix,outside),Is.False,"The witness must also be outside the shared packed apron.");
                    Assert.That(RoofCovers(course.SkirtMesh("main"),course.SkirtRenderer("main").localToWorldMatrix,outside),Is.True,
                        "The actual return-fork bank needs an upward roof 0.3m beyond its exposed front edge; a retained vertical wall cannot satisfy this independent witness.");
                }
                foreach(var pair in originals)
                {
                    var native=course.NativeLoopMesh(pair.Key);CollectionAssert.AreEqual(pair.Value.Item1,native.vertices);CollectionAssert.AreEqual(pair.Value.Item2,native.triangles);
                    // Actual terrain safety may constrain the separate render rim, but
                    // its source remains exact, its heights/widths/centers stay fixed.
                    var actual=course.LoopMesh(pair.Key).vertices;var prior=pair.Value.Item3;
                    CollectionAssert.AreEqual(pair.Value.Item4,course.LoopMesh(pair.Key).triangles);
                    for(int row=0;row<actual.Length;row+=2)
                    {
                        Assert.That(actual[row].y,Is.EqualTo(prior[row].y));Assert.That(actual[row+1].y,Is.EqualTo(prior[row+1].y));
                        Assert.That(Vector3.Distance((actual[row]+actual[row+1])*.5f,(prior[row]+prior[row+1])*.5f),Is.LessThan(1e-4f));
                        Assert.That(Vector3.Distance(actual[row],actual[row+1]),Is.EqualTo(Vector3.Distance(prior[row],prior[row+1])).Within(1e-4f));
                    }
                }
                Assert.That(plan.Track.ToJson(),Is.EqualTo(trackBefore));Assert.That(plan.Visual.ToJson(),Is.EqualTo(visualBefore));Assert.That(plan.Paths.ToJson(),Is.EqualTo(pathsBefore));
                AssertComposite();
                void AssertComposite()
                {
                    int size=course.LoopCount+1;var ribbons=new Mesh[size];var natives=new Mesh[size];var ids=new string[size];var joins=new List<CombineInstance>();
                    for(int i=0;i<course.LoopCount;i++)
                    {
                        string id=ids[i]=course.LoopId(i);var frame=course.LoopRenderer(id).localToWorldMatrix;
                        ribbons[i]=World(course.LoopMesh(id),frame);natives[i]=World(course.LoopMesh(id),frame);natives[i].triangles=course.NativeLoopMesh(id).triangles;
                        joins.Add(new CombineInstance{mesh=World(course.SkirtMesh(id),course.SkirtRenderer(id).localToWorldMatrix)});
                    }
                    ids[size-1]="actual_continuous_apron";ribbons[size-1]=World(course.Apron.Top,course.ApronRenderer.localToWorldMatrix);natives[size-1]=ribbons[size-1];
                    joins.Add(new CombineInstance{mesh=World(course.ApronSkirt,course.ApronSkirtRenderer.localToWorldMatrix)});
                    var composite=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};owned.Add(composite);composite.CombineMeshes(joins.ToArray(),true,false);
                    witnessObject=new GameObject("Independent whole-format witness");var witness=witnessObject.AddComponent<ProductionCoursePresentation>();
                    Set("loopIds",ids);Set("meshes",ribbons);Set("nativeMeshes",natives);Set("<PresentationRouteId>k__BackingField",course.PresentationRouteId);
                    var skirts=new Mesh[size];int main=Array.IndexOf(ids,"main");skirts[main]=composite;Set("skirts",skirts);
                    var mapping=(Dictionary<string,int>)typeof(ProductionCoursePresentation).GetField("indices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(witness);
                    for(int i=0;i<ids.Length;i++)mapping.Add(ids[i],i);
                    var world=new ProductionTerrainContactTests.JoinedWorld(environment,witness);
                    ProductionTerrainContactTests.AssertDrawnRibbonGroundClearance(environment,witness,world);
                    ProductionTerrainContactTests.AssertJoinedGeometry(environment,witness,"main",natives[main].vertices,true,world,entireVenue:true);
                    void Set(string field,object value)=>typeof(ProductionCoursePresentation).GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(witness,value);
                }
                Mesh World(Mesh mesh,Matrix4x4 frame)
                {
                    var points=mesh.vertices;for(int i=0;i<points.Length;i++)points[i]=frame.MultiplyPoint3x4(points[i]);
                    var copy=new Mesh{name=mesh.name,indexFormat=mesh.indexFormat,vertices=points,triangles=mesh.triangles,normals=mesh.normals,uv=mesh.uv,colors=mesh.colors};copy.RecalculateBounds();owned.Add(copy);return copy;
                }
            }
            finally
            {
                if(witnessObject!=null){typeof(ProductionCoursePresentation).GetField("skirts",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(witnessObject.GetComponent<ProductionCoursePresentation>(),null);Object.DestroyImmediate(witnessObject);}
                foreach(var mesh in owned)Object.DestroyImmediate(mesh);Object.DestroyImmediate(root);
            }
        }
        private static bool RoofCovers(Mesh mesh,Matrix4x4 frame,Vector3 point)
        {
            var p=mesh.vertices;for(int i=0;i<p.Length;i++)p[i]=frame.MultiplyPoint3x4(p[i]);var faces=mesh.triangles;
            for(int i=0;i<faces.Length;i+=3)
            {
                var a=p[faces[i]];var b=p[faces[i+1]];var c=p[faces[i+2]];double area=Cross(a,b,c);
                if(area>=-1e-12)continue;
                double u=Cross(a,point,c)/area,v=Cross(a,b,point)/area;
                if(u>=-1e-8&&v>=-1e-8&&u+v<=1+1e-8)return true;
            }
            return false;
            double Cross(Vector3 a,Vector3 b,Vector3 c)=>((double)b.x-a.x)*((double)c.z-a.z)-((double)b.z-a.z)*((double)c.x-a.x);
        }
    }
}

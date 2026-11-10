using System;
using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public sealed class ForestLodContinuityTests
    {
        [UnityTest] public IEnumerator NativeFoliageDoesNotChangeAbruptlyAcrossLodBoundaries()
        {
            var root=new GameObject("Native foliage continuity");
            string folder=Path.GetFullPath("artifacts/m16-4/forest-ui/epsilon-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));Directory.CreateDirectory(folder);
            bool fog=RenderSettings.fog;var ambient=RenderSettings.ambientLight;
            try
            {
                var camera=new GameObject("Continuity camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.enabled=false;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;camera.fieldOfView=30;camera.aspect=1;camera.farClipPlane=300;
                var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(camera);quality.Apply("high");
                RenderSettings.fog=false;RenderSettings.ambientLight=Color.white;
                var sun=new GameObject("Continuity light").AddComponent<Light>();sun.transform.SetParent(root.transform);sun.type=LightType.Directional;sun.intensity=2;sun.transform.rotation=Quaternion.Euler(35,-40,0);
                var policy=ProductionVisibilityDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text).Bind(250);
                var d=VegetationDefinition.Load();
                foreach(byte species in new byte[]{0,1})
                {
                    var grove=new GameObject("Single native tree");grove.transform.SetParent(root.transform);
                    var forest=grove.AddComponent<VegetationPresentation>();forest.Initialize(null,true,null,true,true,new[]{new VegetationInstance(Vector3.zero,1,0,0)},new[]{species},completeVisibility:policy);
                    try
                    {
                        foreach(float boundary in new[]{d.near_lod_m,d.near_lod_m+policy.ForestNearBlendMeters*.5f,d.near_lod_m+policy.ForestNearBlendMeters,
                            policy.ForestFarLodMeters,policy.ForestFarLodMeters+policy.ForestFarBlendMeters*.5f,policy.ForestFarLodMeters+policy.ForestFarBlendMeters})
                        {
                            Color32[][] captures=new Color32[2][];
                            for(int side=0;side<2;side++)
                            {
                                camera.transform.position=new Vector3(0,0,-(boundary+(side==0?-.001f:.001f)));camera.transform.LookAt(Vector3.up*4);
                                using(var target=new BenchmarkRenderTarget(512,512))
                                {
                                    for(int frame=0;frame<3;frame++){forest.Render(camera,"high",0,Vector2.zero);target.Render(camera);yield return null;}
                                    string path=Path.Combine(folder,species+"-"+boundary+"-"+side+".png");target.SavePng(path);
                                    var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(path));captures[side]=image.GetPixels32();Object.DestroyImmediate(image);
                                }
                            }
                            int changed=0,treePixels=0;
                            for(int i=0;i<captures[0].Length;i++)
                            {
                                var a=captures[0][i];var b=captures[1][i];
                                if(a.g>20||a.r<150||a.b<150)treePixels++;
                                if(Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b)>60)changed++;
                            }
                            Assert.That(treePixels,Is.GreaterThan(100),"The actual tree must be rendered, not an empty output.");
                            TestContext.WriteLine("species="+species+" boundary="+boundary+" changed="+changed+" tree_pixels="+treePixels);
                            File.AppendAllText(Path.Combine(folder,"measurements.txt"),"species="+species+" distance="+boundary+" changed="+changed+" tree_pixels="+treePixels+" fraction="+(changed/(double)treePixels).ToString("R",System.Globalization.CultureInfo.InvariantCulture)+"\n");
                            Assert.That(changed/(double)treePixels,Is.LessThan(.02),"Moving 2 mm must not replace the native canopy/branches. Captures: "+folder);
                        }
                    }
                    finally{Object.DestroyImmediate(grove);}
                }
            }
            finally{RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;Object.DestroyImmediate(root);}
        }
        [TestCase("high")][TestCase("balanced")][TestCase("low")]
        public void AdjacentSubmissionsKeepUniqueCountsRewindAndAllocateNothing(string tier)
        {
            var root=new GameObject("Bounded transition submission");
            try
            {
                var camera=root.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=80;camera.farClipPlane=300;
                var policy=ProductionVisibilityDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text).Bind(250);
                var forest=root.AddComponent<VegetationPresentation>();forest.Initialize(null,true,null,true,true,new[]{
                    new VegetationInstance(new Vector3(0,0,30),1,0,.04f),new VegetationInstance(new Vector3(0,0,50),1,0,.04f),
                    new VegetationInstance(new Vector3(4,0,30),1,0,.98f),new VegetationInstance(new Vector3(0,0,90),1,0,.04f)},new byte[]{0,1,0,1},completeVisibility:policy);
                forest.Prepare(camera,tier,12);int visible=forest.VisibleInstances,rendered=forest.RenderedInstances,transitions=forest.TransitionInstances;long triangles=forest.SubmittedTriangles;
                Assert.That(visible,Is.EqualTo(4));Assert.That(transitions,Is.EqualTo(tier=="high"?3:2));Assert.That(rendered,Is.EqualTo(visible+transitions));
                Assert.That(rendered,Is.LessThanOrEqualTo(visible*2));Assert.That(forest.FarCount,Is.GreaterThanOrEqualTo(tier=="high"?1:2),"Low density ranks retain the same far silhouette even inside a fade band.");
                for(int i=0;i<20;i++)forest.Prepare(camera,tier,i);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)forest.Prepare(camera,tier,i);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(forest.GeometryBuildCount,Is.EqualTo(1));
                camera.transform.position=new Vector3(0,0,-150);forest.Prepare(camera,tier,100);camera.transform.position=Vector3.zero;forest.Prepare(camera,tier,12);
                Assert.That(forest.VisibleInstances,Is.EqualTo(visible));Assert.That(forest.RenderedInstances,Is.EqualTo(rendered));Assert.That(forest.SubmittedTriangles,Is.EqualTo(triangles));
                var shader=Resources.Load<Material>("Biathlon/Vegetation/G2Fir/Fir");Assert.That(shader.shader.isSupported,Is.True);Assert.That(shader.HasProperty("_ForestLodEnabled"),Is.True);
                Assert.That(shader.FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));Assert.That(shader.FindPass("DepthOnly"),Is.GreaterThanOrEqualTo(0));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test] public void TransitionDefinitionRejectsInvalidAndOneSidedRanges()
        {
            string json=Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text;
            foreach(string invalid in new[]{json.Replace("12","-1"),json.Replace("12","0"),json.Replace("16","31"),json.Replace("42","121")})
                Assert.Throws<ArgumentException>(()=>ProductionVisibilityDefinition.FromJson(invalid));
            var d=ProductionVisibilityDefinition.FromJson(json);var bound=d.Bind(250);d.forest_near_blend_m=1;
            Assert.That(bound.ForestNearBlendMeters,Is.EqualTo(12),"The prepared policy is immutable.");
        }
        [Test] public void WoodlandUsesFewerMeshTrianglesWithIdenticalCoverageAndCasters()
        {
            var root=new GameObject("Woodland forest work comparison");
            try
            {
                var camera=new GameObject("Independent forest observer",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(root.transform);
                camera.enabled=false;camera.fieldOfView=60;camera.farClipPlane=300;
                var current=ProductionVisibilityDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text);
                var old=ProductionVisibilityDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text);
                old.forest_far_lod_m=0;old.forest_far_blend_m=20;
                var positions=new[]{new VegetationInstance(new Vector3(0,0,62),1,0,0),new VegetationInstance(new Vector3(5,0,60),1,0,0)};
                VegetationPresentation Build(ProductionVisibilityDefinition data)
                {
                    var tree=new GameObject("Forest comparison grove");tree.transform.SetParent(root.transform);
                    var forest=tree.AddComponent<VegetationPresentation>();forest.Initialize(null,true,null,true,true,positions,new byte[]{0,1},completeVisibility:data.Bind(250));
                    forest.Prepare(camera,"high",0);return forest;
                }
                var before=Build(old);var after=Build(current);
                Assert.That(after.VisibleInstances,Is.EqualTo(2).And.EqualTo(before.VisibleInstances));
                Assert.That(after.ShadowSourceTriangles,Is.EqualTo(before.ShadowSourceTriangles));
                Assert.That(after.SubmittedTriangles,Is.LessThan(before.SubmittedTriangles));
                var key=new GameObject("Independent sunlight",typeof(Light)).GetComponent<Light>();key.transform.SetParent(root.transform);
                key.type=LightType.Directional;key.transform.rotation=Quaternion.Euler(60,90,0);
                after.ShadowLight=key;camera.transform.position=new Vector3(500,0,0);after.Prepare(camera,"high",0);
                Assert.That(after.PreparedInstances,Is.Zero,"An invisible grove with no possible shadow contribution can be skipped as a unit.");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}

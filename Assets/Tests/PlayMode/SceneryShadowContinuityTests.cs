using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class SceneryShadowContinuityTests
    {
        [Test] public void ProductionCrowdKeepsShadowsAcrossDistanceAndViewRotationWithoutAllocations()
        {
            var root=new GameObject("Crowd caster continuity");
            try
            {
                var observer=new GameObject("Independent shadow observer",typeof(Camera));observer.transform.SetParent(root.transform);var camera=observer.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=70;camera.farClipPlane=300;
                var seats=root.AddComponent<SpectatorSeating>();seats.Configure(new[]{new Vector3(0,0,30)},SpectatorPose.Standing);
                var crowd=root.AddComponent<SpectatorCrowd>();crowd.Initialize(seats,true,false,ProductionVisibilityDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text).Bind(220));
                crowd.Prepare(camera,"high",0);Assert.That(crowd.ShadowSourceTriangles,Is.GreaterThan(0));long triangles=crowd.ShadowSourceTriangles;
                camera.transform.rotation=Quaternion.Euler(0,180,0);crowd.Prepare(camera,"high",0);
                Assert.That(crowd.Visible,Is.Zero);Assert.That(crowd.ShadowSourceTriangles,Is.EqualTo(triangles));Assert.That(crowd.ShadowCasters,Is.EqualTo(1));
                for(int i=0;i<10;i++)crowd.Prepare(camera,"high",0);
                long before=System.GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)crowd.Prepare(camera,"high",0);
                Assert.That(System.GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator OffscreenNativeTreeStillShadowsVisibleSnow()
        {
            var root=new GameObject("Offscreen shadow regression");bool fog=RenderSettings.fog;var ambient=RenderSettings.ambientLight;var oldSun=RenderSettings.sun;
            Material snow=null;
            try
            {
                var camera=new GameObject("Shadow receiver camera",typeof(Camera)).GetComponent<Camera>();camera.transform.SetParent(root.transform);
                camera.enabled=false;camera.cullingMask=1<<31;camera.fieldOfView=40;camera.aspect=1;camera.farClipPlane=200;
                camera.transform.position=new Vector3(0,3,-10);camera.transform.LookAt(new Vector3(0,0,15));
                root.AddComponent<RenderQualityPresentation>().Initialize(camera);root.GetComponent<RenderQualityPresentation>().Apply("high");
                RenderSettings.fog=false;RenderSettings.ambientLight=Color.gray*.2f;
                var sun=new GameObject("Directional shadow light",typeof(Light)).GetComponent<Light>();sun.transform.SetParent(root.transform);
                sun.type=LightType.Directional;sun.shadows=LightShadows.Hard;sun.intensity=1.5f;sun.shadowBias=.02f;sun.shadowNormalBias=.1f;
                sun.transform.rotation=Quaternion.Euler(20,90,0);sun.cullingMask=1<<31;RenderSettings.sun=sun;
                var plane=GameObject.CreatePrimitive(PrimitiveType.Plane);plane.transform.SetParent(root.transform);plane.transform.localScale=Vector3.one*10;plane.layer=31;
                snow=new Material(Shader.Find("Universal Render Pipeline/Lit"));snow.color=Color.white;plane.GetComponent<Renderer>().sharedMaterial=snow;
                var position=new Vector3(-12,0,8);
                var reference=new GameObject("Ordinary native shadow caster",typeof(MeshFilter),typeof(MeshRenderer));reference.transform.SetParent(root.transform);reference.transform.position=position;reference.layer=31;
                reference.GetComponent<MeshFilter>().sharedMesh=Resources.Load<Mesh>("Biathlon/Vegetation/G2Fir/FirLOD1");
                var renderer=reference.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("Biathlon/Vegetation/G2Fir/Fir");renderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
                var grove=new GameObject("Production explicit-camera forest");grove.transform.SetParent(root.transform);grove.layer=31;
                var forest=grove.AddComponent<VegetationPresentation>();forest.Initialize(null,true,null,true,true,new[]{new VegetationInstance(position,1,0,0)},new byte[]{0},completeVisibility:ProductionVisibilityDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text).Bind(220));
                var captures=new Color32[3][];string folder=Path.GetFullPath("artifacts/m16-6/russia-shadows");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(512,512))for(int mode=0;mode<3;mode++)
                {
                    renderer.enabled=mode==1;
                    for(int f=0;f<4;f++){if(mode==2)forest.Render(camera,"high",0,Vector2.zero);target.Render(camera);yield return null;}
                    string file=Path.Combine(folder,"offscreen-"+mode+".png");target.SavePng(file);
                    var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(file));captures[mode]=image.GetPixels32();Object.Destroy(image);
                }
                int referencePixels=0,missing=0;
                for(int i=0;i<captures[0].Length;i++)if(captures[0][i].r-captures[1][i].r>25)
                {referencePixels++;if(captures[0][i].r-captures[2][i].r<15)missing++;}
                Assert.That(referencePixels,Is.GreaterThan(100),"Reference must cast a visible shadow into the camera's snow receiver.");
                Assert.That(missing/(float)referencePixels,Is.LessThan(.15f),"Frustum-culling the tree must not remove its shadow from visible snow.");
            }
            finally{RenderSettings.fog=fog;RenderSettings.ambientLight=ambient;RenderSettings.sun=oldSun;Object.DestroyImmediate(root);if(snow!=null)Object.DestroyImmediate(snow);}
        }
        [Test] public void ProductionFarTreeKeepsShadowGeometryBeyondVisibleLodTransition()
        {
            var root=new GameObject("Far shadow regression");
            try
            {
                var observer=new GameObject("Independent shadow observer",typeof(Camera));observer.transform.SetParent(root.transform);var camera=observer.GetComponent<Camera>();camera.enabled=false;camera.fieldOfView=70;camera.farClipPlane=300;
                var forest=root.AddComponent<VegetationPresentation>();forest.Initialize(null,true,null,true,true,new[]{new VegetationInstance(new Vector3(0,0,80),1,0,0)},new byte[]{0},completeVisibility:ProductionVisibilityDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/venue_visibility_v1").text).Bind(220));
                forest.Prepare(camera,"high",0);Assert.That(forest.VisibleInstances,Is.EqualTo(1));
                Assert.That(forest.ShadowSourceTriangles,Is.GreaterThan(0),"Visible far foliage must not silently lose its shadow at the final LOD.");
            }
            finally{Object.DestroyImmediate(root);}
        }
    }
}

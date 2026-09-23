using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2CrowdTests
    {
        [Test] public void SleeveChannelsKeepElbowAndShoulderPivotsDistinct()
        {
            for(int variant=0;variant<2;variant++)for(int lod=0;lod<2;lod++)
            {
                var mesh=Resources.Load<Mesh>("Biathlon/RestrictedG2Crowd/Crowd"+variant+"LOD"+lod);
                var pivots=new List<Vector4>();mesh.GetUVs(0,pivots);int forearms=0,upper=0;
                foreach(var p in pivots)
                {
                    Assert.That(p.w,Is.InRange(0,2));
                    if(p.w>1.1f){Assert.That(p.y,Is.EqualTo(.69f).Within(.0001f));forearms++;}
                    if(p.w==1&&p.y>.9f)upper++;
                }
                Assert.That(forearms,Is.GreaterThan(50));Assert.That(upper,Is.GreaterThan(50));
            }
        }
        [Test] public void LocalDerivativesHaveBoundedSeatedLodsAndHeadOnlyTextureChannels()
        {
            for(int variant=0;variant<2;variant++)for(int lod=0;lod<2;lod++)
            {
                var m=Resources.Load<Mesh>("Biathlon/RestrictedG2Crowd/Crowd"+variant+"LOD"+lod);Assert.That(m,Is.Not.Null,"Local licensed inputs required: scripts/Build-G2Crowd.ps1.");
                Assert.That(m.GetIndexCount(0)/3,Is.LessThan(lod==0?3500:1400));var v=m.vertices;var uv=new List<Vector3>();m.GetUVs(1,uv);Assert.That(uv.Count,Is.EqualTo(v.Length));int face=0;
                for(int i=0;i<v.Length;i++){Assert.That(v[i].x,Is.InRange(-.4f,.4f));Assert.That(v[i].y,Is.InRange(-.11f,1.55f));Assert.That(v[i].z,Is.InRange(-.6f,.3f));if(uv[i].z>.5f){face++;Assert.That(v[i].y,Is.GreaterThanOrEqualTo(1.079f));Assert.That(uv[i].x,Is.InRange(0,1));Assert.That(uv[i].y,Is.InRange(0,1));}}
                Assert.That(face,Is.GreaterThan(100));var material=Resources.Load<Material>("Biathlon/RestrictedG2Crowd/Spectator"+variant);Assert.That(material.shader.isSupported,Is.True);Assert.That(material.enableInstancing,Is.True);Assert.That(material.GetTexture("_FaceMap").width,Is.EqualTo(1024));
                var normals=m.normals;for(int i=0;i<v.Length;i++){if(uv[i].z<.5f&&v[i].y>1.44f)Assert.That(normals[i].y,Is.GreaterThan(0),"Hat cap must face outwards.");}
                var indices=m.triangles;for(int i=0;i<indices.Length;i+=3){var a=v[indices[i]];var b=v[indices[i+1]];var c=v[indices[i+2]];if(a.y>1.44f&&b.y>1.44f&&c.y>1.44f&&uv[indices[i]].z<.5f)Assert.That(Vector3.Cross(b-a,c-a).y,Is.GreaterThan(0),"Closed hat cap winding.");}
            }
        }
        [Test] public void VariantsKeepDensityCullingLodAndZeroAllocationSubmission()
        {
            var go=new GameObject("local crowd");var view=new GameObject("observer");
            try
            {
                var seating=go.AddComponent<SpectatorSeating>();seating.Configure(Resources.Load<GameObject>("Biathlon/Stadium/G2Grandstand/Grandstand").GetComponent<SpectatorSeating>().CopyAnchors());var c=go.AddComponent<SpectatorCrowd>();c.Initialize(seating,true,true);
                var camera=view.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(0,4,-20);camera.transform.LookAt(new Vector3(0,4,3));c.Prepare(camera,"high",8);Assert.That(c.Visible,Is.EqualTo(168));Assert.That(c.VisibleVariant(0),Is.InRange(40,128));Assert.That(c.VisibleVariant(1),Is.InRange(40,128));long shadow=c.ShadowSourceTriangles;Assert.That(shadow,Is.GreaterThan(0).And.LessThan(c.SubmittedTriangles*.5));
                c.Prepare(camera,"balanced",8);int balanced=c.Visible;c.Prepare(camera,"low",8);Assert.That(c.Visible,Is.LessThan(balanced));camera.transform.position=new Vector3(0,4,-60);camera.transform.LookAt(new Vector3(0,4,3));c.Prepare(camera,"high",8);Assert.That(c.Far,Is.EqualTo(168));
                for(int i=0;i<20;i++)c.Render(camera,"high",8);long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)c.Render(camera,"high",i*.1f);Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                camera.transform.position=new Vector3(0,4,-140);c.Prepare(camera,"high",8);Assert.That(c.Visible,Is.Zero);Assert.Throws<ArgumentException>(()=>c.Prepare(camera,"high",float.NaN));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(view);}
        }
        [UnityTest] public IEnumerator IntegratedCrowdAndNearFarReactionsProduceReview()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                VisualBenchmarkPresentation p=null;foreach(var root in scene.GetRootGameObjects())if(root.TryGetComponent(out VisualBenchmarkPresentation found))p=found;Assert.That(p,Is.Not.Null);Assert.That(p.GetComponent<SpectatorCrowd>().UsesAuditedCrowd,Is.True);
                p.enabled=false;p.ShowControls=false;p.Tier="high";var folder=Path.GetFullPath("artifacts/g2-crowd/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))
                {
                    foreach(var shot in new[]{"sector","crowd_zoom","range"}){p.Shot=shot;p.Seek(8);for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,shot+".png"));}
                    foreach(int time in new[]{0,3,8})
                    {
                        p.Seek(time);p.View.transform.position=p.Seating.transform.TransformPoint(new Vector3(-3,4,-5));p.View.transform.LookAt(p.Seating.transform.TransformPoint(new Vector3(0,3,2)));p.View.fieldOfView=35;
                        for(int i=0;i<3;i++){p.GetComponent<SpectatorCrowd>().Render(p.View,"high",time);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,"near-"+time+".png"));
                    }
                }
            }
            finally{if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}

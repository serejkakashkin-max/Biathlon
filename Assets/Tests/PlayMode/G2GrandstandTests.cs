using System;
using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2GrandstandTests
    {
        private const string Root="Biathlon/Stadium/G2Grandstand/";
        [Test]
        public void AuditedStructureUsesBoundedNeutralGeometryWithoutColliders()
        {
            var prefab=Resources.Load<GameObject>(Root+"Grandstand");Assert.That(prefab,Is.Not.Null);Assert.That(prefab.GetComponentsInChildren<Collider>(true).Length,Is.Zero);
            int triangles=0;foreach(var f in prefab.GetComponentsInChildren<MeshFilter>())
            {
                var m=f.sharedMesh;Assert.That(m,Is.Not.Null);triangles+=(int)m.GetIndexCount(0)/3;
                foreach(var p in m.vertices){Assert.That(p.x,Is.InRange(-11,11));Assert.That(p.y,Is.InRange(-.01f,9));Assert.That(p.z,Is.InRange(-1.1f,8));}
                var renderer=f.GetComponent<Renderer>();Assert.That(renderer.sharedMaterial.shader.isSupported,Is.True);
                Assert.That(renderer.sharedMaterial.name,Does.Not.Contain("Advert"));
            }
            Assert.That(triangles,Is.InRange(13000,15000));Assert.That(prefab.GetComponent<SpectatorSeating>().Count,Is.EqualTo(168));
            var snow=Resources.Load<Mesh>(Root+"CanopySnow");Assert.That(snow.bounds.min.y,Is.GreaterThan(8.70f));foreach(var color in snow.colors)Assert.That(color,Is.EqualTo(new Color(1,0,0,0)));
            string credits=Resources.Load<TextAsset>("Biathlon/G2ThirdPartyCredits").text;Assert.That(credits,Does.Contain("William Prosser").And.Contain("f47b51bcda6e449495795c66eb7089a9").And.Contain("all advertisement objects removed"));
        }
        [Test]
        public void EverySpectatorAnchorMatchesActualHorizontalSeatGeometry()
        {
            var anchors=Resources.Load<GameObject>(Root+"Grandstand").GetComponent<SpectatorSeating>().CopyAnchors();
            var mesh=Resources.Load<Mesh>(Root+"Seats");var vertices=mesh.vertices;var indices=mesh.triangles;
            foreach(var anchor in anchors)
            {
                bool supported=false;
                for(int i=0;i<indices.Length;i+=3)
                {
                    var a=vertices[indices[i]];var b=vertices[indices[i+1]];var c=vertices[indices[i+2]];
                    if(Mathf.Abs(a.y-anchor.y-.30f)>.002f||Mathf.Abs(b.y-a.y)>.002f||Mathf.Abs(c.y-a.y)>.002f)continue;
                    var v0=new Vector2(b.x-a.x,b.z-a.z);var v1=new Vector2(c.x-a.x,c.z-a.z);var v2=new Vector2(anchor.x-a.x,anchor.z-a.z);
                    float den=v0.x*v1.y-v1.x*v0.y;if(Mathf.Abs(den)<.000001f)continue;
                    float u=(v2.x*v1.y-v1.x*v2.y)/den,v=(v0.x*v2.y-v2.x*v0.y)/den;
                    if(u>=-.002f&&v>=-.002f&&u+v<=1.002f){supported=true;break;}
                }
                Assert.That(supported,Is.True,"Seat support missing at "+anchor);
                Assert.That(anchor.y+1.5f,Is.LessThan(8.1f),"Head must clear roof.");
            }
        }
        [UnityTest]
        public IEnumerator BenchmarkUsesAuditedSeatingAndProducesIntegratedReview()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                VisualBenchmarkPresentation p=null;int activeSeats=0;foreach(var root in scene.GetRootGameObjects()){if(root.TryGetComponent(out VisualBenchmarkPresentation found))p=found;activeSeats+=root.GetComponentsInChildren<SpectatorSeating>().Length;}
                Assert.That(p,Is.Not.Null);Assert.That(activeSeats,Is.EqualTo(1));Assert.That(p.Seating.Count,Is.EqualTo(168));
                var route=G2BenchmarkSurface.CopyRoute();foreach(var seat in p.Seating.CopyAnchors())Assert.That(VegetationDefinition.DistanceToRoute(p.Seating.transform.TransformPoint(seat),route),Is.GreaterThan(10));
                p.ShowControls=false;p.enabled=false;string folder=Path.GetFullPath("artifacts/g2-grandstand/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))foreach(string shot in new[]{"athlete","approach","range","sector","crowd_zoom","finish"})
                {
                    p.Shot=shot;p.Tier="high";p.Seek(8);for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,shot+"-high.png"));
                }
                foreach(var t in p.Seating.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                p.View.cullingMask=1<<30;p.Key.cullingMask=1<<30;p.View.fieldOfView=48;
                var offsets=new[]{new Vector3(20,11,-19),new Vector3(-20,11,24)};
                using(var target=new BenchmarkRenderTarget(1920,1080))for(int angle=0;angle<offsets.Length;angle++)
                {
                    p.View.transform.position=p.Seating.transform.TransformPoint(offsets[angle]);p.View.transform.LookAt(p.Seating.transform.TransformPoint(new Vector3(0,4,3.5f)));
                    for(int i=0;i<3;i++){target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,"stand-isolated-"+angle+".png"));
                }
            }
            finally{if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}

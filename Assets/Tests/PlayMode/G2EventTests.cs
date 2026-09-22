using System;
using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Biathlon.PlayModeTests
{
    public class G2EventTests
    {
        [Test] public void DefinitionRejectsCourseAndShootingLaneIntrusions()
        {
            var d=G2EventDressing.Load();d.screen.position=new Vector3(0,0,38);Assert.Throws<ArgumentException>(()=>d.Validate());
            d=G2EventDressing.Load();d.banners[0].position=new Vector3(-23,0,-30);Assert.Throws<ArgumentException>(()=>d.Validate());
            d=G2EventDressing.Load();d.banners[1].id=d.banners[0].id;Assert.Throws<ArgumentException>(()=>d.Validate());
            d=G2EventDressing.Load();d.concrete_tile_m=float.NaN;Assert.Throws<ArgumentException>(()=>d.Validate());
        }
        [Test] public void EventGeometryIsBoundedAndMaterialsHaveRealUvAndLinearMasks()
        {
            var prefab=Resources.Load<GameObject>("Biathlon/Stadium/G2Event/Event");Assert.That(prefab,Is.Not.Null);Assert.That(prefab.GetComponentsInChildren<Collider>(),Is.Empty);
            var binding=prefab.GetComponent<G2EventDressing>();Assert.That(binding.Screen,Is.Not.Null);Assert.That(binding.Banners.Length,Is.EqualTo(4));
            int triangles=0;foreach(var f in prefab.GetComponentsInChildren<MeshFilter>())triangles+=(int)f.sharedMesh.GetIndexCount(0)/3;Assert.That(triangles,Is.LessThan(6500));
            var panel=Resources.Load<Mesh>("Biathlon/Stadium/G2Event/EventPanel");Assert.That(panel.bounds.min.y,Is.GreaterThan(4.6f));Assert.That(panel.bounds.max.y,Is.LessThan(7.8f));
            foreach(string name in new[]{"concrete","corrugated_iron"})
            {
                var mat=Resources.Load<Material>("Biathlon/Stadium/G2Event/"+name);Assert.That(mat.shader.isSupported,Is.True);Assert.That(mat.IsKeywordEnabled("_NORMALMAP"),Is.True);
                foreach(string property in new[]{"_BaseMap","_BumpMap","_MetallicGlossMap"}){var t=mat.GetTexture(property);Assert.That(t,Is.Not.Null);Assert.That(t.width,Is.EqualTo(1024));}
#if UNITY_EDITOR
                var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(mat.GetTexture("_MetallicGlossMap")));Assert.That(importer.sRGBTexture,Is.False);
                var rough=new Texture2D(2,2,TextureFormat.RGBA32,false,true);var packed=new Texture2D(2,2,TextureFormat.RGBA32,false,true);var metal=new Texture2D(2,2,TextureFormat.RGBA32,false,true);
                try
                {
                    string folder="Assets/Resources/Biathlon/Stadium/G2Event/Textures/";ImageConversion.LoadImage(rough,File.ReadAllBytes(folder+name+"_Roughness.png"));ImageConversion.LoadImage(packed,File.ReadAllBytes(folder+name+"_MetalSmooth.png"));
                    if(name!="concrete")ImageConversion.LoadImage(metal,File.ReadAllBytes(folder+name+"_Metallic.png"));
                    var a=rough.GetPixels32();var b=packed.GetPixels32();var c=metal.GetPixels32();for(int i=0;i<a.Length;i+=997){Assert.That(b[i].a,Is.EqualTo(255-a[i].r));Assert.That(b[i].r,Is.EqualTo(name=="concrete"?0:c[i].r));}
                }
                finally{UnityEngine.Object.DestroyImmediate(rough);UnityEngine.Object.DestroyImmediate(packed);UnityEngine.Object.DestroyImmediate(metal);}
#endif
            }
            foreach(var text in prefab.GetComponentsInChildren<TextMesh>()){Assert.That(text.GetComponent<VenueSignText>(),Is.Not.Null);Assert.That(text.text,Does.Not.Contain("00:"));}
        }
        [UnityTest] public IEnumerator BenchmarkArchitecturePreservesGeometryAndProducesReview()
        {
#if UNITY_EDITOR
            var scene=UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/VisualBenchmark.unity",new LoadSceneParameters(LoadSceneMode.Additive));yield return null;
            try
            {
                G2EventDressing e=null;VisualBenchmarkPresentation p=null;foreach(var root in scene.GetRootGameObjects()){if(root.TryGetComponent(out G2EventDressing found))e=found;if(root.TryGetComponent(out VisualBenchmarkPresentation v))p=v;}
                Assert.That(e,Is.Not.Null);Assert.That(p,Is.Not.Null);Assert.That(e.Architecture.Length,Is.EqualTo(4));
                foreach(var f in e.Architecture)
                {
                    var m=f.sharedMesh;var original=Resources.Load<Mesh>("Biathlon/Stadium/"+m.name.Substring("Mapped_".Length));Assert.That(original,Is.Not.Null);
                    Assert.That(m.bounds,Is.EqualTo(original.bounds));var indices=original.triangles;var old=original.vertices;var vertices=m.vertices;var uv=m.uv;
                    Assert.That(m.triangles.Length,Is.EqualTo(indices.Length));Assert.That(uv.Length,Is.EqualTo(vertices.Length));
                    for(int i=0;i<vertices.Length;i++){Assert.That(vertices[i],Is.EqualTo(old[indices[i]]));Assert.That(float.IsFinite(uv[i].sqrMagnitude),Is.True);}
                    Assert.That(m.tangents.Length,Is.EqualTo(vertices.Length));
                }
                p.enabled=false;p.ShowControls=false;p.Tier="high";string folder=Path.GetFullPath("artifacts/g2-event/review");Directory.CreateDirectory(folder);
                using(var target=new BenchmarkRenderTarget(1920,1080))foreach(var shot in new[]{"approach","sector","range","finish","crowd_zoom"})
                {
                    p.Shot=shot;p.Seek(8);for(int i=0;i<3;i++){p.RenderFrame(8);target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,shot+".png"));
                }
                p.View.transform.position=e.Screen.TransformPoint(new Vector3(-8,6,-15));p.View.transform.LookAt(e.Screen.TransformPoint(new Vector3(0,5,0)));p.View.fieldOfView=38;
                using(var target=new BenchmarkRenderTarget(1920,1080)){for(int i=0;i<3;i++){target.Render(p.View);yield return null;}target.SavePng(Path.Combine(folder,"screen-close.png"));}
            }
            finally{if(scene.IsValid()&&scene.isLoaded)SceneManager.UnloadSceneAsync(scene);}
#else
            yield return null;
#endif
        }
    }
}

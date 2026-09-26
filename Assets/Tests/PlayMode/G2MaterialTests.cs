using System.Collections;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class G2MaterialTests
    {
        [Test] public void FabricMapsAreBoundAndImportedAsLinearMipmappedData()
        {
            foreach(var id in new[]{"SuitNavy","SuitTeal","BibIvory","CapNavy"})
            {
                var material=Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/"+id);
                foreach(var channel in new[]{"_BumpMap","_RoughnessMap"})
                {
                    Assert.That(material.HasProperty(channel),Is.True,id+" must consume "+channel);
                    var texture=material.GetTexture(channel) as Texture2D;
                    Assert.That(texture,Is.Not.Null);Assert.That(texture.mipmapCount,Is.GreaterThan(1));
#if UNITY_EDITOR
                    var importer=(UnityEditor.TextureImporter)UnityEditor.AssetImporter.GetAtPath(UnityEditor.AssetDatabase.GetAssetPath(texture));
                    Assert.That(importer.sRGBTexture,Is.False);
                    Assert.That(importer.textureType,Is.EqualTo(channel=="_BumpMap"?UnityEditor.TextureImporterType.NormalMap:UnityEditor.TextureImporterType.Default));
#endif
                }
            }
        }

        [UnityTest] public IEnumerator BibNormalActuallyChangesRenderedLighting()
        {
            var root=new GameObject("Fabric regression fixture");var quad=GameObject.CreatePrimitive(PrimitiveType.Quad);quad.transform.SetParent(root.transform);quad.layer=30;
            var material=new Material(Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/BibIvory"));quad.GetComponent<Renderer>().sharedMaterial=material;
            var normal=new Texture2D(2,2,TextureFormat.RGBA32,false,true);normal.SetPixels(new[]{new Color(.8f,.5f,1,1),new Color(.8f,.5f,1,1),new Color(.8f,.5f,1,1),new Color(.8f,.5f,1,1)});normal.Apply();material.SetTexture("_BumpMap",normal);
            var camera=new GameObject("Fabric camera").AddComponent<Camera>();camera.transform.SetParent(root.transform);camera.transform.position=Vector3.back*2;camera.enabled=false;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.orthographic=true;camera.orthographicSize=.6f;
            var light=new GameObject("Fabric key").AddComponent<Light>();light.transform.SetParent(root.transform);light.type=LightType.Directional;light.intensity=.8f;light.cullingMask=1<<30;light.transform.rotation=Quaternion.Euler(0,25,0);
            var oldSun=RenderSettings.sun;RenderSettings.sun=light;
            var folder=Path.GetFullPath("artifacts/g2-materials");Directory.CreateDirectory(folder);
            var images=new Texture2D[2];
            try
            {
                using(var target=new BenchmarkRenderTarget(128,128))for(int sample=0;sample<2;sample++)
                {
                    material.SetFloat("_BumpScale",sample);
                    for(int settle=0;settle<3;settle++){target.Render(camera);yield return null;}
                    var path=Path.Combine(folder,"bib-normal-"+sample+".png");target.SavePng(path);
                    images[sample]=new Texture2D(2,2);images[sample].LoadImage(File.ReadAllBytes(path));
                }
                var a=images[0].GetPixels32();var b=images[1].GetPixels32();int changed=0;
                for(int i=0;i<a.Length;i++)if(Mathf.Abs(a[i].r-b[i].r)+Mathf.Abs(a[i].g-b[i].g)+Mathf.Abs(a[i].b-b[i].b)>6)changed++;
                Assert.That(changed,Is.GreaterThan(500),"A shader that silently ignores the normal map must fail this render test.");
            }
            finally{RenderSettings.sun=oldSun;foreach(var i in images)if(i!=null)Object.DestroyImmediate(i);Object.DestroyImmediate(material);Object.DestroyImmediate(normal);Object.DestroyImmediate(root);}
        }
    }
}

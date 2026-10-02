using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class SnowVerticalSurfaceShaderTests
    {
        [System.Serializable] private sealed class RenderEvidence
        {
            public int schema_version=1;
            public string id="snow_axis_normal_render_witness_v1";
            public AxisEvidence[] axes;
        }
        [System.Serializable] private sealed class AxisEvidence
        {public string axis;public float control_luminance,detail_luminance,ratio;}
        private static Mesh Face(Vector3 normal)
        {
            var right=Mathf.Abs(normal.y)<.9f?Vector3.Cross(Vector3.up,normal).normalized:Vector3.right;
            var up=Vector3.Cross(normal,right);
            var mesh=new Mesh{name="Snow detail basis render face"};
            mesh.vertices=new[]{-right-up,-right+up,right-up,right+up};
            mesh.triangles=new[]{0,2,1,2,3,1};mesh.uv=new[]{Vector2.zero,Vector2.up,Vector2.right,Vector2.one};
            // Use the actual groomed-course packed channel, including the private snow skirts.
            var packed=new Color(0,1,0,0);mesh.colors=new[]{packed,packed,packed,packed};mesh.RecalculateNormals();mesh.RecalculateBounds();
            foreach(var value in mesh.normals)Assert.That(Vector3.Dot(value,normal),Is.GreaterThan(.99999f));
            return mesh;
        }
        private static Texture2D FlatNormal()
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,true){name="Neutral snow normal witness"};
            var value=new Color(.5f,.5f,1,1);texture.SetPixels(new[]{value,value,value,value});texture.Apply();return texture;
        }
        private static float CentreLuminance(Texture2D image)
        {
            float total=0;int count=0;
            for(int y=48;y<80;y++)for(int x=48;x<80;x++)
            {
                var value=image.GetPixel(x,y);
                Assert.That(float.IsFinite(value.r)&&float.IsFinite(value.g)&&float.IsFinite(value.b),Is.True);
                total+=value.grayscale;count++;
            }
            return total/count;
        }
        [UnityTest] public IEnumerator ActualSnowDetailKeepsBothVerticalXAxisFacesAndUpwardSurfaceLit()
        {
            var root=new GameObject("Snow axis-normal shader regression");var quad=new GameObject("Snow render face");quad.transform.SetParent(root.transform,false);quad.layer=30;
            var filter=quad.AddComponent<MeshFilter>();var renderer=quad.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            var template=Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend");Assert.That(template,Is.Not.Null);
            var material=new Material(template);renderer.sharedMaterial=material;
            // Neutral detail should preserve a lit geometric normal. Disable procedural normal
            // perturbation to reproduce the exact +/-X skirt case when its distant grain fades.
            material.SetFloat("_NormalStrength",0);material.SetVector("_LayerNormals",Vector4.zero);
            material.SetFloat("_AlbedoVariation",0);material.SetFloat("_SlopeDarkening",0);material.SetFloat("_RoughnessVariation",0);
            material.SetFloat("_DetailNormalStrength",.45f);material.SetTexture("_DetailAlbedo",Texture2D.whiteTexture);
            var neutral=FlatNormal();material.SetTexture("_DetailNormal",neutral);
            var cameraObject=new GameObject("Snow shader witness camera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.cullingMask=1<<30;
            camera.orthographic=true;camera.orthographicSize=1.2f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var lightObject=new GameObject("Snow shader witness key");lightObject.transform.SetParent(root.transform,false);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.cullingMask=1<<30;light.shadows=LightShadows.None;
            var oldSun=RenderSettings.sun;RenderSettings.sun=light;
            var folder=Path.GetFullPath("artifacts/wp169q/snow-basis");Directory.CreateDirectory(folder);
            var image=new Texture2D(2,2);Mesh mesh=null;var observations=new List<AxisEvidence>(3);
            try
            {
                using(var target=new BenchmarkRenderTarget(128,128))
                foreach(var normal in new[]{Vector3.right,Vector3.left,Vector3.up})
                {
                    if(mesh!=null)Object.DestroyImmediate(mesh);mesh=Face(normal);filter.sharedMesh=mesh;
                    camera.transform.SetPositionAndRotation(normal*3,Quaternion.LookRotation(-normal,Mathf.Abs(normal.y)>.9f?Vector3.forward:Vector3.up));
                    light.transform.rotation=Quaternion.LookRotation(-normal);
                    string axis=normal==Vector3.right?"positive-x":normal==Vector3.left?"negative-x":"up";
                    var luminance=new float[2];
                    for(int detail=0;detail<2;detail++)
                    {
                        material.SetFloat("_DetailWeight",detail);
                        for(int settle=0;settle<3;settle++){target.Render(camera);yield return null;}
                        string path=Path.Combine(folder,axis+"-detail-"+detail+".png");target.SavePng(path);
                        Assert.That(image.LoadImage(File.ReadAllBytes(path)),Is.True);luminance[detail]=CentreLuminance(image);
                    }
                    observations.Add(new AxisEvidence{axis=axis,control_luminance=luminance[0],detail_luminance=luminance[1],ratio=luminance[0]>0?luminance[1]/luminance[0]:0});
                    File.WriteAllText(Path.Combine(folder,"render-evidence.json"),JsonUtility.ToJson(new RenderEvidence{axes=observations.ToArray()},true));
                    Assert.That(luminance[0],Is.GreaterThan(.1f),axis+" must be visible and lit before testing detail.");
                    Assert.That(luminance[1],Is.InRange(luminance[0]*.8f,luminance[0]*1.35f),
                        axis+" neutral detail must retain a finite lit normal; a degenerate detail tangent must fail this GPU render test.");
                }
            }
            finally
            {
                RenderSettings.sun=oldSun;if(mesh!=null)Object.DestroyImmediate(mesh);
                Object.DestroyImmediate(image);Object.DestroyImmediate(neutral);Object.DestroyImmediate(material);Object.DestroyImmediate(root);
            }
        }
    }
}

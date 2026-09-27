#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class G2CrowdAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/RestrictedG2Crowd/";
#pragma warning disable CS0649
        [Serializable] private sealed class Data{public int schema_version;public string id;public float[] vertices,normals,colors,pivots,face_uv;public int[] indices;}
#pragma warning restore CS0649
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();var shader=Shader.Find("Biathlon/AuditedSpectator");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidDataException("Audited crowd shader unavailable.");
            for(int variant=0;variant<10;variant++)
            {
                string texturePath=Root+"Face"+(variant%2)+".png";var importer=AssetImporter.GetAtPath(texturePath) as TextureImporter;if(importer==null)throw new InvalidDataException("Run local restricted crowd recipe first.");
                importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
                string path=Root+"Spectator"+variant+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,path);}mat.shader=shader;mat.enableInstancing=true;mat.SetTexture("_FaceMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetVector("_Garment",new Vector4(variant>=6&&variant<8?.55f:variant>=2&&variant<4?.4f:.12f,variant<2?1:0,variant<2?1:0,0));EditorUtility.SetDirty(mat);
                mat.SetFloat("_FaceDetail",0); // Owner corrective directive: readable winter silhouettes, no photo faces.
                for(int lod=0;lod<2;lod++)
                {
                    string id="Crowd"+variant+"LOD"+lod;var d=JsonUtility.FromJson<Data>(File.ReadAllText("artifacts/g2-crowd/"+id+".json"));
                    if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.vertices==null||d.normals==null||d.colors==null||d.pivots==null||d.face_uv==null||d.indices==null||d.vertices.Length%3!=0||d.normals.Length!=d.vertices.Length||d.face_uv.Length!=d.vertices.Length||d.colors.Length!=d.vertices.Length/3*4||d.pivots.Length!=d.colors.Length||d.indices.Length%3!=0)throw new InvalidDataException("Invalid local crowd channels.");
                    var v=new Vector3[d.vertices.Length/3];var n=new Vector3[v.Length];var c=new Color[v.Length];var pivot=new List<Vector4>(v.Length);var uv=new List<Vector3>(v.Length);
                    for(int i=0;i<v.Length;i++)
                    {
                        v[i]=new Vector3(d.vertices[3*i],d.vertices[3*i+1],d.vertices[3*i+2]);n[i]=new Vector3(d.normals[3*i],d.normals[3*i+1],d.normals[3*i+2]);c[i]=new Color(d.colors[4*i],d.colors[4*i+1],d.colors[4*i+2],d.colors[4*i+3]);
                        pivot.Add(new Vector4(d.pivots[4*i],d.pivots[4*i+1],d.pivots[4*i+2],d.pivots[4*i+3]));uv.Add(new Vector3(d.face_uv[3*i],d.face_uv[3*i+1],d.face_uv[3*i+2]));
                        if(!float.IsFinite(v[i].sqrMagnitude)||!float.IsFinite(n[i].sqrMagnitude)||!float.IsFinite(uv[i].sqrMagnitude)||!float.IsFinite(pivot[i].sqrMagnitude))throw new InvalidDataException("Nonfinite local crowd data.");
                    }
                    foreach(int i in d.indices)if(i<0||i>=v.Length)throw new InvalidDataException("Invalid local crowd index.");
                    var mesh=new Mesh{name=id,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=v,normals=n,colors=c,triangles=d.indices};mesh.SetUVs(0,pivot);mesh.SetUVs(1,uv);mesh.RecalculateBounds();path=Root+id+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);}
                }
            }
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class G2RockAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/Terrain/G2Rock";
#pragma warning disable CS0649
        [Serializable] private sealed class Data {public int schema_version;public string id;public float[] vertices,normals,uv;public int[] indices;}
#pragma warning restore CS0649
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            foreach(string name in new[]{"RockAlbedo","RockNormal","RockRoughness"})
            {
                var importer=AssetImporter.GetAtPath(Root+"/Textures/"+name+".png") as TextureImporter;
                if(importer==null)throw new InvalidDataException("Generate rock texture "+name);
                importer.textureType=name=="RockNormal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=name=="RockAlbedo";
                importer.maxTextureSize=name=="RockAlbedo"?2048:1024;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
                importer.textureCompression=TextureImporterCompression.Compressed;importer.wrapMode=TextureWrapMode.Repeat;importer.SaveAndReimport();
            }
            for(int lod=0;lod<2;lod++)
            {
                var d=JsonUtility.FromJson<Data>(File.ReadAllText("artifacts/g2-rock/rock_lod"+lod+".json"));
                if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.vertices.Length%3!=0)throw new InvalidDataException("Invalid rock schema.");
                int count=d.vertices.Length/3;
                if(count==0||count>5000||d.normals.Length!=count*3||d.uv.Length!=count*2||d.indices.Length%3!=0)throw new InvalidDataException("Invalid rock channels.");
                var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++)
                {
                    vertices[i]=new Vector3(d.vertices[i*3],d.vertices[i*3+1],d.vertices[i*3+2]);normals[i]=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);
                    if(!float.IsFinite(vertices[i].sqrMagnitude)||!float.IsFinite(normals[i].sqrMagnitude)||!float.IsFinite(uv[i].sqrMagnitude))throw new InvalidDataException("Nonfinite rock channels.");
                }
                foreach(int index in d.indices)if(index<0||index>=count)throw new InvalidDataException("Invalid rock index.");
                var mesh=new Mesh{name="RockLOD"+lod,vertices=vertices,normals=normals,uv=uv,triangles=d.indices};mesh.RecalculateBounds();mesh.RecalculateTangents();
                string path=Root+"/RockLOD"+lod+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            }
            var shader=Shader.Find("Biathlon/AuditedRock");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidDataException("Rock shader errors.");
            string materialPath=Root+"/SnowyRock.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,materialPath);}
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/RockAlbedo.png"));mat.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/RockNormal.png"));
            mat.SetTexture("_RoughnessMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/RockRoughness.png"));mat.enableInstancing=true;
            mat.SetColor("_SnowColor",new Color(.83f,.88f,.93f));mat.SetFloat("_SnowCoverage",.85f);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
        }
    }
}
#endif

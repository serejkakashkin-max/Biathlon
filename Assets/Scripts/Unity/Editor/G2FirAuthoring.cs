#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class G2FirAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/Vegetation/G2Fir";
#pragma warning disable CS0649
        [Serializable] private sealed class Data { public int schema_version; public string id; public float[] vertices,normals,uv,kinds; public int[] indices; }
#pragma warning restore CS0649
        public static void Generate()=>GenerateSpecies(Root,"fir","Fir");
        public static void GenerateSpecies(string root,string sourceId,string name,bool singleAtlas=false)
        {
            Directory.CreateDirectory(root);AssetDatabase.Refresh();
            foreach(string textureName in singleAtlas?new[]{"LeafAlbedo","LeafNormal"}:new[]{"BarkAlbedo","LeafAlbedo","BarkNormal","LeafNormal"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(root+"/Textures/"+textureName+".png");
                if(importer==null)throw new InvalidDataException("Generate audited vegetation texture "+textureName);
                bool normal=textureName.EndsWith("Normal",StringComparison.Ordinal);
                importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=!normal;
                importer.maxTextureSize=textureName=="LeafAlbedo"?2048:1024;importer.mipmapEnabled=true;importer.mipMapsPreserveCoverage=!normal;
                importer.alphaTestReferenceValue=.4f;importer.alphaIsTransparency=!normal;importer.wrapMode=TextureWrapMode.Repeat;
                importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
            }
            for(int lod=0;lod<3;lod++)
            {
                var d=JsonUtility.FromJson<Data>(File.ReadAllText("artifacts/g2-"+sourceId+"/"+sourceId+"_lod"+lod+".json"));
                if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.vertices.Length%3!=0)throw new InvalidDataException("Invalid fir schema.");
                int count=d.vertices.Length/3;
                if(count==0||count>60000||d.normals.Length!=count*3||d.uv.Length!=count*2||d.kinds.Length!=count||d.indices.Length%3!=0)throw new InvalidDataException("Invalid fir channels.");
                var vertices=new Vector3[count];var normals=new Vector3[count];var uv=new Vector2[count];var colors=new Color[count];
                for(int i=0;i<count;i++)
                {
                    vertices[i]=new Vector3(d.vertices[i*3],d.vertices[i*3+1],d.vertices[i*3+2]);normals[i]=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);
                    if(!float.IsFinite(vertices[i].sqrMagnitude)||!float.IsFinite(normals[i].sqrMagnitude)||!float.IsFinite(uv[i].sqrMagnitude)||!float.IsFinite(d.kinds[i])||d.kinds[i]<0||d.kinds[i]>1)throw new InvalidDataException("Nonfinite fir channels.");
                    colors[i]=new Color(d.kinds[i],0,0,1);
                }
                foreach(int index in d.indices)if(index<0||index>=count)throw new InvalidDataException("Invalid fir index.");
                var mesh=new Mesh{name=name+"LOD"+lod,vertices=vertices,normals=normals,uv=uv,colors=colors,triangles=d.indices};mesh.RecalculateBounds();mesh.RecalculateTangents();
                string path=root+"/"+name+"LOD"+lod+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            }
            string materialPath=root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);var shader=Shader.Find("Biathlon/AuditedFir");
            if(shader==null)throw new InvalidDataException("Missing audited fir shader.");
            if(mat==null){mat=new Material(shader);AssetDatabase.CreateAsset(mat,materialPath);}
            else{var clean=new Material(shader);EditorUtility.CopySerialized(clean,mat);Object.DestroyImmediate(clean);}
            mat.name=name;mat.enableInstancing=true;mat.doubleSidedGI=true;
            foreach(string textureName in new[]{"BarkAlbedo","LeafAlbedo","BarkNormal","LeafNormal"})mat.SetTexture("_"+textureName,AssetDatabase.LoadAssetAtPath<Texture2D>(root+"/Textures/"+(singleAtlas?textureName.Replace("Bark","Leaf"):textureName)+".png"));
            mat.SetFloat("_Cutoff",.4f);mat.SetFloat("_SnowCoverage",.72f);EditorUtility.SetDirty(mat);AssetDatabase.SaveAssets();
        }
    }
}
#endif

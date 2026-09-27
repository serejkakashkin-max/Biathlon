#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity
{
    public static class G2HeroFaceAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/RestrictedG2Hero";
#pragma warning disable CS0649
        [Serializable] private sealed class Data{public int schema_version;public string id;public float[] vertices,normals,uv;public int[] indices;}
#pragma warning restore CS0649
        public static void Apply(AthleteRigBinding rig)
        {
            if(!File.Exists(Root+"/Face.png"))throw new InvalidOperationException("Run the licensed face step of scripts/Build-G2Hero.ps1 first.");
            AssetDatabase.Refresh();var texture=(TextureImporter)AssetImporter.GetAtPath(Root+"/Face.png");
            texture.textureType=TextureImporterType.Default;texture.sRGBTexture=true;texture.maxTextureSize=2048;texture.mipmapEnabled=true;texture.wrapMode=TextureWrapMode.Clamp;texture.SaveAndReimport();
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Face.mat");
            if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,Root+"/Face.mat");}
            material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Face.png"));material.SetFloat("_Smoothness",.24f);material.enableInstancing=true;EditorUtility.SetDirty(material);
            var faceLevels=ImportLevels(rig,"Face");var capLevels=ImportLevels(rig,"Cap");
            MeshFilter CreatePart(string label,Mesh mesh,Material mat)
            {
                var go=new GameObject(label);go.transform.SetParent(rig.Bone(AthleteBone.Head),false);
                var filter=go.AddComponent<MeshFilter>();filter.sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;return filter;
            }
            var face=CreatePart("Licensed athlete face",faceLevels[0],material);
            var cap=CreatePart("Fitted athlete cap",capLevels[0],AssetDatabase.LoadAssetAtPath<Material>(G2HeroAuthoring.DirectoryPath+"/Materials/CapNavy.mat"));
            CreateNeckGaiter(rig);
            bool active=rig.gameObject.activeSelf;rig.gameObject.SetActive(false);
            rig.gameObject.AddComponent<AthleteFaceDetailBinding>().Configure(face,faceLevels,cap,capLevels);
            rig.gameObject.SetActive(active);
        }
        private static Mesh[] ImportLevels(AthleteRigBinding rig,string label)
        {
            var bone=rig.Bone(AthleteBone.Head);var levels=new Mesh[3];
            for(int lod=0;lod<3;lod++)
            {
                var d=JsonUtility.FromJson<Data>(File.ReadAllText("artifacts/g2-hero-face/"+label+"LOD"+lod+".json"));
                if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.vertices.Length%3!=0||d.normals.Length!=d.vertices.Length||d.uv.Length!=d.vertices.Length/3*2)throw new InvalidDataException("Incomplete licensed face channels.");
                int count=d.vertices.Length/3;var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];
                for(int i=0;i<count;i++){v[i]=bone.InverseTransformPoint(rig.transform.TransformPoint(new Vector3(d.vertices[i*3],d.vertices[i*3+1],d.vertices[i*3+2])));n[i]=bone.InverseTransformDirection(rig.transform.TransformDirection(new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2])));uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);}
                var mesh=new Mesh{name="Licensed hero "+label+"LOD"+lod,indexFormat=count>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16,vertices=v,normals=n,uv=uv,triangles=d.indices};mesh.RecalculateTangents();mesh.RecalculateBounds();
                var path=Root+"/"+label+"LOD"+lod+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{UpdateMesh(saved,mesh);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}levels[lod]=saved;
            }
            return levels;
        }
        private static void UpdateMesh(Mesh saved,Mesh source)
        {
            // CopySerialized updates serialized bounds but can retain stale native vertex
            // buffers on an already loaded Mesh. Reassign channels through the Mesh API.
            saved.Clear();saved.indexFormat=source.indexFormat;saved.vertices=source.vertices;
            saved.normals=source.normals;saved.uv=source.uv;saved.triangles=source.triangles;
            saved.tangents=source.tangents;saved.bindposes=source.bindposes;saved.boneWeights=source.boneWeights;saved.bounds=source.bounds;
        }
        private static void CreateNeckGaiter(AthleteRigBinding rig)
        {
            const int segments=48;var vertices=new Vector3[segments*4];var uv=new Vector2[vertices.Length];var indices=new int[segments*3*6];int at=0;
            var neck=rig.Bone(AthleteBone.Neck);var head=rig.Bone(AthleteBone.Head);var weights=new BoneWeight[vertices.Length];
            for(int row=0;row<4;row++)for(int col=0;col<segments;col++)
            {
                float angle=col*Mathf.PI*2/segments,bulge=row==0||row==3?1:1.06f;
                float radius=Mathf.Lerp(.073f,.071f,row/3f);
                float front=Mathf.Max(0,Mathf.Sin(angle));
                var point=new Vector3(-.009f*row/3f+Mathf.Cos(angle)*radius*bulge,Mathf.Lerp(1.54f,1.682f-.062f*front*front*front*front,row/3f),.012f*row/3f+Mathf.Sin(angle)*radius*bulge);
                vertices[row*segments+col]=point;uv[row*segments+col]=new Vector2(col/(float)segments,row/3f);
                weights[row*segments+col]=new BoneWeight{boneIndex0=0,weight0=1-row/3f,boneIndex1=1,weight1=row/3f};
                if(row==3)continue;int a=row*segments+col,b=row*segments+(col+1)%segments;
                indices[at++]=a;indices[at++]=a+segments;indices[at++]=b;indices[at++]=b;indices[at++]=a+segments;indices[at++]=b+segments;
            }
            var mesh=new Mesh{name="Original winter neck gaiter",vertices=vertices,uv=uv,triangles=indices,boneWeights=weights,bindposes=new[]{neck.worldToLocalMatrix*rig.transform.localToWorldMatrix,head.worldToLocalMatrix*rig.transform.localToWorldMatrix}};mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();
            const string path=G2HeroAuthoring.DirectoryPath+"/Meshes/NeckGaiter.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{UpdateMesh(saved,mesh);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            var go=new GameObject("Original neck gaiter");go.transform.SetParent(rig.transform,false);var renderer=go.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=saved;renderer.bones=new[]{neck,head};renderer.rootBone=rig.transform;renderer.localBounds=saved.bounds;renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(G2HeroAuthoring.DirectoryPath+"/Materials/SuitNavy.mat");
        }
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class G2RifleAuthoring
    {
#pragma warning disable CS0649
        [Serializable] private sealed class Indices { public int[] indices; }
        [Serializable] private sealed class Data { public int schema_version; public string id; public float[] vertices,normals; public Indices[] submeshes; }
#pragma warning restore CS0649
        public static void Apply(AthleteRigBinding rig)
        {
            var data=JsonUtility.FromJson<Data>(File.ReadAllText("artifacts/g2-hero/rifle.json"));
            if(data.schema_version!=1||string.IsNullOrWhiteSpace(data.id)||data.vertices.Length%3!=0||data.normals.Length!=data.vertices.Length||data.submeshes.Length!=4)
                throw new InvalidDataException("Invalid original rifle derivative.");
            int count=data.vertices.Length/3;
            if(count==0||count>30000)throw new InvalidDataException("Unbounded rifle geometry.");
            var vertices=new Vector3[count];var normals=new Vector3[count];
            for(int i=0;i<count;i++)
            {
                vertices[i]=new Vector3(data.vertices[i*3],data.vertices[i*3+1],data.vertices[i*3+2]);
                normals[i]=new Vector3(data.normals[i*3],data.normals[i*3+1],data.normals[i*3+2]);
                if(!float.IsFinite(vertices[i].sqrMagnitude)||!float.IsFinite(normals[i].sqrMagnitude))throw new InvalidDataException("Nonfinite rifle geometry.");
            }
            var mesh=new Mesh{name="HeroRifle",vertices=vertices,normals=normals,subMeshCount=4};
            for(int i=0;i<4;i++)mesh.SetTriangles(data.submeshes[i].indices,i);
            mesh.RecalculateBounds();string root=G2HeroAuthoring.DirectoryPath;
            string path=root+"/Meshes/HeroRifle.asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            string[] names={"RifleComposite","RifleMetal","RifleRubber","RifleAccent"};
            Color[] colors={new Color(.055f,.095f,.12f),new Color(.09f,.105f,.12f),new Color(.019f,.025f,.028f),new Color(.04f,.52f,.53f)};
            var materials=new Material[4];
            for(int i=0;i<4;i++)
            {
                path=root+"/Materials/"+names[i]+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Resources.Load<Material>("Biathlon/GreyboxLit"));AssetDatabase.CreateAsset(material,path);}
                material.name=names[i];material.SetColor("_BaseColor",colors[i]);material.SetFloat("_Smoothness",i==1?.55f:.25f);material.SetFloat("_Metallic",i==1?.8f:0);material.enableInstancing=true;
                EditorUtility.SetDirty(material);materials[i]=material;
            }
            var rifle=rig.GetComponent<AthleteEquipmentBinding>().Rifle;
            rifle.GetComponent<MeshFilter>().sharedMesh=saved;rifle.GetComponent<MeshRenderer>().sharedMaterials=materials;
        }
    }
}
#endif

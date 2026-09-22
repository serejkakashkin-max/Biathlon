#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class G2GrandstandAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/Stadium/G2Grandstand/";
#pragma warning disable CS0649
        [Serializable] private sealed class MeshData{public int schema_version;public string id;public float[] vertices,normals,uv;public int[] indices;}
        [Serializable] private sealed class Seat{public float[] seat_surface,anchor;public string source_object;}
        [Serializable] private sealed class Seats{public int schema_version;public string id;public Seat[] seats;}
#pragma warning restore CS0649
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();var root=new GameObject("Audited winter grandstand");
            try
            {
                foreach(string group in new[]{"Structure","Seats","Railings","CanopySteel","CanopyRoof","CanopyFascia"})
                {
                    var d=JsonUtility.FromJson<MeshData>(File.ReadAllText("artifacts/g2-grandstand/"+group+".json"));
                    if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.vertices==null||d.normals==null||d.uv==null||d.indices==null||d.vertices.Length%3!=0||d.vertices.Length/3>60000||d.normals.Length!=d.vertices.Length||d.uv.Length!=d.vertices.Length/3*2||d.indices.Length%3!=0)throw new InvalidDataException("Invalid stand mesh channels: "+group);
                    var vertices=new Vector3[d.vertices.Length/3];var normals=new Vector3[vertices.Length];var uv=new Vector2[vertices.Length];
                    for(int i=0;i<vertices.Length;i++)
                    {
                        vertices[i]=new Vector3(d.vertices[i*3],d.vertices[i*3+1],d.vertices[i*3+2]);normals[i]=new Vector3(d.normals[i*3],d.normals[i*3+1],d.normals[i*3+2]);uv[i]=new Vector2(d.uv[i*2],d.uv[i*2+1]);
                        if(!float.IsFinite(vertices[i].sqrMagnitude)||!float.IsFinite(normals[i].sqrMagnitude)||!float.IsFinite(uv[i].sqrMagnitude))throw new InvalidDataException("Nonfinite stand geometry.");
                    }
                    foreach(int index in d.indices)if(index<0||index>=vertices.Length)throw new InvalidDataException("Invalid stand index.");
                    var mesh=new Mesh{name=group,vertices=vertices,normals=normals,uv=uv,triangles=d.indices};mesh.RecalculateBounds();mesh.RecalculateTangents();string path=Root+group+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
                    var go=new GameObject(group);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=saved;go.AddComponent<MeshRenderer>().sharedMaterial=Material(group);
                }
                var cap=new StadiumArtMesh();cap.Box(new Vector3(0,8.74f,3.4f),new Vector3(19.12f,.075f,8.5f),.025f);
                var snowMesh=cap.Build("CanopySnow");var colors=new Color[snowMesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=new Color(1,0,0,0);snowMesh.colors=colors;
                var savedSnow=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"CanopySnow.asset");if(savedSnow==null){AssetDatabase.CreateAsset(snowMesh,Root+"CanopySnow.asset");savedSnow=snowMesh;}else{EditorUtility.CopySerialized(snowMesh,savedSnow);Object.DestroyImmediate(snowMesh);EditorUtility.SetDirty(savedSnow);}
                var snowGo=new GameObject("Original roof snow cap");snowGo.transform.SetParent(root.transform,false);snowGo.AddComponent<MeshFilter>().sharedMesh=savedSnow;snowGo.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend");
                var seats=JsonUtility.FromJson<Seats>(File.ReadAllText("artifacts/g2-grandstand/seat-bindings.json"));
                if(seats==null||seats.schema_version!=1||string.IsNullOrWhiteSpace(seats.id)||seats.seats.Length!=168)throw new InvalidDataException("Unexpected audited seat count.");
                var anchors=new Vector3[seats.seats.Length];for(int i=0;i<anchors.Length;i++){var a=seats.seats[i].anchor;if(a==null||a.Length!=3)throw new InvalidDataException("Invalid seat anchor.");anchors[i]=new Vector3(a[0],a[1],a[2]);}
                root.AddComponent<SpectatorSeating>().Configure(anchors);
                PrefabUtility.SaveAsPrefabAsset(root,Root+"Grandstand.prefab");AssetDatabase.SaveAssets();
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static Material Material(string group)
        {
            string path=Root+group+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else{var clean=new Material(shader);EditorUtility.CopySerialized(clean,m);Object.DestroyImmediate(clean);}
            bool seat=group=="Seats",roof=group=="CanopyRoof";m.name=group;m.enableInstancing=true;
            m.SetColor("_BaseColor",seat?new Color(.045f,.19f,.235f):roof?new Color(.09f,.145f,.175f):group=="CanopyFascia"?new Color(.66f,.72f,.75f):new Color(.33f,.38f,.41f));
            m.SetFloat("_Metallic",seat?0:.75f);m.SetFloat("_Smoothness",seat?.3f:.35f);EditorUtility.SetDirty(m);return m;
        }
    }
}
#endif

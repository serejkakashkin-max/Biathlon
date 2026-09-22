#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class G2EventAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/Stadium/G2Event/";
#pragma warning disable CS0649
        [Serializable] private sealed class MeshData{public int schema_version;public string id;public float[] vertices,normals;public int[] indices;}
#pragma warning restore CS0649
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();var d=G2EventDressing.Load();
            ArchitectureMaterial("concrete",d.concrete_tile_m);ArchitectureMaterial("corrugated_iron",d.iron_tile_m);
            var steel=Lit("Steel",new Color(.31f,.37f,.4f),.75f,.36f);var dark=Lit("Charcoal",new Color(.027f,.048f,.067f),.25f,.3f);
            var teal=Lit("EventTeal",new Color(.017f,.27f,.34f),0,.22f);
            var display=Lit("Display",new Color(.013f,.105f,.14f),0,0);display.shader=Shader.Find("Universal Render Pipeline/Unlit");EditorUtility.SetDirty(display);
            var root=new GameObject("G2 event infrastructure");var binding=root.AddComponent<G2EventDressing>();
            try
            {
                var screen=new GameObject("Original event screen");screen.transform.SetParent(root.transform,false);Place(screen.transform,d.screen);binding.Screen=screen.transform;
                foreach(var group in new[]{"ScreenFrame","ScreenSteel"})
                {
                    var data=JsonUtility.FromJson<MeshData>(File.ReadAllText("artifacts/g2-event/"+group+".json"));
                    if(data==null||data.schema_version!=1||string.IsNullOrWhiteSpace(data.id)||data.vertices==null||data.normals==null||data.indices==null||data.vertices.Length%3!=0||data.normals.Length!=data.vertices.Length||data.indices.Length%3!=0)throw new InvalidDataException("Invalid screen mesh.");
                    var v=new Vector3[data.vertices.Length/3];var n=new Vector3[v.Length];for(int i=0;i<v.Length;i++){v[i]=new Vector3(data.vertices[3*i],data.vertices[3*i+1],data.vertices[3*i+2]);n[i]=new Vector3(data.normals[3*i],data.normals[3*i+1],data.normals[3*i+2]);if(!float.IsFinite(v[i].sqrMagnitude)||!float.IsFinite(n[i].sqrMagnitude))throw new InvalidDataException("Nonfinite screen geometry.");}
                    foreach(int i in data.indices)if(i<0||i>=v.Length)throw new InvalidDataException("Invalid screen index.");
                    var mesh=new Mesh{name=group,vertices=v,normals=n,triangles=data.indices};mesh.RecalculateBounds();Part(screen.transform,Save(mesh,group),group=="ScreenSteel"?steel:dark);
                }
                // Original static event identity, never fabricated race timings/rankings.
                var panel=new StadiumArtMesh();panel.Box(new Vector3(0,6.2f,-.21f),new Vector3(5.95f,3.12f,.04f),.01f);Part(screen.transform,Save(panel.Build("EventPanel"),"EventPanel"),display);
                Text(screen.transform,"/\\  /\\",new Vector3(-1.65f,7.15f,-.24f),.33f,new Color(.55f,.86f,.91f));
                Text(screen.transform,"BIATHLON",new Vector3(0,6.3f,-.24f),.64f,Color.white);
                Text(screen.transform,"WINTER  /  RACE VENUE",new Vector3(0,5.57f,-.24f),.25f,new Color(.75f,.93f,.96f));
                Text(screen.transform,"PRECISION     ENDURANCE",new Vector3(0,4.95f,-.24f),.21f,Color.white);
                var pole=new StadiumArtMesh();pole.Box(new Vector3(0,.04f,0),new Vector3(.72f,.08f,.72f),.025f);pole.Box(new Vector3(0,2.3f,0),new Vector3(.07f,4.6f,.07f),.015f);pole.Box(new Vector3(.5f,4.55f,0),new Vector3(1,.055f,.055f),.008f);
                var poleMesh=Save(pole.Build("BannerPole"),"BannerPole");var flag=new StadiumArtMesh();flag.Box(new Vector3(.5f,3.15f,.035f),new Vector3(.94f,2.7f,.02f),.006f);var flagMesh=Save(flag.Build("BannerFabric"),"BannerFabric");
                binding.Banners=new Transform[d.banners.Length];for(int i=0;i<d.banners.Length;i++)
                {
                    var go=new GameObject("Event banner");go.transform.SetParent(root.transform,false);Place(go.transform,d.banners[i]);binding.Banners[i]=go.transform;Part(go.transform,poleMesh,steel);Part(go.transform,flagMesh,i%2==0?teal:dark);
                    Text(go.transform,"BIATHLON",new Vector3(.5f,3.85f,.016f),.13f,Color.white);Text(go.transform,i<2?"RANGE":"FINISH",new Vector3(.5f,2.7f,.016f),.14f,Color.white);
                }
                PrefabUtility.SaveAsPrefabAsset(root,Root+"Event.prefab");AssetDatabase.SaveAssets();
            }
            finally{Object.DestroyImmediate(root);}
        }
        public static void ApplyArchitecture(GameObject sector,G2EventDressing binding)
        {
            var concrete=Resources.Load<Material>("Biathlon/Stadium/concrete");var ironSource=Resources.Load<Material>("Biathlon/Stadium/dark");var d=G2EventDressing.Load();var modified=new List<MeshFilter>();
            foreach(var f in sector.GetComponentsInChildren<MeshFilter>())
            {
                var renderer=f.GetComponent<MeshRenderer>();if(renderer==null)continue;bool isConcrete=renderer.sharedMaterial==concrete;
                // Authoring asset identity, never a gameplay identifier. Only the lodge's shell/frame uses the metal map.
                bool isIron=renderer.sharedMaterial==ironSource&&f.sharedMesh==Resources.Load<Mesh>("Biathlon/Stadium/TimingLodge_dark");if(!isConcrete&&!isIron)continue;
                string id="Mapped_"+f.sharedMesh.name;var mesh=Planar(f.sharedMesh);mesh.name=id;f.sharedMesh=Save(mesh,id);
                renderer.sharedMaterial=Resources.Load<Material>("Biathlon/Stadium/G2Event/"+(isConcrete?"concrete":"corrugated_iron"));modified.Add(f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(f);PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
            binding.Architecture=modified.ToArray();PrefabUtility.RecordPrefabInstancePropertyModifications(binding);
        }
        public static Mesh Planar(Mesh source)
        {
            var sv=source.vertices;var sn=source.normals;var indices=source.triangles;var v=new Vector3[indices.Length];var n=new Vector3[v.Length];var uv=new Vector2[v.Length];var t=new int[v.Length];
            for(int i=0;i<t.Length;i+=3)
            {
                var face=Vector3.Cross(sv[indices[i+1]]-sv[indices[i]],sv[indices[i+2]]-sv[indices[i]]);var a=new Vector3(Mathf.Abs(face.x),Mathf.Abs(face.y),Mathf.Abs(face.z));
                for(int j=0;j<3;j++){int k=i+j;v[k]=sv[indices[k]];n[k]=sn[indices[k]];t[k]=k;uv[k]=a.y>=a.x&&a.y>=a.z?new Vector2(v[k].x,v[k].z):a.x>=a.z?new Vector2(v[k].z,v[k].y):new Vector2(v[k].x,v[k].y);}
            }
            var m=new Mesh{indexFormat=v.Length>65535?UnityEngine.Rendering.IndexFormat.UInt32:UnityEngine.Rendering.IndexFormat.UInt16,vertices=v,normals=n,uv=uv,triangles=t};m.RecalculateBounds();m.RecalculateTangents();return m;
        }
        private static void ArchitectureMaterial(string name,float meters)
        {
            foreach(var suffix in name=="concrete"?new[]{"Albedo","Normal","Roughness"}:new[]{"Albedo","Normal","Roughness","Metallic"})
            {
                var p=Root+"Textures/"+name+"_"+suffix+".png";var importer=AssetImporter.GetAtPath(p) as TextureImporter;if(importer==null)throw new InvalidDataException("Missing architecture map "+p);
                importer.textureType=suffix=="Normal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=suffix=="Albedo";importer.maxTextureSize=1024;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.anisoLevel=4;importer.SaveAndReimport();
            }
            var rough=new Texture2D(2,2,TextureFormat.RGBA32,false,true);ImageConversion.LoadImage(rough,File.ReadAllBytes(Root+"Textures/"+name+"_Roughness.png"));var pixels=rough.GetPixels32();
            Texture2D metal=null;Color32[] metalPixels=null;if(name!="concrete"){metal=new Texture2D(2,2,TextureFormat.RGBA32,false,true);ImageConversion.LoadImage(metal,File.ReadAllBytes(Root+"Textures/"+name+"_Metallic.png"));metalPixels=metal.GetPixels32();if(metalPixels.Length!=pixels.Length)throw new InvalidDataException("Architecture mask dimensions differ.");}
            for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(metalPixels==null?(byte)0:metalPixels[i].r,0,0,(byte)(255-pixels[i].r));if(metal!=null)Object.DestroyImmediate(metal);
            rough.SetPixels32(pixels);rough.Apply();string maskPath=Root+"Textures/"+name+"_MetalSmooth.png";File.WriteAllBytes(maskPath,rough.EncodeToPNG());Object.DestroyImmediate(rough);AssetDatabase.ImportAsset(maskPath);
            var maskImporter=(TextureImporter)AssetImporter.GetAtPath(maskPath);maskImporter.sRGBTexture=false;maskImporter.maxTextureSize=1024;maskImporter.mipmapEnabled=true;maskImporter.wrapMode=TextureWrapMode.Repeat;maskImporter.SaveAndReimport();
            var m=Lit(name,Color.white,0,.6f);m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+name+"_Albedo.png"));m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"Textures/"+name+"_Normal.png"));m.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath));m.SetTextureScale("_BaseMap",Vector2.one/meters);m.SetFloat("_BumpScale",.65f);m.SetFloat("_Smoothness",1);m.EnableKeyword("_NORMALMAP");m.EnableKeyword("_METALLICSPECGLOSSMAP");EditorUtility.SetDirty(m);
        }
        private static Material Lit(string id,Color color,float metal,float smooth)
        {
            var path=Root+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(m==null){m=new Material(shader);AssetDatabase.CreateAsset(m,path);}else{var clean=new Material(shader);EditorUtility.CopySerialized(clean,m);Object.DestroyImmediate(clean);}
            m.name=id;m.enableInstancing=true;m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metal);m.SetFloat("_Smoothness",smooth);EditorUtility.SetDirty(m);return m;
        }
        private static Mesh Save(Mesh mesh,string id)
        {
            string path=Root+id+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);return saved;
        }
        private static void Part(Transform parent,Mesh mesh,Material material){var go=new GameObject(mesh.name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;}
        private static void Place(Transform t,G2EventDressing.Placement p){t.localPosition=p.position;t.localRotation=Quaternion.Euler(0,p.yaw,0);}
        private static void Text(Transform parent,string value,Vector3 position,float height,Color color)
        {
            var go=new GameObject("Original event text");go.transform.SetParent(parent,false);go.transform.localPosition=position;var t=go.AddComponent<TextMesh>();t.text=value;t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=96;t.characterSize=height/7.2f;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=color;go.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;go.AddComponent<VenueSignText>();
        }
    }
}
#endif

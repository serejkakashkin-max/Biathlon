#if UNITY_EDITOR
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;

namespace Biathlon.Unity
{
    public static class WoodlandServicesAuthoring
    {
        private const string Root = "Assets/Resources/Biathlon/Stadium/WoodlandServices/";

        public static void Generate()
        {
            var snow = Material("RoofSnow", new Color(.86f,.91f,.96f));
            var metal = Material("Frame", new Color(.18f,.22f,.25f));
            var wood = Material("Timber", new Color(.88f,.76f,.63f), "DarkPlanks");
            var glass = Material("Glazing", new Color(.64f,.45f,.25f), emission: new Color(.30f,.17f,.06f));
            var teal = Material("PetrolCanvas", new Color(.055f,.31f,.35f));
            var blue = Material("BlueCanvas", new Color(.10f,.25f,.45f));
            var red = Material("BurgundyCanvas", new Color(.44f,.13f,.14f));
            var crate = Material("Crate", Color.white, "CrateAlbedo");
            GenerateTent("WaxService", "SKI SERVICE", teal);
            GenerateTent("TeamService", "TEAM SERVICE", blue);
            GenerateTent("EquipmentService", "EQUIPMENT", red);
            GenerateLodge();
            SpectatorCrowdAuthoring.GenerateWinterCrowd();
            AssetDatabase.SaveAssets();

            void GenerateTent(string id, string label, Material canvas)
            {
                using var model = new Model(id);
                model.Box(metal,new Vector3(0,-.38f,0),new Vector3(5.22f,.76f,5.16f));
                model.Box(wood,new Vector3(0,-.035f,0),new Vector3(5.26f,.08f,5.20f));
                foreach (int x in new[] {-1,1})
                {
                    foreach (int z in new[] {-1,1})
                    {
                        model.Box(metal, new Vector3(x*2.48f,1.18f,z*2.45f), new Vector3(.10f,2.4f,.10f));
                        model.Box(metal, new Vector3(x*2.48f,.065f,z*2.45f), new Vector3(.35f,.13f,.37f));
                        model.Beam(metal, new Vector3(x*2.46f,1.86f,z*2.46f), new Vector3(x*1.85f,2.35f,z*2.46f), .055f);
                    }
                    var slope = Quaternion.Euler(0,0,-x*24);
                    model.Box(canvas,new Vector3(x*1.26f,2.94f,0),new Vector3(2.83f,.075f,5.34f),slope);
                    model.Box(snow,new Vector3(x*1.32f,3.01f,.04f),new Vector3(2.54f,.07f,5.09f),slope);
                    model.Box(canvas,new Vector3(x*2.57f,2.38f,0),new Vector3(.045f,.35f,5.27f));
                    model.Box(canvas,new Vector3(x*2.49f,1.28f,1.12f),new Vector3(.06f,2.04f,2.65f));
                }
                model.Box(canvas,new Vector3(0,1.31f,2.49f),new Vector3(5,2.1f,.06f));
                model.Box(canvas,new Vector3(0,2.41f,-2.68f),new Vector3(5.3f,.37f,.055f));
                model.Box(metal,new Vector3(0,2.41f,-2.719f),new Vector3(2.48f,.28f,.035f));
                model.Text(label,new Vector3(0,2.418f,-2.745f),.20f);
                model.Box(wood,new Vector3(.67f,.98f,.83f),new Vector3(2.55f,.10f,.67f));
                foreach (int x in new[] {-1,1})
                {
                    model.Box(metal,new Vector3(.67f+x*1.16f,.48f,.83f),new Vector3(.095f,.95f,.54f));
                    model.Prop(crate,new Vector3(.67f+x*.51f,1.033f,.86f),2.15f,x*12);
                }
                model.Prop(crate,new Vector3(1.6f,0,-.94f),3,24);
                model.Prop(crate,new Vector3(1.75f,0,-1.56f),2.3f,-12);
                model.Box(wood,new Vector3(-.32f,.58f,-1.65f),new Vector3(2.50f,.08f,.51f));
                for(int ski=0;ski<7;ski++)
                {
                    float x=-1.31f+ski*.335f;
                    var lean=Quaternion.Euler(12,0,(ski-3)*1.1f);
                    model.Box(ski%3==0?red:ski%3==1?teal:blue,new Vector3(x,.92f,-1.68f),new Vector3(.053f,1.82f,.02f),lean);
                    model.Box(metal,new Vector3(x,.92f,-1.708f),new Vector3(.07f,.14f,.035f),lean);
                }
                model.Save();
            }

            void GenerateLodge()
            {
                using var model=new Model("ServiceLodge");
                model.Box(metal,new Vector3(0,-.575f,0),new Vector3(11.7f,1.80f,7.4f));
                model.Box(wood,new Vector3(0,1.8f,0),new Vector3(11.4f,3.01f,7.06f));
                model.Triangle(wood,new Vector3(5.62f,3.30f,-3.53f),new Vector3(-5.62f,3.30f,-3.53f),new Vector3(0,4.95f,-3.53f));
                model.Triangle(wood,new Vector3(-5.62f,3.30f,3.53f),new Vector3(5.62f,3.30f,3.53f),new Vector3(0,4.95f,3.53f));
                for(int side=-1;side<=1;side+=2)
                {
                    var slope=Quaternion.Euler(0,0,-side*17);
                    model.Box(metal,new Vector3(side*3.03f,4.03f,0),new Vector3(6.54f,.18f,8.10f),slope);
                    model.Box(snow,new Vector3(side*3.05f,4.16f,.04f),new Vector3(6.39f,.13f,7.94f),slope);
                    foreach(int bay in new[] {1,2})
                    {
                        float x=side*(.15f+bay*2.12f);
                        model.Box(metal,new Vector3(x,1.99f,-3.60f),new Vector3(1.68f,1.36f,.15f));
                        model.Box(glass,new Vector3(x,1.99f,-3.69f),new Vector3(1.49f,1.17f,.035f));
                        model.Box(metal,new Vector3(x,1.99f,-3.72f),new Vector3(.068f,1.19f,.038f));
                    }
                    model.Box(wood,new Vector3(side*5.27f,1.54f,-4.62f),new Vector3(.22f,2.93f,.23f));
                }
                model.Box(wood,new Vector3(0,.26f,-4.05f),new Vector3(11.45f,.27f,2.40f));
                model.Box(metal,new Vector3(0,.09f,-5.34f),new Vector3(2.40f,.18f,.58f));
                model.Box(metal,new Vector3(0,1.58f,-3.59f),new Vector3(1.40f,2.54f,.16f));
                model.Box(glass,new Vector3(0,1.97f,-3.68f),new Vector3(.93f,1.40f,.04f));
                model.Box(teal,new Vector3(0,3.13f,-3.71f),new Vector3(4.1f,.42f,.075f));
                model.Text("WOODLAND SERVICE",new Vector3(0,3.135f,-3.765f),.26f);
                model.Box(metal,new Vector3(3.81f,4.17f,1.5f),new Vector3(.53f,1.39f,.69f));
                model.Box(snow,new Vector3(3.81f,4.89f,1.5f),new Vector3(.64f,.09f,.81f));
                model.Save();
            }
        }

        private static Material Material(string name,Color color,string texture=null,Color emission=default)
        {
            string path=Root+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                material=new Material(Shader.Find("Biathlon/Illuminated Venue Lit"));
                AssetDatabase.CreateAsset(material,path);
            }
            material.SetColor("_BaseColor",color);
            material.SetFloat("_Smoothness",name=="Glazing"?.44f:.19f);
            if(texture!=null)
                material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+texture+".jpg"));
            if(emission!=default)
            {
                material.SetColor("_EmissionColor",emission);
                material.EnableKeyword("_EMISSION");
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private sealed class Model : System.IDisposable
        {
            private readonly GameObject root;
            private readonly Dictionary<Material,StadiumArtMesh> parts=new Dictionary<Material,StadiumArtMesh>();
            public Model(string id) { root=new GameObject(id); }
            private StadiumArtMesh Part(Material material)
            {
                if(!parts.TryGetValue(material,out var mesh))parts.Add(material,mesh=new StadiumArtMesh());
                return mesh;
            }
            public void Box(Material material,Vector3 point,Vector3 size,Quaternion? rotation=null)
                => Part(material).Box(point,size,.018f,rotation);
            public void Beam(Material material,Vector3 a,Vector3 b,float width)=>Part(material).Beam(a,b,width);
            public void Triangle(Material material,Vector3 a,Vector3 b,Vector3 c)=>Part(material).Triangle(a,b,c);
            public void Prop(Material material,Vector3 point,float scale,float yaw)
            {
                var source=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"EquipmentCrate.obj");
                var part=new GameObject("Equipment case");
                part.transform.SetParent(root.transform,false);
                part.transform.SetLocalPositionAndRotation(point,Quaternion.Euler(0,yaw,0));
                part.transform.localScale=Vector3.one*scale;
                part.AddComponent<MeshFilter>().sharedMesh=source.GetComponentInChildren<MeshFilter>().sharedMesh;
                part.AddComponent<MeshRenderer>().sharedMaterial=material;
            }
            public void Text(string value,Vector3 point,float height)
            {
                var part=new GameObject(value);
                part.transform.SetParent(root.transform,false);
                part.transform.localPosition=point;
                var text=part.AddComponent<TextMesh>();
                text.text=value;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                text.fontSize=96;text.characterSize=height/7.2f;text.anchor=TextAnchor.MiddleCenter;text.color=Color.white;
                part.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
                part.AddComponent<VenueSignText>();
            }
            public void Save()
            {
                foreach(var part in parts)
                {
                    string name=root.name+"_"+part.Key.name;
                    var mesh=part.Value.Build(name);
                    if(part.Key.name=="Timber")
                    {
                        var mapped=G2EventAuthoring.Planar(mesh);
                        Object.DestroyImmediate(mesh);
                        mesh=mapped;
                        mesh.name=name;
                        var uv=mesh.uv;
                        for(int i=0;i<uv.Length;i++)uv[i]*=.5f;
                        mesh.uv=uv;
                    }
                    string path=Root+name+".asset";
                    var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
                    else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
                    var go=new GameObject(part.Key.name);go.transform.SetParent(root.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=saved;
                    go.AddComponent<MeshRenderer>().sharedMaterial=part.Key;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Root+root.name+".prefab");
            }
            public void Dispose()=>Object.DestroyImmediate(root);
        }
    }
}
#endif

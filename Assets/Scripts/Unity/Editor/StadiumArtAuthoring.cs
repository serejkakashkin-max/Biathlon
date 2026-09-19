#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class StadiumArtAuthoring
    {
        private const string Root="Assets/Resources/Biathlon/Stadium/";
        private static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        [MenuItem("BIATHLON/Generate Stadium Art Benchmark")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();Materials();var d=StadiumArtDefinition.Load();var g=TargetGeometryDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/target_geometry_v1").text);
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var bank=BuildBank(g,d);PrefabUtility.SaveAsPrefabAsset(bank.gameObject,Root+"TargetBank.prefab");Object.DestroyImmediate(bank.gameObject);
                var sector=new GameObject("Original stadium sector");
                var apron=new Part("Apron",sector.transform);apron.Box("snow",new Vector3(0,-.18f,15),new Vector3(100,.35f,135));
                apron.Box("packed",new Vector3(0,-.006f,-3),new Vector3(30,.012f,9));apron.Box("packed",new Vector3(-23,.002f,-22),new Vector3(12,.012f,48));apron.Save();
                var lane=new Part("FiringLine",sector.transform);float width=d.lane_count*d.lane_pitch_m;
                lane.Box("accent",new Vector3(0,.018f,1.3f),new Vector3(width,.018f,.085f));
                for(int i=0;i<d.lane_count;i++)
                {
                    float x=d.LaneX(i);var target=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"TargetBank.prefab"));target.transform.SetParent(sector.transform,false);target.transform.localPosition=new Vector3(x,d.target_height_m,(float)g.DistanceMeters);target.name="Target bank "+(i+1);
                    lane.Box("rubber",new Vector3(x,.03f,0),new Vector3(d.mat_width_m,.05f,d.mat_length_m));
                    for(int rib=0;rib<17;rib++)lane.Box("mat",new Vector3(x,.06f,-.98f+rib*.12f),new Vector3(d.mat_width_m-.06f,.015f,.055f),.003f);
                    lane.Box("white",new Vector3(x,.18f,-1.3f),new Vector3(.42f,.32f,.15f));Text(sector.transform,(i+1).ToString("00"),new Vector3(x,.19f,-1.381f),.18f,materials["ink"].color);
                    Text(sector.transform,(i+1).ToString("00"),new Vector3(x,1.57f,(float)g.DistanceMeters-.06f),.3f,Color.white);
                    lane.Box("dark",new Vector3(x,1.57f,(float)g.DistanceMeters+.03f),new Vector3(.6f,.5f,.1f));
                }
                lane.Save();
                var stop=new Part("Backstop",sector.transform);stop.Box("concrete",new Vector3(0,1.9f,(float)g.DistanceMeters+2),new Vector3(width+4,3.8f,.5f));
                stop.Box("dark",new Vector3(0,3.83f,(float)g.DistanceMeters+2),new Vector3(width+4.3f,.12f,.7f));stop.Box("snow",new Vector3(0,3.91f,(float)g.DistanceMeters+2),new Vector3(width+4.3f,.065f,.7f));
                for(float x=-width*.5f;x<=width*.5f;x+=2.6f)stop.Box("metal",new Vector3(x,1.8f,(float)g.DistanceMeters+1.72f),new Vector3(.07f,3.6f,.07f));stop.Save();
                Barrier(sector.transform,"RangeBarrierLeft",new Vector3(-width*.5f-1,0,-6),22,0,d);
                Barrier(sector.transform,"RangeBarrierRight",new Vector3(width*.5f+1,0,-6),22,0,d);
                Barrier(sector.transform,"FinishBarrierLeft",new Vector3(-29,0,-43),14,0,d);
                Barrier(sector.transform,"FinishBarrierRight",new Vector3(-17,0,-43),14,0,d);
                Finish(sector.transform,d);Building(sector.transform);Stand(sector.transform,d);
                PrefabUtility.SaveAsPrefabAsset(sector,Root+"StadiumSector.prefab");
                var preview=new GameObject("Stadium benchmark presentation").AddComponent<StadiumArtPreview>();
                var key=new GameObject("Cold daylight key").AddComponent<Light>();key.type=LightType.Directional;key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(25,-32,0);preview.Key=key;
                var camera=new GameObject("Stadium overview camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(40,16,-42);camera.transform.LookAt(new Vector3(-3,2,13));camera.fieldOfView=52;camera.farClipPlane=250;camera.gameObject.AddComponent<AudioListener>();preview.View=camera;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/StadiumBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
        private static TargetBankArt BuildBank(TargetGeometryDefinition g,StadiumArtDefinition d)
        {
            var p=new Part("TargetBank",null);float span=(float)(g.SpacingMeters*5);
            p.Box("white",new Vector3(0,0,.028f),new Vector3(span+.14f,.32f,.035f),.008f);
            p.Box("dark",new Vector3(0,0,.26f),new Vector3(span+.23f,.49f,.39f));
            p.Box("metal",new Vector3(0,.22f,.15f),new Vector3(span+.32f,.06f,.56f));
            p.Box("snow",new Vector3(0,.265f,.18f),new Vector3(span+.32f,.035f,.51f));
            p.Box("metal",new Vector3(0,-.23f,.11f),new Vector3(span+.27f,.045f,.31f));
            for(int sign=-1;sign<=1;sign+=2)
            {
                p.Box("metal",new Vector3(sign*(span*.36f),-d.target_height_m*.5f,.23f),new Vector3(.06f,d.target_height_m,.08f));
                p.Box("metal",new Vector3(sign*(span*.36f),-d.target_height_m+.04f,.26f),new Vector3(.22f,.07f,.48f));
                p.Box("metal",new Vector3(sign*(span*.5f+.055f),0,.003f),new Vector3(.018f,.024f,.012f),.003f);
            }
            p.Save();
            // Millimetre details at 50m and a narrow aim FOV amplify cascade self-shadow acne.
            // The small housing keeps PBR lighting and casts ground shadows, but does not receive cascades.
            foreach(var r in p.Root.GetComponentsInChildren<Renderer>())
            {
                r.receiveShadows=false;
                var source=r.sharedMaterial;if(source.shader.name!="Universal Render Pipeline/Lit")continue;
                string path=Root+"Target_"+source.name+".mat";var isolated=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(isolated==null){isolated=new Material(source);AssetDatabase.CreateAsset(isolated,path);}else isolated.CopyPropertiesFromMaterial(source);
                isolated.SetFloat("_ReceiveShadows",0);isolated.EnableKeyword("_RECEIVE_SHADOWS_OFF");EditorUtility.SetDirty(isolated);r.sharedMaterial=isolated;
            }
            var disk=SaveMesh(StadiumArtMesh.Disk(),"Aperture");var faces=new Renderer[5];
            for(int i=0;i<5;i++){var f=new GameObject("Geometric aperture "+i);f.transform.SetParent(p.Root.transform,false);f.AddComponent<MeshFilter>().sharedMesh=disk;faces[i]=f.AddComponent<MeshRenderer>();faces[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
            var binding=p.Root.AddComponent<TargetBankArt>();binding.Bind(faces,materials["ink"],materials["closure"]);binding.Apply(g,ShootingStance.Standing,0);return binding;
        }
        private static void Barrier(Transform parent,string id,Vector3 origin,int count,float yaw,StadiumArtDefinition d)
        {
            var p=new Part(id,parent);p.Root.transform.localPosition=origin;p.Root.transform.localRotation=Quaternion.Euler(0,yaw,0);
            for(int i=0;i<count;i++)
            {
                float z=i*d.barrier_module_m,h=d.barrier_height_m;
                p.Box("metal",new Vector3(0,h*.5f,z),new Vector3(.065f,h,.065f));p.Box("metal",new Vector3(0,.045f,z),new Vector3(.65f,.07f,.18f));
                if(i==count-1)continue;
                p.Box("metal",new Vector3(0,h,z+d.barrier_module_m*.5f),new Vector3(.07f,.055f,d.barrier_module_m));
                p.Box("accent",new Vector3(0,h*.54f,z+d.barrier_module_m*.5f),new Vector3(.045f,h*.66f,d.barrier_module_m-.11f),.006f);
                p.Box("white",new Vector3(-.025f,h*.72f,z+d.barrier_module_m*.5f),new Vector3(.008f,.032f,d.barrier_module_m-.16f),.002f);
            }p.Save();
        }
        private static void Finish(Transform parent,StadiumArtDefinition d)
        {
            var p=new Part("FinishGantry",parent);p.Root.transform.localPosition=new Vector3(-23,0,-22);float half=d.finish_width_m*.5f,h=d.finish_height_m;
            for(int s=-1;s<=1;s+=2)
            {
                p.Box("concrete",new Vector3(s*(half+.35f),.2f,0),new Vector3(1.25f,.4f,1.3f));
                p.Box("dark",new Vector3(s*(half+.35f),h*.5f,0),new Vector3(.55f,h,.65f));
                p.Box("accent",new Vector3(s*(half+.35f),h*.5f,-.34f),new Vector3(.42f,h-.6f,.05f));
                for(int i=0;i<6;i++)p.Beam("metal",new Vector3(s*(half+.66f),.5f+i*.65f,-.3f),new Vector3(s*(half+.66f),1.1f+i*.65f,.3f),.045f);
            }
            p.Box("dark",new Vector3(0,h,0),new Vector3(d.finish_width_m+1.6f,.9f,.75f));p.Box("accent",new Vector3(0,h,-.4f),new Vector3(d.finish_width_m+.6f,.7f,.065f));
            p.Box("snow",new Vector3(0,h+.49f,.01f),new Vector3(d.finish_width_m+1.6f,.07f,.85f));
            p.Box("accent",new Vector3(0,.017f,0),new Vector3(d.finish_width_m,.03f,.16f),.006f);
            p.Box("white",new Vector3(0,h-.85f,-.08f),new Vector3(2.5f,.53f,.2f));p.Save();
            Text(p.Root.transform,"FINISH",new Vector3(0,h,-.445f),.6f,Color.white);Text(p.Root.transform,"BIATHLON",new Vector3(0,h-.86f,-.19f),.21f,materials["ink"].color);
        }
        private static void Building(Transform parent)
        {
            var p=new Part("TimingLodge",parent);p.Root.transform.localPosition=new Vector3(-24,0,15);
            p.Box("concrete",new Vector3(0,.35f,0),new Vector3(12,.7f,9));p.Box("wood",new Vector3(0,2.4f,0),new Vector3(11.7f,3.5f,8.6f));
            // Individual original timber battens and framed opaque glazing give facade depth.
            for(int i=0;i<59;i++)p.Box(i%4==0?"woodLight":"wood",new Vector3(-5.75f+i*.2f,2.4f,-4.36f),new Vector3(.145f,3.45f,.10f),.008f);
            for(int i=0;i<5;i++)
            {
                if(i==2)continue;
                float x=-4.7f+i*2.35f;p.Box("dark",new Vector3(x,2.7f,-4.47f),new Vector3(1.9f,1.8f,.12f));p.Box("glass",new Vector3(x,2.7f,-4.541f),new Vector3(1.74f,1.64f,.025f),.006f);
                p.Box("metal",new Vector3(x,2.7f,-4.56f),new Vector3(.045f,1.66f,.025f));p.Box("metal",new Vector3(x,2.65f,-4.56f),new Vector3(1.75f,.045f,.025f));
            }
            p.Box("dark",new Vector3(0,2.21f,-4.48f),new Vector3(1.25f,2.22f,.14f));p.Box("glass",new Vector3(0,2.3f,-4.56f),new Vector3(1.04f,1.78f,.025f));
            p.Box("metal",new Vector3(.4f,2.04f,-4.61f),new Vector3(.035f,.32f,.045f));
            for(int step=0;step<4;step++)p.Box("concrete",new Vector3(0,(step+1)*.2775f*.5f,-7.25f+step*.4f),new Vector3(1.6f,(step+1)*.2775f,.42f));
            for(int side=-1;side<=1;side+=2){p.Box("metal",new Vector3(side*.86f,.64f,-7.15f),new Vector3(.05f,1.05f,.05f));p.Box("metal",new Vector3(side*.86f,1.4f,-5.95f),new Vector3(.05f,1.05f,.05f));p.Beam("metal",new Vector3(side*.86f,1.17f,-7.15f),new Vector3(side*.86f,1.93f,-5.95f),.05f);}
            p.Box("dark",new Vector3(0,4.3f,0),new Vector3(12.9f,.27f,10.2f),.045f);p.Box("snow",new Vector3(0,4.47f,0),new Vector3(12.8f,.10f,10.1f),.04f);
            for(int i=0;i<8;i++)p.Box("metal",new Vector3(-5.8f+i*1.65f,4.43f,0),new Vector3(.07f,.14f,10.1f));
            p.Box("dark",new Vector3(0,1.01f,-5.1f),new Vector3(12,.2f,1.9f));
            for(int s=-1;s<=1;s+=2)p.Box("metal",new Vector3(s*5.7f,2.65f,-5.5f),new Vector3(.12f,3.15f,.12f));
            p.Box("accent",new Vector3(0,4,-5.2f),new Vector3(5.4f,.6f,.12f));p.Save();Text(p.Root.transform,"RACE CONTROL",new Vector3(0,4,-5.27f),.32f,Color.white);
        }
        private static void Stand(Transform parent,StadiumArtDefinition d)
        {
            var p=new Part("Grandstand",parent);p.Root.transform.localPosition=new Vector3(22,0,9);p.Root.transform.localRotation=Quaternion.Euler(0,90,0);
            float half=d.stand_width_m*.5f;
            for(int r=0;r<d.stand_rows;r++)
            {
                float z=r*d.stand_row_depth_m,y=(r+1)*d.stand_rise_m;
                p.Box("concrete",new Vector3(0,y*.5f,z),new Vector3(d.stand_width_m,y,d.stand_row_depth_m));
                p.Box("concrete",new Vector3(0,(y-d.stand_rise_m*.5f)*.5f,z-d.stand_row_depth_m*.65f),new Vector3(1.35f,y-d.stand_rise_m*.5f,d.stand_row_depth_m*.5f));
                p.Box("metal",new Vector3(0,y+.025f,z-.35f),new Vector3(d.stand_width_m,.035f,.07f));
                for(int seat=0;seat<d.seats_per_row;seat++)
                {
                    float x=(seat-(d.seats_per_row-1)*.5f)*d.seat_pitch_m;if(Mathf.Abs(x)<d.aisle_half_width_m)continue;
                    p.Box("seat",new Vector3(x,y+.28f,z+.08f),new Vector3(.48f,.08f,.42f),.035f);
                    p.Box("seat",new Vector3(x,y+.51f,z+.27f),new Vector3(.48f,.4f,.07f),.025f);
                    p.Box("metal",new Vector3(x,y+.14f,z+.08f),new Vector3(.07f,.24f,.18f));
                }
            }
            for(int side=-1;side<=1;side+=2)
            {
                for(int r=0;r<=d.stand_rows;r++){float z=r*d.stand_row_depth_m-.4f,y=Mathf.Max(d.stand_rise_m,r*d.stand_rise_m);p.Box("metal",new Vector3(side*half,y+.5f,z),new Vector3(.06f,1,.06f));}
                p.Beam("metal",new Vector3(side*half,1.4f,-.4f),new Vector3(side*half,d.stand_rows*d.stand_rise_m+1,d.stand_rows*d.stand_row_depth_m-.4f),.06f);
            }
            float rear=d.stand_rows*d.stand_row_depth_m;
            for(int i=0;i<5;i++)p.Box("dark",new Vector3(-half+i*d.stand_width_m/4,2.5f,rear),new Vector3(.16f,5,.16f));
            p.Box("dark",new Vector3(0,5,rear*.5f),new Vector3(d.stand_width_m+1,.19f,rear+1.5f));p.Box("snow",new Vector3(0,5.13f,rear*.5f),new Vector3(d.stand_width_m+1,.06f,rear+1.5f));
            p.Box("accent",new Vector3(0,4.78f,-.75f),new Vector3(d.stand_width_m+1,.42f,.12f));p.Save();Text(p.Root.transform,"BIATHLON  /  SPECTATORS",new Vector3(0,4.78f,-.82f),.27f,Color.white);
            p.Root.AddComponent<SpectatorSeating>().Configure(d.SeatAnchors());
        }
        private static void Materials()
        {
            materials.Clear();Lit("white",new Color(.85f,.87f,.86f),0,.32f);Lit("dark",new Color(.055f,.085f,.105f),.55f,.35f);Lit("metal",new Color(.30f,.35f,.39f),.75f,.48f);Lit("accent",new Color(.025f,.24f,.29f),.2f,.32f);
            Material("SignText","Biathlon/VenueSignText");
            Lit("concrete",new Color(.39f,.42f,.43f),0,.15f);Lit("wood",new Color(.25f,.14f,.075f),0,.18f);Lit("woodLight",new Color(.34f,.20f,.11f),0,.18f);Lit("glass",new Color(.12f,.23f,.30f),.65f,.88f);
            Lit("rubber",new Color(.08f,.10f,.105f),0,.05f);Lit("mat",new Color(.13f,.18f,.19f),0,.12f);Lit("seat",new Color(.055f,.19f,.24f),.1f,.4f);
            Unlit("ink",new Color(.015f,.02f,.024f));Unlit("closure",new Color(.88f,.9f,.9f));materials["snow"]=Resources.Load<Material>("Biathlon/Snow/cold");materials["packed"]=Resources.Load<Material>("Biathlon/Snow/wet");
        }
        private static void Lit(string id,Color color,float metallic,float smoothness){var m=Material(id,"Universal Render Pipeline/Lit");m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);EditorUtility.SetDirty(m);}
        private static void Unlit(string id,Color color){var m=Material(id,"Universal Render Pipeline/Unlit");m.SetColor("_BaseColor",color);EditorUtility.SetDirty(m);}
        private static Material Material(string id,string shader){var path=Root+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,path);}materials[id]=m;return m;}
        private static Mesh SaveMesh(Mesh mesh,string id){var path=Root+id+".asset";mesh.name=id;var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(mesh,path);return mesh;}EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);return old;}
        private static void Text(Transform parent,string label,Vector3 position,float height,Color color)
        {
            var go=new GameObject("Venue text");go.transform.SetParent(parent,false);go.transform.localPosition=position;
            var text=go.AddComponent<TextMesh>();text.text=label;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=96;text.characterSize=height/7.2f;text.anchor=TextAnchor.MiddleCenter;text.alignment=TextAlignment.Center;text.color=color;go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
            go.AddComponent<VenueSignText>();
        }
        private sealed class Part
        {
            public readonly GameObject Root;private readonly string id;private readonly Dictionary<string,StadiumArtMesh> meshes=new Dictionary<string,StadiumArtMesh>();
            public Part(string name,Transform parent){id=name;Root=new GameObject(name);Root.transform.SetParent(parent,false);}
            private StadiumArtMesh Mesh(string key){if(!meshes.TryGetValue(key,out var m)){m=new StadiumArtMesh();meshes.Add(key,m);}return m;}
            public void Box(string mat,Vector3 p,Vector3 size,float bevel=.015f)=>Mesh(mat).Box(p,size,bevel);
            public void Beam(string mat,Vector3 a,Vector3 b,float width)=>Mesh(mat).Beam(a,b,width);
            public void Save(){foreach(var pair in meshes){var go=new GameObject(pair.Key);go.transform.SetParent(Root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=SaveMesh(pair.Value.Build(id+"_"+pair.Key),id+"_"+pair.Key);go.AddComponent<MeshRenderer>().sharedMaterial=materials[pair.Key];}}
        }
    }
}
#endif

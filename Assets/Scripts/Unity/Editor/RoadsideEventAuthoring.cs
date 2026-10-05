#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Biathlon.Unity
{
    /// <summary>Original event presentation only. Explicit owned assets, no scene/settings/global save.</summary>
    public static class RoadsideEventAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/Roadside/";
        public const string CrowdRoot="Assets/Resources/Biathlon/Crowd/";

        [MenuItem("BIATHLON/Generate Original Roadside Event Assets")]
        public static void Generate()
        {
            // Temporary authoring objects must never enter an owner's currently opened scene.
            var prior=SceneManager.GetActiveScene();
            var temporary=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                if(!SceneManager.SetActiveScene(temporary))throw new InvalidOperationException("Temporary roadside authoring scene activation failed.");
                Folder("Assets/Resources/Biathlon","Roadside");
                var steel=RequireMaterial("Biathlon/Stadium/G2Event/Steel");
                var teal=RequireMaterial("Biathlon/Stadium/G2Event/EventTeal");
                var white=RequireMaterial("Biathlon/Stadium/white");
                RequireMaterial("Biathlon/Crowd/Spectator");
                for(int lod=0;lod<2;lod++)SaveMesh(SpectatorCrowdAuthoring.Build(lod,SpectatorPose.Standing),CrowdRoot+"StandingSpectatorLOD"+lod+".asset");
                Tape(steel,teal,white);Billboard(steel,teal,white);Flag(steel,teal,white);Crowd();
            }
            finally
            {
                if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
                EditorSceneManager.CloseScene(temporary,true);
            }
        }

        private static void Tape(Material steel,Material teal,Material white)
        {
            var posts=new StadiumArtMesh();
            // Root is the snow level. Narrow stakes extend below it for authored snow embedding.
            for(int side=-1;side<=1;side+=2)posts.Box(new Vector3(side,.375f,0),new Vector3(.045f,1.35f,.045f),.003f);
            var tealTape=new StadiumArtMesh();var whiteTape=new StadiumArtMesh();
            for(int i=0;i<8;i++)
                (i%2==0?tealTape:whiteTape).Box(new Vector3(-.875f+i*.25f,.91f,0),new Vector3(.25f,.105f,.014f),.001f);
            SavePrefab("RibbonTape2m",new[]{SavePart(posts,"TapePosts",steel),SavePart(tealTape,"TapeTeal",teal),SavePart(whiteTape,"TapeWhite",white)});
        }

        private static void Billboard(Material steel,Material teal,Material white)
        {
            var frame=new StadiumArtMesh();
            for(int side=-1;side<=1;side+=2)
            {
                frame.Box(new Vector3(side*1.32f,.075f,0),new Vector3(.46f,.15f,.64f),.012f);
                frame.Box(new Vector3(side*1.32f,1.28f,0),new Vector3(.075f,2.56f,.075f),.007f);
            }
            frame.Box(new Vector3(0,2.47f,0),new Vector3(3.4f,.095f,.13f),.007f);
            frame.Box(new Vector3(0,1.20f,0),new Vector3(3.4f,.095f,.13f),.007f);
            for(int side=-1;side<=1;side+=2)frame.Box(new Vector3(side*1.655f,1.835f,0),new Vector3(.09f,1.27f,.13f),.007f);
            var panel=new StadiumArtMesh();panel.Box(new Vector3(0,1.835f,0),new Vector3(3.22f,1.18f,.08f),.006f);
            var lettering=new StadiumArtMesh();
            foreach(int side in new[]{-1,1})
            {
                Glyphs(lettering,"BIATHLON",2.07f,.045f,side*.075f,side==1);
                Glyphs(lettering,"WOODLAND BASIN",1.62f,.027f,side*.075f,side==1);
                lettering.Box(new Vector3(0,1.34f,side*.075f),new Vector3(2.78f,.035f,.014f),.001f);
            }
            SavePrefab("Billboard",new[]{SavePart(frame,"BillboardFrame",steel),SavePart(panel,"BillboardPanel",teal),SavePart(lettering,"BillboardLettering",white)});
        }

        private static void Flag(Material steel,Material teal,Material white)
        {
            var pole=new StadiumArtMesh();
            pole.Box(new Vector3(0,.075f,0),new Vector3(.65f,.15f,.65f),.014f);
            pole.Box(new Vector3(0,2.03f,0),new Vector3(.07f,4.06f,.07f),.006f);
            pole.Box(new Vector3(.60f,3.98f,0),new Vector3(1.20f,.055f,.055f),.005f);
            var fabric=new StadiumArtMesh();fabric.Box(new Vector3(.60f,3.12f,0),new Vector3(1.06f,1.65f,.02f),.002f);
            var print=new StadiumArtMesh();
            foreach(int side in new[]{-1,1})
            {
                Glyphs(print,"RACE",3.56f,.025f,side*.022f,side==1,.60f);
                print.Box(new Vector3(.60f,2.87f,side*.022f),new Vector3(.79f,.04f,.012f),.001f);
                print.Box(new Vector3(.60f,2.50f,side*.022f),new Vector3(.79f,.10f,.012f),.001f);
            }
            SavePrefab("FlagStela",new[]{SavePart(pole,"FlagPole",steel),SavePart(fabric,"FlagFabric",teal),SavePart(print,"FlagPrint",white)});
        }

        private static void Crowd()
        {
            var root=new GameObject("Original standing roadside spectators");
            try
            {
                var anchors=new Vector3[16];
                for(int row=0;row<2;row++)for(int i=0;i<8;i++)anchors[row*8+i]=new Vector3((i-3.5f)*.90f+(row==1?.15f:0),0,row*1.10f);
                root.AddComponent<SpectatorSeating>().Configure(anchors,SpectatorPose.Standing);
                // The environment owns renderer initialization. This prefab contains feet anchors only.
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+"StandingCrowd16.prefab");
                if(prefab==null)throw new InvalidOperationException("Standing roadside prefab save failed.");
                AssetDatabase.SaveAssetIfDirty(prefab);
            }
            finally{Object.DestroyImmediate(root);}
        }

        private readonly struct Part
        {
            public readonly Mesh Mesh;public readonly Material Material;
            public Part(Mesh mesh,Material material){Mesh=mesh;Material=material;}
        }
        private static Part SavePart(StadiumArtMesh mesh,string id,Material material)=>new Part(SaveMesh(mesh.Build(id),Root+id+".asset"),material);
        private static Mesh SaveMesh(Mesh mesh,string path)
        {
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null){AssetDatabase.CreateAsset(mesh,path);saved=mesh;}
            else{EditorUtility.CopySerialized(mesh,saved);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}
            AssetDatabase.SaveAssetIfDirty(saved);return saved;
        }
        private static void SavePrefab(string id,Part[] parts)
        {
            var root=new GameObject(id);
            try
            {
                foreach(var part in parts)
                {
                    var go=new GameObject(part.Mesh.name);go.transform.SetParent(root.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=part.Mesh;go.AddComponent<MeshRenderer>().sharedMaterial=part.Material;
                }
                var prefab=PrefabUtility.SaveAsPrefabAsset(root,Root+id+".prefab");
                if(prefab==null)throw new InvalidOperationException("Roadside prefab save failed.");
                AssetDatabase.SaveAssetIfDirty(prefab);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static Material RequireMaterial(string resource)
        {
            var material=Resources.Load<Material>(resource);
            if(material==null)throw new InvalidOperationException("Existing audited roadside material missing: "+resource);
            return material;
        }
        private static void Folder(string parent,string name)
        {
            string path=parent+"/"+name;if(!AssetDatabase.IsValidFolder(path))AssetDatabase.CreateFolder(parent,name);
        }

        // Original five-column pixel lettering, baked in metres into the same native mesh frame.
        private static readonly Dictionary<char,string[]> glyphs=new Dictionary<char,string[]>
        {
            ['A']=new[]{"01110","10001","10001","11111","10001","10001","10001"},
            ['B']=new[]{"11110","10001","10001","11110","10001","10001","11110"},
            ['C']=new[]{"01111","10000","10000","10000","10000","10000","01111"},
            ['D']=new[]{"11110","10001","10001","10001","10001","10001","11110"},
            ['E']=new[]{"11111","10000","10000","11110","10000","10000","11111"},
            ['H']=new[]{"10001","10001","10001","11111","10001","10001","10001"},
            ['I']=new[]{"11111","00100","00100","00100","00100","00100","11111"},
            ['L']=new[]{"10000","10000","10000","10000","10000","10000","11111"},
            ['N']=new[]{"10001","11001","11001","10101","10011","10011","10001"},
            ['O']=new[]{"01110","10001","10001","10001","10001","10001","01110"},
            ['R']=new[]{"11110","10001","10001","11110","10100","10010","10001"},
            ['S']=new[]{"01111","10000","10000","01110","00001","00001","11110"},
            ['T']=new[]{"11111","00100","00100","00100","00100","00100","00100"},
            ['W']=new[]{"10001","10001","10001","10101","10101","10101","01010"}
        };
        private static void Glyphs(StadiumArtMesh mesh,string text,float centerY,float cell,float z,bool rear,float centerX=0)
        {
            float start=-(text.Length*6-1)*cell*.5f;
            for(int i=0;i<text.Length;i++)
            {
                if(text[i]==' ')continue;if(!glyphs.TryGetValue(text[i],out var pattern))throw new InvalidOperationException("Unsupported original glyph.");
                for(int row=0;row<7;row++)for(int column=0;column<5;column++)if(pattern[row][column]=='1')
                {
                    float x=start+(i*6+column+.5f)*cell;
                    mesh.Box(new Vector3(centerX+(rear?-x:x),centerY+(3-row)*cell,z),new Vector3(cell*.83f,cell*.83f,.012f),.001f);
                }
            }
        }
    }
}
#endif

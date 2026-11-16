#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace Biathlon.Unity
{
    public static class SpectatorCrowdAuthoring
    {
        private const string Root="Assets/Resources/Biathlon/Crowd/";
        [MenuItem("BIATHLON/Generate Crowd Benchmark")]
        public static void Generate()
        {
            StadiumArtAuthoring.Generate();Directory.CreateDirectory(Root);AssetDatabase.Refresh();var shader=Shader.Find("Biathlon/Spectator");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.InvalidOperationException("Spectator shader failed.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Spectator.mat");if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"Spectator.mat");}material.enableInstancing=true;EditorUtility.SetDirty(material);
            for(int lod=0;lod<2;lod++){var mesh=Build(lod);string path=Root+"SpectatorLOD"+lod+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);}}
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var sector=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Biathlon/Stadium/StadiumSector"));
                var preview=new GameObject("Crowd benchmark presentation").AddComponent<StadiumArtPreview>();var crowd=preview.gameObject.AddComponent<SpectatorCrowdPreview>();crowd.Scene=preview;crowd.Seating=sector.GetComponentInChildren<SpectatorSeating>();
                var key=new GameObject("Spectator winter light").AddComponent<Light>();key.type=LightType.Directional;key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(25,-32,0);preview.Key=key;
                var camera=new GameObject("Crowd benchmark camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(13,2.4f,-1);camera.transform.LookAt(new Vector3(24,2,9));camera.fieldOfView=55;camera.farClipPlane=250;camera.gameObject.AddComponent<AudioListener>();preview.View=camera;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/CrowdBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
        public static Mesh Build(int lod)=>Build(lod,SpectatorPose.Seated);
        public static void GenerateWinterCrowd()
        {
            foreach(var pose in new[]{SpectatorPose.Seated,SpectatorPose.Standing})
            for(int lod=0;lod<2;lod++)
            {
                var mesh=Build(lod,pose);
                string path=Root+mesh.name+".asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null)AssetDatabase.CreateAsset(mesh,path);
                else{EditorUtility.CopySerialized(mesh,saved);EditorUtility.SetDirty(saved);Object.DestroyImmediate(mesh);}
            }
        }
        public static void GenerateDistantSilhouettes()
        {
            foreach(var pose in new[]{SpectatorPose.Seated,SpectatorPose.Standing})
            {
                var mesh=Build(2,pose);string path=Root+(pose==SpectatorPose.Seated?"SpectatorLOD2":"StandingSpectatorLOD2")+".asset";
                var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);}
            }
            AssetDatabase.SaveAssets();
        }
        public static Mesh Build(int lod,SpectatorPose pose)
        {
            if(lod<0||lod>2)throw new System.ArgumentOutOfRangeException(nameof(lod));
            if(pose!=SpectatorPose.Seated&&pose!=SpectatorPose.Standing)throw new System.ArgumentOutOfRangeException(nameof(pose));
            bool standing=pose==SpectatorPose.Standing;
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var pivots=new List<Vector4>();var triangles=new List<int>();int sides=lod==0?10:6,rings=lod==0?7:4;
            var coat=new Color(.8f,.8f,.8f,1);var cloth=new Color(.07f,.085f,.11f,0);var boot=new Color(.035f,.043f,.052f,0);var skin=new Color(.64f,.41f,.29f,0);var scarf=new Color(.65f,.64f,.59f,0);
            float upperOffset=standing?.46f:0;
            if(lod==2)
            {
                Ellipsoid(new Vector3(0,.76f+upperOffset,0),new Vector3(.225f,.32f,.15f),coat);
                Ellipsoid(new Vector3(0,.45f+upperOffset,0),new Vector3(.22f,.15f,.16f),coat);
            }
            else
            {
                Coat();
                Ellipsoid(new Vector3(0,1.035f+upperOffset,.066f),new Vector3(.154f,.11f,.12f),coat*.89f);
            }
            Ellipsoid(new Vector3(0,1.09f+upperOffset,-.026f),new Vector3(.128f,.060f,.119f),scarf);
            Ellipsoid(new Vector3(0,1.235f+upperOffset,-.03f),new Vector3(.096f,.129f,.093f),skin);
            Ellipsoid(new Vector3(0,1.355f+upperOffset,-.011f),new Vector3(.105f,.080f,.10f),coat);
            if(lod==0)
            {
                Ellipsoid(new Vector3(0,1.315f+upperOffset,-.017f),new Vector3(.105f,.029f,.103f),cloth);
                Ellipsoid(new Vector3(0,1.232f+upperOffset,-.122f),new Vector3(.017f,.023f,.018f),skin);
                for(int side=-1;side<=1;side+=2)
                    Ellipsoid(new Vector3(side*.035f,1.263f+upperOffset,-.109f),new Vector3(.009f,.004f,.004f),boot);
            }
            for(int s=-1;s<=1;s+=2)
            {
                var hip=new Vector3(s*.11f,standing?.91f:.43f,0);var knee=new Vector3(s*(standing?.13f:.12f),standing?.49f:.34f,standing?-.025f:-.30f);var ankle=new Vector3(s*(standing?.14f:.12f),standing?.10f:.09f,standing?0:-.33f);
                Limb(hip,knee,.075f,cloth,Vector4.zero);Limb(knee,ankle,.061f,cloth,Vector4.zero);Ellipsoid(new Vector3(s*(standing?.14f:.12f),standing?.08f:.07f,standing?-.07f:-.41f),new Vector3(.077f,standing?.08f:.07f,.15f),boot);
                var shoulder=new Vector3(s*.183f,(standing?.99f:.955f)+upperOffset,0);var elbow=new Vector3(s*.245f,.70f+upperOffset,standing?-.083f:-.03f);
                var hand=new Vector3(s*.22f,(standing?(s<0?.855f:.75f):.70f)+upperOffset,standing?-.27f:-.245f);
                var pivot=new Vector4(shoulder.x,shoulder.y,shoulder.z,s<0?1:.72f);
                Limb(shoulder,elbow,.07f,coat,pivot);Limb(elbow,hand,.06f,coat,pivot);Ellipsoid(hand,new Vector3(.06f,.05f,.07f),boot,pivot);
            }
            var mesh=new Mesh{name=(standing?"StandingSpectatorLOD":"SpectatorLOD")+lod};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetColors(colors);mesh.SetUVs(0,pivots);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();return mesh;
            void Coat()
            {
                var seams=new[]{new Vector3(.425f,.192f,.137f),new Vector3(.53f,.208f,.151f),
                    new Vector3(.69f,.206f,.146f),new Vector3(.85f,.223f,.151f),
                    new Vector3(.97f,.211f,.123f),new Vector3(1.048f,.129f,.103f)};
                int count=lod==0?16:9;
                int start=vertices.Count;
                foreach(var seam in seams)
                for(int vertex=0;vertex<=count;vertex++)
                {
                    float angle=vertex*Mathf.PI*2/count;
                    var normal=new Vector3(Mathf.Cos(angle),.13f,Mathf.Sin(angle));
                    float puff=lod==0?1+.023f*Mathf.Cos(angle*5):1;
                    vertices.Add(new Vector3(normal.x*seam.y*puff,seam.x+upperOffset,normal.z*seam.z*puff));
                    normals.Add(normal.normalized);
                    colors.Add(vertex==count*3/4&&lod==0?new Color(.58f,.58f,.58f,1):coat);
                    pivots.Add(Vector4.zero);
                }
                for(int row=0;row<seams.Length-1;row++)
                for(int vertex=0;vertex<count;vertex++)
                {
                    int a=start+row*(count+1)+vertex,b=a+count+1;
                    triangles.Add(a);triangles.Add(b);triangles.Add(a+1);
                    triangles.Add(a+1);triangles.Add(b);triangles.Add(b+1);
                }
            }
            void Limb(Vector3 a,Vector3 b,float radius,Color color,Vector4 pivot)=>Ellipsoid((a+b)*.5f,new Vector3(radius,radius,(b-a).magnitude*.5f+radius*.35f),color,pivot,Quaternion.LookRotation(b-a));
            void Ellipsoid(Vector3 center,Vector3 extent,Color color,Vector4 pivot=default,Quaternion? orientation=null)
            {
                int start=vertices.Count;var q=orientation??Quaternion.identity;
                if(lod==2)
                {
                    // Original body proportions/colours, six vertices per part. Used only beyond the old disappearance radius.
                    foreach(var n in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
                    {vertices.Add(center+q*Vector3.Scale(n,extent));normals.Add(q*n);colors.Add(color);pivots.Add(pivot);}
                    foreach(int v in new[]{2,0,5,2,5,1,2,1,4,2,4,0,3,5,0,3,1,5,3,4,1,3,0,4})triangles.Add(start+v);
                    return;
                }
                for(int y=0;y<=rings;y++)for(int x=0;x<=sides;x++)
                {
                    float a=x*2*Mathf.PI/sides,b=y*Mathf.PI/rings;var n=new Vector3(Mathf.Sin(b)*Mathf.Cos(a),Mathf.Cos(b),Mathf.Sin(b)*Mathf.Sin(a));
                    vertices.Add(center+q*Vector3.Scale(n,extent));normals.Add(q*new Vector3(n.x/extent.x,n.y/extent.y,n.z/extent.z).normalized);colors.Add(color);pivots.Add(pivot);
                }
                for(int y=0;y<rings;y++)for(int x=0;x<sides;x++){int a=start+y*(sides+1)+x,b=a+sides+1;triangles.Add(a);triangles.Add(a+1);triangles.Add(b);triangles.Add(a+1);triangles.Add(b+1);triangles.Add(b);}
            }
        }
    }
}
#endif

#if UNITY_EDITOR
using System.IO;
using Biathlon.Domain;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Biathlon.Unity
{
    public static class GreyboxVenueAuthoring
    {
        public const string ScenePath="Assets/Scenes/GreyboxVenue.unity";
        private static Material Material(string id,Color color)
        {
            string path="Assets/Generated/VenueMaterials/"+id+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}m.color=color;EditorUtility.SetDirty(m);return m;
        }
        private static GameObject Box(string name,Vector3 position,Vector3 scale,Material material,Transform parent)
        {var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;Object.DestroyImmediate(go.GetComponent<Collider>());return go;}
        [MenuItem("BIATHLON/Generate Greybox Venue")]
        public static void Generate()
        {
            TrackGeometryAuthoring.Generate();Directory.CreateDirectory("Assets/Generated/VenueMaterials");Directory.CreateDirectory("Assets/Scenes");AssetDatabase.Refresh();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);var prior=SceneManager.GetActiveScene();SceneManager.SetActiveScene(scene);
            try
            {
                var root=new GameObject("Greybox venue");var snow=Material("snow",new Color(.78f,.85f,.9f));var trail=Material("trail",new Color(.35f,.52f,.6f));var range=Material("range",new Color(.15f,.44f,.52f));var penalty=Material("penalty",new Color(.9f,.48f,.17f));var dark=Material("structures",new Color(.08f,.14f,.2f));
                Box("Ground",new Vector3(20,-2,100),new Vector3(520,2,420),snow,root.transform);
                var track=TrackDefinition.FromJson(File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/synthetic_stadium_v1.json")));
                var visual=TrackVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/stadium_visual_v1").text,track);
                for(int i=0;i<visual.Count;i++)
                {
                    string id=visual.Get(i).LoopId;if(id=="main_short")continue;var go=new GameObject(id);go.transform.SetParent(root.transform,false);go.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Generated/TrackMeshes/"+visual.Id+"_"+id+".asset");go.AddComponent<MeshRenderer>().sharedMaterial=id=="penalty_150"?penalty:id.StartsWith("range_lane_")?range:trail;
                }
                Box("Stadium platform",new Vector3(-100,-.4f,50),new Vector3(110,.5f,70),snow,root.transform);
                for(int lane=1;lane<=30;lane++)
                {
                    float x=-100+(lane-15.5f)*3;Box("Target bank "+lane,new Vector3(x,1.2f,-30),new Vector3(2.4f,1.4f,.3f),dark,root.transform);
                    for(int target=0;target<5;target++)Box("Target",new Vector3(x- .8f+target*.4f,1.2f,-30.2f),new Vector3(.25f,.25f,.1f),snow,root.transform);
                }
                for(int i=0;i<track.GetLoop("main_long").SegmentCount;i++)
                {
                    var segment=track.GetLoop("main_long").GetSegment(i);var p=visual.Get("main_long").Sample(segment.StartDistance.Meters);
                    var marker=new GameObject(segment.SemanticZone.ToString());marker.transform.SetParent(root.transform,false);marker.transform.position=new Vector3((float)p.X,(float)p.Y+3,(float)p.Z);var label=marker.AddComponent<TextMesh>();label.text=segment.SemanticZone.ToString();label.characterSize=2;label.fontSize=40;label.color=Color.black;marker.transform.rotation=Quaternion.Euler(90,0,0);
                }
                var light=new GameObject("Sun").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(50,-30,0);RenderSettings.ambientLight=new Color(.55f,.6f,.65f);
                var camera=new GameObject("Venue overview").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(25,420,-220);camera.transform.LookAt(new Vector3(25,0,95));camera.orthographic=true;camera.orthographicSize=220;camera.farClipPlane=1500;camera.backgroundColor=new Color(.08f,.13f,.18f);camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();
                EditorSceneManager.SaveScene(scene,ScenePath);AssetDatabase.SaveAssets();Debug.Log("BIATHLON greybox venue generated: "+ScenePath);
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif

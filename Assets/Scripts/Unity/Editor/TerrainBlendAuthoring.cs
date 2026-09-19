#if UNITY_EDITOR
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
namespace Biathlon.Unity
{
    public static class TerrainBlendAuthoring
    {
        private const string Root="Assets/Resources/Biathlon/Terrain/";
        [MenuItem("BIATHLON/Generate Terrain Blend Benchmark")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();var definition=TerrainBlendDefinition.Load();var shader=Shader.Find("Biathlon/Snow");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.InvalidOperationException("Snow shader failed.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"WinterBlend.mat");if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"WinterBlend.mat");}definition.Configure(material);EditorUtility.SetDirty(material);
            var route=new[]{new Vector3(-2,0,-50),new Vector3(2,0,0),new Vector3(-2,0,50)};
            var mesh=TerrainBlendMesh.Build(route,3.5f,new Vector2(40,100),.25f,definition,p=>.01f*p.z+.025f*Mathf.Sin(p.x*1.3f+p.z*.4f),p=>Mathf.Exp(-((p.x-1)*(p.x-1)*.35f+(p.z-5)*(p.z-5)*.025f)));
            const string meshPath=Root+"BlendSurface.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(old==null){AssetDatabase.CreateAsset(mesh,meshPath);old=mesh;}else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);}
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var preview=new GameObject("Terrain blend validation").AddComponent<SnowMaterialPreview>();
                preview.Caption="WINTER SURFACE LAB\nLoose snow · packed trail · ice · churned edge";
                var ground=new GameObject("Single blended surface");ground.AddComponent<MeshFilter>().sharedMesh=old;ground.AddComponent<MeshRenderer>().sharedMaterial=material;
                var light=new GameObject("Grazing winter key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.8f;light.color=new Color(1,.96f,.9f);light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(24,-35,0);
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.42f,.52f);RenderSettings.ambientEquatorColor=new Color(.2f,.26f,.32f);RenderSettings.ambientGroundColor=new Color(.12f,.15f,.19f);
                var c=new GameObject("Terrain blend camera").AddComponent<Camera>();c.tag="MainCamera";c.transform.position=new Vector3(11,9,-15);c.transform.LookAt(new Vector3(1,0,4));c.fieldOfView=55;c.farClipPlane=180;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.55f,.62f,.69f);c.gameObject.AddComponent<AudioListener>();preview.View=c;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/TerrainBlendBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif

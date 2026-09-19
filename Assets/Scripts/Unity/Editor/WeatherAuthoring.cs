#if UNITY_EDITOR
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Biathlon.Unity
{
    public static class WeatherAuthoring
    {
        [MenuItem("BIATHLON/Generate Weather Benchmark")]
        public static void Generate()
        {
            const string root="Assets/Resources/Biathlon/Weather/";Directory.CreateDirectory(root);AssetDatabase.Refresh();var shader=Shader.Find("Biathlon/WinterSky");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.InvalidOperationException("Weather sky shader failed.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(root+"WinterSky.mat");if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,root+"WinterSky.mat");}
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var preview=new GameObject("Weather benchmark").AddComponent<WeatherPreview>();
                var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="Winter floor";ground.transform.localScale=new Vector3(18,1,20);Object.DestroyImmediate(ground.GetComponent<Collider>());ground.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Biathlon/Snow/cold");
                var light=new GameObject("Weather directional key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(28,-35,0);preview.Key=light;
                var c=new GameObject("Weather benchmark camera").AddComponent<Camera>();c.tag="MainCamera";c.transform.position=new Vector3(0,2,-48);c.transform.LookAt(new Vector3(0,3,0));c.fieldOfView=55;c.farClipPlane=250;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.57f,.64f,.71f);c.gameObject.AddComponent<AudioListener>();preview.View=c;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/WeatherBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif

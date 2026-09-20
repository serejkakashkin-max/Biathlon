#if UNITY_EDITOR
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Biathlon.Unity
{
    public static class LightingBenchmarkAuthoring
    {
        [MenuItem("BIATHLON/Generate Lighting Benchmark")]
        public static void Generate()
        {
            LightingVisualDefinition.Load();var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var sector=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Biathlon/Stadium/StadiumSector"));
                var preview=new GameObject("Lighting benchmark presentation").AddComponent<StadiumArtPreview>();preview.LightingPreset="clear_cold";
                var crowd=preview.gameObject.AddComponent<SpectatorCrowdPreview>();crowd.Scene=preview;crowd.Seating=sector.GetComponentInChildren<SpectatorSeating>();crowd.Animate=false;crowd.FrozenTime=12;
                var key=new GameObject("Visual sun").AddComponent<Light>();key.type=LightType.Directional;key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(25,-32,0);preview.Key=key;
                var camera=new GameObject("Lighting benchmark camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(-8,2,-18);camera.transform.LookAt(new Vector3(0,1,-8));camera.fieldOfView=55;camera.farClipPlane=250;camera.gameObject.AddComponent<AudioListener>();preview.View=camera;
                var athlete=new GameObject("Observed athlete lighting fixture").AddComponent<LightingAthletePreview>();athlete.transform.position=new Vector3(4,0,-8);athlete.View=camera;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/LightingBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif

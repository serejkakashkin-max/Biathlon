#if UNITY_EDITOR
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Biathlon.Unity
{
    public static class BroadcastBenchmarkAuthoring
    {
        [MenuItem("BIATHLON/Generate Broadcast Benchmark")]
        public static void Generate()
        {
            BroadcastShotDefinition.Load();var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var sector=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Biathlon/Stadium/StadiumSector"));
                var preview=new GameObject("Broadcast presentation").AddComponent<StadiumArtPreview>();preview.LightingPreset="clear_cold";
                var crowd=preview.gameObject.AddComponent<SpectatorCrowdPreview>();crowd.Scene=preview;crowd.Seating=sector.GetComponentInChildren<SpectatorSeating>();crowd.Animate=false;crowd.FrozenTime=12;
                var key=new GameObject("Visual sun").AddComponent<Light>();key.type=LightType.Directional;key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(25,-32,0);preview.Key=key;
                var camera=new GameObject("Dedicated broadcast camera").AddComponent<Camera>();camera.enabled=false;camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<ProjectionAwareLod>();preview.View=camera;
                var athlete=new GameObject("Observed athlete pose fixture").AddComponent<LightingAthletePreview>();athlete.transform.position=new Vector3(4,0,-8);athlete.View=camera;
                var subject=new GameObject("Explicit broadcast subject binding");subject.transform.SetParent(athlete.transform,false);subject.transform.localPosition=new Vector3(-4,0,0);
                var director=preview.gameObject.AddComponent<BroadcastBenchmarkPreview>();director.View=camera;director.Subject=subject.transform;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/BroadcastBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif

#if UNITY_EDITOR
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biathlon.Unity
{
    public static class AthleteAnimationPreviewAuthoring
    {
        public const string ScenePath = "Assets/Scenes/AthleteAnimationValidation.unity";
        [MenuItem("BIATHLON/Generate Athlete Animation Validation")]
        public static void Generate()
        {
            var prior = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(ScenePath);
            if (existing.IsValid() && existing.isLoaded)
            {
                if (existing.isDirty) throw new System.InvalidOperationException("Save the animation validation scene before regeneration.");
                // Unity cannot save a second scene over an open asset. Keep the open scene
                // and replace only its generated fixture contents, preserving its asset GUID.
                SceneManager.SetActiveScene(existing);
                foreach (var root in existing.GetRootGameObjects()) Object.DestroyImmediate(root);
                Populate(existing);
                SceneManager.SetActiveScene(prior);
                return;
            }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try { Populate(scene); }
            finally { SceneManager.SetActiveScene(prior); EditorSceneManager.CloseScene(scene, true); }
        }
        private static void Populate(Scene scene)
            {
                new GameObject("Locomotion validation").AddComponent<AthleteAnimationPreview>();
                var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Validation ground"; ground.transform.position = new Vector3(0, -.08f, 0); ground.transform.localScale = new Vector3(40, .1f, 20);
                ground.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/VenueMaterials/snow.mat");
                Object.DestroyImmediate(ground.GetComponent<Collider>());
                var light = new GameObject("Validation key").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f;
                light.transform.rotation = Quaternion.Euler(40, -35, 0); light.shadows = LightShadows.Soft;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(.45f, .5f, .55f);
                var camera = new GameObject("Validation camera").AddComponent<Camera>(); camera.tag = "MainCamera";
                camera.transform.position = new Vector3(2, 4, 12); camera.transform.LookAt(new Vector3(0, .7f, 0));
                camera.orthographic = true; camera.orthographicSize = 4.2f;
                camera.fieldOfView = 55; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.085f, .12f, .16f);
                camera.gameObject.AddComponent<AudioListener>();
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new System.InvalidOperationException("Could not save animation validation scene.");
            }
    }
}
#endif

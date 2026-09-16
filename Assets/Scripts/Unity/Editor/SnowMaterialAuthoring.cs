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
    public static class SnowMaterialAuthoring
    {
        public const string Root = "Assets/Resources/Biathlon/Snow/";
        [MenuItem("BIATHLON/Generate Snow Materials and Validation Scene")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            var definition = SnowMaterialDefinition.Load(); var shader = Shader.Find("Biathlon/Snow");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new System.InvalidOperationException("Snow shader must compile.");
            foreach (var preset in definition.presets)
            {
                var path = Root + preset.id + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, path); }
                definition.Configure(material, preset.id); material.enableInstancing = true; EditorUtility.SetDirty(material);
            }
            const string meshPath = Root + "ValidationSurface.asset";
            var surface = BuildSurface(); var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null) { AssetDatabase.CreateAsset(surface, meshPath); existing = surface; }
            else { EditorUtility.CopySerialized(surface, existing); Object.DestroyImmediate(surface); EditorUtility.SetDirty(existing); }
            var prior = SceneManager.GetActiveScene(); var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("Snow material fixture"); var preview = root.AddComponent<SnowMaterialPreview>();
                for (int i = 0; i < 2; i++)
                {
                    var preset = definition.presets[i]; float x = i == 0 ? -3.3f : 3.3f;
                    var material = AssetDatabase.LoadAssetAtPath<Material>(Root + preset.id + ".mat");
                    var ground = new GameObject(preset.id + " surface"); ground.transform.SetParent(root.transform,false); ground.transform.position = new Vector3(x,0,0);
                    ground.AddComponent<MeshFilter>().sharedMesh = existing; ground.AddComponent<MeshRenderer>().sharedMaterial = material;
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name = preset.id + " slope sample";
                    sphere.transform.SetParent(root.transform,false); sphere.transform.position = new Vector3(x, .6f, 2.5f); sphere.transform.localScale = new Vector3(2.8f,1.6f,2.8f);
                    sphere.GetComponent<Renderer>().sharedMaterial = material; Object.DestroyImmediate(sphere.GetComponent<Collider>());
                }
                var light = new GameObject("Winter directional key").AddComponent<Light>(); light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(28,-35,0); light.intensity = 1.8f; light.color = new Color(1,.96f,.9f); light.shadows = LightShadows.Soft;
                RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.28f,.36f,.46f);
                RenderSettings.ambientEquatorColor = new Color(.18f,.23f,.3f); RenderSettings.ambientGroundColor = new Color(.09f,.12f,.15f);
                var camera = new GameObject("Snow validation camera").AddComponent<Camera>(); camera.tag = "MainCamera";
                camera.transform.position = new Vector3(10,9,-14); camera.transform.LookAt(new Vector3(0,.2f,0)); camera.fieldOfView = 45;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.18f,.25f,.32f);
                camera.gameObject.AddComponent<AudioListener>(); preview.View = camera;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/SnowMaterialValidation.unity"); AssetDatabase.SaveAssets();
            }
            finally { SceneManager.SetActiveScene(prior); EditorSceneManager.CloseScene(scene,true); }
        }
        public static Mesh BuildSurface()
        {
            const int columns = 48, rows = 96;
            var vertices = new Vector3[(columns+1)*(rows+1)]; var indices = new int[columns*rows*6]; int k=0;
            for (int z=0;z<=rows;z++) for(int x=0;x<=columns;x++)
            {
                float px=(float)x/columns*6-3, pz=(float)z/rows*14-7;
                float h=.07f*Mathf.Sin(px*1.7f+pz*.6f)+.04f*Mathf.Sin(pz*3+px*.4f);
                h+=.65f*Mathf.Exp(-((px+1)*(px+1)+(pz-4)*(pz-4))*.4f);
                vertices[z*(columns+1)+x]=new Vector3(px,h,pz);
                if(x==columns||z==rows)continue; int a=z*(columns+1)+x,b=a+columns+1;
                indices[k++]=a;indices[k++]=b;indices[k++]=a+1;indices[k++]=a+1;indices[k++]=b;indices[k++]=b+1;
            }
            var mesh=new Mesh{name="SnowValidationSurface"}; mesh.vertices=vertices;mesh.triangles=indices;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
#endif

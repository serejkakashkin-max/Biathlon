#if UNITY_EDITOR
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Biathlon.Unity
{
    public static class VisualBenchmarkAuthoring
    {
        [MenuItem("BIATHLON/Generate Integrated Visual Benchmark")]
        public static void Generate()
        {
            const string root="Assets/Resources/Biathlon/Benchmark/";Directory.CreateDirectory(root);AssetDatabase.Refresh();
            var route=G2BenchmarkSurface.CopyRoute();
            var mesh=TerrainBlendMesh.Build(route,3.5f,new Vector2(280,300),1,TerrainBlendDefinition.Load(),G2BenchmarkSurface.Height);
            string path=root+"SectorSurface.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(mesh,path);old=mesh;}else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);}
            var shadowMesh=G2TerrainShadowMesh.Build(old,route);
            string shadowPath=root+"SectorSurfaceShadow.asset";var savedShadow=AssetDatabase.LoadAssetAtPath<Mesh>(shadowPath);
            if(savedShadow==null){AssetDatabase.CreateAsset(shadowMesh,shadowPath);savedShadow=shadowMesh;}else{EditorUtility.CopySerialized(shadowMesh,savedShadow);Object.DestroyImmediate(shadowMesh);EditorUtility.SetDirty(savedShadow);}
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var sector=(GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>("Biathlon/Stadium/StadiumSector"));
                // Authoring-only module lookup; no object name enters gameplay identity or simulation.
                var apron=sector.transform.Find("Apron");if(apron==null)throw new System.InvalidOperationException("Stadium apron module missing.");apron.gameObject.SetActive(false);
                var ground=new GameObject("Continuous winter surface");ground.AddComponent<MeshFilter>().sharedMesh=old;
                var snow=Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend");if(snow==null)throw new System.InvalidOperationException("Generate G2 snow derivatives first.");
                var visibleSurface=ground.AddComponent<MeshRenderer>();visibleSurface.sharedMaterial=snow;visibleSurface.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var shadowSurface=new GameObject("Terrain shadow geometry");shadowSurface.AddComponent<MeshFilter>().sharedMesh=savedShadow;
                var shadowRenderer=shadowSurface.AddComponent<MeshRenderer>();shadowRenderer.sharedMaterial=snow;shadowRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;shadowRenderer.receiveShadows=false;
                var preview=new GameObject("Integrated visual benchmark").AddComponent<VisualBenchmarkPresentation>();preview.Seating=sector.GetComponentInChildren<SpectatorSeating>();
                var key=new GameObject("Visual sun").AddComponent<Light>();key.type=LightType.Directional;key.shadows=LightShadows.Soft;key.transform.rotation=Quaternion.Euler(25,-32,0);preview.Key=key;
                var camera=new GameObject("Benchmark broadcast camera").AddComponent<Camera>();camera.enabled=false;camera.tag="MainCamera";camera.gameObject.AddComponent<AudioListener>();camera.gameObject.AddComponent<ProjectionAwareLod>();preview.View=camera;
                preview.Forests=new VegetationPresentation[3];var centers=new[]{new Vector3(0,0,122),new Vector3(-103,0,20),new Vector3(103,0,20)};
                for(int i=0;i<3;i++){var forest=new GameObject("Winter forest cluster").AddComponent<VegetationPresentation>();forest.transform.position=centers[i];preview.Forests[i]=forest;}
                preview.ForestEdge=new GameObject("Trail forest edge groves").AddComponent<VegetationPresentation>();
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/VisualBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
#endif

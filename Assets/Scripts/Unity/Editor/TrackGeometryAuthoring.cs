#if UNITY_EDITOR
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class TrackGeometryAuthoring
    {
        [MenuItem("BIATHLON/Generate Stadium Track Meshes")]
        public static void Generate()
        {
            var track=TrackDefinition.FromJson(File.ReadAllText(Path.Combine(Application.dataPath,"../tests/fixtures/tracks/synthetic_stadium_v1.json")));
            var visual=TrackVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/stadium_visual_v1").text,track);
            const string folder="Assets/Generated/TrackMeshes";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            for(int i=0;i<visual.Count;i++)
            {
                var mesh=TrackRibbonBuilder.Build(track,visual,visual.Get(i).LoopId);string path=folder+"/"+mesh.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
            }
            AssetDatabase.SaveAssets();Debug.Log("BIATHLON geometry generated: "+visual.Count+" loop meshes; "+visual.Id);
        }
    }
}
#endif

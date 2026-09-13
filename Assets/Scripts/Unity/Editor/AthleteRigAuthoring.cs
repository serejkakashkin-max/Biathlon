#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;

namespace Biathlon.Unity
{
    /// <summary>Persists only original authored geometry and material assets; requires no external downloads.</summary>
    public static class AthleteRigAuthoring
    {
        public const string AssetDirectory="Assets/Resources/Biathlon/Athletes";
        public const string PrefabPath=AssetDirectory+"/OriginalAthlete.prefab";

        [MenuItem("BIATHLON/Generate Original Development Athlete")]
        public static void Generate()
        {
            Directory.CreateDirectory(AssetDirectory+"/Meshes");Directory.CreateDirectory(AssetDirectory+"/Materials");AssetDatabase.Refresh();
            var rig=OriginalAthleteRigBuilder.CreateDevelopment(null);
            try
            {
                var persistedMeshes=new Dictionary<Mesh,Mesh>();var persistedMaterials=new Dictionary<Material,Material>();
                foreach(var filter in rig.GetComponentsInChildren<MeshFilter>(true))filter.sharedMesh=PersistMesh(filter.sharedMesh);
                foreach(var skin in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))skin.sharedMesh=PersistMesh(skin.sharedMesh);
                foreach(var renderer in rig.GetComponentsInChildren<Renderer>(true))
                {
                    var palette=renderer.sharedMaterials;
                    for(int i=0;i<palette.Length;i++)
                    {
                        var source=palette[i];
                        if(!persistedMaterials.TryGetValue(source,out var material))
                        {
                            string path=AssetDirectory+"/Materials/"+source.name+".mat";material=AssetDatabase.LoadAssetAtPath<Material>(path);
                            if(material==null){material=Object.Instantiate(source);material.name=source.name;AssetDatabase.CreateAsset(material,path);}
                            else{EditorUtility.CopySerialized(source,material);EditorUtility.SetDirty(material);}
                            persistedMaterials.Add(source,material);
                        }
                        palette[i]=material;
                    }
                    renderer.sharedMaterials=palette;
                }
                PrefabUtility.SaveAsPrefabAsset(rig.gameObject,PrefabPath);AssetDatabase.SaveAssets();
                Debug.Log("BIATHLON original athlete generated: "+PrefabPath+"; meshes="+persistedMeshes.Count+"; materials="+persistedMaterials.Count);

                Mesh PersistMesh(Mesh source)
                {
                    if(persistedMeshes.TryGetValue(source,out var saved))return saved;
                    string path=AssetDirectory+"/Meshes/"+source.name+".asset";saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(saved==null){saved=Object.Instantiate(source);saved.name=source.name;AssetDatabase.CreateAsset(saved,path);}
                    else{EditorUtility.CopySerialized(source,saved);EditorUtility.SetDirty(saved);}
                    persistedMeshes.Add(source,saved);return saved;
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}
#endif

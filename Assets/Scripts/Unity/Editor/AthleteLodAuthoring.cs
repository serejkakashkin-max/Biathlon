#if UNITY_EDITOR
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;

namespace Biathlon.Unity
{
    public static class AthleteLodAuthoring
    {
        [MenuItem("BIATHLON/Generate Original Athlete Distant Mesh")]
        public static void Generate()
        {
            const string path = "Assets/Resources/Biathlon/Athletes/Meshes/AthleteBodyDistant.asset";
            var source = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Resources/Biathlon/Athletes/Meshes/AthleteBody.asset");
            if (source == null) throw new System.InvalidOperationException("Generate the original athlete before its distant mesh.");
            var generated = Build(source, AthleteDetailSettings.Load().mesh_cell_m);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(generated, path);
            else { EditorUtility.CopySerialized(generated, existing); Object.DestroyImmediate(generated); EditorUtility.SetDirty(existing); }
            AssetDatabase.SaveAssets();
        }
        /// <summary>Original rigid-weight mesh clustering. Bone IDs are part of the key: no vertex
        /// crosses a joint. Degenerate faces are removed, material slots and bindposes retained.</summary>
        public static Mesh Build(Mesh source, float cell)
        {
            if (source == null || !float.IsFinite(cell) || cell <= 0) throw new System.ArgumentException();
            var positions = source.vertices; var normals = source.normals; var weights = source.boneWeights; var uv = source.uv;
            var map = new Dictionary<(int, int, int, int), int>();
            var vertices = new List<Vector3>(); var summedNormals = new List<Vector3>(); var texture = new List<Vector2>();
            var skin = new List<BoneWeight>(); var counts = new List<int>(); var remap = new int[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                if (weights[i].weight0 != 1) throw new System.ArgumentException("Distant builder expects the original rigid skin weights.");
                var p = positions[i];
                var key = (weights[i].boneIndex0, Mathf.RoundToInt(p.x / cell), Mathf.RoundToInt(p.y / cell), Mathf.RoundToInt(p.z / cell));
                if (!map.TryGetValue(key, out int target))
                {
                    target = vertices.Count; map.Add(key, target); vertices.Add(Vector3.zero); summedNormals.Add(Vector3.zero);
                    texture.Add(uv[i]); skin.Add(weights[i]); counts.Add(0);
                }
                remap[i] = target; vertices[target] += p; summedNormals[target] += normals[i]; counts[target]++;
            }
            for (int i = 0; i < vertices.Count; i++) { vertices[i] /= counts[i]; summedNormals[i] = summedNormals[i].normalized; }
            var mesh = new Mesh { name = "AthleteBodyDistant" };
            mesh.SetVertices(vertices); mesh.SetNormals(summedNormals); mesh.SetUVs(0, texture); mesh.boneWeights = skin.ToArray(); mesh.bindposes = source.bindposes;
            mesh.subMeshCount = source.subMeshCount;
            for (int sub = 0; sub < source.subMeshCount; sub++)
            {
                var indices = source.GetTriangles(sub); var reduced = new List<int>();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int a = remap[indices[i]], b = remap[indices[i + 1]], c = remap[indices[i + 2]];
                    if (a == b || a == c || b == c) continue;
                    reduced.Add(a); reduced.Add(b); reduced.Add(c);
                }
                mesh.SetTriangles(reduced, sub);
            }
            mesh.bounds = source.bounds;
            return mesh;
        }
    }
}
#endif

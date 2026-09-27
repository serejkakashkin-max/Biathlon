#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.Unity
{
    /// <summary>Controlled import of audited CC0 derivatives; never imports the source bundle.</summary>
    public static class G2HeroAuthoring
    {
        public const string DirectoryPath = "Assets/Resources/Biathlon/Athletes/G2Hero";
        public const string PrefabPath = DirectoryPath + "/HeroAthlete.prefab";
        // JsonUtility populates these audited intermediate channels.
#pragma warning disable CS0649
        [Serializable] private sealed class Indices { public int[] indices; }
        [Serializable] private sealed class MeshData
        {
            public int schema_version;
            public string id;
            public string[] materials;
            public float[] vertices, normals, uv, bone_weights;
            public int[] bone_indices;
            public Indices[] submeshes;
        }
#pragma warning restore CS0649

        public static void Generate()
        {
            Directory.CreateDirectory(DirectoryPath + "/Meshes");
            Directory.CreateDirectory(DirectoryPath + "/Materials");
            AssetDatabase.Refresh();
            foreach (string texture in new[] { "SuitNormal", "BibNormal", "SkinNormal", "SuitRoughness", "BibRoughness" })
            {
                string path = DirectoryPath + "/Textures/" + texture + ".png";
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidDataException("Generate audited material derivative: " + path);
                importer.textureType = texture.EndsWith("Normal",StringComparison.Ordinal) ? TextureImporterType.NormalMap : TextureImporterType.Default; importer.sRGBTexture = false;
                importer.maxTextureSize = 1024; importer.mipmapEnabled = true; importer.textureCompression = TextureImporterCompression.Compressed;
                importer.wrapMode = TextureWrapMode.Repeat; importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }
            var rig = OriginalAthleteRigBuilder.CreateDevelopment(null);
            try
            {
                // Use the retained original equipment assets, not transient procedural meshes.
                foreach (var filter in rig.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AthleteRigAuthoring.AssetDirectory + "/Meshes/" + filter.sharedMesh.name + ".asset");
                    if (mesh == null) throw new InvalidOperationException("Missing retained equipment mesh: " + filter.sharedMesh.name);
                    filter.sharedMesh = mesh;
                    var renderer = filter.GetComponent<MeshRenderer>(); var palette = renderer.sharedMaterials;
                    for (int i = 0; i < palette.Length; i++)
                    {
                        var material = AssetDatabase.LoadAssetAtPath<Material>(AthleteRigAuthoring.AssetDirectory + "/Materials/" + palette[i].name + ".mat");
                        if (material == null) throw new InvalidOperationException("Missing equipment material.");
                        palette[i] = material;
                    }
                    renderer.sharedMaterials = palette;
                }
                var skin = rig.GetComponentInChildren<SkinnedMeshRenderer>();
                var bones = new Transform[(int)AthleteBone.Count];
                var bind = new Matrix4x4[bones.Length];
                for (int i = 0; i < bones.Length; i++)
                { bones[i] = rig.Bone((AthleteBone)i); bind[i] = bones[i].worldToLocalMatrix * rig.transform.localToWorldMatrix; }
                MeshData data = null;
                var lodMeshes = new Mesh[3];
                for (int lod = 0; lod < 3; lod++)
                {
                    data = JsonUtility.FromJson<MeshData>(File.ReadAllText("artifacts/g2-hero/hero_lod" + lod + ".json"));
                    var mesh = Import(data, bind);
                    // Public mesh remains a CC0 derivative. The separately licensed rigid head
                    // replaces only this anatomical submesh; no restricted vertices enter it.
                    mesh.SetTriangles(Array.Empty<int>(),3);
                    mesh.SetTriangles(Array.Empty<int>(),6);
                    G2JointVolumeAuthoring.Apply(mesh);
                    mesh.name = "HeroLOD" + lod;
                    string path = DirectoryPath + "/Meshes/HeroLOD" + lod + ".asset";
                    var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (saved == null) { AssetDatabase.CreateAsset(mesh, path); saved = mesh; }
                    else { EditorUtility.CopySerialized(mesh, saved); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(saved); }
                    if (lod == 0) skin.sharedMesh = saved;
                    lodMeshes[lod] = saved;
                }
                var colors = new[] { new Color(.025f,.07f,.13f), new Color(.025f,.42f,.46f), new Color(.88f,.91f,.86f),
                    new Color(.58f,.34f,.23f), new Color(.025f,.032f,.040f), new Color(.075f,.095f,.115f), new Color(.018f,.045f,.075f), new Color(.12f,.38f,.43f) };
                var materials = new Material[data.materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    string path = DirectoryPath + "/Materials/" + data.materials[i] + ".mat";
                    var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (mat == null) { mat = new Material(Resources.Load<Material>("Biathlon/GreyboxLit")); AssetDatabase.CreateAsset(mat, path); }
                    mat.shader=Shader.Find(i==2?"Biathlon/AthleteBib":"Biathlon/AthleteSurface");
                    if(mat.shader==null)throw new InvalidDataException("Hero surface shader missing.");
                    mat.name = data.materials[i]; mat.color = colors[i]; mat.SetColor("_BaseColor", colors[i]);
                    if(i!=2){mat.SetFloat("_SurfaceKind",i==3?1:i==7?2:i==4||i==5?3:0);mat.SetFloat("_KitWeight",i==1||i==6?1:i==0?.12f:0);}
                    mat.SetFloat("_Smoothness", i == 7 ? .92f : i == 3 ? .28f : i == 5 ? .35f : .16f);
                    mat.SetFloat("_Metallic", i == 7 ? .7f : i == 5 ? .15f : 0); mat.enableInstancing = true;
                    if (i == 7) { mat.SetFloat("_Cull", 0); mat.doubleSidedGI = true; }
                    if (i <= 3 || i == 6)
                    {
                        string texture = i == 3 ? "SkinNormal" : i == 2 ? "BibNormal" : "SuitNormal";
                        mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DirectoryPath + "/Textures/" + texture + ".png"));
                        mat.SetFloat("_BumpScale", i == 3 ? .12f : .3f);
                        mat.SetFloat("_DetailTiling", i == 3 || i == 2 ? 6 : 16);
                        if(i!=3)mat.SetTexture("_RoughnessMap",AssetDatabase.LoadAssetAtPath<Texture2D>(DirectoryPath+"/Textures/"+(i==2?"BibRoughness":"SuitRoughness")+".png"));
                    }
                    if (i == 2)
                    {
                        mat.shader=Shader.Find("Biathlon/AthleteBib");
                        if (mat.shader==null) throw new InvalidDataException("Bib shader missing.");
                        mat.SetColor("_BaseColor",colors[i]); mat.SetColor("_InkColor",new Color(.015f,.045f,.07f));
                        mat.SetTexture("_Atlas",AssetDatabase.LoadAssetAtPath<Texture2D>(DirectoryPath+"/Textures/BibAtlas.png"));
                        mat.SetFloat("_BibNumber",1);
                    }
                    EditorUtility.SetDirty(mat); materials[i] = mat;
                }
                skin.sharedMaterials = materials; skin.bones = bones; skin.quality = SkinQuality.Bone4;
                G2HeroFaceAuthoring.Apply(rig);
                rig.gameObject.AddComponent<AthleteSkinDetailBinding>().Configure(skin, lodMeshes, 12, 45, 2);
                rig.gameObject.AddComponent<AthleteBibPresentation>().Configure(skin,2);
                rig.gameObject.AddComponent<AthleteGripPresentation>();
                rig.gameObject.AddComponent<AthleteAppearancePresentation>();
                rig.gameObject.AddComponent<AthleteJointVolume>();
                G2RifleAuthoring.Apply(rig);
                skin.name = "Continuous athlete body"; rig.name = "G2 benchmark hero athlete";
                PrefabUtility.SaveAsPrefabAsset(rig.gameObject, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }

        private static Mesh Import(MeshData data, Matrix4x4[] bind)
        {
            if (data == null || data.schema_version != 1 || string.IsNullOrWhiteSpace(data.id) || data.vertices == null || data.vertices.Length % 3 != 0)
                throw new InvalidDataException("Invalid hero derivative schema.");
            int count = data.vertices.Length / 3;
            if (count == 0 || count > 60000 || data.normals?.Length != count * 3 || data.uv?.Length != count * 2 ||
                data.bone_indices?.Length != count * 4 || data.bone_weights?.Length != count * 4 || data.submeshes?.Length != data.materials?.Length)
                throw new InvalidDataException("Incomplete hero mesh channels.");
            var vertices = new Vector3[count]; var normals = new Vector3[count]; var uv = new Vector2[count]; var weights = new BoneWeight[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(data.vertices[i*3], data.vertices[i*3+1], data.vertices[i*3+2]);
                normals[i] = new Vector3(data.normals[i*3], data.normals[i*3+1], data.normals[i*3+2]);
                uv[i] = new Vector2(data.uv[i*2], data.uv[i*2+1]); float sum = 0;
                for (int j = 0; j < 4; j++)
                {
                    int at = i*4+j; float value = data.bone_weights[at];
                    if (data.bone_indices[at] < 0 || data.bone_indices[at] >= bind.Length || !float.IsFinite(value) || value < 0)
                        throw new InvalidDataException("Invalid skin weight.");
                    sum += value;
                }
                if (Mathf.Abs(sum-1) > .0001f || !float.IsFinite(vertices[i].sqrMagnitude) || !float.IsFinite(normals[i].sqrMagnitude))
                    throw new InvalidDataException("Nonfinite vertex or non-normalized weights.");
                weights[i] = new BoneWeight { boneIndex0=data.bone_indices[i*4], boneIndex1=data.bone_indices[i*4+1], boneIndex2=data.bone_indices[i*4+2], boneIndex3=data.bone_indices[i*4+3],
                    weight0=data.bone_weights[i*4], weight1=data.bone_weights[i*4+1], weight2=data.bone_weights[i*4+2], weight3=data.bone_weights[i*4+3] };
            }
            var mesh = new Mesh { name = data.id, vertices = vertices, normals = normals, uv = uv, boneWeights = weights, bindposes = bind, subMeshCount = data.submeshes.Length };
            for (int sub = 0; sub < data.submeshes.Length; sub++)
            {
                var indices = data.submeshes[sub].indices;
                if (indices == null || indices.Length % 3 != 0) throw new InvalidDataException("Incomplete triangles.");
                foreach (int i in indices) if (i < 0 || i >= count) throw new InvalidDataException("Triangle outside vertex buffer.");
                mesh.SetTriangles(indices, sub);
            }
            mesh.SetUVs(1,vertices); // Bind-space material coordinates, preserved by every LOD.
            mesh.RecalculateBounds(); mesh.RecalculateTangents(); return mesh;
        }
    }
}
#endif

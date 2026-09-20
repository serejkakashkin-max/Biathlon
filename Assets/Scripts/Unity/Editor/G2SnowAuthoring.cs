#if UNITY_EDITOR
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class G2SnowAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/Terrain/G2Snow";
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            foreach(string name in new[]{"SnowAlbedo","SnowNormal","SnowRoughness"})
            {
                string path=Root+"/Textures/"+name+".png";var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer==null)throw new InvalidDataException("Generate selected CC0 snow texture "+name);
                importer.textureType=name=="SnowNormal"?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=name=="SnowAlbedo";
                importer.maxTextureSize=name=="SnowRoughness"?1024:2048;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;
                importer.wrapMode=TextureWrapMode.Repeat;importer.textureCompression=TextureImporterCompression.Compressed;importer.SaveAndReimport();
            }
            var shader=Shader.Find("Biathlon/Snow");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidDataException("Snow shader errors.");
            string materialPath=Root+"/WinterBlend.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
            TerrainBlendDefinition.Load().Configure(material);var settings=G2BenchmarkSurface.Load();material.name="WinterBlend";
            material.SetTexture("_DetailAlbedo",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SnowAlbedo.png"));
            material.SetTexture("_DetailNormal",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SnowNormal.png"));
            material.SetTexture("_DetailRoughness",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/SnowRoughness.png"));
            material.SetFloat("_DetailMeters",settings.snow_detail_m);material.SetFloat("_DetailWeight",settings.snow_detail_weight);material.SetFloat("_DetailNormalStrength",settings.snow_normal_strength);
            EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();VisualBenchmarkAuthoring.Generate();
        }
    }
}
#endif

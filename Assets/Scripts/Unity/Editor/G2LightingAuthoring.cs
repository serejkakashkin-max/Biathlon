#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class G2LightingAuthoring
    {
        public static void Generate()
        {
            const string root="Assets/Resources/Biathlon/Weather/G2Lighting/";
            AssetDatabase.Refresh();var importer=AssetImporter.GetAtPath(root+"PassendorfSky.hdr") as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Run scripts/art/Build-G2Lighting.py first.");
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=false;importer.maxTextureSize=2048;importer.mipmapEnabled=true;importer.wrapModeU=TextureWrapMode.Repeat;importer.wrapModeV=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var shader=Shader.Find("Biathlon/BenchmarkSky");if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Benchmark sky shader failed.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(root+"BenchmarkSky.mat");if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,root+"BenchmarkSky.mat");}
            material.shader=shader;material.SetTexture("_Panorama",AssetDatabase.LoadAssetAtPath<Texture2D>(root+"PassendorfSky.hdr"));EditorUtility.SetDirty(material);AssetDatabase.SaveAssets();
        }
    }
}
#endif

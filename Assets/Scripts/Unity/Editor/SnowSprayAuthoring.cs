#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class SnowSprayAuthoring
    {
        [MenuItem("BIATHLON/Generate Snow Spray Material")]
        public static void Generate()
        {
            var shader=Shader.Find("Biathlon/SnowSpray");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.InvalidOperationException("Snow spray shader must compile.");
            const string path="Assets/Resources/Biathlon/Snow/Spray.mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path)==null)AssetDatabase.CreateAsset(new Material(shader),path);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

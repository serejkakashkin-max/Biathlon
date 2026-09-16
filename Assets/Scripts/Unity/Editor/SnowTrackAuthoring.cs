#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class SnowTrackAuthoring
    {
        [MenuItem("BIATHLON/Generate Snow Contact Material")]
        public static void Generate()
        {
            var shader=Shader.Find("Biathlon/SnowContact");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.InvalidOperationException("Snow contact shader must compile.");
            const string path="Assets/Resources/Biathlon/Snow/ContactMarks.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,path);}
            AssetDatabase.SaveAssets();
        }
    }
}
#endif

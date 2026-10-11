#if UNITY_EDITOR
using System;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Biathlon.Unity
{
    public static class FrontEndAuthoring
    {
        public const string ScenePath="Assets/Scenes/FrontEnd.unity";
        [MenuItem("BIATHLON/UI/Generate Front End Scene")]
        public static void Generate()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle Edit Mode required.");
            FrontEndTheme.Load();
            string guid=AssetDatabase.AssetPathToGUID("Assets/Scripts/Unity/Presentation/FrontEndMenu.cs");
            if(string.IsNullOrEmpty(guid))throw new InvalidOperationException("Imported front-end script required.");
            // Match the production scene generator: import a minimal scene and
            // validate it in a preview, preserving every open/untitled scene.
            string yaml="%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"+
                "--- !u!1 &1\nGameObject:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  serializedVersion: 6\n  m_Component:\n  - component: {fileID: 2}\n  - component: {fileID: 3}\n  m_Layer: 0\n  m_Name: Biathlon front end\n  m_TagString: Untagged\n  m_IsActive: 1\n"+
                "--- !u!4 &2\nTransform:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 1}\n  serializedVersion: 2\n  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}\n  m_LocalPosition: {x: 0, y: 0, z: 0}\n  m_LocalScale: {x: 1, y: 1, z: 1}\n  m_Children: []\n  m_Father: {fileID: 0}\n"+
                "--- !u!114 &3\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 1}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: "+guid+", type: 3}\n  m_Name:\n  m_EditorClassIdentifier:\n  AutoStart: 1\n  PersistPreferences: 1\n"+
                "--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 2}\n";
            File.WriteAllText(ScenePath,yaml,new System.Text.UTF8Encoding(false));AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceSynchronousImport);
            var scene=EditorSceneManager.OpenPreviewScene(ScenePath);
            try
            {
                var roots=scene.GetRootGameObjects();var menu=roots.Length==1?roots[0].GetComponent<FrontEndMenu>():null;
                if(menu==null||!menu.AutoStart||!menu.PersistPreferences)throw new IOException("Imported scene lost its front-end binding.");
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
#endif

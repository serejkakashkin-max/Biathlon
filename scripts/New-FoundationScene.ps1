$ErrorActionPreference = 'Stop'
$code = @'
using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Biathlon.Unity.Presentation;
public static class CreateFoundationScene
{
    public static string Run()
    {
        const string path = "Assets/Scenes/Foundation.unity";
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save dirty scenes before creating/opening Foundation.");
        if (System.IO.File.Exists(path))
        {
            EditorSceneManager.OpenScene(path);
            return "Opened existing Foundation scene.";
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("Foundation Bootstrap").AddComponent<FoundationBootstrap>();
        var camera = new GameObject("Debug Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.09f, 0.12f);
        EditorSceneManager.SaveScene(scene, path);
        return "Created and saved " + path;
    }
}
'@
& (Join-Path $PSScriptRoot 'Invoke-UnityMcp.ps1') -Tool script-execute -Arguments @{ csharpCode=$code; className='CreateFoundationScene'; methodName='Run' }

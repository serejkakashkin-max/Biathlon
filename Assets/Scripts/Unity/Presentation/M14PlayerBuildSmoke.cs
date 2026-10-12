using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit standalone startup probe; absent unless launched with -biathlonM14Smoke.</summary>
    public sealed class M14PlayerBuildSmoke:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void StartIfRequested()
        {
            if(Application.isEditor||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonM14Smoke")<0)return;
            new GameObject("M14 startup probe").AddComponent<M14PlayerBuildSmoke>();
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;
            for(int i=0;i<30;i++)yield return null;
            var menu=FindAnyObjectByType<FrontEndMenu>();var evidence=new Evidence{unity=Application.unityVersion,profile_directory=Path.Combine(Application.persistentDataPath,"Profiles"),replay_directory=Path.Combine(Application.persistentDataPath,"Replays")};
            if(menu!=null&&menu.Root!=null)
            {
                evidence.width=menu.Root.worldBound.width;evidence.height=menu.Root.worldBound.height;
                evidence.success=menu.Root.panel!=null&&evidence.width>500&&menu.Root.Q<Button>("new_career")!=null&&menu.Root.Q<Button>("settings")!=null&&menu.Replays!=null;
            }
            string directory=Path.GetFullPath(Path.Combine(Application.dataPath,".."));
            if(evidence.success)
            {
                var panel=menu.GetComponent<UIDocument>().panelSettings;var target=new RenderTexture(Screen.width,Screen.height,0);target.Create();panel.targetTexture=target;
                for(int i=0;i<8;i++)yield return null;
                var previous=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
                for(int y=0;y<target.height;y+=32)for(int x=0;x<target.width;x+=32){var c=pixels.GetPixel(x,y);if(c.r+c.g+c.b>.1f)evidence.visible_samples++;}
                File.WriteAllBytes(Path.Combine(directory,"startup-menu.png"),pixels.EncodeToPNG());
                RenderTexture.active=previous;panel.targetTexture=null;Destroy(pixels);Destroy(target);evidence.success=evidence.visible_samples>10;
            }
            File.WriteAllText(Path.Combine(directory,"startup-smoke.json"),JsonUtility.ToJson(evidence,true));
            Application.Quit(evidence.success?0:1);
        }
        [Serializable]private sealed class Evidence
        {public int schema_version=1;public string id="m14_standalone_startup",unity,profile_directory,replay_directory;public float width,height;public int visible_samples;public bool success;}
    }
}

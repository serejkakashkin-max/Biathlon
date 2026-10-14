using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Use the desktop display on ordinary Windows launches, including
    /// a direct EXE launch after Unity saved a narrow window. Explicit QA window
    /// arguments remain available; no per-frame display work or gameplay changes.</summary>
    public static class StandaloneDisplayStartup
    {
        public static bool UseDesktopFullscreen(string[] args)
        {
            for(int i=0;i<args.Length;i++)
            {
                if(args[i]=="-batchmode"||args[i]=="-nographics")return false;
                if(args[i]=="-screen-fullscreen"&&i+1<args.Length&&args[i+1]=="0")return false;
                if(args[i]=="-window-mode"&&i+1<args.Length&&args[i+1]=="exclusive")return false;
            }
            return true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if(Application.isEditor||Application.platform!=RuntimePlatform.WindowsPlayer||
                !UseDesktopFullscreen(Environment.GetCommandLineArgs()))return;
            var display=Display.main;
            int width=display.systemWidth,height=display.systemHeight;
            if(width<=0||height<=0)return;
            Screen.SetResolution(width,height,FullScreenMode.FullScreenWindow);
        }
    }
}

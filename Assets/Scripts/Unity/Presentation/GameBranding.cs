using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Public game title. Unity's legacy product identifier remains stable for save/settings paths.</summary>
    public static class GameBranding
    {
        public const string EnglishTitle="BIATHLON: PULSE";
        public const string RussianTitle="Биатлон: Пульс гонки";
        public static string WindowTitle {get;private set;}
        public static void ApplyWindowTitle(string title)
        {
            if(title!=EnglishTitle&&title!=RussianTitle)throw new ArgumentException("Official game title required.");
            if(WindowTitle==title)return;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            // Changing productName would move Unity's existing profile and settings directory.
            // Change the visible window caption without changing that storage identity.
            using(var process=System.Diagnostics.Process.GetCurrentProcess())
            {
                // MainWindowHandle excludes hidden windows, including automated review launches.
                IntPtr window=IntPtr.Zero;uint ownerId=(uint)process.Id;
                EnumWindows((candidate,unused)=>
                {
                    GetWindowThreadProcessId(candidate,out uint candidateId);
                    if(candidateId!=ownerId)return true;
                    var name=new System.Text.StringBuilder(128);GetClassName(candidate,name,name.Capacity);
                    if(name.ToString()!="UnityWndClass")return true;
                    window=candidate;return false;
                },IntPtr.Zero);
                if(window==IntPtr.Zero||!SetWindowText(window,title))return;
            }
#endif
            WindowTitle=title;
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private delegate bool WindowCallback(IntPtr window,IntPtr parameter);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool EnumWindows(WindowCallback callback,IntPtr parameter);
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window,out uint processId);
        [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int GetClassName(IntPtr window,System.Text.StringBuilder name,int size);
        [System.Runtime.InteropServices.DllImport("user32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode,SetLastError=true)]
        private static extern bool SetWindowText(IntPtr window,string title);
#endif
    }
}

#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
namespace Biathlon.Unity
{
    public static class M15AudioAuthoring
    {
        public const string Folder="Assets/Resources/Biathlon/Audio/";
        public static void EnsureSoundBank()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Audio authoring requires Edit Mode.");
            var definition=BiathlonAudioDefinition.Load();
            if(AssetDatabase.LoadAssetAtPath<AudioMixer>(Folder+"Biathlon.mixer")==null)CreateMixer();
            foreach(var cue in definition.cues)foreach(string clip in cue.clips)
            {
                string path=Folder+clip+".wav";if(!File.Exists(path))throw new FileNotFoundException("Generate the original audio sources first.",path);
                var importer=AssetImporter.GetAtPath(path) as AudioImporter;if(importer==null)throw new InvalidOperationException("Import sound bank before authoring.");
                var settings=importer.defaultSampleSettings;
                if(settings.loadType==AudioClipLoadType.DecompressOnLoad&&settings.compressionFormat==AudioCompressionFormat.PCM&&settings.preloadAudioData&&importer.forceToMono&&!importer.loadInBackground)continue;
                settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                settings.sampleRateSetting=AudioSampleRateSetting.PreserveSampleRate;settings.preloadAudioData=true;
                importer.defaultSampleSettings=settings;importer.forceToMono=true;importer.loadInBackground=false;importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
        }
        private static void CreateMixer()
        {
            // Editor-only bridge: Unity exposes native mixer authoring through this Editor type.
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Audio.AudioMixerController")).FirstOrDefault(t=>t!=null);
            if(type==null)throw new NotSupportedException("Unity audio mixer authoring unavailable.");
            const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
            var mixer=(AudioMixer)type.GetMethod("CreateMixerControllerAtPath",flags).Invoke(null,new object[]{Folder+"Biathlon.mixer"});
            var master=type.GetProperty("masterGroup",flags).GetValue(mixer);var groupType=master.GetType();var groups=Array.CreateInstance(groupType,5);
            for(int i=0;i<5;i++)groups.SetValue(type.GetMethod("CreateNewGroup",flags).Invoke(mixer,new object[]{((AudioBus)i).ToString(),true}),i);
            groupType.GetProperty("children").SetValue(master,groups);EditorUtility.SetDirty((UnityEngine.Object)master);EditorUtility.SetDirty(mixer);AssetDatabase.SaveAssets();
        }
    }
}
#endif

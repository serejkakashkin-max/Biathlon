using System;
using System.IO;
using Biathlon.Presentation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>One product preference boundary; legacy keys remain read-compatible with the approved HUD.</summary>
    public static class PlayerSettingsPreferences
    {
        private static InputBindingCatalog catalog;
        private static PlayerSettings current;
        private static FilePlayerSettingsStore store;
        public static InputBindingCatalog Catalog=>catalog??(catalog=InputBindingCatalog.FromJson(ProductionVenueHost.ReadResource("Biathlon/input_bindings_v1")));
        public static PlayerSettingsLoadStatus LoadStatus {get;private set;}
        public static string LastSaveError {get;private set;}
        public static PlayerSettings Current {get{EnsureLoaded();return current;}}
        private static void EnsureLoaded()
        {
            if(current!=null)return;
            store=new FilePlayerSettingsStore(Path.Combine(Application.persistentDataPath,"Settings"),Catalog);
            var loaded=store.Load(ReadLegacy());current=loaded.Settings;LoadStatus=loaded.Status;
            // Only a missing record is migrated automatically. Invalid original bytes remain available for recovery.
            if(LoadStatus==PlayerSettingsLoadStatus.New)
            {
                try{store.Save(current);LoadStatus=PlayerSettingsLoadStatus.Loaded;}
                catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){LastSaveError=e.GetType().Name;LoadStatus=PlayerSettingsLoadStatus.Unavailable;}
            }
            Mirror(current);
        }
        private static PlayerSettings ReadLegacy()
        {
            string locale=PlayerPrefs.GetString("biathlon.ui.locale","ru"),quality=PlayerPrefs.GetString("biathlon.visual.quality",RenderQualityDefinition.Load().default_tier);
            BiathlonAudioPreferences audio=null;
            try{audio=JsonUtility.FromJson<BiathlonAudioPreferences>(PlayerPrefs.GetString("biathlon.audio.v1",""));}catch(ArgumentException){}
            if(audio==null||!audio.Valid())audio=new BiathlonAudioPreferences();
            var p=new PlayerSettings{Locale=locale=="en"?"en":"ru",TextSize=Mathf.Clamp(PlayerPrefs.GetInt("biathlon.ui.text_size",0),0,2),
                HighContrast=PlayerPrefs.GetInt("biathlon.ui.high_contrast",0)==1,ReducedMotion=PlayerPrefs.GetInt("biathlon.ui.reduced_motion",0)==1,
                Quality=quality=="low"||quality=="high"||quality=="balanced"?quality:RenderQualityDefinition.Load().default_tier};
            CopyAudio(audio,p);return p;
        }
        public static void Save(PlayerSettings value)
        {
            EnsureLoaded();value.Validate(Catalog);
            try{store.Save(value);current=value.Clone(Catalog);Mirror(current);PlayerPrefs.Save();LastSaveError=null;LoadStatus=PlayerSettingsLoadStatus.Loaded;}
            catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){LastSaveError=e.GetType().Name;throw;}
        }
        private static void Mirror(PlayerSettings p)
        {
            PlayerPrefs.SetString("biathlon.ui.locale",p.Locale);PlayerPrefs.SetInt("biathlon.ui.text_size",p.TextSize);
            PlayerPrefs.SetInt("biathlon.ui.high_contrast",p.HighContrast?1:0);PlayerPrefs.SetInt("biathlon.ui.reduced_motion",p.ReducedMotion?1:0);
            PlayerPrefs.SetString("biathlon.visual.quality",p.Quality);PlayerPrefs.SetString("biathlon.audio.v1",JsonUtility.ToJson(Audio(p)));
        }
        public static BiathlonAudioPreferences Audio(PlayerSettings p)=>new BiathlonAudioPreferences{master=p.Master/1000f,effects=p.Effects/1000f,body=p.Body/1000f,
            ambience=p.Ambience/1000f,ui=p.Interface/1000f,music=p.Music/1000f,quiet=p.Quiet,mute_unfocused=p.MuteUnfocused};
        public static void CopyAudio(BiathlonAudioPreferences p,PlayerSettings target)
        {if(p==null||!p.Valid())throw new ArgumentException("Invalid audio settings.");target.Master=Mathf.RoundToInt(p.master*1000);target.Effects=Mathf.RoundToInt(p.effects*1000);target.Body=Mathf.RoundToInt(p.body*1000);
            target.Ambience=Mathf.RoundToInt(p.ambience*1000);target.Interface=Mathf.RoundToInt(p.ui*1000);target.Music=Mathf.RoundToInt(p.music*1000);target.Quiet=p.quiet;target.MuteUnfocused=p.mute_unfocused;}
        public static void SaveAudio(BiathlonAudioPreferences audio){var p=Current.Clone(Catalog);CopyAudio(audio,p);Save(p);}
    }
}

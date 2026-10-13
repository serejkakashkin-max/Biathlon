using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public static class RenderQualityPreferences
    {
        private const string Key="biathlon.visual.quality";
        public static string Read(string fallback)
        {var id=PlayerPrefs.GetString(Key,fallback);return id=="low"||id=="balanced"||id=="high"?id:fallback;}
        public static void Save(string id)
        {RenderQualityDefinition.DisplayName(id);PlayerPrefs.SetString(Key,id);PlayerPrefs.Save();}
    }
}

using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public static class RenderQualityPreferences
    {
        public static string Read(string fallback)
        {var id=PlayerSettingsPreferences.Current.Quality;return id=="low"||id=="balanced"||id=="high"?id:fallback;}
        public static void Save(string id)
        {RenderQualityDefinition.DisplayName(id);var next=PlayerSettingsPreferences.Current.Clone(PlayerSettingsPreferences.Catalog);next.Quality=id;PlayerSettingsPreferences.Save(next);}
    }
}

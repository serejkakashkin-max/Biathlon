using System;
using Biathlon.Career;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public static class AthleteLookCatalog
    {
        private static AppearanceDefinition cached;
        public static AppearanceDefinition Load()
        {
            if(cached!=null)return cached;
            var asset=Resources.Load<TextAsset>("Biathlon/athlete_creator_appearance_v1");
            if(asset==null)throw new ArgumentException("Missing athlete cosmetic policy.");
            return cached=AppearanceDefinition.FromJson(asset.text);
        }
        public static Color Color(string group,string id)
        {
            var d=Load();var option=d.Option(group,d.Index(group,id));
            if(!ColorUtility.TryParseHtmlString(option.Color,out var color))throw new ArgumentException("Appearance option has no color.");
            return color;
        }
    }
}

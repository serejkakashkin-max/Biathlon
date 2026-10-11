using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class FrontEndTheme
    {
        public int schema_version,reference_width,reference_height,body_size,title_size,section_size,caption_size,control_height,spacing,page_padding;
        public string id,snow,paper,ink,muted,accent,focus,danger;
        public static Color Color(string hex)
        { if(!ColorUtility.TryParseHtmlString(hex,out var c))throw new ArgumentException("Invalid UI color.");return c; }
        public static FrontEndTheme Load()
        {
            var value=JsonUtility.FromJson<FrontEndTheme>(ProductionVenueHost.ReadResource("Biathlon/UI/front_end_theme_v1"));
            if(value.schema_version!=1||string.IsNullOrWhiteSpace(value.id)||value.reference_width<800||value.reference_height<600||value.body_size<16||value.caption_size<14||value.title_size<value.section_size||value.section_size<value.body_size||value.control_height<value.body_size*2||value.spacing<0||value.page_padding<0)throw new ArgumentException("Invalid UI theme.");
            foreach(string hex in new[]{value.snow,value.paper,value.ink,value.muted,value.accent,value.focus,value.danger})Color(hex);
            return value;
        }
    }
}

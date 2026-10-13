using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Shared presentation-only FROSTLINE materials and typography. No authority state.</summary>
    public static class FrostlineStyle
    {
        public static readonly Color Ink=FrontEndTheme.Color("#E8F5F8"), Muted=FrontEndTheme.Color("#B0C4D0"),
            Surface=FrontEndTheme.Color("#132A3B"), Deep=FrontEndTheme.Color("#0B1928"),
            Accent=FrontEndTheme.Color("#85D5DF"), Gold=FrontEndTheme.Color("#D6AA69"), Danger=FrontEndTheme.Color("#F3988D");
        private static Font display,body;
        private static StyleSheet sheet;
        public static Font DisplayFont=>display??(display=Resources.Load<Font>("Biathlon/UI/Fonts/Oswald"));
        public static Font BodyFont=>body??(body=Resources.Load<Font>("Biathlon/UI/Fonts/Roboto"));
        public static void Text(VisualElement element,bool display=false)
        {element.style.unityFont=display?DisplayFont:BodyFont;element.style.unityFontStyleAndWeight=FontStyle.Normal;}
        public static void Apply(VisualElement root,bool reducedMotion=false)
        {
            if(sheet==null)sheet=Resources.Load<StyleSheet>("Biathlon/UI/frostline");
            if(sheet!=null&&!root.styleSheets.Contains(sheet))root.styleSheets.Add(sheet);
            root.AddToClassList("frostline");root.EnableInClassList("reduced-motion",reducedMotion);Text(root);
        }
        public static void SurfaceCard(VisualElement element,bool hero=false)
        {
            element.AddToClassList(hero?"fl-hero":"fl-surface");
            element.style.backgroundColor=hero?Deep:Surface;
            element.style.borderTopLeftRadius=element.style.borderTopRightRadius=element.style.borderBottomLeftRadius=element.style.borderBottomRightRadius=2;
            element.style.borderTopWidth=1;element.style.borderTopColor=new Color(.52f,.84f,.87f,.16f);
        }
    }
}

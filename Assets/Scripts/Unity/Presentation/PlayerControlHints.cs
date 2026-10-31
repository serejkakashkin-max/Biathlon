using System;
using System.Text;
using Biathlon.Presentation;

namespace Biathlon.Unity.Presentation
{
    public static class PlayerControlHints
    {
        public static string Label(PlayerSettings p,string action)
        {var catalog=PlayerSettingsPreferences.Catalog;return catalog.Path(p.Binding(catalog,action,p.Device)).Label;}
        public static string Description(PlayerSettings p,string locale,string[] actions)
        {
            var catalog=PlayerSettingsPreferences.Catalog;var b=new StringBuilder();
            for(int i=0;i<actions.Length;i++)
            {if(i>0)b.Append(i%2==0?'\n':'·').Append(' ');b.Append(Label(p,actions[i])).Append(" — ").Append(catalog.Action(actions[i]).Title(locale));}
            return b.ToString();
        }
        public static string PreRace(PlayerSettings p,string locale,string original)
        {
            if(p.Device=="keyboard"&&p.BindingOverrideCount==0)return original;
            return Description(p,locale,new[]{"pace_up","pace_down","steer_left","steer_right","attack","recovery","tuck","breath","bolt","shoot"})+"\nEsc / Start — "+(locale=="ru"?"пауза":"pause");
        }
        public static string Gameplay(PlayerSettings p,string locale,bool shooting,string original)
        {
            if(p.Device=="keyboard"&&p.BindingOverrideCount==0)return original;
            return Description(p,locale,shooting?new[]{"fine_aim","range_overview","breath","bolt","shoot"}:new[]{"pace_up","pace_down","attack","recovery","steer_left","steer_right","tuck","splits"});
        }
    }
}

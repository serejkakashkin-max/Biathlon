using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Shared national kits, bound to saved nationality rather than bib number. Presentation only.</summary>
    public sealed class AthleteAppearancePresentation : MonoBehaviour
    {
        [Serializable] private sealed class Palette { public int schema_version;public string id;public string[] nations,kits,trims; }
        private static Color[] kits,trims;
        private static string[] nations;
        private static Material[][] variants;
        private SkinnedMeshRenderer skin;private int current=-1;
        public string Nation {get;private set;}
        public static int NationCount {get{LoadPalette();return nations.Length;}}
        public static string NationAt(int index){LoadPalette();return nations[index];}
        public static Color NationColor(string nation){LoadPalette();return kits[NationIndex(nation)];}
        public void Apply(int ordinal)
        {
            if(ordinal<1||ordinal>60)throw new ArgumentOutOfRangeException(nameof(ordinal));
            LoadPalette();
            // Renumbering a registered athlete must not repaint their national kit.
            if(Nation!=null)return;
            ApplyVariant((ordinal-1)%(kits.Length-1));
        }
        public void ApplyNation(string nation)
        {
            if(string.IsNullOrWhiteSpace(nation))throw new ArgumentException("A saved nationality is required.",nameof(nation));
            LoadPalette();ApplyVariant(NationIndex(nation));Nation=nation;
        }
        private static int NationIndex(string nation)
        {for(int i=0;i<nations.Length;i++)if(nations[i]==nation)return i;return kits.Length-1;}
        private static void LoadPalette()
        {
            if(kits==null)
            {
                var p=JsonUtility.FromJson<Palette>(Resources.Load<TextAsset>("Biathlon/athlete_appearance_v1").text);
                if(p==null||p.schema_version!=1||string.IsNullOrWhiteSpace(p.id)||p.kits==null||p.nations==null||p.nations.Length<2||p.kits.Length!=p.nations.Length+1||p.trims?.Length!=p.kits.Length)throw new ArgumentException("Invalid national kit palette.");
                var k=new Color[p.kits.Length];var t=new Color[k.Length];
                for(int i=0;i<k.Length;i++)if(!ColorUtility.TryParseHtmlString(p.kits[i],out k[i])||!ColorUtility.TryParseHtmlString(p.trims[i],out t[i]))throw new ArgumentException("Invalid kit color.");
                for(int i=0;i<p.nations.Length;i++)
                {if(string.IsNullOrWhiteSpace(p.nations[i]))throw new ArgumentException("Missing kit nationality.");for(int j=0;j<i;j++)if(p.nations[i]==p.nations[j]||k[i]==k[j])throw new ArgumentException("National kits must be distinct.");}
                nations=p.nations;kits=k;trims=t;
            }
        }
        private void ApplyVariant(int variant)
        {
            if(current==variant)return;
            if(skin==null)skin=GetComponentInChildren<SkinnedMeshRenderer>();
            // Shared, bounded palette keeps SRP batching; per-renderer surface blocks disable it.
            // The independent bib slot and its number block remain untouched.
            if(variants==null||variants[0][0]==null)
            {
                var source=skin.sharedMaterials;variants=new Material[kits.Length][];
                for(int v=0;v<variants.Length;v++)
                {
                    variants[v]=(Material[])source.Clone();
                    for(int slot=0;slot<source.Length;slot++)
                    {
                        if(slot==2||slot==4||slot==5||slot==7)continue;
                        var mat=new Material(source[slot]){name=source[slot].name+" kit "+v,hideFlags=HideFlags.DontSave};
                        mat.SetColor("_KitColor",kits[v]);mat.SetColor("_TrimColor",trims[v]);mat.SetFloat("_SkinVariation",.25f+.5f*v/(kits.Length-1));variants[v][slot]=mat;
                    }
                }
            }
            skin.sharedMaterials=variants[variant];
            current=variant;
        }
    }
}

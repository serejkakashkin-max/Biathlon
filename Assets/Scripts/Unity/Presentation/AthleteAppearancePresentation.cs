using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Fictional kit variants keyed by the existing visual roster ordinal. No gameplay identity or RNG.</summary>
    public sealed class AthleteAppearancePresentation : MonoBehaviour
    {
        [Serializable] private sealed class Palette { public int schema_version;public string id;public string[] kits,trims; }
        private static Color[] kits,trims;
        private static Material[][] variants;
        private SkinnedMeshRenderer skin;private int current;
        public void Apply(int ordinal)
        {
            if(ordinal<1||ordinal>60)throw new ArgumentOutOfRangeException(nameof(ordinal));
            if(current==ordinal)return;
            if(kits==null)
            {
                var p=JsonUtility.FromJson<Palette>(Resources.Load<TextAsset>("Biathlon/athlete_appearance_v1").text);
                if(p==null||p.schema_version!=1||string.IsNullOrWhiteSpace(p.id)||p.kits==null||p.kits.Length<2||p.trims?.Length!=p.kits.Length)throw new ArgumentException("Invalid visual kit palette.");
                var k=new Color[p.kits.Length];var t=new Color[k.Length];
                for(int i=0;i<k.Length;i++)if(!ColorUtility.TryParseHtmlString(p.kits[i],out k[i])||!ColorUtility.TryParseHtmlString(p.trims[i],out t[i]))throw new ArgumentException("Invalid kit color.");
                kits=k;trims=t;
            }
            if(skin==null)skin=GetComponentInChildren<SkinnedMeshRenderer>();
            int variant=(ordinal-1)%kits.Length;
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
            current=ordinal;
        }
    }
}

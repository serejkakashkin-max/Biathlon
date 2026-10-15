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
        private static Material[] skinVariants;
        private SkinnedMeshRenderer skin;private int current=-1,currentSkin=-1;
        private Material[] palette;
        private string identity;
        public string Nation {get;private set;}
        public int CosmeticVariant {get;private set;}
        public int FaceVariant {get;private set;}
        public int SkinVariant {get;private set;}
        public void ApplyIdentity(string stableId)
        {
            uint hash=AthleteFaceCatalog.Hash(stableId);
            if(identity==stableId)return;
            var faces=AthleteFaceCatalog.Load();faces.Resolve(stableId,out int face,out int tone);
            FaceVariant=face;SkinVariant=tone;identity=stableId;
            CosmeticVariant=(int)(hash%3);
            if(skin==null)skin=GetComponentInChildren<SkinnedMeshRenderer>();
            for(int i=0;i<3;i++)
            {int index=skin.sharedMesh.GetBlendShapeIndex("OriginalBuild"+i);if(index>=0)skin.SetBlendShapeWeight(index,i==CosmeticVariant?100:0);}
            for(int i=0;i<faces.profiles.Length;i++)
            {int index=skin.sharedMesh.GetBlendShapeIndex(faces.profiles[i].ShapeName);if(index>=0)skin.SetBlendShapeWeight(index,i==FaceVariant?100:0);}
            LoadPalette();ApplyVariant(current<0?0:current);
        }
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
            if(current==variant&&currentSkin==SkinVariant)return;
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
                        if(slot==2||slot==3||slot==4||slot==5||slot==7)continue;
                        var mat=new Material(source[slot]){name=source[slot].name+" kit "+v,hideFlags=HideFlags.DontSave};
                        mat.SetColor("_KitColor",kits[v]);mat.SetColor("_TrimColor",trims[v]);variants[v][slot]=mat;
                    }
                }
            }
            if(skinVariants==null||skinVariants[0]==null)
            {
                var faces=AthleteFaceCatalog.Load();skinVariants=new Material[faces.skin_colors.Length];
                var source=variants[0][3];
                for(int i=0;i<skinVariants.Length;i++)
                {
                    var mat=new Material(source){name=source.name+" tone "+i,hideFlags=HideFlags.DontSave};
                    mat.SetColor("_BaseColor",faces.SkinColor(i));mat.SetFloat("_SkinVariation",.5f);skinVariants[i]=mat;
                }
            }
            if(palette==null)palette=new Material[variants[variant].Length];
            Array.Copy(variants[variant],palette,palette.Length);palette[3]=skinVariants[SkinVariant];
            skin.sharedMaterials=palette;
            current=variant;currentSkin=SkinVariant;
        }
    }
}

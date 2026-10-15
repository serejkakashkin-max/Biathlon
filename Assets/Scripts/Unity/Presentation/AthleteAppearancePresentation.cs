using System;
using System.Collections.Generic;
using Biathlon.Career;
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
        private AppearanceProfile profile;
        private AppearanceProfile details,currentDetails;
        private bool trainingKit,currentTraining;
        private static readonly Dictionary<string,Material> cosmeticMaterials=new Dictionary<string,Material>(StringComparer.Ordinal);
        public float VisualScale {get;private set;}=1;
        public AppearanceProfile AppliedProfile=>profile;
        public void ShowTrainingKit(bool show)
        {if(trainingKit==show)return;trainingKit=show;if(kits!=null)ApplyVariant(current<0?0:current);}
        public void ApplyProfile(AppearanceProfile appearance)
        {
            if(appearance==null)throw new ArgumentNullException(nameof(appearance));
            if(ReferenceEquals(profile,appearance))return;
            appearance.Validate();
            if(appearance.HasDetails)AthleteLookCatalog.Load().ValidateProfile(appearance);
            if(appearance.IsLegacy)ApplyIdentity(appearance.LegacyIdentity);
            else
            {
                var recipe=AthleteFaceCatalog.Load();ApplyCosmeticSelection(appearance.FaceId,recipe.SkinIndex(appearance.SkinId),recipe.BodyIndex(appearance.BodyId));
                if(appearance.HasDetails)
                {
                    details=appearance;VisualScale=appearance.HeightMillimeters/(float)AthleteLookCatalog.Load().BaseHeightMillimeters;
                    GetComponent<AthleteRigBinding>().ModelRoot.localScale=Vector3.one*VisualScale;
                    ApplyDetailMorphs();ApplyVariant(current<0?0:current);
                }
            }
            profile=appearance;
        }
        public void ApplyIdentity(string stableId)
        {
            uint hash=AthleteFaceCatalog.Hash(stableId);
            if(identity==stableId)return;
            var faces=AthleteFaceCatalog.Load();faces.Resolve(stableId,out int face,out int tone);
            FaceVariant=face;SkinVariant=tone;identity=stableId;
            profile=null;ResetDetails();
            CosmeticVariant=(int)(hash%3);
            ApplyMorphs(faces);
            LoadPalette();ApplyVariant(current<0?0:current);
        }
        public void ApplyCosmeticSelection(string faceProfileId,int skinPaletteIndex,int buildIndex)
        {
            var faces=AthleteFaceCatalog.Load();int selected=-1;
            for(int i=0;i<faces.profiles.Length;i++)if(faces.profiles[i].id==faceProfileId)selected=i;
            if(selected<0||skinPaletteIndex<0||skinPaletteIndex>=faces.skin_colors.Length||buildIndex<0||buildIndex>2)
                throw new ArgumentException("Unsupported cosmetic selection.");
            FaceVariant=selected;SkinVariant=skinPaletteIndex;CosmeticVariant=buildIndex;identity=null;
            profile=null;ResetDetails();
            ApplyMorphs(faces);LoadPalette();ApplyVariant(current<0?0:current);
        }
        private void ApplyMorphs(AthleteFaceCatalog faces)
        {
            if(skin==null)skin=GetComponentInChildren<SkinnedMeshRenderer>();
            for(int i=0;i<3;i++)
            {int index=skin.sharedMesh.GetBlendShapeIndex("OriginalBuild"+i);if(index>=0)skin.SetBlendShapeWeight(index,i==CosmeticVariant?100:0);}
            for(int i=0;i<faces.profiles.Length;i++)
            {int index=skin.sharedMesh.GetBlendShapeIndex(faces.profiles[i].ShapeName);if(index>=0)skin.SetBlendShapeWeight(index,i==FaceVariant?100:0);}
            ApplyDetailMorphs();
        }
        private void ResetDetails()
        {details=null;VisualScale=1;var rig=GetComponent<AthleteRigBinding>();if(rig!=null)rig.ModelRoot.localScale=Vector3.one;}
        private void ApplyDetailMorphs()
        {
            if(skin==null)skin=GetComponentInChildren<SkinnedMeshRenderer>();
            float centre=AthleteLookCatalog.Load().DefaultShoulderWidthBasis;
            float shoulder=details==null?0:(details.ShoulderWidthBasis-centre)/(details.ShoulderWidthBasis<centre?centre:10000-centre);
            Shape("CreatorShouldersNarrow",Mathf.Max(0,-shoulder)*100);Shape("CreatorShouldersWide",Mathf.Max(0,shoulder)*100);
            Shape("CreatorHairCrew",details!=null&&details.HeadwearId!="cap"?100:0);
            Shape("CreatorHairShort",details!=null&&details.HeadwearId!="cap"&&details.HairStyleId=="short_swept"?100:0);
            Shape("CreatorGogglesRaised",details!=null&&details.GogglesId=="raised"?100:0);
            void Shape(string name,float weight){int i=skin.sharedMesh.GetBlendShapeIndex(name);if(i>=0)skin.SetBlendShapeWeight(i,weight);}
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
            if(current==variant&&currentSkin==SkinVariant&&ReferenceEquals(currentDetails,details)&&currentTraining==trainingKit)return;
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
            if(details!=null)
            {
                var d=details;
                palette[3]=Cosmetic("skin_"+SkinVariant+"_"+d.EyeColorId+"_"+d.HairColorId,palette[3],m=>{m.SetColor("_EyeColor",AthleteLookCatalog.Color("eye_color",d.EyeColorId));m.SetColor("_HairColor",AthleteLookCatalog.Color("hair_color",d.HairColorId));});
                palette[4]=Cosmetic("hands_"+d.GlovesId+"_"+d.BootsId,palette[4],m=>{m.SetColor("_GloveColor",AthleteLookCatalog.Color("gloves",d.GlovesId));m.SetColor("_BootColor",AthleteLookCatalog.Color("boots",d.BootsId));m.SetFloat("_CosmeticOverride",1);});
                palette[5]=Cosmetic("boots_"+d.BootsId,palette[5],m=>{m.SetColor("_BootColor",AthleteLookCatalog.Color("boots",d.BootsId));m.SetFloat("_CosmeticOverride",1);});
                palette[6]=Cosmetic("cap_"+variant+"_"+SkinVariant+"_"+d.HeadwearId+"_"+d.HairColorId+"_"+d.HairStyleId,palette[6],m=>{m.SetColor("_HairColor",AthleteLookCatalog.Color("hair_color",d.HairColorId));m.SetColor("_ScalpColor",AthleteFaceCatalog.Load().SkinColor(SkinVariant));m.SetFloat("_HeadwearMode",d.HeadwearId=="headband"?1:d.HeadwearId=="none"?2:0);});
                palette[7]=Cosmetic("lens_"+d.LensId,palette[7],m=>{m.SetColor("_LensColor",AthleteLookCatalog.Color("lens",d.LensId));m.SetFloat("_CosmeticOverride",1);});
                if(trainingKit&&d.TrainingAccentId!="national")for(int slot=0;slot<2;slot++)
                {int s=slot;palette[s]=Cosmetic("training_"+variant+"_"+s+"_"+d.TrainingAccentId,palette[s],m=>{m.SetColor("_TrimColor",AthleteLookCatalog.Color("training_accent",d.TrainingAccentId));if(s==1)m.SetColor("_KitColor",AthleteLookCatalog.Color("training_accent",d.TrainingAccentId));});}
            }
            skin.sharedMaterials=palette;
            current=variant;currentSkin=SkinVariant;currentDetails=details;currentTraining=trainingKit;
        }
        private static Material Cosmetic(string key,Material source,Action<Material> configure)
        {
            if(cosmeticMaterials.TryGetValue(key,out var material)&&material!=null)return material;
            material=new Material(source){name=source.name+" "+key,hideFlags=HideFlags.DontSave};configure(material);cosmeticMaterials[key]=material;return material;
        }
    }
}

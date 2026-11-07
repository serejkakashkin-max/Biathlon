using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Shared authored national textiles for the isolated hero; no career authority.</summary>
    public sealed class Biathlete167Outfit:MonoBehaviour
    {
        [SerializeField] private string[] nations;
        [SerializeField] private Material[] suits,caps,accents;
        [SerializeField] private SkinnedMeshRenderer[] skins;
        [SerializeField] private string nation;
        private Material[][] working;
        private byte[][] slots;
        public string Nation=>nation;
        public event Action<string> NationChanged;
        public Material HeadwearMaterial=>caps[Array.IndexOf(nations,nation)];
        public IReadOnlyList<string> Nations=>Array.AsReadOnly(nations);
        public void Configure(string[] codes,Material[] suitVariants,Material[] capVariants,Material[] accentVariants,SkinnedMeshRenderer[] renderers)
        {
            if(codes==null||codes.Length==0||suitVariants?.Length!=codes.Length||capVariants?.Length!=codes.Length||accentVariants?.Length!=codes.Length||renderers==null||renderers.Length!=3)throw new ArgumentException("Complete national outfit variants and all three body LODs required.");
            nations=codes;suits=suitVariants;caps=capVariants;accents=accentVariants;skins=renderers;working=null;ApplyNation(codes[0]);
        }
        private void Awake(){if(nations!=null&&nations.Length>0)ApplyNation(string.IsNullOrEmpty(nation)?nations[0]:nation);}
        public void ApplyNation(string code)
        {
            int index=Array.IndexOf(nations,code);if(index<0)throw new ArgumentException("Unsupported hero outfit nationality: "+code,nameof(code));
            if(working==null)
            {
                working=new Material[skins.Length][];slots=new byte[skins.Length][];
                for(int i=0;i<skins.Length;i++)
                {
                    working[i]=skins[i].sharedMaterials;slots[i]=new byte[working[i].Length];
                    for(int j=0;j<working[i].Length;j++)slots[i][j]=working[i][j].name switch{"PULSE_Natural_Tailored_Textile"=>1,"PULSE_Race_Red"=>2,"PULSE_Scarlet_Accent"=>3,_=>0};
                }
            }
            for(int i=0;i<skins.Length;i++)
            {
                for(int j=0;j<working[i].Length;j++)switch(slots[i][j]){case 1:working[i][j]=suits[index];break;case 2:working[i][j]=caps[index];break;case 3:working[i][j]=accents[index];break;}
                skins[i].sharedMaterials=working[i];
            }
            nation=code;
            NationChanged?.Invoke(code);
        }
    }
}

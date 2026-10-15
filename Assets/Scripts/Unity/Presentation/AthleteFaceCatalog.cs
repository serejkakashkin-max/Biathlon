using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class AthleteFaceProfile
    {
        public string id;
        public float jaw_width_m, cheek_width_m, chin_length_m, chin_depth_m, nose_width_m, nose_depth_m;
        public string ShapeName => "FaceIdentity_" + id;
    }

    /// <summary>Cosmetic recipe only. Stable identity, independent of nationality, race number and simulation RNG.</summary>
    [Serializable]
    public sealed class AthleteFaceCatalog
    {
        public int schema_version;
        public string id;
        public string[] skin_colors;
        public AthleteFaceProfile[] profiles;
        [NonSerialized] private Color[] colors;
        private static AthleteFaceCatalog cached;
        public static AthleteFaceCatalog Load()
        {
            if(cached!=null)return cached;
            var asset=Resources.Load<TextAsset>("Biathlon/athlete_faces_v1");
            var value=asset==null?null:JsonUtility.FromJson<AthleteFaceCatalog>(asset.text);
            if(value==null)throw new ArgumentException("Missing face recipe.");
            value.Validate();cached=value;return value;
        }
        public void Validate()
        {
            if(schema_version!=1||string.IsNullOrWhiteSpace(id)||skin_colors==null||skin_colors.Length<4||skin_colors.Length>16||profiles==null||profiles.Length<3||profiles.Length>12)
                throw new ArgumentException("Invalid face recipe.");
            colors=new Color[skin_colors.Length];
            for(int i=0;i<colors.Length;i++)
            {
                if(!ColorUtility.TryParseHtmlString(skin_colors[i],out colors[i])||colors[i].a!=1)throw new ArgumentException("Invalid skin color.");
                for(int j=0;j<i;j++)if(colors[i]==colors[j])throw new ArgumentException("Duplicate skin color.");
            }
            for(int i=0;i<profiles.Length;i++)
            {
                var p=profiles[i];
                if(p==null||string.IsNullOrWhiteSpace(p.id))throw new ArgumentException("Missing face profile.");
                for(int j=0;j<i;j++)if(p.id==profiles[j].id)throw new ArgumentException("Duplicate face profile.");
                foreach(float v in new[]{p.jaw_width_m,p.cheek_width_m,p.chin_length_m,p.chin_depth_m,p.nose_width_m,p.nose_depth_m})
                    if(!float.IsFinite(v)||Mathf.Abs(v)>.015f)throw new ArgumentException("Unbounded face shape.");
            }
        }
        public Color SkinColor(int index)=>colors[index];
        public void Resolve(string stableId,out int face,out int skin)
        {
            uint hash=Hash(stableId);
            face=(int)(Mix(hash^0x8da6b343u)%(uint)profiles.Length);
            skin=(int)(Mix(hash^0xd8163841u)%(uint)skin_colors.Length);
        }
        public static uint Hash(string stableId)
        {
            if(string.IsNullOrWhiteSpace(stableId))throw new ArgumentException("Stable cosmetic identity required.");
            uint hash=2166136261;unchecked{foreach(char c in stableId)hash=(hash^c)*16777619;}return hash;
        }
        private static uint Mix(uint value)
        {unchecked{value^=value>>16;value*=0x7feb352du;value^=value>>15;value*=0x846ca68bu;return value^(value>>16);}}
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>World text needs a depth-tested font atlas; the builtin GUI font material draws through roofs.</summary>
    [RequireComponent(typeof(TextMesh))]
    public sealed class VenueSignText : MonoBehaviour
    {
        private sealed class Lease{public Material Material;public int Owners;}
        private static readonly Dictionary<Font,Lease> leases=new Dictionary<Font,Lease>();
        private Font font;private Renderer target;private Material prior;private bool acquired;
        public static int ActiveFontCount=>leases.Count;
        private void OnEnable()
        {
            if(!Application.isPlaying)return;var text=GetComponent<TextMesh>();font=text.font;target=GetComponent<Renderer>();if(font==null||target==null)throw new InvalidOperationException("Bound world font required.");
            if(!leases.TryGetValue(font,out var lease))
            {
                var template=Resources.Load<Material>("Biathlon/Stadium/SignText");if(template==null)throw new InvalidOperationException("Generate depth-tested sign material.");
                lease=new Lease{Material=new Material(template)};lease.Material.mainTexture=font.material.mainTexture;if(leases.Count==0)Font.textureRebuilt+=AtlasRebuilt;leases.Add(font,lease);
            }
            prior=target.sharedMaterial;lease.Owners++;target.sharedMaterial=lease.Material;acquired=true;
        }
        private static void AtlasRebuilt(Font value){if(leases.TryGetValue(value,out var lease))lease.Material.mainTexture=value.material.mainTexture;}
        private void OnDisable()
        {
            if(!acquired)return;acquired=false;if(target!=null)target.sharedMaterial=prior;
            if(leases.TryGetValue(font,out var lease)&&--lease.Owners==0){leases.Remove(font);Destroy(lease.Material);}if(leases.Count==0)Font.textureRebuilt-=AtlasRebuilt;
        }
    }
}

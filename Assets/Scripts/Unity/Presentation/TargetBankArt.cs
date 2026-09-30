using System;
using Biathlon.Domain;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Decorative bank, in metres; local Z=0 is the authoritative target plane.
    /// A caller provides detached stance/closure values. This component never resolves a shot.</summary>
    public sealed class TargetBankArt : MonoBehaviour
    {
        [SerializeField] private Renderer[] apertures;
        [SerializeField] private Material openMaterial,closedMaterial;
        private Material proneMaterial;
        private MaterialPropertyBlock apertureProperties;
        private static readonly int ScoringRadius=Shader.PropertyToID("_ScoringRadius");
        public Renderer Aperture(int index)=>apertures[index];
        public void Bind(Renderer[] renderers,Material open,Material closed)
        {
            if(renderers==null||renderers.Length!=TargetGeometryDefinition.TargetCount||open==null||closed==null)throw new ArgumentException("Five bound target faces and materials required.");
            for(int i=0;i<renderers.Length;i++){if(renderers[i]==null||!renderers[i].transform.IsChildOf(transform))throw new ArgumentException("Foreign target face.");for(int j=0;j<i;j++)if(renderers[j]==renderers[i])throw new ArgumentException("Duplicate target face.");}
            apertures=(Renderer[])renderers.Clone();openMaterial=open;closedMaterial=closed;
        }
        public void Apply(TargetGeometryDefinition geometry,ShootingStance stance,int closedMask)
        {
            if(geometry==null||apertures==null||apertures.Length!=5||closedMask<0||closedMask>31)throw new ArgumentException("Valid geometry, bound bank and five-bit closure mask required.");
            // The aiming mark stays 115 mm in both stances; scoring remains 45/115 mm in simulation.
            float scoringRadius=(float)geometry.Radius(stance);
            float diameter=(float)(2*geometry.Radius(ShootingStance.Standing));
            if(proneMaterial==null)
            {
                proneMaterial=Resources.Load<Material>("Biathlon/Stadium/ProneTargetFace");
                if(proneMaterial==null)throw new InvalidOperationException("Generate the prone target face material first.");
                apertureProperties=new MaterialPropertyBlock();
            }
            apertureProperties.SetFloat(ScoringRadius,scoringRadius/diameter);
            for(int i=0;i<apertures.Length;i++)
            {
                var face=apertures[i];face.transform.localPosition=new Vector3((float)geometry.CenterX(i),0,0);
                face.transform.localRotation=Quaternion.identity;face.transform.localScale=new Vector3(diameter,diameter,1);
                bool closed=(closedMask&(1<<i))!=0;
                bool showInner=stance==ShootingStance.Prone&&!closed;
                face.sharedMaterial=closed?closedMaterial:showInner?proneMaterial:openMaterial;
                face.SetPropertyBlock(showInner?apertureProperties:null);
            }
        }
    }
}

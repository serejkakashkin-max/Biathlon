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
        private TargetImpactMarks impactMarks;
        private ShootingStance lastStance=ShootingStance.Standing;
        private int lastMask;
        public TargetImpactMarks ImpactMarks=>impactMarks;
        public void ConfigureImpacts(TargetGeometryDefinition geometry,ShotImpactVisualDefinition settings)
        {
            if(impactMarks!=null)return;
            Bounds plate=default;float nearest=float.PositiveInfinity;
            // Derive the actual front plate from its authored mesh, excluding the bound faces.
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                bool face=false;foreach(var aperture in apertures)if(filter.gameObject==aperture.gameObject){face=true;break;}if(face)continue;
                var mesh=filter.sharedMesh;if(mesh==null)continue;
                var local=mesh.bounds;var matrix=transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;
                var bounds=new Bounds(matrix.MultiplyPoint3x4(local.center),Vector3.zero);
                for(int c=0;c<8;c++)bounds.Encapsulate(matrix.MultiplyPoint3x4(local.center+Vector3.Scale(local.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1))));
                if(bounds.min.z<=0||bounds.min.z>=nearest||bounds.min.x>geometry.CenterX(0)||bounds.max.x<geometry.CenterX(4)||bounds.min.y>=0||bounds.max.y<=0)continue;
                plate=bounds;nearest=bounds.min.z;
            }
            if(!float.IsFinite(nearest))throw new InvalidOperationException("Target bank needs its authored front plate.");
            var go=new GameObject("Accepted front-plate impacts");go.transform.SetParent(transform,false);impactMarks=go.AddComponent<TargetImpactMarks>();impactMarks.Initialize(geometry,settings,plate);
            impactMarks.SetFace(lastStance,lastMask);
        }
        public bool RecordImpact(in Biathlon.Presentation.ShotImpactEvidence evidence)=>impactMarks!=null&&impactMarks.Add(evidence);
        public void ClearImpacts(){if(impactMarks!=null)impactMarks.ClearMarks();}
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
            lastStance=stance;lastMask=closedMask;
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
            impactMarks?.SetFace(stance,closedMask);
        }
    }
}

using System;
using Biathlon.Domain;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>One preallocated bank mesh. Only front-plate strikes/scuffs are visible;
    /// shots through an aperture do not drill the white closure flap.</summary>
    public sealed class TargetImpactMarks : MonoBehaviour
    {
        private Mesh mesh;private MeshRenderer view;private Vector3[] vertices;
        private MaterialPropertyBlock properties;
        private ShotImpactVisualDefinition definition;private TargetGeometryDefinition geometry;
        private Bounds plate;private int next;
        public int Count {get;private set;}
        public Mesh MarkMesh=>mesh;
        public Renderer MarkRenderer=>view;
        public void Initialize(TargetGeometryDefinition targets,ShotImpactVisualDefinition settings,Bounds frontPlate)
        {
            if(mesh!=null||targets==null||settings==null)throw new ArgumentException("Fresh marks and bound geometry required.");
            definition=settings;geometry=targets;plate=frontPlate;
            vertices=new Vector3[settings.marks_per_bank*4];var uv=new Vector2[vertices.Length];var indices=new int[settings.marks_per_bank*6];
            for(int i=0;i<settings.marks_per_bank;i++)
            {int v=i*4,t=i*6;uv[v]=new Vector2(-1,-1);uv[v+1]=new Vector2(-1,1);uv[v+2]=new Vector2(1,1);uv[v+3]=new Vector2(1,-1);indices[t]=v;indices[t+1]=v+1;indices[t+2]=v+2;indices[t+3]=v;indices[t+4]=v+2;indices[t+5]=v+3;}
            mesh=new Mesh{name="Bounded accepted target impact marks"};mesh.MarkDynamic();mesh.vertices=vertices;mesh.uv=uv;mesh.triangles=indices;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;view=gameObject.AddComponent<MeshRenderer>();
            view.sharedMaterial=Resources.Load<Material>(settings.material_resource);if(view.sharedMaterial==null)throw new InvalidOperationException("Generate the impact material before loading a venue.");
            view.shadowCastingMode=ShadowCastingMode.Off;view.receiveShadows=false;view.enabled=false;
            properties=new MaterialPropertyBlock();properties.SetFloat("_Spacing",(float)targets.SpacingMeters);
            properties.SetFloat("_StandingRadius",(float)targets.Radius(ShootingStance.Standing));
            properties.SetFloat("_CoreRatio",settings.bullet_diameter_m/settings.scuff_diameter_m);
            properties.SetVector("_PlateBounds",new Vector4(plate.min.x,plate.min.y,plate.max.x,plate.max.y));SetFace(ShootingStance.Standing,0);
        }
        public bool Add(in ShotImpactEvidence evidence)
        {
            if(!evidence.Valid||!evidence.Impact.ReachesPlane)return false;
            float x=(float)evidence.Impact.X,y=(float)evidence.Impact.Y,half=definition.scuff_diameter_m*.5f;
            if(x+half<plate.min.x||x-half>plate.max.x||y+half<plate.min.y||y-half>plate.max.y)return false;
            // The entire bullet/scuff footprint went through the aperture: no front-plate damage.
            if(evidence.EdgeClearanceMeters>=half)return false;
            int v=next*4;float z=-definition.surface_offset_m;
            vertices[v]=new Vector3(x-half,y-half,z);vertices[v+1]=new Vector3(x-half,y+half,z);
            vertices[v+2]=new Vector3(x+half,y+half,z);vertices[v+3]=new Vector3(x+half,y-half,z);
            next=(next+1)%definition.marks_per_bank;Count=Math.Min(Count+1,definition.marks_per_bank);
            mesh.vertices=vertices;mesh.RecalculateBounds();view.enabled=true;return true;
        }
        public void SetFace(ShootingStance stance,int mask)
        {properties.SetFloat("_ScoringRadius",(float)geometry.Radius(stance));properties.SetFloat("_ClosedMask",mask);view.SetPropertyBlock(properties);}
        public void ClearMarks(){Array.Clear(vertices,0,vertices.Length);mesh.vertices=vertices;Count=next=0;view.enabled=false;}
        private void OnDestroy(){if(mesh!=null){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
    }
}

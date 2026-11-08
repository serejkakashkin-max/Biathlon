using System;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>The anatomical visual consumes the evaluated G2 pose. It owns no clock, race or save.</summary>
    [DisallowMultipleComponent]
    public sealed class Biathlete167RacePresentation : MonoBehaviour
    {
        private static Biathlete167RaceDefinition definition;
        private AthleteRigBinding source;
        private Biathlete167MainMotion retarget;
        private Biathlete167SportMotion sport;
        private Biathlete167Appearance appearance;
        private SkinnedMeshRenderer[] skins;
        private int[] bibSlots;
        private MaterialPropertyBlock bib;
        private MeshRenderer[] skiRenderers;
        private int[][] skiSlots;
        private Material[][] skiMaterials;
        private bool initialized;
        private Func<Vector3,float> groundSurface;
        private Renderer[] renderers;
        private UnityEngine.Rendering.ShadowCastingMode[] shadows;
        private bool distantShadows;
        private double lastShootingPose = -1;
        private ShootingPhase lastShootingPhase;
        public float GroundOffsetMeters {get;private set;}
        public void SetGroundSurface(Func<Vector3,float> height)=>groundSurface=height;
        public Biathlete167Rig Rig { get; private set; }
        public AppearanceProfile AppliedProfile => appearance.Current;
        public string SelectedSkiId { get; private set; }
        public int Number { get; private set; }
        public CareerLeaderBib LeaderRole { get; private set; }
        public int RenderCount { get; private set; }
        public Material SkiDeckMaterial => skiRenderers[0].sharedMaterials[skiSlots[0][0]];

        public static Biathlete167RacePresentation Attach(AthleteRigBinding source)
        {
            if(source==null)throw new ArgumentNullException(nameof(source));
            var visual=source.GetComponent<Biathlete167RacePresentation>();
            if(visual!=null)return visual;
            if(definition==null)definition=Resources.Load<Biathlete167RaceDefinition>(Biathlete167RaceDefinition.ResourcePath);
            if(definition==null)throw new InvalidOperationException("M16.7 runtime resource is missing.");
            definition.Validate();
            visual=source.gameObject.AddComponent<Biathlete167RacePresentation>();
            visual.Initialize(source);
            return visual;
        }

        private void Initialize(AthleteRigBinding binding)
        {
            source=binding;
            Rig=Object.Instantiate(definition.athlete,source.transform.parent,false);
            Rig.name="M16.7 athlete visual";
            Rig.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            Rig.Lods.ForceLOD(-1);
            Biathlete167BodyShape.Prepare(Rig,definition);
            appearance=Rig.GetComponent<Biathlete167Appearance>();
            skins=new SkinnedMeshRenderer[3];bibSlots=new int[3];
            var levels=Rig.Lods.GetLODs();
            for(int i=0;i<3;i++)
            {
                skins[i]=Array.Find(levels[i].renderers,r=>r is SkinnedMeshRenderer) as SkinnedMeshRenderer;
                skins[i].updateWhenOffscreen=false;skins[i].forceMatrixRecalculationPerRender=false;
                var materials=skins[i].sharedMaterials;
                bibSlots[i]=Array.FindIndex(materials,m=>m.name=="PULSE_Bib_Printed_Textile");
                if(bibSlots[i]<0)throw new InvalidOperationException("The real bib material slot is missing.");
                materials[bibSlots[i]]=definition.bibMaterial;skins[i].sharedMaterials=materials;
            }
            bib=new MaterialPropertyBlock();
            skiRenderers=new MeshRenderer[2];skiSlots=new int[2][];skiMaterials=new Material[2][];
            for(int side=0;side<2;side++)
            {
                var foot=Rig.Bone(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
                MeshFilter longest=null;
                foreach(var mesh in foot.GetComponentsInChildren<MeshFilter>(true))
                    if(longest==null||mesh.sharedMesh.bounds.size.sqrMagnitude>longest.sharedMesh.bounds.size.sqrMagnitude)longest=mesh;
                skiRenderers[side]=longest.GetComponent<MeshRenderer>();
                skiMaterials[side]=skiRenderers[side].sharedMaterials;
                var slots=new System.Collections.Generic.List<int>();
                for(int slot=0;slot<skiMaterials[side].Length;slot++)
                    if(skiMaterials[side][slot].name.Contains("Ski")&&!skiMaterials[side][slot].name.Contains("Binding"))slots.Add(slot);
                if(slots.Count==0)throw new InvalidOperationException("The authored ski deck material is missing.");
                skiSlots[side]=slots.ToArray();
            }
            foreach(var renderer in source.GetComponentsInChildren<Renderer>(true))renderer.forceRenderingOff=true;
            initialized=true;
            RefreshAppearance();
            retarget=new Biathlete167MainMotion(Rig,source);
            sport=new Biathlete167SportMotion(Rig,retarget);
            sport.ConfigureEvaluatedSource();
            renderers=Rig.GetComponentsInChildren<Renderer>(true);shadows=new UnityEngine.Rendering.ShadowCastingMode[renderers.Length];
            for(int i=0;i<renderers.Length;i++)shadows[i]=renderers[i].shadowCastingMode;
        }

        public void RefreshAppearance()
        {
            if(!initialized)return;
            var legacy=source.GetComponent<AthleteAppearancePresentation>();
            var faces=AthleteFaceCatalog.Load();
            var profile=legacy.AppliedProfile;
            if(profile==null||profile.IsLegacy)profile=AppearanceProfile.Create(faces.profiles[legacy.FaceVariant].id,
                faces.skin_ids[legacy.SkinVariant],faces.body_ids[legacy.CosmeticVariant]);
            profile=AthleteLookCatalog.Load().Edit(profile);
            var outfit=Rig.GetComponent<Biathlete167Outfit>();
            if(!string.IsNullOrEmpty(legacy.Nation)&&outfit.Nation!=legacy.Nation)outfit.ApplyNation(legacy.Nation);
            appearance.ApplyProfile(profile);
            for(int i=0;i<skins.Length;i++)
            {
                var materials=skins[i].sharedMaterials;
                if(materials[bibSlots[i]]!=definition.bibMaterial)
                {materials[bibSlots[i]]=definition.bibMaterial;skins[i].sharedMaterials=materials;}
            }
            var body=definition.Body(profile.BodyId);
            float height=profile.HeightMillimeters/(float)AthleteLookCatalog.Load().BaseHeightMillimeters;
            float shoulders=Mathf.Lerp(definition.narrowShoulderScale,definition.wideShoulderScale,profile.ShoulderWidthBasis/10000f);
            Rig.transform.localScale=Vector3.one*height;
            Biathlete167BodyShape.Apply(skins,body.width*shoulders,body.depth,definition);
            var equipment=source.GetComponent<CareerSkiAppearance>();
            string ski=equipment!=null&&equipment.SelectedEquipmentId!=null?equipment.SelectedEquipmentId:"ski_neutral";
            {
                var material=definition.Ski(ski);
                for(int side=0;side<2;side++)
                {
                    foreach(int slot in skiSlots[side])skiMaterials[side][slot]=material;
                    skiRenderers[side].sharedMaterials=skiMaterials[side];
                }
                SelectedSkiId=ski;
            }
            var identity=source.GetComponent<AthleteBibPresentation>();
            Number=identity!=null&&identity.Number>0?identity.Number:1;
            LeaderRole=identity!=null?identity.LeaderRole:CareerLeaderBib.None;
            bib.SetFloat("_BibNumber",Number);
            bib.SetColor("_BaseColor",AthleteBibPresentation.FabricColor(LeaderRole));
            bib.SetColor("_SecondaryColor",AthleteBibPresentation.FabricColor(CareerLeaderBib.Discipline));
            bib.SetFloat("_ColorSplit",LeaderRole==(CareerLeaderBib.Total|CareerLeaderBib.Discipline)?1:0);
            for(int i=0;i<skins.Length;i++)skins[i].SetPropertyBlock(bib,bibSlots[i]);
            sport?.RefreshPreviewEyewear(Rig.PreviewEyewearRaised?1:0);
        }

        public void Render(AthleteAnimationFrame frame, bool distant = false, bool visibleShootingPose = true)
        {
            if (!Rig.gameObject.activeSelf)
            {
                lastShootingPose = -1;
                return;
            }
            if(distant!=distantShadows)
            {
                distantShadows=distant;
                for(int i=0;i<renderers.Length;i++)renderers[i].shadowCastingMode=distant?UnityEngine.Rendering.ShadowCastingMode.Off:shadows[i];
            }
            Rig.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            if (frame.Athlete.ShootingActive)
            {
                // Pose off-camera shadow casters at a reduced rate. The next visible
                // frame always solves against the current accepted shooting state.
                const double offCameraInterval = .2;
                double time = frame.TimeSeconds;
                if (!visibleShootingPose && lastShootingPose >= 0 && time >= lastShootingPose &&
                    time - lastShootingPose < offCameraInterval && lastShootingPhase == frame.Athlete.ShootingPhase)
                {
                    Rig.transform.position += Vector3.up * GroundOffsetMeters;
                    return;
                }
                lastShootingPose = time;
                lastShootingPhase = frame.Athlete.ShootingPhase;
            }
            else lastShootingPose = -1;
            sport.RenderEvaluatedRace(frame);ApplyGround((float)frame.PresentationGrade);RenderCount++;
        }
        public void RenderPreview()
        {
            Rig.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            sport.RenderEvaluatedTravel(0);ApplyGround(0);RenderCount++;
        }
        public void RenderShootingPreview(ShootingStance stance,float progress,WeaponAimState sight)
        {
            Rig.transform.SetPositionAndRotation(source.transform.position,source.transform.rotation);
            sport.RenderEvaluatedShooting(stance,progress,sight,0);ApplyGround(0);RenderCount++;
        }
        private void ApplyGround(float grade)
        {
            GroundOffsetMeters=0;if(groundSurface==null)return;
            var origin=source.transform.position;var forward=source.transform.forward;
            Add(origin);Add(Rig.ToeHinge(false).position);Add(Rig.ToeHinge(true).position);
            Rig.transform.position+=Vector3.up*GroundOffsetMeters;
            void Add(Vector3 point)
            {
                float plane=origin.y+grade*Vector3.Dot(point-origin,forward);
                GroundOffsetMeters=Mathf.Max(GroundOffsetMeters,groundSurface(point)-plane);
            }
        }
        private void OnDestroy()
        {
            sport?.Dispose();retarget?.Dispose();
            if(Rig!=null){if(Application.isPlaying)Object.Destroy(Rig.gameObject);else Object.DestroyImmediate(Rig.gameObject);}
        }
    }
}

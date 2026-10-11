using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RifleSightSettings
    {
        public int schema_version;public string id;
        public float field_of_view_deg,fine_field_of_view_deg,far_clip_m,standing_eye_height_m,prone_eye_height_m;
        public float overview_field_of_view_deg,overview_back_m,overview_up_m;
        public Vector3 rifle_local_position;
        public float rifle_scale,rifle_pitch_deg;
        public float post_width_m,post_base_y_m,post_tip_y_m,post_z_m;
        public float bolt_drop_m,bolt_pitch_deg,bolt_roll_deg;
        public static RifleSightSettings Load()
        {
            var d=JsonUtility.FromJson<RifleSightSettings>(Resources.Load<TextAsset>("Biathlon/rifle_sight_presentation_v1").text);
            if(d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!(d.fine_field_of_view_deg>1&&d.field_of_view_deg>d.fine_field_of_view_deg&&d.field_of_view_deg<30)||
                !(d.far_clip_m>50&&d.rifle_scale>0)||
                !(d.overview_field_of_view_deg>d.field_of_view_deg&&d.overview_field_of_view_deg<=60&&d.overview_back_m>0&&d.overview_up_m>0))throw new ArgumentException("Invalid original rifle sight presentation data.");
            return d;
        }
    }
    /// <summary>Original camera-local rifle and open diopter. The camera observes
    /// detached sight rays with bounded visual prediction between ticks.</summary>
    public sealed class RifleSightPresentation : MonoBehaviour
    {
        public Transform Weapon {get;private set;}
        private RifleSightSettings settings;
        private Mesh ownedMesh;
        /// <summary>Predict at most one tick of procedural motion after the latest
        /// observation. Current control aim is never delayed or extrapolated. Phase
        /// seams and accepted shots use their exact observed ray. This is visual
        /// prediction only; simulation never reads it when resolving a shot.</summary>
        public static WeaponAimState SampleSight(RacePresentationSnapshot previous,RacePresentationSnapshot current,int index,double alpha)
        {
            if(current==null)throw new ArgumentNullException(nameof(current));
            if(!double.IsFinite(alpha)||alpha<0||alpha>1)throw new ArgumentOutOfRangeException(nameof(alpha));
            var b=current.Get(index);
            if(b.ShotOutcome!=ShotOutcome.NoShot)return b.ShotSight;
            if(alpha==0||!b.ShootingActive||previous==null||previous.Tick+1!=current.Tick||
                previous.TickFrequency!=current.TickFrequency||previous.TrackId!=current.TrackId||previous.ProfileId!=current.ProfileId||index>=previous.Count)return b.Sight;
            var a=previous.Get(index);
            if(!a.ShootingActive||a.AthleteId!=b.AthleteId||a.CompletedBouts!=b.CompletedBouts||a.RangeLane!=b.RangeLane||
                a.Stance!=b.Stance||a.ShootingPhase!=b.ShootingPhase||
                b.ShootingPhase!=ShootingPhase.Aiming&&b.ShootingPhase!=ShootingPhase.BoltCycle)return b.Sight;
            double yaw=(b.Sight.YawRadians-b.ControlledAim.YawRadians)-(a.Sight.YawRadians-a.ControlledAim.YawRadians);
            double pitch=(b.Sight.PitchRadians-b.ControlledAim.PitchRadians)-(a.Sight.PitchRadians-a.ControlledAim.PitchRadians);
            // WeaponAimState's public angle domain bounds visual prediction too.
            return new WeaponAimState(Math.Max(-.5,Math.Min(.5,b.Sight.YawRadians+yaw*alpha)),
                Math.Max(-.5,Math.Min(.5,b.Sight.PitchRadians+pitch*alpha)));
        }
        public static void Observe(Camera camera,VenueAuthoringPlan venue,int lane,TargetGeometryDefinition geometry,ShotRay ray,ShootingStance stance,RifleSightSettings settings,float targetHeight,bool overview=false)
        {
            if(camera==null||venue==null||!ray.IsValid||ray.DirectionZ<=0||lane<1||lane>venue.LaneCount)throw new ArgumentException("Bound venue lane and forward sight ray required.");
            var p=venue.MatPoint(lane-1);var forward=new Vector3((float)venue.Manifest.Range.Forward.X,0,(float)venue.Manifest.Range.Forward.Z);
            var right=new Vector3(forward.z,0,-forward.x);
            var datum=new Vector3((float)p.X,(float)p.Y,(float)p.Z)+forward*(float)venue.FiringDatumDepth+Vector3.up*targetHeight;
            // The eye may move for stance; its optical axis intersects the supplied
            // lane-local ray on the target plane. Accepted shots supply their exact
            // pre-ballistic ray; render prediction never alters a resolved shot.
            double t=(geometry.DistanceMeters-ray.OriginZ)/ray.DirectionZ;
            var target=datum+right*(float)(ray.OriginX+t*ray.DirectionX)+Vector3.up*(float)(ray.OriginY+t*ray.DirectionY)+forward*(float)geometry.DistanceMeters;
            var eye=new Vector3((float)p.X,(float)p.Y,(float)p.Z)+forward*(float)venue.FiringDatumDepth+
                Vector3.up*(stance==ShootingStance.Prone?settings.prone_eye_height_m:settings.standing_eye_height_m);
            if(overview)eye+=-forward*settings.overview_back_m+Vector3.up*settings.overview_up_m;
            camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye,Vector3.up));
        }
        public void Initialize(RifleSightSettings settings)
        {
            this.settings=settings;
            var weapon=new GameObject("Original first person rifle");weapon.transform.SetParent(transform,false);Weapon=weapon.transform;
            Weapon.localPosition=settings.rifle_local_position;Weapon.localRotation=Quaternion.Euler(settings.rifle_pitch_deg,0,0);Weapon.localScale=Vector3.one*settings.rifle_scale;
            var original=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroRifle");
            ownedMesh=Instantiate(original);ownedMesh.name="Original rifle with pointed front sight";
            // Replace only the original tiny cylindrical sight insert. The stock,
            // rear diopter and protective front hood stay project-authored.
            var vertices=new List<Vector3>(original.vertices);var normals=new List<Vector3>(original.normals);
            int[] source=original.GetTriangles(1);var triangles=new List<int>(source.Length+6);
            bool Insert(Vector3 p)=>Mathf.Abs(p.x)<.0021f&&p.y>.068f&&p.y<.087f&&Mathf.Abs(p.z-1.008f)<.003f;
            for(int i=0;i<source.Length;i+=3)if(!(Insert(vertices[source[i]])&&Insert(vertices[source[i+1]])&&Insert(vertices[source[i+2]])))
            {triangles.Add(source[i]);triangles.Add(source[i+1]);triangles.Add(source[i+2]);}
            int at=vertices.Count;
            vertices.Add(new Vector3(-settings.post_width_m*.5f,settings.post_base_y_m,settings.post_z_m));
            vertices.Add(new Vector3(settings.post_width_m*.5f,settings.post_base_y_m,settings.post_z_m));
            vertices.Add(new Vector3(0,settings.post_tip_y_m,settings.post_z_m));
            for(int i=0;i<3;i++)normals.Add(Vector3.back);
            triangles.AddRange(new[]{at,at+2,at+1,at,at+1,at+2});
            ownedMesh.SetVertices(vertices);ownedMesh.SetNormals(normals);ownedMesh.SetTriangles(triangles,1);ownedMesh.RecalculateBounds();
            weapon.AddComponent<MeshFilter>().sharedMesh=ownedMesh;
            var renderer=weapon.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{
                Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleComposite"),Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleMetal"),
                Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleRubber"),Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleAccent")};
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        public void SetVisible(bool value)=>Weapon.gameObject.SetActive(value);
        public void AnimateCycle(float progress)
        {
            float motion=Mathf.Sin(Mathf.Clamp01(progress)*Mathf.PI);
            Weapon.localPosition=settings.rifle_local_position+Vector3.down*(settings.bolt_drop_m*motion);
            Weapon.localRotation=Quaternion.Euler(settings.rifle_pitch_deg+settings.bolt_pitch_deg*motion,0,settings.bolt_roll_deg*motion);
        }
        private void OnDestroy(){if(ownedMesh!=null)Destroy(ownedMesh);}
    }
}

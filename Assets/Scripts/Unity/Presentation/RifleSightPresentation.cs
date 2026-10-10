using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RifleSightSettings
    {
        public int schema_version;public string id;
        public float field_of_view_deg,fine_field_of_view_deg,far_clip_m,standing_eye_height_m,prone_eye_height_m;
        public float overview_field_of_view_deg,overview_back_m,overview_up_m;
        public Vector3 rifle_local_position;
        public float rifle_scale,rifle_pitch_deg;
        public static RifleSightSettings Load()
        {
            var d=JsonUtility.FromJson<RifleSightSettings>(Resources.Load<TextAsset>("Biathlon/rifle_sight_presentation_v1").text);
            if(d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!(d.fine_field_of_view_deg>1&&d.field_of_view_deg>d.fine_field_of_view_deg&&d.field_of_view_deg<30)||
                !(d.far_clip_m>50&&d.rifle_scale>0)||
                !(d.overview_field_of_view_deg>d.field_of_view_deg&&d.overview_field_of_view_deg<=60&&d.overview_back_m>0&&d.overview_up_m>0))throw new ArgumentException("Invalid original rifle sight presentation data.");
            return d;
        }
    }
    /// <summary>Original camera-local rifle and open diopter. The camera observes the authoritative sight ray in the venue.</summary>
    public sealed class RifleSightPresentation : MonoBehaviour
    {
        public Transform Weapon {get;private set;}
        public static void Observe(Camera camera,VenueAuthoringPlan venue,int lane,TargetGeometryDefinition geometry,ShotRay ray,ShootingStance stance,RifleSightSettings settings,float targetHeight,bool overview=false)
        {
            if(camera==null||venue==null||!ray.IsValid||ray.DirectionZ<=0||lane<1||lane>venue.LaneCount)throw new ArgumentException("Bound venue lane and forward sight ray required.");
            var p=venue.MatPoint(lane-1);var forward=new Vector3((float)venue.Manifest.Range.Forward.X,0,(float)venue.Manifest.Range.Forward.Z);
            var right=new Vector3(forward.z,0,-forward.x);
            var datum=new Vector3((float)p.X,(float)p.Y,(float)p.Z)+forward*(float)venue.FiringDatumDepth+Vector3.up*targetHeight;
            // The eye may move for stance; its optical axis still intersects the exact
            // pre-ballistic simulation ray on the target plane. No shot is altered.
            double t=(geometry.DistanceMeters-ray.OriginZ)/ray.DirectionZ;
            var target=datum+right*(float)(ray.OriginX+t*ray.DirectionX)+Vector3.up*(float)(ray.OriginY+t*ray.DirectionY)+forward*(float)geometry.DistanceMeters;
            var eye=new Vector3((float)p.X,(float)p.Y,(float)p.Z)+forward*(float)venue.FiringDatumDepth+
                Vector3.up*(stance==ShootingStance.Prone?settings.prone_eye_height_m:settings.standing_eye_height_m);
            if(overview)eye+=-forward*settings.overview_back_m+Vector3.up*settings.overview_up_m;
            camera.transform.SetPositionAndRotation(eye,Quaternion.LookRotation(target-eye,Vector3.up));
        }
        public void Initialize(RifleSightSettings settings)
        {
            var weapon=new GameObject("Original first person rifle");weapon.transform.SetParent(transform,false);Weapon=weapon.transform;
            Weapon.localPosition=settings.rifle_local_position;Weapon.localRotation=Quaternion.Euler(settings.rifle_pitch_deg,0,0);Weapon.localScale=Vector3.one*settings.rifle_scale;
            weapon.AddComponent<MeshFilter>().sharedMesh=Resources.Load<Mesh>("Biathlon/Athletes/G2Hero/Meshes/HeroRifle");
            var renderer=weapon.AddComponent<MeshRenderer>();renderer.sharedMaterials=new[]{
                Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleComposite"),Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleMetal"),
                Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleRubber"),Resources.Load<Material>("Biathlon/Athletes/G2Hero/Materials/RifleAccent")};
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        public void SetVisible(bool value)=>Weapon.gameObject.SetActive(value);
    }
}

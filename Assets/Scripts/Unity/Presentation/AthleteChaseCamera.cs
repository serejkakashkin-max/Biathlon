using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class ChaseCameraSettings
    {
        public int schema_version;
        public string id;
        public float distance_m,height_m,look_ahead_m,look_height_m,response_per_s,clearance_m,collision_radius_m,ground_probe_m;
        public static ChaseCameraSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<ChaseCameraSettings>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id))throw new ArgumentException("Invalid chase camera settings.");
            foreach(float value in new[]{d.distance_m,d.height_m,d.look_ahead_m,d.look_height_m,d.response_per_s,d.clearance_m,d.collision_radius_m,d.ground_probe_m})if(float.IsNaN(value)||float.IsInfinity(value)||value<=0)throw new ArgumentException("Camera settings must be finite and positive.");
            if(d.height_m<d.clearance_m||d.distance_m<=d.collision_radius_m||d.ground_probe_m<d.height_m)throw new ArgumentException("Invalid camera clearance/probe bounds.");return d;
        }
    }
    [DefaultExecutionOrder(100),RequireComponent(typeof(Camera))]
    public sealed class AthleteChaseCamera : MonoBehaviour
    {
        private Transform target;
        private ChaseCameraSettings settings;
        private int environmentMask;
        private bool initialized;
        private float requestedGrade,blendedGrade;
        private CameraOrbitSettings orbit;
        private float orbitYaw,orbitPitch,orbitLook;
        public bool OrbitHeld{get;private set;}
        public float OrbitYawDegrees=>orbitYaw;
        public float OrbitPitchDegrees=>orbitPitch;
        public Transform Target=>target;
        public void SnapToTarget(){initialized=false;Render(0);}
        public void SetTerrainGrade(float grade)
        {
            if(!float.IsFinite(grade)||Mathf.Abs(grade)>.5f)throw new ArgumentOutOfRangeException(nameof(grade));
            requestedGrade=grade;
        }
        public void Follow(Transform athlete,ChaseCameraSettings definition,int collisionMask=Physics.DefaultRaycastLayers)
        {if(athlete==null||definition==null)throw new ArgumentNullException();if(athlete==transform)throw new ArgumentException("Camera cannot follow itself.");target=athlete;settings=ChaseCameraSettings.FromJson(JsonUtility.ToJson(definition));orbit=CameraOrbitSettings.Load();environmentMask=collisionMask;CancelOrbit();initialized=false;}
        public void SetOrbitInput(bool held,Vector2 mouseDelta)
        {
            if(!float.IsFinite(mouseDelta.x)||!float.IsFinite(mouseDelta.y))throw new ArgumentOutOfRangeException(nameof(mouseDelta));
            if(orbit==null)throw new InvalidOperationException("Follow a target before orbiting.");
            OrbitHeld=held;if(!held)return;mouseDelta=Vector2.ClampMagnitude(mouseDelta,orbit.maximum_delta_pixels);
            orbitYaw=Mathf.DeltaAngle(0,orbitYaw+mouseDelta.x*orbit.degrees_per_pixel);
            orbitPitch=Mathf.Clamp(orbitPitch-mouseDelta.y*orbit.degrees_per_pixel,orbit.minimum_pitch_degrees,orbit.maximum_pitch_degrees);
        }
        public void CancelOrbit(){OrbitHeld=false;orbitYaw=orbitPitch=orbitLook=0;}
        public void Render(float deltaTime)
        {
            if(float.IsNaN(deltaTime)||float.IsInfinity(deltaTime)||deltaTime<0)throw new ArgumentOutOfRangeException(nameof(deltaTime));if(target==null||settings==null)return;
            var forward=Vector3.ProjectOnPlane(target.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            float weight=1-Mathf.Exp(-settings.response_per_s*deltaTime);
            blendedGrade=initialized?Mathf.Lerp(blendedGrade,requestedGrade,weight):requestedGrade;
            var terrainForward=(forward+Vector3.up*blendedGrade).normalized;
            var pivot=target.position+Vector3.up*settings.look_height_m;
            float orbitWeight=1-Mathf.Exp(-orbit.return_per_s*deltaTime);
            if(!OrbitHeld)
            {
                orbitYaw=Mathf.DeltaAngle(0,Mathf.LerpAngle(orbitYaw,0,orbitWeight));orbitPitch=Mathf.Lerp(orbitPitch,0,orbitWeight);
                if(Mathf.Abs(orbitYaw)<orbit.settle_degrees)orbitYaw=0;if(Mathf.Abs(orbitPitch)<orbit.settle_degrees)orbitPitch=0;
            }
            orbitLook=initialized?Mathf.Lerp(orbitLook,OrbitHeld?1:0,orbitWeight):OrbitHeld?1:0;
            var armOffset=-terrainForward*settings.distance_m+Vector3.up*(settings.height_m-settings.look_height_m);float appliedPitch=orbitPitch;
            if(OrbitHeld||orbitPitch!=0)
            {
                float elevation=Mathf.Atan2(armOffset.y,new Vector2(armOffset.x,armOffset.z).magnitude)*Mathf.Rad2Deg;
                appliedPitch=Mathf.Clamp(appliedPitch,orbit.minimum_elevation_degrees-elevation,orbit.maximum_elevation_degrees-elevation);
            }
            var yaw=Quaternion.AngleAxis(orbitYaw,Vector3.up);var pitch=Quaternion.AngleAxis(appliedPitch,yaw*Vector3.Cross(Vector3.up,forward));
            var desired=pivot+pitch*(yaw*armOffset);
            var position=initialized?Vector3.Lerp(transform.position,desired,weight):desired;
            // Terrain probing and obstruction handling are presentation-only. No physics
            // result is fed to race movement, aim or the deterministic command stream.
            var probe=position+Vector3.up*settings.ground_probe_m;
            if(Physics.Raycast(probe,Vector3.down,out var ground,settings.ground_probe_m*2,environmentMask,QueryTriggerInteraction.Ignore))position.y=Mathf.Max(position.y,ground.point.y+settings.clearance_m);
            else position.y=Mathf.Max(position.y,target.position.y+settings.clearance_m);
            var arm=position-pivot;
            if(arm.magnitude>settings.collision_radius_m&&Physics.SphereCast(pivot,settings.collision_radius_m,arm.normalized,out var obstacle,arm.magnitude,environmentMask,QueryTriggerInteraction.Ignore))position=pivot+arm.normalized*Mathf.Max(0,obstacle.distance-settings.clearance_m);
            transform.position=position;var look=target.position+terrainForward*settings.look_ahead_m+Vector3.up*settings.look_height_m;
            look=Vector3.Lerp(look,pivot,orbitLook);
            if((look-position).sqrMagnitude>.0001f)transform.rotation=Quaternion.LookRotation(look-position,Vector3.up);initialized=true;
        }
        private void LateUpdate()=>Render(Time.deltaTime);
        private void OnDisable()=>CancelOrbit();
    }
}

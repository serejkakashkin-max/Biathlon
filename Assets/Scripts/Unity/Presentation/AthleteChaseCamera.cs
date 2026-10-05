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
        public Transform Target=>target;
        public void SetTerrainGrade(float grade)
        {
            if(!float.IsFinite(grade)||Mathf.Abs(grade)>.5f)throw new ArgumentOutOfRangeException(nameof(grade));
            requestedGrade=grade;
        }
        public void Follow(Transform athlete,ChaseCameraSettings definition,int collisionMask=Physics.DefaultRaycastLayers)
        {if(athlete==null||definition==null)throw new ArgumentNullException();if(athlete==transform)throw new ArgumentException("Camera cannot follow itself.");target=athlete;settings=ChaseCameraSettings.FromJson(JsonUtility.ToJson(definition));environmentMask=collisionMask;initialized=false;}
        public void Render(float deltaTime)
        {
            if(float.IsNaN(deltaTime)||float.IsInfinity(deltaTime)||deltaTime<0)throw new ArgumentOutOfRangeException(nameof(deltaTime));if(target==null||settings==null)return;
            var forward=Vector3.ProjectOnPlane(target.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.001f)forward=Vector3.forward;
            float weight=1-Mathf.Exp(-settings.response_per_s*deltaTime);
            blendedGrade=initialized?Mathf.Lerp(blendedGrade,requestedGrade,weight):requestedGrade;
            var terrainForward=(forward+Vector3.up*blendedGrade).normalized;
            var pivot=target.position+Vector3.up*settings.look_height_m;
            var desired=target.position-terrainForward*settings.distance_m+Vector3.up*settings.height_m;
            var position=initialized?Vector3.Lerp(transform.position,desired,weight):desired;
            // Terrain probing and obstruction handling are presentation-only. No physics
            // result is fed to race movement, aim or the deterministic command stream.
            var probe=position+Vector3.up*settings.ground_probe_m;
            if(Physics.Raycast(probe,Vector3.down,out var ground,settings.ground_probe_m*2,environmentMask,QueryTriggerInteraction.Ignore))position.y=Mathf.Max(position.y,ground.point.y+settings.clearance_m);
            else position.y=Mathf.Max(position.y,target.position.y+settings.clearance_m);
            var arm=position-pivot;
            if(arm.magnitude>settings.collision_radius_m&&Physics.SphereCast(pivot,settings.collision_radius_m,arm.normalized,out var obstacle,arm.magnitude,environmentMask,QueryTriggerInteraction.Ignore))position=pivot+arm.normalized*Mathf.Max(0,obstacle.distance-settings.clearance_m);
            transform.position=position;var look=target.position+terrainForward*settings.look_ahead_m+Vector3.up*settings.look_height_m;
            if((look-position).sqrMagnitude>.0001f)transform.rotation=Quaternion.LookRotation(look-position,Vector3.up);initialized=true;
        }
        private void LateUpdate()=>Render(Time.deltaTime);
    }
}

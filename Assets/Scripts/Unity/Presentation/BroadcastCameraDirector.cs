using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit-time presentation camera. Never takes over an active gameplay camera.</summary>
    public sealed class BroadcastCameraDirector : MonoBehaviour
    {
        private static readonly Dictionary<Camera,BroadcastCameraDirector> owners=new Dictionary<Camera,BroadcastCameraDirector>();
        private readonly Dictionary<string,Transform> subjects=new Dictionary<string,Transform>(StringComparer.Ordinal);
        private Camera view; private BroadcastShotDefinition definition; private bool leased;
        private Vector3 oldPosition; private Quaternion oldRotation; private float oldFov,oldNear,oldFar; private bool oldOrthographic;
        public string ShotId {get;private set;}
        public void Initialize(Camera camera,BroadcastShotDefinition shots)
        {
            if(view!=null||camera==null||shots==null||!isActiveAndEnabled||camera.enabled||camera.GetComponent<AthleteChaseCamera>()!=null||owners.ContainsKey(camera))throw new ArgumentException("Fresh director and idle dedicated camera required.");
            var copy=BroadcastShotDefinition.FromJson(JsonUtility.ToJson(shots));
            view=camera;definition=copy;oldPosition=view.transform.position;oldRotation=view.transform.rotation;oldFov=view.fieldOfView;oldNear=view.nearClipPlane;oldFar=view.farClipPlane;oldOrthographic=view.orthographic;owners.Add(view,this);leased=true;
        }
        public void Bind(string stableId,Transform subject)
        {
            if(!leased||string.IsNullOrWhiteSpace(stableId)||subject==null||subject==view.transform)throw new ArgumentException("Explicit external subject required.");
            if(subjects.ContainsKey(stableId))throw new ArgumentException("Duplicate subject binding.");subjects.Add(stableId,subject);
        }
        public void Render(string shotId,double elapsedSeconds)
        {
            if(!leased||view==null||!isActiveAndEnabled)throw new InvalidOperationException("Active broadcast lease required.");
            if(!double.IsFinite(elapsedSeconds)||elapsedSeconds<0)throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
            var shot=definition.Get(shotId);var position=shot.position;var look=shot.look_at;
            if(shot.mode=="track")
            {
                if(!subjects.TryGetValue(shot.subject_id,out var subject)||subject==null)throw new InvalidOperationException("Broadcast subject is unavailable.");
                position+=subject.position;look+=subject.position;
            }
            else if(shot.mode=="rail")position=Vector3.LerpUnclamped(position,shot.end_position,Mathf.SmoothStep(0,1,(float)Math.Min(1,elapsedSeconds/shot.duration_s)));
            var direction=look-position;
            if(!float.IsFinite(position.sqrMagnitude)||!float.IsFinite(direction.sqrMagnitude)||direction.sqrMagnitude<.0001f)throw new InvalidOperationException("Invalid observed subject pose.");
            var up=Mathf.Abs(Vector3.Dot(direction.normalized,Vector3.up))>.999f?Vector3.forward:Vector3.up;
            view.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction,up));view.orthographic=false;view.fieldOfView=shot.fov;view.nearClipPlane=shot.near_m;view.farClipPlane=shot.far_m;view.enabled=true;ShotId=shot.id;
        }
        public void Release()
        {
            if(!leased)return;leased=false;
            if(view!=null){owners.Remove(view);view.enabled=false;view.transform.SetPositionAndRotation(oldPosition,oldRotation);view.fieldOfView=oldFov;view.nearClipPlane=oldNear;view.farClipPlane=oldFar;view.orthographic=oldOrthographic;}
            else { // Remove the destroyed Unity object key without relying on overloaded null equality.
                if(!ReferenceEquals(view,null))owners.Remove(view);
            }
            subjects.Clear();ShotId=null;
        }
        private void OnDisable()=>Release();
        private void OnDestroy()=>Release();
    }
}

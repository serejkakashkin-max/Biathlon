using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class BroadcastShot
    {
        public string id, mode, subject_id;
        public Vector3 position, end_position, look_at;
        public float fov, near_m, far_m, duration_s;
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || (mode != "fixed" && mode != "track" && mode != "rail")) throw new ArgumentException("Invalid broadcast shot identity/mode.");
            if (mode == "track" && string.IsNullOrWhiteSpace(subject_id)) throw new ArgumentException("Tracking requires a stable subject binding.");
            Finite(position); Finite(end_position); Finite(look_at);
            Bound(fov, 1, 100); Bound(near_m, .01f, 2); Bound(far_m, near_m + 1, 2000); Bound(duration_s, .1f, 600);
            if ((look_at-position).sqrMagnitude < .0001f || (mode == "rail" && DistanceToSegment(look_at,position,end_position) < .01f)) throw new ArgumentException("Camera path intersects its look target.");
        }
        private static float DistanceToSegment(Vector3 p,Vector3 a,Vector3 b) { var v=b-a;return Vector3.Distance(p,a+v*(v.sqrMagnitude>0?Mathf.Clamp01(Vector3.Dot(p-a,v)/v.sqrMagnitude):0)); }
        private static void Finite(Vector3 v) { for(int i=0;i<3;i++) if(!float.IsFinite(v[i])||Mathf.Abs(v[i])>10000)throw new ArgumentException("Invalid camera coordinate."); }
        private static void Bound(float v,float lo,float hi) { if(!float.IsFinite(v)||v<lo||v>hi)throw new ArgumentException("Invalid camera range."); }
    }
    [Serializable] public sealed class BroadcastShotDefinition
    {
        public int schema_version; public string id; public BroadcastShot[] shots;
        public static BroadcastShotDefinition Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/broadcast_shots_v1").text);
        public static BroadcastShotDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<BroadcastShotDefinition>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.shots==null||d.shots.Length<1||d.shots.Length>64)throw new ArgumentException("Invalid broadcast shot schema.");
            for(int i=0;i<d.shots.Length;i++){if(d.shots[i]==null)throw new ArgumentException("Missing shot.");d.shots[i].Validate();for(int j=0;j<i;j++)if(d.shots[i].id==d.shots[j].id)throw new ArgumentException("Duplicate shot ID.");}return d;
        }
        public BroadcastShot Get(string id){foreach(var shot in shots)if(shot.id==id)return shot;throw new ArgumentException("Unknown broadcast shot.");}
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class Biathlete167SkiStance
    {
        public Vector3 left_euler,right_euler,left_toe_offset,right_toe_offset,left_ankle_from_hip,right_ankle_from_hip;
        public float blend_start,blend_end,heel_pitch_degrees,ground_clearance_m;
        public float turn_full_weight;
        public void Validate()
        {
            if(!(blend_start>=0&&blend_start<blend_end&&blend_end<=1)||!float.IsFinite(heel_pitch_degrees)||heel_pitch_degrees<0||heel_pitch_degrees>45||!(ground_clearance_m>=0&&ground_clearance_m<.02f))throw new ArgumentException("Invalid shooting ski phase/hinge definition.");
            foreach(var v in new[]{left_euler,right_euler})if(!float.IsFinite(v.sqrMagnitude)||Mathf.Abs(v.x)>.001f||Mathf.Abs(v.y)>160||Mathf.Abs(v.z)>90)throw new ArgumentException("Invalid shooting ski yaw/edge definition.");
            foreach(var v in new[]{left_toe_offset,right_toe_offset,left_ankle_from_hip,right_ankle_from_hip})if(!float.IsFinite(v.sqrMagnitude)||v.sqrMagnitude>4)throw new ArgumentException("Invalid shooting foot position.");
            if(!(turn_full_weight>0&&turn_full_weight<=1))throw new ArgumentException("Invalid ski turn phase.");
        }
    }
    /// <summary>Pose the ski/boot through the real toe hinge, with cached ski support hulls.</summary>
    internal sealed class Biathlete167ShootingSkis
    {
        private readonly Transform root,hips;private readonly Foot[] feet=new Foot[2];
        private readonly Vector3[] glideAnkles = new Vector3[2], glideToes = new Vector3[2], glideHints = new Vector3[2];
        private readonly Quaternion[] glideSkiRotations = new Quaternion[2], glideFootRotations = new Quaternion[2];
        private sealed class Foot
        {public Transform Upper,Knee,Ankle,Toe,Ski;public Vector3 ToeInFoot,ToeInSki,ForwardInFoot,ForwardInSki,UpInSki;public Quaternion FootBind,SkiBind;public Vector2[] SupportHull;}
        public float MaximumBindingResidualMeters{get;private set;}
        public Biathlete167ShootingSkis(Biathlete167Rig rig)
        {
            root=rig.transform;hips=rig.Bone(HumanBodyBones.Hips);
            for(int side=0;side<2;side++)
            {
                var ankle=rig.Bone(side==0?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);MeshFilter longest=null;float length=0;
                foreach(var mesh in ankle.GetComponentsInChildren<MeshFilter>(true))if(mesh.sharedMesh.bounds.size.sqrMagnitude>length){longest=mesh;length=mesh.sharedMesh.bounds.size.sqrMagnitude;}
                if(longest==null)throw new InvalidOperationException("Actual Nordic ski is missing.");var ski=longest.transform;var toe=rig.ToeHinge(side==1);
                var f=new Foot{Upper=rig.Bone(side==0?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg),Knee=rig.Bone(side==0?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg),Ankle=ankle,Toe=toe,Ski=ski,ToeInFoot=ankle.InverseTransformPoint(toe.position),ToeInSki=ski.InverseTransformPoint(toe.position),FootBind=Quaternion.Inverse(root.rotation)*ankle.rotation,SkiBind=Quaternion.Inverse(root.rotation)*ski.rotation,ForwardInFoot=ankle.InverseTransformDirection(root.forward),ForwardInSki=ski.InverseTransformDirection(root.forward),UpInSki=ski.InverseTransformDirection(root.up)};
                var points=new List<Vector2>();foreach(var vertex in longest.sharedMesh.vertices){var p=root.InverseTransformVector(ski.TransformVector(vertex-f.ToeInSki));points.Add(new Vector2(p.x,p.y));}f.SupportHull=Hull(points);feet[side]=f;
            }
        }
        public void Apply(Biathlete167SkiStance definition,float progress,bool prone)
        {
            MaximumBindingResidualMeters=0;float weight=Smooth(Mathf.InverseLerp(definition.blend_start,definition.blend_end,progress));if(weight<=0)return;
            for(int side=0;side<2;side++)
            {
                var f=feet[side];var angles=side==0?definition.left_euler:definition.right_euler;float turn=Smooth(Mathf.InverseLerp(0,definition.turn_full_weight,weight)),yaw=angles.y*turn,roll=angles.z*turn;
                var frame=Quaternion.AngleAxis(yaw,Vector3.up)*Quaternion.AngleAxis(roll,Vector3.forward);
                var skiRotation=root.rotation*frame*f.SkiBind;
                var currentForward=f.Ankle.TransformDirection(f.ForwardInFoot);var skiForward=f.Ski.TransformDirection(f.ForwardInSki);var skiUp=f.Ski.TransformDirection(f.UpInSki);var lateral=Vector3.Cross(skiUp,skiForward).normalized;
                float pitch=Vector3.SignedAngle(skiForward,currentForward,lateral);pitch=Mathf.Lerp(pitch,definition.heel_pitch_degrees,weight);
                var footRotation=root.rotation*frame*Quaternion.AngleAxis(pitch,Vector3.right)*f.FootBind;
                float sine=Mathf.Sin(roll*Mathf.Deg2Rad),cosine=Mathf.Cos(roll*Mathf.Deg2Rad),bottom=float.PositiveInfinity;foreach(var point in f.SupportHull)bottom=Mathf.Min(bottom,point.x*sine+point.y*cosine);
                // Standing rotates the supported foot around its ankle. Keeping
                // the old travelling toe while turning would pull the ankle
                // sideways beyond the leg's reach. Prone uses authored spread.
                var toe=root.InverseTransformPoint(prone?f.Toe.position:f.Ankle.position+footRotation*Vector3.Scale(f.ToeInFoot,f.Ankle.lossyScale));
                if(prone)
                {
                    // Author the extended leg from its own hip, rather than
                    // rotating the ski around a leftover travelling toe.
                    var ankleGoal=f.Upper.position+root.TransformVector(side==0?definition.left_ankle_from_hip:definition.right_ankle_from_hip);
                    var toeGoal=root.InverseTransformPoint(ankleGoal+footRotation*Vector3.Scale(f.ToeInFoot,f.Ankle.lossyScale));toe=Vector3.Lerp(toe,toeGoal,weight);
                }
                toe+=(side==0?definition.left_toe_offset:definition.right_toe_offset)*weight;toe.y=definition.ground_clearance_m-bottom;var targetToe=root.TransformPoint(toe);
                var ankle=targetToe-footRotation*Vector3.Scale(f.ToeInFoot,f.Ankle.lossyScale);var hint=f.Knee.position-(f.Upper.position+f.Ankle.position)*.5f;
                var preferred=prone?-root.up:root.rotation*frame*Vector3.forward;hint=Vector3.Slerp(hint.normalized,preferred.normalized,weight);
                Solve(f,ankle,hint);f.Ankle.rotation=footRotation;
                f.Ski.SetPositionAndRotation(targetToe-skiRotation*Vector3.Scale(f.ToeInSki,f.Ski.lossyScale),skiRotation);
                MaximumBindingResidualMeters=Mathf.Max(MaximumBindingResidualMeters,Vector3.Distance(f.Toe.position,f.Ski.TransformPoint(f.ToeInSki)));
            }
        }
        public void ApplyGlide(float cycle, float glideFraction, float grade, float recoveryHeight,
            float clearance, float pelvisFollow, float transitionFraction, float extensionMeters, float heelReleaseDegrees,
            float maximumKneeDegrees)
        {
            var normal = new Vector3(0, 1, -grade).normalized;
            var terrain = Quaternion.LookRotation(new Vector3(0, grade, 1), normal);
            Vector3 leftToe = root.InverseTransformPoint(feet[0].Toe.position);
            Vector3 rightToe = root.InverseTransformPoint(feet[1].Toe.position);
            // Begin shifting before touchdown so the pelvis keeps travelling through the handoff.
            float transferPhase = Mathf.Repeat(cycle + transitionFraction * .45f, 1);
            float half = Mathf.Repeat(transferPhase, .5f);
            float handoff = Smooth(half / transitionFraction);
            float leftSupport = transferPhase < .5f ? handoff : 1f - handoff;
            var pelvis = root.InverseTransformPoint(hips.position);
            pelvis.x = Mathf.Lerp(pelvis.x, Mathf.Lerp(rightToe.x, leftToe.x, leftSupport), pelvisFollow);
            MaximumBindingResidualMeters = 0;
            float liftLimit = float.PositiveInfinity;
            for (int side = 0; side < feet.Length; side++)
            {
                var f = feet[side];
                var toe = side == 0 ? leftToe : rightToe;
                var skiForward = f.Ski.TransformDirection(f.ForwardInSki);
                var skiUp = f.Ski.TransformDirection(f.UpInSki);
                var footForward = f.Ankle.TransformDirection(f.ForwardInFoot);
                var lateral = Vector3.Cross(skiUp, skiForward);
                float inheritedHeel = Vector3.SignedAngle(skiForward, footForward, lateral);
                float phase = Mathf.Repeat(cycle + side * .5f, 1);
                // A loaded ski carries the whole boot. Hinge at the toe as
                // load leaves the ski, then reseat the boot before touchdown.
                float release = Smooth(Mathf.InverseLerp(glideFraction * .76f, glideFraction + .06f, phase)) *
                    (1 - Smooth(Mathf.InverseLerp(glideFraction + .14f, .94f, phase)));
                float heel = Mathf.Clamp(inheritedHeel, 0, heelReleaseDegrees) * release;
                var direction = Quaternion.Inverse(terrain) * root.InverseTransformDirection(skiForward);
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                var unedged = terrain * Quaternion.AngleAxis(yaw, Vector3.up);
                var baselineUp = root.TransformDirection(unedged * Vector3.up);
                float roll = Vector3.SignedAngle(baselineUp, skiUp, skiForward);
                var frame = unedged * Quaternion.AngleAxis(roll, Vector3.forward);
                var skiRotation = root.rotation * frame * f.SkiBind;
                var footRotation = root.rotation * frame * Quaternion.AngleAxis(heel, Vector3.right) * f.FootBind;

                float recovery = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(glideFraction, 1, phase));
                float lift = clearance + recoveryHeight * recovery * recovery;
                float sine = Mathf.Sin(roll * Mathf.Deg2Rad), cosine = Mathf.Cos(roll * Mathf.Deg2Rad);
                float bottom = float.PositiveInfinity;
                foreach (var point in f.SupportHull)
                    bottom = Mathf.Min(bottom, point.x * sine + point.y * cosine);
                toe.y = (lift - bottom - normal.z * toe.z) / normal.y;

                glideToes[side] = root.TransformPoint(toe);
                glideAnkles[side] = glideToes[side] - footRotation * Vector3.Scale(f.ToeInFoot, f.Ankle.lossyScale);
                // Keep the production leg's bend plane. Turning this plane with
                // the edged ski bows the knees outward independently of the hip.
                glideHints[side] = f.Knee.position - (f.Upper.position + f.Ankle.position) * .5f;
                glideSkiRotations[side] = skiRotation;
                glideFootRotations[side] = footRotation;
                var upper = root.InverseTransformPoint(f.Upper.position);
                upper.x += pelvis.x - root.InverseTransformPoint(hips.position).x;
                var delta = root.InverseTransformPoint(glideAnkles[side]) - upper;
                float thigh = Vector3.Distance(f.Upper.position, f.Knee.position);
                float shin = Vector3.Distance(f.Knee.position, f.Ankle.position);
                float reach = Mathf.Sqrt(thigh * thigh + shin * shin - 2 * thigh * shin * Mathf.Cos(maximumKneeDegrees * Mathf.Deg2Rad));
                liftLimit = Mathf.Min(liftLimit, delta.y + Mathf.Sqrt(Mathf.Max(0, reach * reach - delta.x * delta.x - delta.z * delta.z)));
            }

            // Rise over the new glide ski and compress gradually into the next push.
            float glide = Mathf.Repeat(cycle * 2, 1);
            float extension = .48f + .52f * (.5f + .5f * Mathf.Cos((glide - .29f) * Mathf.PI * 2));
            pelvis.y += Mathf.Min(liftLimit, extensionMeters * extension);
            hips.position = root.TransformPoint(pelvis);

            for (int side = 0; side < feet.Length; side++)
            {
                var f = feet[side];
                Solve(f, glideAnkles[side], glideHints[side]);
                f.Ankle.rotation = glideFootRotations[side];
                // Preserve the real toe hinge even when an extreme target is out of reach.
                var rotation = glideSkiRotations[side];
                f.Ski.SetPositionAndRotation(f.Toe.position - rotation * Vector3.Scale(f.ToeInSki, f.Ski.lossyScale), rotation);
                MaximumBindingResidualMeters = Mathf.Max(MaximumBindingResidualMeters, Vector3.Distance(glideToes[side], f.Toe.position));
            }
        }
        private static void Solve(Foot f,Vector3 target,Vector3 hint)
        {
            var origin=f.Upper.position;float a=Vector3.Distance(origin,f.Knee.position),b=Vector3.Distance(f.Knee.position,f.Ankle.position);var delta=target-origin;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.0001f,a+b-.0001f);var direction=delta.normalized;
            var bend=Vector3.ProjectOnPlane(hint,direction).normalized;if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;float along=(a*a-b*b+distance*distance)/(2*distance);var joint=origin+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            f.Upper.rotation=Quaternion.FromToRotation(f.Knee.position-origin,joint-origin)*f.Upper.rotation;f.Knee.rotation=Quaternion.FromToRotation(f.Ankle.position-f.Knee.position,origin+direction*distance-f.Knee.position)*f.Knee.rotation;
        }
        private static Vector2[] Hull(List<Vector2> points)
        {
            points.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));var unique=new List<Vector2>();foreach(var p in points)if(unique.Count==0||p!=unique[unique.Count-1])unique.Add(p);
            float Cross(Vector2 a,Vector2 b,Vector2 c)=>(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);var hull=new List<Vector2>();
            foreach(var p in unique){while(hull.Count>=2&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}int lower=hull.Count;
            for(int i=unique.Count-2;i>=0;i--){var p=unique[i];while(hull.Count>lower&&Cross(hull[hull.Count-2],hull[hull.Count-1],p)<=0)hull.RemoveAt(hull.Count-1);hull.Add(p);}if(hull.Count>1)hull.RemoveAt(hull.Count-1);return hull.ToArray();
        }
        private static float Smooth(float t){t=Mathf.Clamp01(t);return t*t*t*(t*(t*6-15)+10);}
    }
}

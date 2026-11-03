using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class AthleteImpactVisualSettings
    {
        public int schema_version;public string id;
        public float minimum_speed_change_mps,full_speed_change_mps,full_yaw_change_radps,recovery_seconds,pitch_degrees,yaw_degrees,roll_degrees,arm_splay_degrees,hips_fraction;
        public static AthleteImpactVisualSettings Load()
        {
            var d=JsonUtility.FromJson<AthleteImpactVisualSettings>(Resources.Load<TextAsset>("Biathlon/athlete_impact_visual_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||!(d.minimum_speed_change_mps>0&&d.full_speed_change_mps>d.minimum_speed_change_mps&&d.full_yaw_change_radps>0&&d.recovery_seconds>0&&d.recovery_seconds<=2&&d.pitch_degrees>0&&d.pitch_degrees<=45&&d.yaw_degrees>0&&d.yaw_degrees<=60&&d.roll_degrees>0&&d.roll_degrees<=40&&d.arm_splay_degrees>0&&d.arm_splay_degrees<=60&&d.hips_fraction>0&&d.hips_fraction<=1))throw new ArgumentException("Invalid impact visual definition.");
            return d;
        }
    }
    /// <summary>Deterministic visible recovery from an accepted physical impulse.
    /// No random spin, root movement, physics change or gameplay control penalty.</summary>
    public sealed class AthleteImpactPresentation
    {
        private readonly AthleteImpactVisualSettings settings;
        private long observed=-1;private double began=double.NegativeInfinity;
        private Vector3 amplitude;private float strength;
        public AthleteImpactPresentation(AthleteImpactVisualSettings settings){this.settings=settings??throw new ArgumentNullException(nameof(settings));}
        public void Observe(SkiImpactObservation impact,ManualSkiHeadingState pose,long tick,int frequency,bool skiing)
        {
            if(frequency<1||tick<0)throw new ArgumentOutOfRangeException();
            if(!skiing||observed>tick){began=double.NegativeInfinity;strength=0;amplitude=Vector3.zero;}
            if(observed==tick)return;observed=tick;
            double speed=impact.SpeedChange;
            if(!skiing||!pose.IsValid||speed<settings.minimum_speed_change_mps)return;
            strength=Mathf.Clamp01((float)(speed/settings.full_speed_change_mps));
            double forward=impact.DeltaX*Math.Sin(pose.HeadingRadians)+impact.DeltaZ*Math.Cos(pose.HeadingRadians);
            double side=impact.DeltaX*Math.Cos(pose.HeadingRadians)-impact.DeltaZ*Math.Sin(pose.HeadingRadians);
            amplitude=new Vector3((float)(-forward/speed)*settings.pitch_degrees,
                Mathf.Clamp((float)(impact.DeltaYaw/settings.full_yaw_change_radps),-1,1)*settings.yaw_degrees,
                (float)(-side/speed)*settings.roll_degrees)*strength;
            began=Math.Max(0,tick-1)/(double)frequency;
        }
        public Vector3 Angles(double time)
        {
            double t=(time-began)/settings.recovery_seconds;
            return t<=0||t>=1?Vector3.zero:amplitude*(float)(Math.Sin(Math.PI*t)*Math.Exp(-t));
        }
        public void Apply(AthleteRigBinding rig,double time)
        {
            var angles=Angles(time);if(angles.sqrMagnitude==0)return;
            rig.Bone(AthleteBone.Hips).localRotation*=Quaternion.Euler(angles*settings.hips_fraction);
            rig.Bone(AthleteBone.Chest).localRotation*=Quaternion.Euler(angles);
            float wave=angles.magnitude/(settings.pitch_degrees+settings.yaw_degrees+settings.roll_degrees)*settings.arm_splay_degrees;
            rig.Bone(AthleteBone.LeftUpperArm).localRotation*=Quaternion.Euler(-wave,0,wave);
            rig.Bone(AthleteBone.RightUpperArm).localRotation*=Quaternion.Euler(-wave,0,-wave);
        }
    }
}

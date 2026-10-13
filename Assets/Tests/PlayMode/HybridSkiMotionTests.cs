using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class HybridSkiMotionTests
    {
        private static readonly LocomotionInput[] Inputs={new LocomotionInput(6,0,.9f),new LocomotionInput(4,.08f,.9f),new LocomotionInput(3,.15f,1),
            new LocomotionInput(8,0,1),new LocomotionInput(9,0,1.25f,sprint:true),new LocomotionInput(8,0,.1f),new LocomotionInput(9,-.07f,.9f),
            new LocomotionInput(12,-.1f,.7f,tuck:true),new LocomotionInput(8,0,.9f,curvature:-.03f),new LocomotionInput(8,0,.9f,curvature:.03f),
            new LocomotionInput(6,0,.9f),new LocomotionInput(0,0,0,travelling:false)};
        [Test] public void TwelveStatesKeepRootLengthsSupportAndFiniteEquipment()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();var c=rig.GetComponent<AthleteGroundContact>();
                var grip=rig.GetComponent<AthleteGripPresentation>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                float leftLength=Vector3.Distance(rig.Bone(AthleteBone.LeftUpperLeg).position,rig.Bone(AthleteBone.LeftLowerLeg).position);
                for(int state=0;state<Inputs.Length;state++)
                {
                    d.UseOriginalMotion=true;d.Sample(rig,Inputs[state],0);d.UseOriginalMotion=false;
                    for(int i=0;i<240;i++)
                    {
                        double t=i/120.0;d.Sample(rig,Inputs[state],t,state==10?new ExertionInput(.7f,.2f,185):new ExertionInput(0,1,140));
                        rifle.Sample(rig,0);c.SampleMoving(rig,Inputs[state].Grade,d.PoleCycle,d.ContactState,0,t);d.ApplySkiEdges(rig);grip.Render(rig);
                        Assert.That(c.MaxFootResidualMeters,Is.LessThan(.001f),"Both legs must reach their local snow plane without stretching.");
                        float left=Vector3.Dot(rig.Bone(AthleteBone.LeftFoot).position-rig.ModelRoot.position,c.SurfaceNormal);
                        float right=Vector3.Dot(rig.Bone(AthleteBone.RightFoot).position-rig.ModelRoot.position,c.SurfaceNormal);
                        Assert.That(Mathf.Min(left,right),Is.LessThan(.121f),"At least one support ski stays grounded through recovery.");
                        Assert.That(Vector3.Distance(rig.Bone(AthleteBone.LeftUpperLeg).position,rig.Bone(AthleteBone.LeftLowerLeg).position),Is.EqualTo(leftLength).Within(.0001f));
                        for(int b=0;b<(int)AthleteBone.Count;b++){var p=rig.Bone((AthleteBone)b).position;Assert.That(float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z),Is.True);}
                        Assert.That(rig.transform.position,Is.EqualTo(Vector3.zero));Assert.That(rig.ModelRoot.localPosition,Is.EqualTo(Vector3.zero));
                        Assert.That(grip.MaximumResidualMeters,Is.LessThan(.03f));
                    }
                    Assert.That(d.MotionState,Is.EqualTo((SkiMotionState)state));
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void InterruptedStateSwitchesDoNotJumpAtTheSameTimeAndRewindIsRepeatable()
        {
            var rig=AthleteRigFactory.Create(null);var fresh=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                for(int i=0;i<=180;i++)d.Sample(rig,Inputs[0],i/60.0);
                foreach(var input in Inputs)
                {
                    var hip=rig.Bone(AthleteBone.Hips).position;var hand=rig.Bone(AthleteBone.LeftHand).position;
                    d.Sample(rig,input,3);
                    Assert.That(Vector3.Distance(hip,rig.Bone(AthleteBone.Hips).position),Is.LessThan(.0001f));
                    Assert.That(Vector3.Distance(hand,rig.Bone(AthleteBone.LeftHand).position),Is.LessThan(.0001f));
                }
                d.Sample(rig,Inputs[4],1);fresh.GetComponent<AthleteHybridLocomotionDriver>().Sample(fresh,Inputs[4],1);
                Assert.That(Vector3.Distance(rig.Bone(AthleteBone.LeftHand).position,fresh.Bone(AthleteBone.LeftHand).position),Is.LessThan(.0001f));
            }
            finally{Object.DestroyImmediate(rig.gameObject);Object.DestroyImmediate(fresh.gameObject);}
        }
        [Test] public void TerrainPolingPatternsAndSpeedCadenceAreDistinctAndWarmLoopAllocatesNothing()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                var s=SkiMotionSettings.Load();Assert.That(s.profiles[0].poles_per_cycle,Is.EqualTo(1));Assert.That(s.profiles[3].poles_per_cycle,Is.EqualTo(2));Assert.That(s.profiles[1].hand_stagger_m,Is.GreaterThan(0));
                d.Sample(rig,new LocomotionInput(3,0,.9f),0);float slow=d.CadenceHz;
                for(int i=1;i<=180;i++)d.Sample(rig,new LocomotionInput(7,0,.9f),i/60.0);
                Assert.That(d.CadenceHz,Is.GreaterThan(slow));
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=181;i<300;i++)d.Sample(rig,new LocomotionInput(7,0,.9f),i/60.0);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
                s=JsonUtility.FromJson<SkiMotionSettings>(JsonUtility.ToJson(s));s.profiles[0].hip_transfer_m=10;
                Assert.Throws<ArgumentException>(()=>SkiMotionSettings.FromJson(JsonUtility.ToJson(s)));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void FallbackIsTheOriginalPoseWithoutMovingTheRaceRoot()
        {
            var rig=AthleteRigFactory.Create(null);var original=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();d.UseOriginalMotion=true;
                for(int i=0;i<120;i++)
                {
                    double t=i/60.0;d.Sample(rig,Inputs[0],t);original.GetComponent<AthleteLocomotionDriver>().Sample(original,Inputs[0],t);
                    Assert.That(Vector3.Distance(rig.Bone(AthleteBone.LeftFoot).position,original.Bone(AthleteBone.LeftFoot).position),Is.LessThan(.0001f));
                }
                Assert.That(d.Active,Is.False);
            }
            finally{Object.DestroyImmediate(rig.gameObject);Object.DestroyImmediate(original.gameObject);}
        }
    }
}

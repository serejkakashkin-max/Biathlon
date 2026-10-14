using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace Biathlon.PlayModeTests
{
    public class ArmMotionRefinementTests
    {
        [Test] public void ContinuousHandCurvesStayWithinEachAuthoredSegmentAndRepeatAcrossCycles()
        {
            var settings=SkiMotionSettings.Load();
            foreach(int index in new[]{1,3})
            {
                var c=settings.curves[index];
                for(int key=1;key<c.time.Length;key++)for(int sample=0;sample<=100;sample++)
                {
                    float phase=Mathf.Lerp(c.time[key-1],c.time[key],sample/100f);
                    float value=c.AtContinuous(phase);
                    Assert.That(value,Is.InRange(Mathf.Min(c.value[key-1],c.value[key])-.00001f,Mathf.Max(c.value[key-1],c.value[key])+.00001f));
                    Assert.That(c.AtContinuous(phase+3),Is.EqualTo(value).Within(.00001f));
                    Assert.That(c.AtContinuous(phase-2),Is.EqualTo(value).Within(.00001f));
                }
            }
        }
        [Test] public void ContinuousHandCurvesCarryVelocityThroughKeysAndTheCycleSeam()
        {
            const float h=.0001f;
            var settings=SkiMotionSettings.Load();
            foreach(int index in new[]{1,3})
            {
                var c=settings.curves[index];
                for(int key=0;key<c.time.Length-1;key++)
                {
                    float t=c.time[key],value=c.AtContinuous(t);
                    Assert.That(value,Is.EqualTo(c.value[key]).Within(.00001f));
                    float before=(value-c.AtContinuous(t-h))/h,after=(c.AtContinuous(t+h)-value)/h;
                    Assert.That(after,Is.EqualTo(before).Within(.15f),"No velocity step at an authored hand key or the wrap seam.");
                }
            }
        }
        [TestCase(6f,0f)]
        [TestCase(4f,.08f)]
        [TestCase(9f,0f)]
        public void PoweredPoleStrokeLeavesSpaceBetweenTheElbowAndTheTorso(float speed,float grade)
        {
            Measure(speed,grade,out float clearance,out _);
            Assert.That(clearance,Is.GreaterThan(.045f),"Elbow centre must move outward from the shoulder, leaving room for the sleeve instead of rubbing the torso.");
        }
        [TestCase(6f,0f)]
        [TestCase(4f,.08f)]
        [TestCase(9f,0f)]
        public void PoleStrokeAvoidsTheFastHammeringPartOfTheCycle(float speed,float grade)
        {
            Measure(speed,grade,out _,out float maximumRate);
            Assert.That(maximumRate,Is.LessThan(950f),"The visible forearm must not accelerate into the previous hammer-like mid-stroke burst.");
        }
        private static void Measure(float speed,float grade,out float clearance,out float maximumRate)
        {
            var rig=AthleteRigFactory.Create(null);clearance=float.MaxValue;maximumRate=0;
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();var c=rig.GetComponent<AthleteGroundContact>();
                var r=rig.GetComponent<AthleteRiflePresentation>();var grip=rig.GetComponent<AthleteGripPresentation>();
                var previous=new Quaternion[2];
                for(int i=0;i<360;i++)
                {
                    double time=i/120.0;rig.transform.position=new Vector3(0,grade*speed*(float)time,speed*(float)time);
                    d.Sample(rig,new LocomotionInput(speed,grade,speed>8?1.25f:.95f,sprint:speed>8),time);
                    r.Sample(rig,0);c.SampleMoving(rig,grade,d.PoleCycle,d.ContactState,0,time);grip.Render(rig);
                    for(int side=0;side<2;side++)
                    {
                        var shoulder=rig.Bone(side==0?AthleteBone.LeftUpperArm:AthleteBone.RightUpperArm);
                        var elbow=rig.Bone(side==0?AthleteBone.LeftLowerArm:AthleteBone.RightLowerArm);
                        float outward=(side==0?-1:1)*rig.ModelRoot.InverseTransformDirection(elbow.position-shoulder.position).x;
                        clearance=Mathf.Min(clearance,outward);
                        if(i>0)maximumRate=Mathf.Max(maximumRate,Quaternion.Angle(previous[side],elbow.rotation)*120);
                        previous[side]=elbow.rotation;
                    }
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
    }
}

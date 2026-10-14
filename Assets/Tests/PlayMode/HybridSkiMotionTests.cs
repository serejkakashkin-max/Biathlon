using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class HybridSkiMotionTests
    {
        [TestCase(6f,0f,0f)]
        [TestCase(4f,.08f,.03f)]
        [TestCase(9f,0f,0f)]
        [TestCase(6f,0f,.15f)]
        [TestCase(6f,0f,-.15f)]
        [TestCase(3f,.15f,.03f)]
        [TestCase(8f,-.08f,0f)]
        public void PoleStrokeUsesAnAnatomicalElbowHingeWithoutShoulderHeightSpikesOrWideFlares(float speed,float grade,float curve)
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();var c=rig.GetComponent<AthleteGroundContact>();
                var rifle=rig.GetComponent<AthleteRiflePresentation>();var grip=rig.GetComponent<AthleteGripPresentation>();
                var previousLeft=Quaternion.identity;var previousRight=Quaternion.identity;
                for(int i=0;i<360;i++)
                {
                    double t=i/120.0;rig.transform.position=new Vector3(0,grade*(float)t*speed,(float)t*speed);
                    d.Sample(rig,new LocomotionInput(speed,grade,speed>8?1.25f:.95f,sprint:speed>8,curvature:curve),t);
                    rifle.Sample(rig,0);c.SampleMoving(rig,grade,d.PoleCycle,d.ContactState,0,t);grip.Render(rig);
                    foreach(bool right in new[]{false,true})
                    {
                        var s=rig.Bone(right?AthleteBone.RightUpperArm:AthleteBone.LeftUpperArm);
                        var e=rig.Bone(right?AthleteBone.RightLowerArm:AthleteBone.LeftLowerArm);
                        var w=rig.Bone(right?AthleteBone.RightHand:AthleteBone.LeftHand);
                        var bendAxis=Vector3.Cross((e.position-s.position).normalized,(w.position-e.position).normalized).normalized;
                        Assert.That(Vector3.Dot(bendAxis,rig.ModelRoot.right),Is.LessThan(-.7f),"Elbow flexion keeps one anatomical hinge; it must not reverse or twist sideways during the push.");
                        Assert.That(e.position.y-s.position.y,Is.LessThan(.04f),"Poling elbows must not pop above the shoulders.");
                        Assert.That(Mathf.Abs(rig.ModelRoot.InverseTransformDirection(e.position-s.position).x),Is.LessThan(.18f),"Elbows must not flare a quarter-metre away from the upper arm.");
                        var hingeOnly=Quaternion.AngleAxis(-Vector3.Angle(e.position-s.position,w.position-e.position),Vector3.right);
                        Assert.That(Quaternion.Angle(e.localRotation,hingeOnly),Is.LessThan(1f),"The lower-arm bone must flex about its anatomical hinge rather than acquire large yaw/roll twist.");
                        if(i>0)Assert.That(Quaternion.Angle(e.rotation,right?previousRight:previousLeft),Is.LessThan(15f),"The hinge must stay continuous through plant, push and recovery.");
                        if(right)previousRight=e.rotation;else previousLeft=e.rotation;
                    }
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void HingedArmClampsFlexionWithoutStretchingBonesOrMovingTheRaceRoot()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                rig.transform.SetPositionAndRotation(new Vector3(12,4,-8),Quaternion.Euler(0,73,0));
                var root=rig.transform.position;var rotation=rig.transform.rotation;
                var upper=rig.Bone(AthleteBone.LeftUpperArm);var lower=rig.Bone(AthleteBone.LeftLowerArm);var hand=rig.Bone(AthleteBone.LeftHand);
                float a=Vector3.Distance(upper.position,lower.position),b=Vector3.Distance(lower.position,hand.position);
                foreach(float reach in new[]{.001f,.3f,2f})
                {
                    var target=upper.position+rig.ModelRoot.TransformDirection(new Vector3(0,-.7f,.7f).normalized)*reach;
                    AthleteLocomotionDriver.SolveHingedArm(upper,lower,hand,target,rig.ModelRoot.rotation,8,140);
                    float flex=Vector3.Angle(lower.position-upper.position,hand.position-lower.position);
                    Assert.That(flex,Is.InRange(7.99f,140.01f));
                    Assert.That(Vector3.Distance(upper.position,lower.position),Is.EqualTo(a).Within(.00001f));
                    Assert.That(Vector3.Distance(lower.position,hand.position),Is.EqualTo(b).Within(.00001f));
                    Assert.That(Quaternion.Angle(lower.localRotation,Quaternion.AngleAxis(-flex,Vector3.right)),Is.LessThan(.1f));
                    Assert.That(rig.transform.position,Is.EqualTo(root));Assert.That(rig.transform.rotation,Is.EqualTo(rotation));
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void FinishedAthleteKeepsExhaustionHandsInsteadOfReturningToStartReadyPose()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                d.Sample(rig,new LocomotionInput(0,0,0,travelling:false),3,new ExertionInput(1,0,185,finished:true));
                rig.GetComponent<AthleteRiflePresentation>().Sample(rig,0);
                rig.GetComponent<AthleteGroundContact>().SampleMoving(rig,0,d.PoleCycle,d.ContactState,0,3);
                rig.GetComponent<AthleteGripPresentation>().Render(rig);
                Assert.That(rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.LeftHand).position).y,Is.LessThan(1.1f),
                    "The new waiting pose must not override the existing exhausted-finish envelope.");
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [TestCase(-.035f)]
        [TestCase(.035f)]
        [TestCase(-.15f)]
        [TestCase(.15f)]
        public void CornerPolingKeepsBothGlovesOnTheirOwnSideOfTheChest(float curvature)
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();var c=rig.GetComponent<AthleteGroundContact>();
                var rifle=rig.GetComponent<AthleteRiflePresentation>();var grip=rig.GetComponent<AthleteGripPresentation>();
                for(int i=0;i<360;i++)
                {
                    double t=i/120.0;rig.transform.position=new Vector3(0,0,(float)t*8);
                    d.Sample(rig,new LocomotionInput(8,0,.95f,curvature:curvature),t);
                    rifle.Sample(rig,0);c.SampleMoving(rig,0,d.PoleCycle,d.ContactState,0,t);grip.Render(rig);
                    float chest=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.Chest).position).x;
                    float left=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.LeftHand).position).x;
                    float right=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.RightHand).position).x;
                    Assert.That(chest-left,Is.GreaterThan(.15f),"Inside glove must not cross the torso while the body leans into the turn.");
                    Assert.That(right-chest,Is.GreaterThan(.15f),"Inside glove must not cross the torso while the body leans into the turn.");
                    Assert.That(grip.MaximumResidualMeters,Is.LessThan(.03f));
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [TestCase(0f)]
        [TestCase(1f)]
        public void WaitingMassStartKeepsPolesBracedBesideBootsInsteadOfHorizontalTuckShafts(float effort)
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();
                var c=rig.GetComponent<AthleteGroundContact>();var eq=rig.GetComponent<AthleteEquipmentBinding>();
                d.Sample(rig,new LocomotionInput(0,0,effort),0);
                rig.GetComponent<AthleteRiflePresentation>().Sample(rig,0);
                c.SampleMoving(rig,0,d.PoleCycle,d.ContactState,0,0);
                rig.GetComponent<AthleteGripPresentation>().Render(rig);
                Assert.That(d.ContactState,Is.EqualTo(SkiLocomotion.Idle),"A waiting starter is not in downhill tuck.");
                foreach(var p in new[]{eq.LeftPole,eq.RightPole})
                    Assert.That(Vector3.Dot(-p.up,Vector3.down),Is.GreaterThan(.7f),"Ready poles point into the snow, not sideways.");
                Assert.That(c.LeftPoleTip.y,Is.EqualTo(0).Within(.001f));
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }

        [TestCase(6f,0f,.03f,0f)]
        [TestCase(4f,.08f,.03f,0f)]
        [TestCase(3f,.15f,-.03f,0f)]
        [TestCase(6f,0f,0f,2f)]
        public void PropulsiveCornersClimbsAndLaneChangesKeepACompletePoleDriveAndRaisedRecovery(float speed,float grade,float curve,float lateral)
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var d=rig.GetComponent<AthleteHybridLocomotionDriver>();var c=rig.GetComponent<AthleteGroundContact>();
                var grip=rig.GetComponent<AthleteGripPresentation>();var rifle=rig.GetComponent<AthleteRiflePresentation>();
                float low=10,high=-10,front=-10,rear=10;int plants=0;
                for(int i=0;i<480;i++)
                {
                    double t=i/120.0;
                    d.Sample(rig,new LocomotionInput(speed,grade,.95f,curvature:curve,lateralSpeed:lateral),t);
                    rifle.Sample(rig,0);c.SampleMoving(rig,grade,d.PoleCycle,d.ContactState,0,t);grip.Render(rig);
                    var hand=rig.ModelRoot.InverseTransformPoint(rig.Bone(AthleteBone.LeftHand).position);
                    low=Mathf.Min(low,hand.y);high=Mathf.Max(high,hand.y);front=Mathf.Max(front,hand.z);rear=Mathf.Min(rear,hand.z);
                    if(c.LeftPlanted&&c.RightPlanted)plants++;
                }
                Assert.That(plants,Is.GreaterThan(0),"Powered travel must have a paired snow contact, including on a curved climb.");
                Assert.That(high-low,Is.GreaterThan(.15f),"Recovery lifts the hands; horizontal fore/aft rowing is not a pole stroke.");
                Assert.That(front-rear,Is.GreaterThan(.22f),"Both arms complete the push and return while changing lane.");
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
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

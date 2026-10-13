using System;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class G2JointVolumeTests
    {
        [Test] public void AllLodsHaveBoundedLocalJointDeltasAndRestIsUnchanged()
        {
            var rig=AthleteRigFactory.Create(null);
            try
            {
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();var lod=rig.GetComponent<AthleteSkinDetailBinding>();
                foreach(float distance in new[]{0,20,70})
                {
                    lod.Prepare(distance,false,true);var mesh=skin.sharedMesh;
                    Assert.That(mesh.blendShapeCount,Is.EqualTo(7),"Four joint correctives plus three independent original cosmetic builds.");
                    foreach(string name in new[]{"LeftLowerArmVolume","RightLowerArmVolume","LeftLowerLegVolume","RightLowerLegVolume"})Assert.That(mesh.GetBlendShapeIndex(name),Is.InRange(0,3));
                    var delta=new Vector3[mesh.vertexCount];
                    for(int shape=0;shape<4;shape++)
                    {
                        mesh.GetBlendShapeFrameVertices(shape,0,delta,null,null);int changed=0;
                        foreach(var p in delta){Assert.That(float.IsFinite(p.sqrMagnitude),Is.True);Assert.That(p.magnitude,Is.LessThanOrEqualTo(.01801f));if(p.sqrMagnitude>1e-10f)changed++;}
                        Assert.That(changed,Is.GreaterThan(5));Assert.That(changed,Is.LessThan(mesh.vertexCount/3));
                    }
                    rig.ResetPose();rig.GetComponent<AthleteJointVolume>().Render(rig);
                    for(int i=0;i<4;i++)Assert.That(skin.GetBlendShapeWeight(i),Is.Zero);
                }
            }
            finally{Object.DestroyImmediate(rig.gameObject);}
        }
        [Test] public void BentJointsExpandWithoutMovingSocketsAndHaveNoWarmAllocations()
        {
            var rig=AthleteRigFactory.Create(null);var before=new Mesh();var after=new Mesh();
            try
            {
                rig.GetComponent<AthleteShootingPose>().Sample(rig,ShootingStance.Standing,1);
                rig.GetComponent<AthleteRiflePresentation>().Sample(rig,1,stance:ShootingStance.Standing);
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();var joints=rig.GetComponent<AthleteJointVolume>();
                skin.BakeMesh(before);var hand=rig.Socket(AthleteSocket.LeftHandGrip).position;
                joints.Render(rig);skin.BakeMesh(after);
                Assert.That(skin.GetBlendShapeWeight(0),Is.GreaterThan(10));
                var a=before.vertices;var b=after.vertices;float maximum=0;int changed=0;
                for(int i=0;i<a.Length;i++){float delta=Vector3.Distance(a[i],b[i]);maximum=Mathf.Max(maximum,delta);if(delta>.001f)changed++;}
                Assert.That(maximum,Is.InRange(.002f,.0181f));Assert.That(changed,Is.GreaterThan(10));
                Assert.That(rig.Socket(AthleteSocket.LeftHandGrip).position,Is.EqualTo(hand));
                long start=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<300;i++)joints.Render(rig);
                Assert.That(GC.GetAllocatedBytesForCurrentThread()-start,Is.Zero);
            }
            finally{Object.DestroyImmediate(before);Object.DestroyImmediate(after);Object.DestroyImmediate(rig.gameObject);}
        }
    }
}

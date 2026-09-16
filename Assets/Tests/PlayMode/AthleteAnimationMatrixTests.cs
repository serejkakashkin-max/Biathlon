using System;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class AthleteAnimationMatrixTests
    {
        [Test]
        public void EveryPreviewSetIsFiniteRepeatableAndReturnsTheRifleToCarry()
        {
            var root = new GameObject("animation matrix test");
            try
            {
                var preview = root.AddComponent<AthleteAnimationPreview>(); preview.enabled = false;
                foreach (AthletePreviewSet set in Enum.GetValues(typeof(AthletePreviewSet)))
                {
                    preview.StateSet = set; preview.RenderAt(.4);
                    Assert.That(preview.Count, Is.EqualTo(7));
                    for (int i = 0; i < preview.Count; i++)
                    {
                        var rig = preview.Rig(i); var head = rig.Bone(AthleteBone.Head).position;
                        preview.RenderAt(.4);
                        Assert.That(Vector3.Distance(head, rig.Bone(AthleteBone.Head).position), Is.LessThan(.00001f));
                        Assert.That(preview.Label(i), Is.Not.Empty);
                        for (int b = 0; b < (int)AthleteBone.Count; b++)
                        {
                            var p = rig.Bone((AthleteBone)b).position;
                            Assert.That(float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z), Is.True);
                        }
                    }
                    if (set == AthletePreviewSet.ProneSequence || set == AthletePreviewSet.StandingSequence)
                    {
                        var first = preview.Rig(0); var last = preview.Rig(6);
                        var a = first.GetComponent<AthleteEquipmentBinding>().Rifle;
                        var b = last.GetComponent<AthleteEquipmentBinding>().Rifle;
                        Assert.That(Vector3.Distance(first.ModelRoot.InverseTransformPoint(a.position), last.ModelRoot.InverseTransformPoint(b.position)), Is.LessThan(.00001f));
                        Assert.That(Quaternion.Angle(a.rotation, b.rotation), Is.LessThan(.01f));
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void IntegratedStanceTransitionsKeepEquipmentFiniteAndRestoreBaseline()
        {
            var rig = OriginalAthleteRigBuilder.Create(null);
            try
            {
                var driver = rig.GetComponent<AthleteLocomotionDriver>(); var shooting = rig.GetComponent<AthleteShootingPose>();
                var rifle = rig.GetComponent<AthleteRiflePresentation>(); var contact = rig.GetComponent<AthleteGroundContact>();
                foreach (var stance in new[] { ShootingStance.Prone, ShootingStance.Standing })
                {
                    Sample(0); var startHead = rig.Bone(AthleteBone.Head).position;
                    var prior = startHead;
                    for (int i = 1; i <= 400; i++)
                    {
                        Sample(i <= 200 ? i / 200f : (400 - i) / 200f);
                        var head = rig.Bone(AthleteBone.Head).position;
                        Assert.That(Vector3.Distance(prior, head), Is.LessThan(.05f)); prior = head;
                        Assert.That(float.IsFinite(contact.LeftPoleTip.y) && float.IsFinite(contact.RightPoleTip.y), Is.True);
                        Assert.That(Vector3.Distance(rig.GetComponent<AthleteEquipmentBinding>().LeftPole.position, contact.LeftPoleTip), Is.EqualTo(1.36f).Within(.0001f));
                    }
                    Assert.That(Vector3.Distance(startHead, prior), Is.LessThan(.00001f));
                    Assert.That(rig.transform.position, Is.EqualTo(Vector3.zero));
                    void Sample(float progress)
                    {
                        driver.Sample(rig, new LocomotionInput(0, 0, 0, travelling: false), 0);
                        shooting.Sample(rig, stance, progress); rifle.Sample(rig, progress, stance: stance);
                        contact.Sample(rig, 0, driver.Cycle, SkiLocomotion.Idle, rifle.HandWeight);
                    }
                }
            }
            finally { Object.DestroyImmediate(rig.gameObject); }
        }
    }
}

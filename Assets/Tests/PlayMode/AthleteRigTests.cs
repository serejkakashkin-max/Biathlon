using System;
using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class RootMovingTestPose : AthletePoseDriver
    {
        public override void Apply(AthleteRigBinding rig, AthleteAnimationFrame frame)
        {
            transform.parent.position += Vector3.one * 100;
            rig.ModelRoot.localPosition += Vector3.up;
            rig.Rotate(AthleteBone.Head, Quaternion.Euler(0, 20, 0));
        }
    }

    public class AthleteRigTests
    {
        private static string Read(string name) => Resources.Load<TextAsset>("Biathlon/" + Path.GetFileNameWithoutExtension(name)).text;
        private static string Fixture(string name) => File.ReadAllText(Path.Combine(Application.dataPath, "../tests/fixtures/tracks/", name));

        [UnityTest]
        public IEnumerator TypedBindingsEquipmentAndSwappedRigPreserveRaceAndCameraPivot()
        {
            var runner = ChampionshipRaceFactory.Create(Read, Fixture, CompetitionFormat.Sprint, new BalanceEventAudit(5), new AiDecisionCounts());
            var visual = TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"), runner.Field.Track);
            var root = new GameObject("rig_integration");
            var feed = root.AddComponent<RacePresentationFeed>();
            var view = root.AddComponent<AthleteProxyPresentation>();
            var template = OriginalAthleteRigBuilder.Create(null);
            // Asset labels are deliberately unrelated to gameplay IDs or binding conventions.
            for (int i = 0; i < (int)AthleteBone.Count; i++) template.Bone((AthleteBone)i).name = "arbitrary_" + i;
            template.gameObject.AddComponent<RootMovingTestPose>();
            view.Initialize(feed, visual, template);
            for (int tick = 0; tick < 30; tick++) runner.Step();
            var snapshot = RacePresentationSnapshot.Capture(runner.Field);
            feed.Publish(snapshot); var distance = runner.Field.GetAthlete(0).Motion.DistanceNanometers;
            view.Render(1); Assert.That(view.Count, Is.EqualTo(30));
            var athlete = snapshot.Get(0); var pivot = view.GetProxy(athlete.AthleteId);
            var rig = view.GetRig(athlete.AthleteId);
            var point = visual.Get(athlete.LoopId).Sample(athlete.LocalNanometers / 1e9);
            Assert.That(pivot.localPosition.y, Is.EqualTo((float)point.Y + 1).Within(.0001f));
            Assert.That(rig.transform.localPosition, Is.EqualTo(Vector3.down));
            Assert.That(rig.Bone(AthleteBone.Head).name, Is.EqualTo("arbitrary_4"));
            for (int i = 0; i < (int)AthleteSocket.Count; i++) Assert.That(rig.Socket((AthleteSocket)i), Is.Not.Null);
            foreach (var socket in new[] { AthleteSocket.LeftSkiBinding, AthleteSocket.RightSkiBinding, AthleteSocket.LeftHandGrip, AthleteSocket.RightHandGrip, AthleteSocket.RifleBack })
                Assert.That(rig.Socket(socket).GetComponentsInChildren<Renderer>().Length, Is.GreaterThan(0), socket.ToString());
            Assert.That(rig.GetComponentsInChildren<Collider>().Length, Is.Zero);
            var retained = pivot; view.Render(.25);
            Assert.That(view.GetProxy(athlete.AthleteId), Is.SameAs(retained));
            Assert.That(runner.Field.GetAthlete(0).Motion.DistanceNanometers, Is.EqualTo(distance));
            Assert.That(runner.Field.Tick.Value, Is.EqualTo(snapshot.Tick));
            feed.ResetRace(); view.Render(1); Assert.That(view.Count, Is.Zero);
            yield return null;
            feed.Publish(snapshot); view.Render(1); Assert.That(view.Count, Is.EqualTo(30));
            Assert.That(view.GetProxy(athlete.AthleteId), Is.Not.SameAs(retained));
            Object.Destroy(root); Object.Destroy(template.gameObject); yield return null;
        }

        [UnityTest]
        public IEnumerator AnimatorEvaluatedPoseIsPreservedAndRootMotionDisabled()
        {
            var pivot = new GameObject("animator_pivot");
            var rig = OriginalAthleteRigBuilder.Create(pivot.transform);
            var animator = rig.ModelRoot.gameObject.AddComponent<Animator>();
            var bones = new Transform[(int)AthleteBone.Count];
            var sockets = new Transform[(int)AthleteSocket.Count];
            for (int i = 0; i < bones.Length; i++) bones[i] = rig.Bone((AthleteBone)i);
            for (int i = 0; i < sockets.Length; i++) sockets[i] = rig.Socket((AthleteSocket)i);
            rig.Configure(rig.ModelRoot, bones, sockets, animator);
            var adapter = pivot.AddComponent<AthleteAnimationPresentation>(); adapter.Initialize(rig);
            // Simulate the evaluated animation baseline before LateUpdate; procedural consumers
            // must preserve it instead of replacing every animation channel with bind pose.
            var pose = Quaternion.Euler(15, 25, 0);
            rig.Bone(AthleteBone.Head).localRotation = pose;
            animator.applyRootMotion = true;
            adapter.Render(new AthleteAnimationFrame(default, 1, 20, 1));
            Assert.That(Quaternion.Angle(rig.Bone(AthleteBone.Head).localRotation, pose), Is.LessThan(.001f));
            Assert.That(animator.applyRootMotion, Is.False);
            Object.Destroy(pivot); yield return null;
        }

        [UnityTest]
        public IEnumerator PrefabHasCompleteBindingsAndPoseResetDoesNotDrift()
        {
            var prefab = Resources.Load<AthleteRigBinding>("Biathlon/Athletes/OriginalAthlete");
            Assert.That(prefab, Is.Not.Null, "Generate the repeatable athlete prefab before qualification.");
            var rig = Object.Instantiate(prefab);
            rig.Validate();
            var head = rig.Bone(AthleteBone.Head); var rest = head.localRotation;
            for (int i = 0; i < 100; i++)
            {
                rig.Rotate(AthleteBone.Head, Quaternion.Euler(20, 10, 0)); rig.ResetPose();
                Assert.That(Quaternion.Angle(rest, head.localRotation), Is.LessThan(.001f));
            }
            Assert.Throws<InvalidOperationException>(() => rig.Configure(rig.ModelRoot, new Transform[0], new Transform[0]));
            Object.Destroy(rig.gameObject); yield return null;
        }
    }
}

#if UNITY_EDITOR
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
    public sealed class Biathlete167TransitionTests
    {
        private static string Resource(string id) => Resources.Load<TextAsset>(id).text;
        private static string Core(string name) => Resource("Biathlon/" + Path.GetFileNameWithoutExtension(name));

        [UnityTest]
        public IEnumerator PhysicalRangeApproachKeepsBothAthletesContinuous()
        {
            const string venueRoot = "Biathlon/Venues/woodland_basin_v1/";
            var venue = new VenueAuthoringPlan(VenueManifest.FromJson(Resource(venueRoot + "manifest")), Resource);
            var runner = ProductionVenueRaceFactory.Create(venue, CompetitionProfile.FromJson(Resource(venueRoot + "sprint_profile")),
                RaceRoutePlan.FromJson(Resource(venueRoot + "sprint_route")),
                RaceSnowConditions.FromJson(Core("race_snow_neutral_v1.json")), Core, new BalanceEventAudit(5), new AiDecisionCounts(), count: 2);
            var physical = ManualProductionFieldComposition.Create(runner.Field, venue, Core);
            runner.BindManualScopedCandidate(physical, ManualAiSteeringDefinition.FromJson(Core("manual_ai_steering_v2.json")),
                ManualSkiSteeringDefinition.FromJson(Core("manual_ski_steering_v2.json")),
                PlayerCourseAssistanceDefinition.FromJson(Core("player_course_assistance_v1.json")), completeSnowSearch: true);
            var root = new GameObject("Physical mat transition witness");
            try
            {
                var feed = root.AddComponent<RacePresentationFeed>();
                var view = root.AddComponent<AthleteProxyPresentation>();
                view.Initialize(feed, venue);
                view.enabled = false;
                var previous = new Vector3[2];
                var hasPrevious = new bool[2];
                var phase = new ShootingPhase[2];
                var completed = new bool[2];
                float worst = 0;
                long worstTick = 0;
                var evidence = new System.Text.StringBuilder("tick,athlete,phase,shooting_active,hand_step_m\n");
                feed.Publish(RacePresentationSnapshot.Capture(runner.Field));
                for (int tick = 0; tick < 12000 && !(completed[0] && completed[1]); tick++)
                {
                    runner.Step();
                    feed.Publish(RacePresentationSnapshot.Capture(runner.Field));
                    for (int sample = 1; sample <= 3; sample++)
                    {
                        view.Render(sample / 3d);
                        for (int athlete = 0; athlete < 2; athlete++)
                        {
                            var state = feed.Current.Get(athlete);
                            if (!state.OnCourse) continue;
                            var rig = view.GetRig(state.AthleteId).GetComponent<Biathlete167RacePresentation>().Rig;
                            var hand = rig.GripContact(true).position - view.GetProxy(state.AthleteId).position;
                            float step = Vector3.Distance(previous[athlete], hand);
                            if (hasPrevious[athlete] && tick > 120 && state.SpeedMetersPerSecond < 4 && !completed[athlete] && step > worst)
                            { worst = step; worstTick = feed.Current.Tick; }
                            if (state.SpeedMetersPerSecond < 4)
                                evidence.AppendLine(FormattableString.Invariant($"{feed.Current.Tick},{athlete},{state.ShootingPhase},{state.ShootingActive},{step}"));
                            if (state.ShootingPhase == ShootingPhase.Aiming && phase[athlete] == ShootingPhase.Setup)
                                completed[athlete] = true;
                            phase[athlete] = state.ShootingPhase;
                            previous[athlete] = hand;
                            hasPrevious[athlete] = true;
                            if (athlete == 0 && sample == 3 && (feed.Current.Tick == 3202 || feed.Current.Tick == 3210 || feed.Current.Tick == 3240))
                                yield return CapturePose(rig, "mat-" + feed.Current.Tick);
                        }
                    }
                }
                Directory.CreateDirectory("artifacts/m16-7-continuity");
                File.WriteAllText("artifacts/m16-7-continuity/physical-mat.csv", evidence.ToString());
                Assert.That(completed, Is.All.True, "Both real athletes must reach their first aim through the paid course.");
                Assert.That(worst, Is.LessThan(.09f), $"Hand jumps {worst:F3} m at native tick {worstTick} near the mat.");
                if (File.Exists("artifacts/m16-7-continuity/capture.flag"))
                {
                    var source = view.GetRig(feed.Current.Get(0).AthleteId);
                    var visual = source.GetComponent<Biathlete167RacePresentation>();
                    for (int frame = 0; frame < 280; frame++)
                    {
                        RenderTravel(source, visual, new LocomotionInput(6, 0, .9f,
                            curvature: frame >= 180 && frame < 270 ? .06f : 0), frame / 60d);
                        if (frame == 176 || frame == 186 || frame == 200 || frame == 276)
                            yield return CapturePose(visual.Rig, "turn-" + frame);
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(-.06f, false)]
        [TestCase(.06f, false)]
        [TestCase(0f, true)]
        public void TravelChangesKeepTheRenderedBodyContinuous(float curvature, bool approachingMat)
        {
            var root = new GameObject("Live travel transition witness");
            try
            {
                var source = AthleteRigFactory.Create(root.transform);
                var visual = Biathlete167RacePresentation.Attach(source);
                var driver = source.GetComponent<AthleteHybridLocomotionDriver>();
                var baseline = AthleteRigFactory.Create(root.transform);
                var baselineVisual = Biathlete167RacePresentation.Attach(baseline);
                var anatomy = new[] { HumanBodyBones.Hips, HumanBodyBones.Chest, HumanBodyBones.LeftHand, HumanBodyBones.RightHand };
                var previous = new Vector3[anatomy.Length];
                var previousPosition = new Vector3[anatomy.Length];
                float worstStep = 0;
                float onsetStep = 0;
                int worstFrame = 0, worstBone = 0;
                var evidence = new System.Text.StringBuilder("frame,state,maximum_joint_step_m,onset_offset_step_m\n");
                for (int frame = 0; frame < 360; frame++)
                {
                    bool changed = frame >= 180 && frame < 270;
                    float speed = approachingMat && frame >= 180 ? Mathf.Max(.1f, 6 - (frame - 180) / 60f * 3) : 6;
                    var input = new LocomotionInput(speed, 0, approachingMat && frame >= 180 ? 0 : .9f,
                        curvature: changed ? curvature : 0);
                    RenderTravel(source, visual, input, frame / 60d);
                    RenderTravel(baseline, baselineVisual, new LocomotionInput(speed, 0, .9f), frame / 60d);
                    float step = 0;
                    for (int bone = 0; bone < anatomy.Length; bone++)
                    {
                        var position = visual.Rig.transform.InverseTransformPoint(visual.Rig.Bone(anatomy[bone]).position);
                        var reference = baselineVisual.Rig.transform.InverseTransformPoint(baselineVisual.Rig.Bone(anatomy[bone]).position);
                        var offset = position - reference;
                        float distance = Vector3.Distance(previousPosition[bone], position);
                        if (frame > 120 && distance > worstStep)
                        { worstStep = distance; worstFrame = frame; worstBone = bone; }
                        step = Mathf.Max(step, distance);
                        if (frame >= 180 && frame <= 186)
                            onsetStep = Mathf.Max(onsetStep, Vector3.Distance(previous[bone], offset));
                        previous[bone] = offset;
                        previousPosition[bone] = position;
                    }
                    if (frame >= 120)
                        evidence.AppendLine(FormattableString.Invariant($"{frame},{driver.MotionState},{step},{onsetStep}"));
                }
                const string folder = "artifacts/m16-7-continuity";
                Directory.CreateDirectory(folder);
                File.WriteAllText($"{folder}/travel-{curvature}-{approachingMat}.csv", evidence.ToString());
                Assert.That(worstStep, Is.LessThan(.08f),
                    $"Rendered {anatomy[worstBone]} jumps {worstStep:F3} m at frame {worstFrame} when travel changes.");
                Assert.That(onsetStep, Is.LessThan(.045f), "Starting the turn/coast must blend from the same natural stroke.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static IEnumerator CapturePose(Biathlete167Rig rig, string name)
        {
            const string folder = "artifacts/m16-7-continuity";
            if (!File.Exists(folder + "/capture.flag")) yield break;
            var fixture = new GameObject("Continuity camera and floor");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            try
            {
                foreach (var part in rig.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 26;
                rig.Lods.ForceLOD(0);
                foreach (var renderer in rig.GetComponentsInChildren<MeshRenderer>(true))
                { renderer.enabled = true; renderer.forceRenderingOff = false; }
                var camera = fixture.AddComponent<Camera>();
                camera.enabled = false; camera.cullingMask = 1 << 26;
                camera.fieldOfView = 38;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.14f, .20f, .25f);
                var toe = rig.ToeHinge(false).position;
                toe.y = Mathf.Min(toe.y, rig.ToeHinge(true).position.y);
                var focus = (toe + rig.Bone(HumanBodyBones.Head).position) * .5f;
                var offset = rig.transform.TransformDirection(new Vector3(2.3f, 1.1f, 3.3f));
                camera.transform.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset));
                var light = new GameObject("Witness light").AddComponent<Light>();
                light.transform.SetParent(fixture.transform); light.type = LightType.Directional; light.intensity = 1.6f;
                light.transform.rotation = Quaternion.Euler(35, -30, 0);
                var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(fixture.transform);
                floor.layer = 26; floor.transform.position = new Vector3(toe.x, toe.y - .06f, toe.z);
                floor.transform.localScale = new Vector3(6, .06f, 6);
                material.color = new Color(.70f, .78f, .84f); floor.GetComponent<Renderer>().sharedMaterial = material;
                var quality = fixture.AddComponent<RenderQualityPresentation>();
                quality.Initialize(camera); quality.Apply("high");
                using var target = new BenchmarkRenderTarget(960, 960);
                target.Render(camera); yield return new WaitForEndOfFrame();
                target.Render(camera); yield return new WaitForEndOfFrame();
                target.SavePng(folder + "/" + name + ".png");
            }
            finally { Object.DestroyImmediate(fixture); Object.DestroyImmediate(material); }
        }

        private static void RenderTravel(AthleteRigBinding source, Biathlete167RacePresentation visual, LocomotionInput input, double time)
        {
            var driver = source.GetComponent<AthleteHybridLocomotionDriver>();
            driver.Sample(source, input, time, new ExertionInput(0, 1, 140));
            source.GetComponent<AthleteRiflePresentation>().Sample(source, 0);
            source.GetComponent<AthleteGroundContact>().SampleMoving(source, 0, driver.PoleCycle, driver.ContactState, 0, time);
            source.GetComponent<AthleteGripPresentation>().Render(source);
            visual.RenderPreview();
        }
    }
}
#endif

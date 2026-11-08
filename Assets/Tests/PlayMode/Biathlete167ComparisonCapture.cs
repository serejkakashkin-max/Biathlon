#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    // Explicit capture fixture. Keeps the comparison out of the shipped race and saves.
    public sealed class Biathlete167ComparisonCapture
    {
        private const string Hero = "Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Venue = "Biathlon/Venues/woodland_basin_v1/";
        private static bool HighQuality => File.Exists("artifacts/m16-7-race-integration/capture-hq.flag");
        private static string Output => HighQuality ? "artifacts/m16-7-race-integration/youtube-native" : "artifacts/m16-7-race-integration/native";
        private const int OldLayer = 27, NewLayer = 28;
        private static int Fps => HighQuality ? 30 : 24;
        private static int FeedSize => HighQuality ? 1220 : 720;
        private GameObject fixture;
        private Camera view;
        private Biathlete167Rig hero;
        private Biathlete167Motion motion;
        private AthleteRigBinding original;
        private AthleteHybridLocomotionDriver originalDriver;
        private AthleteRiflePresentation originalRifle;
        private AthleteGroundContact originalContact;
        private AthleteGripPresentation originalGrip;
        private AthleteShootingPose originalShooting;
        private ProductionVenueEnvironment environment;

        [Serializable] private sealed class Shot
        {
            public string id;
            public int frames;
            public Shot(string id, float seconds) { this.id = id; frames = Mathf.RoundToInt(Fps * seconds); }
        }

        [Serializable] private sealed class Receipt
        {
            public int schema_version = 1, fps = Fps, width = FeedSize, height = FeedSize;
            public string id, original_asset = AthleteRigFactory.HeroResourcePath, candidate_asset = Hero + "/Prefabs/Biathlete_16_7_Natural.prefab";
            public Shot[] shots;
        }

        private void Create()
        {
            fixture = new GameObject("Matched M16.7 comparison capture");
            var camera = new GameObject("Matched comparison camera");
            camera.transform.SetParent(fixture.transform, false);
            view = camera.AddComponent<Camera>();
            view.enabled = false;
            view.fieldOfView = 38;
            view.nearClipPlane = .035f;
            view.farClipPlane = 400;
            view.allowHDR = true;
            view.allowMSAA = true;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = new Color(.041f, .063f, .084f);
            Assert.That(Resources.Load<AthleteRigBinding>(AthleteRigFactory.HeroResourcePath), Is.Not.Null,
                "Capture the shipping G2 prefab through the production factory, not its fallback.");
            original = AthleteRigFactory.Create(fixture.transform);
            originalDriver = original.GetComponent<AthleteHybridLocomotionDriver>();
            originalDriver.UseOriginalMotion = false;
            originalRifle = original.GetComponent<AthleteRiflePresentation>();
            originalContact = original.GetComponent<AthleteGroundContact>();
            originalGrip = original.GetComponent<AthleteGripPresentation>();
            originalShooting = original.GetComponent<AthleteShootingPose>();
            original.GetComponent<AthleteSkinDetailBinding>().Prepare(0, true, true);
            var oldAppearance = original.GetComponent<AthleteAppearancePresentation>();
            oldAppearance.ApplyNation("NOR");
            foreach (var behaviour in original.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;

            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Hero + "/Prefabs/Biathlete_16_7_Natural.prefab");
            hero = Object.Instantiate(asset, fixture.transform).GetComponent<Biathlete167Rig>();
            hero.GetComponent<Biathlete167Outfit>().ApplyNation("NOR");
            hero.Lods.ForceLOD(0);
            motion = hero.gameObject.AddComponent<Biathlete167Motion>();
            motion.Initialize(hero, true);
            var appearance = hero.GetComponent<Biathlete167Appearance>();
            oldAppearance.ApplyProfile(appearance.Current);
            SetLayer(original.gameObject, OldLayer);
            SetLayer(hero.gameObject, NewLayer);
            foreach (var skin in original.GetComponentsInChildren<SkinnedMeshRenderer>(true).Concat(hero.GetComponentsInChildren<SkinnedMeshRenderer>(true)))
            {
                skin.updateWhenOffscreen = true;
                skin.forceMatrixRecalculationPerRender = true;
            }
            var quality = fixture.AddComponent<RenderQualityPresentation>();
            quality.Initialize(view);
            quality.Apply("high");
        }

        private static void SetLayer(GameObject root, int layer)
        {
            foreach (var part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = layer;
        }

        private void PutBoth(Vector3 at, Vector3 forward)
        {
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            hero.transform.SetPositionAndRotation(at, rotation);
            original.transform.SetPositionAndRotation(at, rotation);
        }

        private void SampleOriginal(LocomotionInput input, double time, ShootingStance stance = ShootingStance.Standing, float progress = -1)
        {
            var effort = new ExertionInput(0, 1, progress < 0 ? 140 : 60, shooting: progress >= 0);
            originalDriver.Sample(original, input, time, effort);
            if (progress >= 0) originalShooting.Sample(original, stance, progress);
            originalRifle.Sample(original, Mathf.Max(0, progress), default, stance);
            originalContact.SampleMoving(original, input.Grade, originalDriver.PoleCycle, originalDriver.ContactState, originalRifle.HandWeight, time);
            originalDriver.ApplySkiEdges(original);
            originalGrip.Render(original);
        }

        private void Aim(Vector3 focus, Vector3 offset, float fov)
        {
            view.transform.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset, Vector3.up));
            view.fieldOfView = fov;
        }

        private IEnumerator Capture(Receipt receipt, Action<Shot, float, float> pose)
        {
            using var target = new BenchmarkRenderTarget(receipt.width, receipt.height);
            foreach (var shot in receipt.shots)
            {
                string folder = Path.Combine(Output, shot.id);
                Directory.CreateDirectory(folder);
                for (int frame = 0; frame < shot.frames; frame++)
                {
                    float time = frame / (float)Fps;
                    pose(shot, time, frame / (float)(shot.frames - 1));
                    foreach (int layer in new[] { OldLayer, NewLayer })
                    {
                        view.cullingMask = ~((1 << (layer == OldLayer ? NewLayer : OldLayer)) | (1 << 30));
                        environment?.Render(view, "high", time);
                        target.Render(view);
                        yield return new WaitForEndOfFrame();
                        if (frame == 0)
                        {
                            yield return null;
                            environment?.Render(view, "high", time);
                            target.Render(view);
                            yield return new WaitForEndOfFrame();
                        }
                        string model = layer == OldLayer ? "game" : "hero";
                        target.SavePng(Path.Combine(folder, $"{model}-{frame:0000}.png"));
                        yield return null;
                    }
                }
            }
            File.WriteAllText(Path.Combine(Output, receipt.id + ".json"), JsonUtility.ToJson(receipt, true));
        }

        private void ShowSkisOnly()
        {
            var gameSkis = new[] { AthleteSocket.LeftSkiBinding, AthleteSocket.RightSkiBinding }
                .SelectMany(socket => original.Socket(socket).GetComponentsInChildren<MeshRenderer>(false)).ToArray();
            var newSkis = new[] { HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot }
                .Select(bone => hero.Bone(bone).GetComponentsInChildren<MeshFilter>(false)
                    .OrderByDescending(mesh => mesh.sharedMesh.bounds.size.sqrMagnitude).First().GetComponent<MeshRenderer>()).ToArray();
            Assert.That(gameSkis.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(newSkis, Is.All.Not.Null);
            foreach (var renderer in original.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = gameSkis.Contains(renderer);
            foreach (var renderer in hero.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = newSkis.Contains(renderer);
        }

        [UnityTest, Timeout(180000)] public IEnumerator CaptureTravelPoses()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Hero + "/Preview/Biathlete_16_7_Preview.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var studio = Object.FindAnyObjectByType<Biathlete167Studio>();
            studio.Rig.gameObject.SetActive(false);
            studio.View.enabled = false;
            Create();
            try
            {
                PutBoth(Vector3.zero, Vector3.forward);
                var receipt = new Receipt { id = "poses", shots = new[] {
                    new Shot("pose_skate_front", .5f), new Shot("pose_skate_side", .5f),
                    new Shot("pose_sprint_front", .5f), new Shot("pose_sprint_side", .5f),
                    new Shot("pose_uphill_front", .5f), new Shot("pose_uphill_side", .5f) } };
                yield return Capture(receipt, (shot, time, phase) =>
                {
                    bool uphill = shot.id.Contains("uphill"), sprint = shot.id.Contains("sprint");
                    motion.EvaluateAction(uphill ? Biathlete167Action.Uphill : sprint ? Biathlete167Action.Acceleration : Biathlete167Action.Skate, phase);
                    SampleOriginal(new LocomotionInput(motion.Performance.SpeedMetersPerSecond, motion.Performance.SurfaceGrade, sprint ? 1.2f : .9f, sprint:sprint), phase * motion.MainReference.CycleSeconds);
                    Aim(Vector3.up * .9f, shot.id.EndsWith("front") ? new Vector3(.15f, .16f, 3.7f) : new Vector3(3.7f, .16f, .05f), 38);
                });
            }
            finally
            {
                Object.Destroy(fixture);
                studio.Rig.gameObject.SetActive(true);
                studio.View.enabled = true;
            }
        }

        [UnityTest, Timeout(900000)] public IEnumerator CaptureStudio()
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(Hero + "/Preview/Biathlete_16_7_Preview.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var studio = Object.FindAnyObjectByType<Biathlete167Studio>();
            Assert.That(studio, Is.Not.Null);
            studio.Rig.gameObject.SetActive(false);
            studio.View.enabled = false;
            Create();
            try
            {
                // Use the stage and light of the existing character inspection studio.
                view.backgroundColor = studio.View.backgroundColor;
                PutBoth(Vector3.zero, Vector3.forward);
                motion.EvaluateAction(Biathlete167Action.Skate, .17f);
                SampleOriginal(SkiMotionComparison.Input(SkiMotionState.Flat), .17f * motion.MainReference.CycleSeconds);
                var looks = AthleteLookCatalog.Load();
                var appearance = hero.GetComponent<Biathlete167Appearance>();
                var start = appearance.Current;
                var clearFace = looks.Edit(start, gogglesId: "raised");
                var receipt = new Receipt { id = "studio", shots = new[] {
                    new Shot("orbit", 6), new Shot("face", 3.5f), new Shot("rifle", 3.5f), new Shot("skis", 3.5f), new Shot("skis_top", 4) } };
                yield return Capture(receipt, (shot, time, phase) =>
                {
                    if (shot.id == "face" && time == 0)
                    {
                        motion.Stop();
                        original.ResetPose();
                        originalRifle.Sample(original, 0);
                        appearance.ApplyProfile(clearFace);
                        original.GetComponent<AthleteAppearancePresentation>().ApplyProfile(clearFace);
                    }
                    if (shot.id == "rifle" && time == 0)
                    {
                        appearance.ApplyProfile(start);
                        original.GetComponent<AthleteAppearancePresentation>().ApplyProfile(start);
                    }
                    if (shot.id == "skis_top")
                    {
                        if (time == 0)
                        {
                            ShowSkisOnly();
                            var fill = new GameObject("Top ski detail light").AddComponent<Light>();
                            fill.transform.SetParent(fixture.transform, false);
                            fill.transform.rotation = Quaternion.Euler(72, -25, 0);
                            fill.type = LightType.Directional;
                            fill.color = new Color(.96f, .97f, 1);
                            fill.intensity = .9f;
                            fill.cullingMask = (1 << OldLayer) | (1 << NewLayer);
                        }
                        float close = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.46f, .95f, phase));
                        var focus = new Vector3(0, .04f, Mathf.Lerp(.03f, .29f, close));
                        var offset = new Vector3(Mathf.Lerp(.12f, -.12f, phase), Mathf.Lerp(2.45f, 1.36f, close), -.14f);
                        view.transform.SetPositionAndRotation(focus + offset, Quaternion.LookRotation(-offset, Vector3.forward) * Quaternion.Euler(0, 0, Mathf.Lerp(-11, 11, phase)));
                        view.fieldOfView = 50;
                        return;
                    }
                    if (shot.id == "orbit")
                    {
                        motion.EvaluateAction(Biathlete167Action.Skate, Mathf.Repeat(time / motion.CycleSeconds, 1));
                        SampleOriginal(SkiMotionComparison.Input(SkiMotionState.Flat), time);
                    }
                    float angle, distance, height, rise, fov;
                    switch (shot.id)
                    {
                        case "face": angle = Mathf.Lerp(35, -35, phase); distance = .79f; height = 1.64f; rise = .03f; fov = 40; break;
                        case "rifle": angle = Mathf.Lerp(140, 205, phase); distance = 1.92f; height = 1.23f; rise = .16f; fov = 37; break;
                        case "skis": angle = Mathf.Lerp(66, 113, phase); distance = 2.54f; height = .19f; rise = .44f; fov = 44; break;
                        default: angle = 25 + phase * 360; distance = 4.16f; height = .97f; rise = .24f; fov = 37; break;
                    }
                    float radians = angle * Mathf.Deg2Rad;
                    Aim(Vector3.up * height, new Vector3(Mathf.Sin(radians) * distance, rise, Mathf.Cos(radians) * distance), fov);
                });
            }
            finally
            {
                Object.Destroy(fixture);
                studio.Rig.gameObject.SetActive(true);
                studio.View.enabled = true;
            }
        }

        [UnityTest, Timeout(900000)] public IEnumerator CaptureWoodland()
        {
            // A fresh scene prevents studio lights and another camera from affecting the snow.
            var previous = SceneManager.GetActiveScene();
            var previousStudio = Object.FindAnyObjectByType<Biathlete167Studio>();
            var stageRoots = previousStudio == null ? Array.Empty<GameObject>() :
                previousStudio.gameObject.scene.GetRootGameObjects().Where(root => root.activeSelf && root != previousStudio.transform.root.gameObject).ToArray();
            foreach (var root in stageRoots) root.SetActive(false);
            var scene = SceneManager.CreateScene("M16.7 Woodland comparison");
            SceneManager.SetActiveScene(scene);
            Create();
            try
            {
                var plan = new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(Venue + "manifest")), ProductionVenueHost.ReadResource);
                var definition = ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(Venue + "environment"));
                var course = fixture.AddComponent<ProductionCoursePresentation>();
                var shoulders = ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(definition.terrain.snow_shoulders_resource), plan);
                course.Initialize(plan, sharedApron: plan.SharedApron, snowShoulders: shoulders);
                environment = fixture.AddComponent<ProductionVenueEnvironment>();
                environment.AutomaticRendering = false;
                environment.Initialize(plan, definition, ProductionVenueHost.ReadResource, fixture.AddComponent<RacePresentationFeed>(), view, apron: course.Apron);
                course.JoinGround(environment, definition.terrain.grid_spacing_m, environment.TerrainGridOrigin);
                if (!string.IsNullOrEmpty(definition.terrain.snow_material_preset))
                    SnowMaterialDefinition.Load().ConfigureBlended(course.GroomedMaterial, definition.terrain.snow_material_preset, definition.terrain.snow_surface_profile);
                var mats = fixture.AddComponent<RangeMatPresentation>();
                mats.Initialize(plan);
                view.clearFlags = CameraClearFlags.Skybox;
                var mat = mats.Anchor(15);
                var main = plan.Visual.Get("main");
                var lane = plan.Paths.GetLane(14);
                var laneLoop = plan.Visual.Get(lane.MatAnchor.LoopId);
                var laneStop = lane.MatAnchor.Distance.Meters;
                motion.EvaluateAction(Biathlete167Action.Uphill, 0);
                float authoredUphillGrade = motion.Performance.SurfaceGrade;
                motion.Stop();
                var receipt = new Receipt { id = "woodland", shots = new[] {
                    new Shot("skate", 4), new Shot("skate_front", 4),
                    new Shot("acceleration", 4), new Shot("acceleration_front", 4),
                    new Shot("uphill", 4), new Shot("uphill_front", 4), new Shot("entry", 4),
                    new Shot("standing", 4.5f), new Shot("prone", 5) } };
                yield return Capture(receipt, (shot, time, phase) =>
                {
                    string technique = shot.id.Replace("_front", "");
                    if (technique is "skate" or "acceleration" or "uphill")
                    {
                        bool uphill = technique == "uphill", acceleration = technique == "acceleration";
                        double metres = (uphill ? 139 : acceleration ? 371 : 246) + (acceleration ? 3 * time + .58 * time * time : (uphill ? 4.2 : 6.0) * time);
                        var p = main.Sample(metres);
                        var before = main.Sample(metres - .4); var after = main.Sample(metres + .4);
                        var forward = new Vector3((float)(after.X-before.X), 0, (float)(after.Z-before.Z)).normalized;
                        var at = new Vector3((float)p.X, (float)p.Y, (float)p.Z);
                        PutBoth(at, forward);
                        float grade = (float)((after.Y-before.Y) / .8);
                        // The prototype's authored ski plane is local to its root. Rotate
                        // that plane to the sampled slope for this presentation fixture.
                        float authoredGrade = uphill ? authoredUphillGrade : 0;
                        float pitch = (Mathf.Atan(authoredGrade) - Mathf.Atan(grade)) * Mathf.Rad2Deg;
                        hero.transform.rotation *= Quaternion.Euler(pitch, 0, 0);
                        var action = uphill ? Biathlete167Action.Uphill : acceleration ? Biathlete167Action.Acceleration : Biathlete167Action.Skate;
                        motion.EvaluateAction(action, acceleration ? phase : Mathf.Repeat(time / Mathf.Max(.35f, motion.CycleSeconds), 1));
                        float speed = uphill ? 4.2f : acceleration ? 3 + 1.16f * time : 6;
                        SampleOriginal(new LocomotionInput(speed, grade, uphill ? .95f : 1f, sprint: acceleration), time);
                        float sweep = Mathf.Lerp(-.65f, .85f, phase);
                        var cameraOffset = shot.id.EndsWith("_front") ? new Vector3(Mathf.Lerp(.4f, -.4f, phase), .63f, 4.8f) :
                            new Vector3(4.5f, .63f, sweep * 2.5f);
                        Aim(at + Vector3.up * .87f, original.transform.TransformDirection(cameraOffset), 40);
                        return;
                    }
                    if (shot.id == "entry")
                    {
                        float travelled = 1 - Mathf.Pow(1 - Mathf.Min(1, phase / .72f), 2);
                        double along = -4.4 + (4.4 + laneStop) * travelled;
                        TrackVisualPoint At(double position) => position < 0 ? main.Sample(lane.Fork.Meters + position) : laneLoop.Sample(Math.Min(position, laneStop));
                        var point = At(along);
                        var at = new Vector3((float)point.X, (float)point.Y, (float)point.Z);
                        var trackDirection = new Vector3((float)point.ForwardX, 0, (float)point.ForwardZ);
                        var facing = Vector3.Slerp(trackDirection, mat.forward, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.49f, .72f, phase)));
                        PutBoth(at, facing);
                        motion.EvaluateAction(Biathlete167Action.RangeEntry, phase);
                        SampleOriginal(new LocomotionInput(motion.Performance.SpeedMetersPerSecond, 0, .15f, travelling: phase < .72f), time);
                        var offset = mat.TransformDirection(new Vector3(4.6f, 1.15f, -2.1f));
                        Aim(at + Vector3.up * .8f, offset, 42);
                        return;
                    }
                    bool prone = shot.id == "prone";
                    PutBoth(mat.position, mat.forward);
                    float setup = Mathf.Clamp01(time / (prone ? 3.2f : 2.85f));
                    var stance = prone ? ShootingStance.Prone : ShootingStance.Standing;
                    motion.EvaluateAction(setup < 1 ? (prone ? Biathlete167Action.EnterProne : Biathlete167Action.EnterStanding) :
                        (prone ? Biathlete167Action.Prone : Biathlete167Action.Standing), setup < 1 ? setup : Mathf.Repeat(time, 1));
                    SampleOriginal(new LocomotionInput(0, 0, 0, travelling: false), time, stance, setup);
                    float focus = prone ? Mathf.Lerp(.9f, .4f, Mathf.SmoothStep(0, 1, setup)) : .89f;
                    Aim(mat.position + Vector3.up * focus, hero.transform.TransformDirection(new Vector3(3.95f, .92f, Mathf.Lerp(-.62f, 1.03f, phase))), 44);
                });
            }
            finally
            {
                Object.Destroy(fixture);
                SceneManager.SetActiveScene(previous);
                SceneManager.UnloadSceneAsync(scene);
                foreach (var root in stageRoots) if (root != null) root.SetActive(true);
            }
        }
    }
}
#endif

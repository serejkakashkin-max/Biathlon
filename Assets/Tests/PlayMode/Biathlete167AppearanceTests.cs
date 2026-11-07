#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.Career;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167AppearanceTests
    {
        private const string Root = "Assets/Biathlon/Characters/Biathlete_16_7";
        private Biathlete167Rig rig;
        private Biathlete167Appearance appearance;
        private SkinnedMeshRenderer[] skins;
        private AppearanceDefinition definition;

        [SetUp] public void Create()
        {
            rig = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                Root + "/Prefabs/Biathlete_16_7_Natural.prefab")).GetComponent<Biathlete167Rig>();
            appearance = rig.GetComponent<Biathlete167Appearance>();
            Assert.That(appearance, Is.Not.Null);
            skins = rig.Lods.GetLODs().SelectMany(level => level.renderers).OfType<SkinnedMeshRenderer>().ToArray();
            definition = AthleteLookCatalog.Load();
        }
        [TearDown] public void Cleanup() { if (rig != null) Object.DestroyImmediate(rig.gameObject); }
        private GameObject Part(string name) => rig.GetComponentsInChildren<Transform>(true).Single(t => t.name == name).gameObject;

        [Test] public void AllCareerFacesHaveBoundedImportedShapesAtEveryLod()
        {
            foreach (var skin in skins)
            {
                var mesh = skin.sharedMesh;
                var delta = new Vector3[mesh.vertexCount];
                var vertices = mesh.vertices;
                var protectedIndices = Enumerable.Range(0, vertices.Length).Where(i =>
                {
                    float height = rig.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i])).y;
                    return height < 1.57f || height > 1.69f;
                }).ToArray();
                int pupils = Array.FindIndex(skin.sharedMaterials, material => material.name == "Eye_Pupil");
                var pupilIndices = mesh.GetTriangles(pupils).Distinct().ToArray();
                var facialDetailIndices = Enumerable.Range(0, vertices.Length).Where(i =>
                {
                    var point = rig.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i]));
                    return Mathf.Abs(point.x) < .033f && point.y > 1.58f && point.y < 1.67f && point.z > .035f;
                }).ToArray();
                Assert.That(facialDetailIndices.Length, Is.GreaterThan(0));
                foreach (var face in AthleteFaceCatalog.Load().profiles)
                {
                    int index = mesh.GetBlendShapeIndex(face.ShapeName);
                    Assert.That(index, Is.GreaterThanOrEqualTo(0), face.id);
                    mesh.GetBlendShapeFrameVertices(index, 0, delta, null, null);
                    float maximum = delta.Max(v => v.magnitude);
                    Assert.That(maximum, Is.LessThanOrEqualTo(.0151f));
                    Assert.That(protectedIndices.All(i => delta[i].sqrMagnitude < .0000000001f), Is.True,
                        "Face choices must preserve the neck, scalp contact and all non-facial anatomy.");
                    Assert.That(pupilIndices.All(i => delta[i].sqrMagnitude < .0000000001f), Is.True,
                        "The eye anchor must remain compatible with the shooting fit.");
                    Assert.That(facialDetailIndices.All(i => delta[i].sqrMagnitude < .0000000001f), Is.True,
                        "Nostril and lip detail must not tear when changing identity.");
                    if (face.id != "oval") Assert.That(maximum, Is.GreaterThan(.002f), "A named face must actually alter the mesh.");
                }
                Assert.That(mesh.GetBlendShapeIndex("HeadwearHidden"), Is.GreaterThanOrEqualTo(0));
            }
        }

        [Test] public void PersonalChoicesReuseSharedMaterialsAndPreserveTheProfile()
        {
            int before = Resources.FindObjectsOfTypeAll<Material>().Length;
            var faces = AthleteFaceCatalog.Load();
            var start = appearance.Current;
            string original = start.ToJson();
            var position = rig.transform.position;
            for (int i = 0; i < 24; i++)
            {
                var profile = definition.Edit(start, faceId: faces.profiles[i % 6].id, skinId: faces.skin_ids[i % 8],
                    headwearId: i % 3 == 0 ? "cap" : i % 3 == 1 ? "headband" : "none",
                    hairStyleId: definition.Option("hair_style", i % 2).Id,
                    hairColorId: definition.Option("hair_color", i % 6).Id,
                    eyeColorId: definition.Option("eye_color", i % 4).Id,
                    glovesId: definition.Option("gloves", i % 3).Id, bootsId: definition.Option("boots", i % 3).Id);
                appearance.ApplyProfile(profile);
                Assert.That(appearance.Current.ToJson(), Is.EqualTo(profile.ToJson()));
                Assert.That(Part("Headband_Prop").activeSelf, Is.EqualTo(profile.HeadwearId == "headband"));
                Assert.That(Part("Hair_Crew_Prop").activeSelf, Is.EqualTo(profile.HeadwearId != "cap" && profile.HairStyleId == "crew"));
                Assert.That(Part("Hair_Short_Prop").activeSelf, Is.EqualTo(profile.HeadwearId != "cap" && profile.HairStyleId == "short_swept"));
                foreach (var skin in skins)
                {
                    Assert.That(skin.GetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex("FaceIdentity_" + profile.FaceId)), Is.EqualTo(100));
                    Assert.That(skin.GetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex("HeadwearHidden")), Is.EqualTo(profile.HeadwearId == "cap" ? 0 : 100));
                }
            }
            Assert.That(start.ToJson(), Is.EqualTo(original));
            Assert.That(rig.transform.position, Is.EqualTo(position));
            Assert.That(Resources.FindObjectsOfTypeAll<Material>().Length, Is.EqualTo(before));
        }

        [Test] public void NationalKitChangesPreserveFaceColorsAndHeadband()
        {
            var profile = definition.Edit(appearance.Current, faceId: "narrow", skinId: "brown_warm",
                hairColorId: "blond", headwearId: "headband");
            appearance.ApplyProfile(profile);
            var outfit = rig.GetComponent<Biathlete167Outfit>();
            var selectedSkin = skins[0].sharedMaterials.Single(m => m.name == "Anatomical_Skin_CC0_2K");
            foreach (var nation in outfit.Nations)
            {
                outfit.ApplyNation(nation);
                Assert.That(appearance.Current.ToJson(), Is.EqualTo(profile.ToJson()));
                Assert.That(Part("Headband_Prop").GetComponent<Renderer>().sharedMaterial, Is.SameAs(outfit.HeadwearMaterial));
                foreach (var skin in skins)
                    Assert.That(skin.sharedMaterials.Single(m => m.name == "Anatomical_Skin_CC0_2K"), Is.SameAs(selectedSkin));
            }
        }

        [Test] public void BareHeadRemovesTheActualCapSurfaceAndRestoresIt()
        {
            var skin = skins[0];
            int red = Array.FindIndex(skin.sharedMaterials, m => m.name == "PULSE_Race_Red");
            var indices = skin.sharedMesh.GetTriangles(red).Distinct().ToArray();
            var baked = new Mesh();
            float Height()
            {
                skin.BakeMesh(baked);
                var vertices = baked.vertices;
                return indices.Max(i => rig.transform.InverseTransformPoint(skin.transform.TransformPoint(vertices[i])).y);
            }
            try
            {
                rig.SetPose(0);
                float cap = Height();
                appearance.ApplyProfile(definition.Edit(appearance.Current, headwearId: "none"));
                float bare = Height();
                Assert.That(cap - bare, Is.GreaterThan(.015f), "The cap must leave the visible scalp.");
                appearance.ApplyProfile(definition.Edit(appearance.Current, headwearId: "cap"));
                Assert.That(Height(), Is.EqualTo(cap).Within(.00002f));
            }
            finally { Object.DestroyImmediate(baked); }
        }

        [Test] public void LegacyAndExplicitProfilesResolveWithoutMutatingTheSave()
        {
            var legacy = AppearanceProfile.Legacy("legacy_hero_appearance");
            string json = legacy.ToJson();
            AthleteFaceCatalog.Load().Resolve(legacy.LegacyIdentity, out int face, out int tone);
            appearance.ApplyProfile(legacy);
            Assert.That(appearance.Current.FaceId, Is.EqualTo(AthleteFaceCatalog.Load().profiles[face].id));
            Assert.That(appearance.Current.SkinId, Is.EqualTo(AthleteFaceCatalog.Load().skin_ids[tone]));
            Assert.That(appearance.Current.BodyId, Is.EqualTo(
                AthleteFaceCatalog.Load().body_ids[AthleteFaceCatalog.Hash(legacy.LegacyIdentity) % 3]));
            Assert.That(legacy.ToJson(), Is.EqualTo(json));
            appearance.ApplyProfile(AppearanceProfile.Create("round", "olive", "athletic"));
            Assert.That(appearance.Current.FaceId, Is.EqualTo("round"));
            string before = appearance.Current.ToJson();
            Assert.Throws<ArgumentException>(() => appearance.ApplyProfile(AppearanceProfile.Create("unknown_face", "olive", "athletic")));
            Assert.That(appearance.Current.ToJson(), Is.EqualTo(before));
        }

        [Test] public void HeadwearAndEyewearChoicesSurviveAnimationScrubbing()
        {
            var motion = rig.gameObject.AddComponent<Biathlete167Motion>();
            motion.Initialize(rig, true);
            foreach (var headwear in new[] { "cap", "headband", "none" })
            {
                appearance.ApplyProfile(definition.Edit(appearance.Current, headwearId: headwear, gogglesId: "raised"));
                foreach (var action in new[] { Biathlete167Action.Skate, Biathlete167Action.Uphill, Biathlete167Action.Standing, Biathlete167Action.Prone })
                    foreach (float phase in new[] { 0f, .3f, .8f, 1f })
                    {
                        motion.EvaluateAction(action, phase);
                        Assert.That(motion.Performance.EyewearLift, Is.EqualTo(1));
                        Assert.That(Part("Headband_Prop").activeSelf, Is.EqualTo(headwear == "headband"));
                        Assert.That(appearance.Current.HeadwearId, Is.EqualTo(headwear));
                    }
            }
            appearance.ApplyProfile(definition.Edit(appearance.Current, gogglesId: "absent"));
            rig.SetEyewear(Biathlete167Eyewear.Mask);
            Assert.That(rig.EyewearRoot(Biathlete167Eyewear.Mask).gameObject.activeSelf, Is.False);
            Assert.That(rig.EyewearRoot(Biathlete167Eyewear.Glasses).gameObject.activeSelf, Is.False);
        }

        [UnityTest] public IEnumerator CaptureFacesAndHeadwearInUnity()
        {
            var studio = Object.FindAnyObjectByType<Biathlete167Studio>();
            if (studio == null)
            {
                yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    Root + "/Preview/Biathlete_16_7_Preview.unity",
                    new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                yield return null;
                studio = Object.FindAnyObjectByType<Biathlete167Studio>();
            }
            if (rig == null) Create();
            studio.Rig.gameObject.SetActive(false);
            var cameraObject = new GameObject("Appearance inspection camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(studio.View);
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = .195f;
            var motion = rig.gameObject.AddComponent<Biathlete167Motion>();
            motion.Initialize(rig, true);
            string folder = "artifacts/m16-7-appearance-20261107/native";
            Directory.CreateDirectory(folder);
            try
            {
                using var target = new BenchmarkRenderTarget(900, 1000);
                IEnumerator Capture(string file, float angle)
                {
                    var focus = rig.Bone(HumanBodyBones.Head).position + Vector3.up * .031f;
                    camera.transform.position = focus + new Vector3(angle, .035f, .8f);
                    camera.transform.LookAt(focus);
                    yield return null;
                    target.Render(camera);
                    yield return new WaitForEndOfFrame();
                    target.Render(camera);
                    target.SavePng(folder + "/" + file + ".png");
                }
                foreach (var face in AthleteFaceCatalog.Load().profiles)
                {
                    appearance.ApplyProfile(definition.Edit(appearance.Current, faceId: face.id, skinId: "light_neutral",
                        headwearId: "none", hairStyleId: "short_swept", gogglesId: "absent"));
                    yield return Capture("face-" + face.id, .08f);
                }
                foreach (var headwear in new[] { "cap", "headband", "none" })
                    foreach (var goggles in new[] { "worn", "raised" })
                    {
                        appearance.ApplyProfile(definition.Edit(appearance.Current, faceId: "oval", skinId: "light_neutral",
                            headwearId: headwear, hairStyleId: "crew", hairColorId: "blond", gogglesId: goggles));
                        motion.Stop();
                        rig.SetPose(0);
                        motion.RefreshAppearance();
                        yield return Capture(headwear + "-" + goggles, .38f);
                        motion.Stop();
                    }
                foreach (var tone in new[] { "tan_warm", "olive", "brown_deep", "dark_deep" })
                {
                    appearance.ApplyProfile(definition.Edit(appearance.Current, skinId: tone, headwearId: "none",
                        gogglesId: "absent", hairColorId: "black"));
                    yield return Capture("skin-" + tone, .08f);
                }
                var ui = studio.GetComponent<UIDocument>().rootVisualElement;
                ui.Q<DropdownField>("athlete-headwear").index = 1;
                ui.Q<DropdownField>("athlete-face").index = 5;
                Assert.That(studio.Rig.GetComponent<Biathlete167Appearance>().Current.HeadwearId, Is.EqualTo("headband"));
                Assert.That(studio.Rig.GetComponent<Biathlete167Appearance>().Current.FaceId, Is.EqualTo("defined_chin"));
            }
            finally
            {
                studio.Rig.gameObject.SetActive(true);
                Object.Destroy(cameraObject);
            }
        }
    }
}
#endif

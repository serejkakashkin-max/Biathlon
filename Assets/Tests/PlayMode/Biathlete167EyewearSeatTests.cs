#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167EyewearSeatTests
    {
        private const string Root = "Assets/Biathlon/Characters/Biathlete_16_7";

        [TestCase("cap"), TestCase("headband"), TestCase("none")]
        public void RaisedGlassesRestOnTheActualHeadInsteadOfFloatingAboveIt(string headwear)
        {
            var hero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Biathlete_16_7_Natural.prefab"));
            var baked = new Mesh();
            var surface = new Mesh();
            var probe = new GameObject("Eyewear seating surface probe");
            try
            {
                var rig = hero.GetComponent<Biathlete167Rig>();
                var motion = hero.AddComponent<Biathlete167Motion>();
                motion.Initialize(rig, true);
                motion.Stop();
                var appearance = hero.GetComponent<Biathlete167Appearance>();
                appearance.ApplyProfile(AthleteLookCatalog.Load().Edit(appearance.Current, headwearId: headwear));
                var eyewear = rig.EyewearRoot(Biathlete167Eyewear.Glasses);
                var lens = eyewear.GetComponentsInChildren<Renderer>(true).Single(r => r.sharedMaterials.Any(m => m.name == "Amber_Mirror"));
                var mesh = lens.GetComponent<MeshFilter>().sharedMesh;
                int sub = Array.FindIndex(lens.sharedMaterials, m => m.name == "Amber_Mirror");
                var points = mesh.vertices;
                var ids = mesh.GetTriangles(sub).Distinct().ToArray();
                var anchors = new List<Vector3>();
                foreach (float x in new[] { -.045f, 0f, .045f })
                {
                    int index = ids.Where(i => Mathf.Abs(hero.transform.InverseTransformPoint(lens.transform.TransformPoint(points[i])).x - x) < .012f)
                        .OrderBy(i => hero.transform.InverseTransformPoint(lens.transform.TransformPoint(points[i])).y).First();
                    anchors.Add(points[index]);
                }
                var skin = rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();
                probe.transform.SetParent(skin.transform, false);
                skin.BakeMesh(baked);
                surface.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                surface.vertices = baked.vertices;
                var triangles = new List<int>();
                for (int i = 0; i < skin.sharedMaterials.Length; i++)
                    if (skin.sharedMaterials[i].name is "Anatomical_Skin_CC0_2K" or "PULSE_Race_Red")
                        triangles.AddRange(skin.sharedMesh.GetTriangles(i));
                surface.triangles = triangles.ToArray();
                var collider = probe.AddComponent<MeshCollider>();
                collider.sharedMesh = surface;
                appearance.ApplyProfile(AthleteLookCatalog.Load().Edit(appearance.Current, gogglesId: "raised"));
                var head = rig.Bone(HumanBodyBones.Head);
                var centreInHead = head.InverseTransformPoint(head.position + hero.transform.up * .06f);
                var seatedPosition = head.InverseTransformPoint(eyewear.position);
                var seatedRotation = Quaternion.Inverse(head.rotation) * eyewear.rotation;
                float maximum = 0, minimum = float.MaxValue;
                void CheckSeat()
                {
                    skin.BakeMesh(baked);
                    collider.sharedMesh = null;
                    surface.vertices = baked.vertices;
                    collider.sharedMesh = surface;
                    foreach (var anchor in anchors)
                    {
                        var point = lens.transform.TransformPoint(anchor);
                        var ray = new Ray(point, head.TransformPoint(centreInHead) - point);
                        Assert.That(collider.Raycast(ray, out var hit, .3f), Is.True, "Raised lower lens rim must remain outside the head.");
                        maximum = Mathf.Max(maximum, hit.distance);
                        minimum = Mathf.Min(minimum, hit.distance);
                    }
                    Assert.That(Vector3.Distance(head.InverseTransformPoint(eyewear.position), seatedPosition), Is.LessThan(.0001f));
                    Assert.That(Quaternion.Angle(Quaternion.Inverse(head.rotation) * eyewear.rotation, seatedRotation), Is.LessThan(.03f));
                }
                CheckSeat();
                foreach (var action in new[] { Biathlete167Action.Skate, Biathlete167Action.Uphill, Biathlete167Action.Standing, Biathlete167Action.Prone })
                    foreach (float phase in new[] { 0f, .35f, .7f, 1f })
                    {
                        motion.EvaluateAction(action, phase);
                        for (int repeat = 0; repeat < 4; repeat++) motion.RefreshAppearance();
                        CheckSeat();
                    }
                string folder = "artifacts/m16-7-eyewear-seat-20261107";
                Directory.CreateDirectory(folder);
                File.WriteAllText(folder + "/seat-" + headwear + ".json", JsonUtility.ToJson(new SeatReport { headwear = headwear, minimum_gap_m = minimum, maximum_gap_m = maximum }, true));
                Assert.That(maximum, Is.LessThan(.012f), "Raised lower lens rim floats " + (maximum * 1000).ToString("F1") + " mm from the actual head/cap.");
            }
            finally
            {
                Object.DestroyImmediate(probe); Object.DestroyImmediate(surface); Object.DestroyImmediate(baked); Object.DestroyImmediate(hero);
            }
        }
        [Serializable] private sealed class SeatReport
        {
            public string headwear;
            public float minimum_gap_m, maximum_gap_m;
        }
    }
}
#endif

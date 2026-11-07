#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class Biathlete167EyewearSeatTests
    {
        private const string Root = "Assets/Biathlon/Characters/Biathlete_16_7";

        [TestCase("cap", "crew"), TestCase("headband", "crew"), TestCase("headband", "short_swept")]
        [TestCase("none", "crew"), TestCase("none", "short_swept")]
        public void RearOfRaisedGlassesHugsTheActualHead(string headwear, string hairstyle)
        {
            var hero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Biathlete_16_7_Natural.prefab"));
            var baked = new Mesh(); var surface = new Mesh(); var probe = new GameObject("Rear glasses surface probe");
            try
            {
                var rig = hero.GetComponent<Biathlete167Rig>();
                var motion = hero.AddComponent<Biathlete167Motion>(); motion.Initialize(rig, true); motion.Stop();
                var appearance = hero.GetComponent<Biathlete167Appearance>();
                var looks = AthleteLookCatalog.Load();
                appearance.ApplyProfile(looks.Edit(appearance.Current, headwearId: headwear, hairStyleId: hairstyle, gogglesId: "raised"));
                var skin = rig.Lods.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().Single();
                probe.transform.SetParent(skin.transform, false); skin.BakeMesh(baked);
                surface.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; surface.vertices = baked.vertices;
                var triangles = new List<int>();
                for (int i = 0; i < skin.sharedMaterials.Length; i++)
                    if (skin.sharedMaterials[i].name is "Anatomical_Skin_CC0_2K" or "PULSE_Race_Red") triangles.AddRange(skin.sharedMesh.GetTriangles(i));
                surface.triangles = triangles.ToArray(); var collider = probe.AddComponent<MeshCollider>(); collider.sharedMesh = surface;
                var glasses = rig.EyewearRoot(Biathlete167Eyewear.Glasses);
                var renderer = glasses.GetComponentsInChildren<Renderer>(true)
                    .FirstOrDefault(r => r.sharedMaterials.Any(m => m.name == "PULSE_Glasses_Elastic")) ?? glasses.GetComponent<Renderer>();
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var props = new List<MeshCollider>();
                foreach (var part in hero.GetComponentsInChildren<MeshFilter>(false))
                {
                    if (part.name is not ("Headband_Prop" or "Hair_Crew_Prop" or "Hair_Short_Prop")) continue;
                    var partProbe = new GameObject("Outer headwear surface probe");
                    partProbe.transform.SetParent(part.transform, false);
                    var partCollider = partProbe.AddComponent<MeshCollider>();
                    partCollider.sharedMesh = part.sharedMesh;
                    props.Add(partCollider);
                }
                var head = rig.Bone(HumanBodyBones.Head);
                var targetInHead = head.InverseTransformPoint(head.position + hero.transform.up * .06f);
                var forwardInHead = head.InverseTransformDirection(hero.transform.forward);
                var lens = glasses.GetComponent<MeshFilter>().sharedMesh;
                var lensVertices = lens.vertices; var lensUvs = lens.uv;
                int lensSlot = Array.FindIndex(glasses.GetComponent<Renderer>().sharedMaterials, m => m.name == "Amber_Mirror");
                var lensIndices = lens.GetTriangles(lensSlot).Distinct().ToArray();
                Vector3 Hinge(float u)
                {
                    var edge = lensIndices.Where(i => Mathf.Abs(lensUvs[i].x-u) < .0001f).Select(i => lensVertices[i]).ToArray();
                    Assert.That(edge.Length, Is.GreaterThan(0));
                    return edge.Aggregate(Vector3.zero,(sum,p)=>sum+p)/edge.Length;
                }
                Vector3 leftHinge = Hinge(0), rightHinge = Hinge(1);
                float maximum = 0;
                void CheckFit()
                {
                    skin.BakeMesh(baked); collider.sharedMesh = null; surface.vertices = baked.vertices; collider.sharedMesh = surface;
                    var points = mesh.vertices.Select(renderer.transform.TransformPoint).ToArray();
                    // Inspect the back and the rear quarters, including both edges of the band.
                    var rear = points.OrderBy(p => Vector3.Dot(head.InverseTransformPoint(p), forwardInHead)).Take(16).ToArray();
                    var target = head.TransformPoint(targetInHead);
                    foreach (var point in rear)
                    {
                        var ray = new Ray(point, target-point);
                        Assert.That(collider.Raycast(ray, out var hit, .3f), Is.True, "Rear strap must stay outside the head.");
                        float distance = hit.distance;
                        foreach (var part in props)
                            if (part.Raycast(ray, out var outer, distance)) distance = outer.distance;
                        maximum = Mathf.Max(maximum, distance);
                    }
                    Assert.That(maximum, Is.LessThan(.006f), "Rear glasses band floats " + (maximum*1000).ToString("F1") + " mm from the actual head.");
                    Assert.That(points.Min(p=>Vector3.Distance(p,glasses.TransformPoint(leftHinge))), Is.LessThan(.004f), "Left end must meet the lens frame.");
                    Assert.That(points.Min(p=>Vector3.Distance(p,glasses.TransformPoint(rightHinge))), Is.LessThan(.004f), "Right end must meet the lens frame.");
                }
                CheckFit();
                motion.EvaluateAction(Biathlete167Action.Prone, .5f); CheckFit();
                appearance.ApplyProfile(looks.Edit(appearance.Current, gogglesId: "worn"));
                foreach (float phase in new[] { .58f, .625f, .68f, .735f, .79f, .83f })
                {
                    motion.EvaluateAction(Biathlete167Action.RangeEntry, phase); CheckFit();
                }
                string folder="artifacts/m16-7-glasses-rear-20261107"; Directory.CreateDirectory(folder);
                File.WriteAllText(folder+"/seat-"+headwear+"-"+hairstyle+".json", JsonUtility.ToJson(new SeatReport { headwear=headwear, maximum_gap_m=maximum },true));
            }
            finally { Object.DestroyImmediate(probe); Object.DestroyImmediate(surface); Object.DestroyImmediate(baked); Object.DestroyImmediate(hero); }
        }

        [UnityTest] public IEnumerator CaptureGlassesFromSideAndBack()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Root + "/Preview/Biathlete_16_7_Preview.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            yield return null;
            var studio = Object.FindAnyObjectByType<Biathlete167Studio>(); studio.Rig.gameObject.SetActive(false);
            var hero = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/Biathlete_16_7_Natural.prefab"));
            var cameraObject = new GameObject("Rear glasses inspection camera"); var camera = cameraObject.AddComponent<Camera>();
            camera.CopyFrom(studio.View); camera.enabled=false; camera.orthographic=true; camera.orthographicSize=.195f;
            try
            {
                var rig = hero.GetComponent<Biathlete167Rig>(); var motion = hero.AddComponent<Biathlete167Motion>(); motion.Initialize(rig,true); motion.Stop();
                var appearance=hero.GetComponent<Biathlete167Appearance>();
                string folder="artifacts/m16-7-glasses-rear-20261107/native"; Directory.CreateDirectory(folder);
                using var target=new BenchmarkRenderTarget(900,1000);
                foreach(var option in new[]{("cap","crew"),("headband","crew"),("headband","short_swept"),("none","crew"),("none","short_swept")})
                {
                    appearance.ApplyProfile(AthleteLookCatalog.Load().Edit(appearance.Current,headwearId:option.Item1,hairStyleId:option.Item2,gogglesId:"raised"));
                    foreach(bool back in new[]{false,true})
                    {
                        var focus=rig.Bone(HumanBodyBones.Head).position+Vector3.up*.031f;
                        camera.transform.position=focus+(back?new Vector3(0,.035f,-.8f):new Vector3(.8f,.035f,0)); camera.transform.LookAt(focus);
                        yield return null; target.Render(camera); yield return new WaitForEndOfFrame(); target.Render(camera);
                        string suffix=option.Item2=="crew"?"":"-swept";
                        target.SavePng(folder+"/"+option.Item1+suffix+(back?"-back.png":"-side.png"));
                    }
                }
            }
            finally { Object.Destroy(cameraObject); Object.Destroy(hero); studio.Rig.gameObject.SetActive(true); }
        }

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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using Point = Biathlon.PlayModeTests.ProductionTerrainContactTests.Point;
using Triangle = Biathlon.PlayModeTests.ProductionTerrainContactTests.Triangle;

namespace Biathlon.PlayModeTests
{
    /// <summary>Detached native art acceptance. No race, clock, input or results are advanced.</summary>
    public sealed class WoodlandCourseDressingTests
    {
        private const string Prefix = "Biathlon/Venues/woodland_basin_v1/";
        private const double NativeFloatTolerance = .0002;
        private static readonly string[] Banners = {
            "course_climb_entry_banner", "course_summit_banner", "course_plateau_hut_banner",
            "course_descent_entry_banner", "course_descent_exit_banner", "course_return_landmark_banner"
        };

        [UnityTest]
        public IEnumerator ActualDressingKeepsGroundedLandmarksPaidClearanceAndSeatedAndStandingCrowds()
        {
            var plan = new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(Prefix + "manifest")), ProductionVenueHost.ReadResource);
            var recipe = ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(Prefix + "environment"));
            string sourceBefore = Sources(plan), recipeBefore = recipe.ToJson();
            var standingMesh = Resources.Load<Mesh>("Biathlon/Crowd/StandingSpectatorLOD0");
            Assert.That(standingMesh, Is.Not.Null, "Actual imported standing geometry is required.");
            var standingBoundsBefore = standingMesh.bounds;
            var standingVerticesBefore = standingMesh.vertices;
            var standingFacesBefore = standingMesh.triangles;
            var standingFootVertices = new HashSet<Vector3>();
            foreach (var vertex in standingVerticesBefore)
                if (Math.Abs(vertex.y - standingBoundsBefore.min.y) <= 1e-6) standingFootVertices.Add(vertex);
            Assert.That(standingFootVertices.Count, Is.GreaterThanOrEqualTo(2), "Actual native geometry must expose contact points for both standing feet.");
            var sourceStandingAnchors = new Dictionary<string, Vector3[]>(StringComparer.Ordinal);
            foreach (var placement in recipe.assets)
            {
                var source = Resources.Load<GameObject>(placement.resource);
                Assert.That(source, Is.Not.Null, placement.id);
                var seating = source.GetComponentInChildren<SpectatorSeating>();
                if (seating != null && seating.Pose == SpectatorPose.Standing)
                    sourceStandingAnchors[placement.resource] = seating.CopyAnchors();
            }
            var renderBefore = new RenderLeaseSnapshot();
            var root = new GameObject("Detached Woodland course dressing acceptance");
            ProductionVenueEnvironment environment = null;
            ProductionCoursePresentation course = null;
            try
            {
                var cameraObject = new GameObject("Detached dressing camera"); cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                var feed = root.AddComponent<RacePresentationFeed>();
                course = root.AddComponent<ProductionCoursePresentation>(); course.Initialize(plan, sharedApron: plan.SharedApron);
                var nativeBefore = new Dictionary<string, (Vector3[] Vertices, int[] Faces)>(StringComparer.Ordinal);
                for (int loop = 0; loop < course.LoopCount; loop++)
                {
                    string id = course.LoopId(loop); var mesh = course.NativeLoopMesh(id);
                    nativeBefore.Add(id, (mesh.vertices, mesh.triangles));
                }
                environment = root.AddComponent<ProductionVenueEnvironment>();
                environment.Initialize(plan, recipe, ProductionVenueHost.ReadResource, feed, camera, apron: course.Apron);
                int authoredTrees = 0, authoredCrowds = 0;
                foreach (var group in recipe.forest.groups) authoredTrees += group.count;
                foreach (var placement in recipe.assets)
                {
                    var source = Resources.Load<GameObject>(placement.resource);
                    Assert.That(source, Is.Not.Null, placement.id + ": source prefab must actually exist.");
                    if (source.GetComponentInChildren<SpectatorSeating>() != null) authoredCrowds++;
                }
                Assert.That(environment.TerrainTileCount, Is.EqualTo(recipe.terrain.tiles.Length));
                Assert.That(environment.ForestGroupCount, Is.EqualTo(recipe.forest.groups.Length));
                Assert.That(environment.TreeCount, Is.EqualTo(authoredTrees));
                Assert.That(environment.CrowdCount, Is.EqualTo(authoredCrowds));
                Assert.That(feed.Current, Is.Null, "This fixture owns detached scenery, never authoritative race state.");

                // This terrain-only index reads actual renderer triangle buffers. A
                // shared-apron course intentionally draws outside-apron meshes instead
                // of its paid source ribbons, so JoinedWorld's source-ribbon guard is
                // not the right helper for off-course landmark footings.
                var world = new NativeTerrainWorld(environment);
                var filters = environment.GetComponentsInChildren<MeshFilter>(true);
                double maximumEmbedding = 0, minimumClearance = double.PositiveInfinity;
                int overlayVertices = 0, slopeBanners = 0;
                foreach (string id in Banners)
                {
                    var parts = MeshParts(recipe, id, 2);
                    Assert.That(parts[0].mesh_resource, Is.EqualTo("Biathlon/Stadium/G2Event/BannerPole"));
                    Assert.That(parts[1].mesh_resource, Is.EqualTo("Biathlon/Stadium/G2Event/BannerFabric"));
                    var footprint = NativeMeshFootprint(parts, filters, out double bottom);
                    Extrema(world, footprint, out double low, out double high, out int witnesses);
                    overlayVertices += witnesses;
                    Assert.That(bottom - low, Is.LessThanOrEqualTo(NativeFloatTolerance), id + ": a baseplate must not float over any part of its actual terrain footprint.");
                    Assert.That(low - bottom, Is.LessThanOrEqualTo(NativeFloatTolerance), id + ": lowest terrain contact must be retained rather than burying the entire pole.");
                    Assert.That(high - bottom, Is.LessThanOrEqualTo(.25 + NativeFloatTolerance), id + ": visible snow embedding remains within the authored 25cm limit.");
                    maximumEmbedding = Math.Max(maximumEmbedding, high - bottom);
                    if (high - low > .1) slopeBanners++;
                    minimumClearance = Math.Min(minimumClearance, PaidClearance(recipe, footprint, id));
                }
                Assert.That(slopeBanners, Is.GreaterThan(0), "The real slope witness must reject the former maximum-height placement, which leaves over 10cm of floating footing.");

                // Every pre-existing non-portal mesh is included, especially the
                // former event screen intersection at the penalty loop. Six finish
                // parts form the intended overhead portal and are covered by the
                // existing dedicated portal/aperture acceptance tests.
                var portalParts = new HashSet<string>(StringComparer.Ordinal) {
                    "finish_gantry_white", "finish_gantry_snow", "finish_gantry_metal",
                    "finish_gantry_dark", "finish_gantry_concrete", "finish_gantry_accent"
                };
                int checkedMeshes = 0, actualPortalParts = 0;
                foreach (var placement in recipe.mesh_assets)
                {
                    var footprint = NativeMeshFootprint(new List<ProductionMeshPlacement> { placement }, filters, out _);
                    if (portalParts.Contains(placement.id)) { actualPortalParts++; continue; }
                    minimumClearance = Math.Min(minimumClearance, PaidClearance(recipe, footprint, placement.id)); checkedMeshes++;
                }
                Assert.That(actualPortalParts, Is.EqualTo(portalParts.Count), "Only the six explicitly known finish portal parts have a separate aperture contract.");
                foreach (var item in new[] { ("event_screen", 3), ("course_plateau_support_hut", 8) })
                {
                    var footprint = NativeMeshFootprint(MeshParts(recipe, item.Item1, item.Item2), filters, out double bottom);
                    Extrema(world, footprint, out double low, out double high, out _);
                    Assert.That(high - bottom, Is.LessThanOrEqualTo(NativeFloatTolerance), item.Item1 + ": rigid foundation must stay above its complete actual terrain footprint.");
                    Assert.That(bottom - low, Is.LessThanOrEqualTo(.25 + NativeFloatTolerance), item.Item1 + ": rigid footing must remain in contact within its authored 25cm terrain variation.");
                    minimumClearance = Math.Min(minimumClearance, PaidClearance(recipe, footprint, item.Item1));
                }

                int actualSeated = 0, actualStanding = 0, originalStands = 0, tapes = 0, flags = 0, billboards = 0, standingPockets = 0;
                int projectedStandingAnchors = 0, uphillStandingGroups = 0;
                double maximumStandingFootSurfaceGap = 0;
                var originalIds = new HashSet<string>(StringComparer.Ordinal);
                for (int index = 1; index <= 9; index++) originalIds.Add("grandstand_" + index.ToString("00", CultureInfo.InvariantCulture));
                var requiredPockets = new HashSet<string>(StringComparer.Ordinal) { "course_plateau_spectators", "course_return_spectators" };
                SpectatorCrowd firstStanding = null;
                foreach (var placement in recipe.assets)
                {
                    var source = Resources.Load<GameObject>(placement.resource);
                    var frame = NativePrefabFrame(environment, placement, source);
                    var seating = frame.GetComponentInChildren<SpectatorSeating>();
                    Bounds bounds;
                    if (seating != null && seating.Pose == SpectatorPose.Standing)
                    {
                        var crowd = frame.GetComponent<SpectatorCrowd>();
                        Assert.That(crowd, Is.Not.Null, placement.id);
                        Assert.That(crowd.Pose, Is.EqualTo(SpectatorPose.Standing));
                        Assert.That(crowd.UsesAuditedCrowd, Is.False, "The approved roadside spectators use public simplified standing geometry.");
                        Assert.That(crowd.Total, Is.EqualTo(16)); Assert.That(seating.Count, Is.EqualTo(16));
                        var sourceSeating = source.GetComponentInChildren<SpectatorSeating>();
                        Assert.That(sourceSeating.Pose, Is.EqualTo(SpectatorPose.Standing));
                        var originalAnchors = sourceStandingAnchors[placement.resource];
                        CollectionAssert.AreEqual(originalAnchors, sourceSeating.CopyAnchors(), placement.id + ": terrain projection must never alter the source prefab anchors.");
                        var actualAnchors = seating.CopyAnchors();
                        Assert.That(actualAnchors.Length, Is.EqualTo(originalAnchors.Length));
                        double minimumFootHeight = double.PositiveInfinity, maximumFootHeight = double.NegativeInfinity;
                        bool adjustedGroup = false;
                        for (int anchor = 0; anchor < actualAnchors.Length; anchor++)
                        {
                            Assert.That(originalAnchors[anchor].y, Is.EqualTo(0), placement.id + ": reusable source anchors remain flat local templates.");
                            Assert.That(actualAnchors[anchor].x, Is.EqualTo(originalAnchors[anchor].x), placement.id + ": terrain projection preserves the exact authored lateral anchor.");
                            Assert.That(actualAnchors[anchor].z, Is.EqualTo(originalAnchors[anchor].z), placement.id + ": terrain projection preserves the exact authored forward anchor.");
                            var foot = seating.transform.TransformPoint(actualAnchors[anchor] + Vector3.up * standingMesh.bounds.min.y);
                            double nativeGround = world.Height(foot);
                            Assert.That((double)foot.y, Is.EqualTo(nativeGround).Within(NativeFloatTolerance), placement.id + ": every actual standing foot must contact the rendered native terrain triangles.");
                            foreach (var vertex in standingFootVertices)
                            {
                                var nativeFoot = seating.transform.TransformPoint(actualAnchors[anchor] + vertex);
                                double gap = nativeFoot.y - world.Height(nativeFoot);
                                Assert.That(Math.Abs(gap), Is.LessThanOrEqualTo(.03 + NativeFloatTolerance), placement.id + ": both actual native boot contact points must remain within 3cm of the rendered sloping snow.");
                                maximumStandingFootSurfaceGap = Math.Max(maximumStandingFootSurfaceGap, Math.Abs(gap));
                            }
                            minimumFootHeight = Math.Min(minimumFootHeight, nativeGround); maximumFootHeight = Math.Max(maximumFootHeight, nativeGround);
                            if (Math.Abs(actualAnchors[anchor].y - originalAnchors[anchor].y) > NativeFloatTolerance)
                            { projectedStandingAnchors++; adjustedGroup = true; }
                        }
                        if (adjustedGroup && maximumFootHeight - minimumFootHeight > .1) uphillStandingGroups++;
                        bounds = StandingBounds(frame, seating);
                        actualStanding += crowd.Total; standingPockets++; requiredPockets.Remove(placement.id);
                        if (firstStanding == null) firstStanding = crowd;
                    }
                    else
                    {
                        bounds = LocalBounds(frame, frame.GetComponentsInChildren<MeshFilter>(true));
                        if (originalIds.Contains(placement.id))
                        {
                            var crowd = frame.GetComponent<SpectatorCrowd>();
                            Assert.That(seating, Is.Not.Null); Assert.That(crowd, Is.Not.Null);
                            Assert.That(placement.resource, Is.EqualTo("Biathlon/Stadium/G2Grandstand/Grandstand"));
                            Assert.That(seating.Pose, Is.EqualTo(SpectatorPose.Seated)); Assert.That(crowd.Pose, Is.EqualTo(SpectatorPose.Seated));
                            Assert.That(crowd.Total, Is.EqualTo(168)); Assert.That(seating.Count, Is.EqualTo(168));
                            actualSeated += crowd.Total; originalStands++; originalIds.Remove(placement.id);
                        }
                        var footprint = Footprint(bounds, frame.localToWorldMatrix);
                        Extrema(world, footprint, out double low, out double high, out _);
                        double bottom = frame.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z)).y;
                        if (placement.resource == "Biathlon/Roadside/RibbonTape2m")
                        {
                            tapes++;
                            Assert.That(bottom - low, Is.LessThanOrEqualTo(NativeFloatTolerance), placement.id + ": tape footings must contact snow throughout the exact terrain footprint.");
                            Assert.That(high - bottom, Is.LessThanOrEqualTo(.4 + NativeFloatTolerance), placement.id + ": tape post embedding remains within its authored 40cm allowance.");
                        }
                        else
                        {
                            Assert.That(high - bottom, Is.LessThanOrEqualTo(NativeFloatTolerance), placement.id + ": rigid native footprint must not bury its base.");
                            Assert.That(bottom - low, Is.LessThanOrEqualTo(.25 + NativeFloatTolerance), placement.id + ": rigid foundation contact must stay within 25cm variation.");
                            if (placement.resource == "Biathlon/Roadside/FlagStela") flags++;
                            if (placement.resource == "Biathlon/Roadside/Billboard") billboards++;
                        }
                    }
                    minimumClearance = Math.Min(minimumClearance, PaidClearance(recipe, Footprint(bounds, frame.localToWorldMatrix), placement.id));
                }
                Assert.That(originalIds, Is.Empty, "Every one of the original nine stadium stands must clear the full penalty and all other paid corridors.");
                Assert.That(requiredPockets, Is.Empty, "Both stable course-side pockets must use actual standing geometry.");
                Assert.That(originalStands, Is.EqualTo(9)); Assert.That(actualSeated, Is.EqualTo(1512));
                Assert.That(standingPockets, Is.GreaterThanOrEqualTo(2)); Assert.That(actualStanding, Is.GreaterThanOrEqualTo(32));
                Assert.That(projectedStandingAnchors, Is.GreaterThan(0), "The actual venue must exercise independent standing foot projection rather than only a flat stadium template.");
                Assert.That(uphillStandingGroups, Is.GreaterThan(0), "At least one actual roadside cluster must follow over 10cm of rendered uphill height variation while preserving source anchors.");
                Assert.That(tapes, Is.GreaterThanOrEqualTo(4)); Assert.That(flags, Is.GreaterThanOrEqualTo(2)); Assert.That(billboards, Is.GreaterThanOrEqualTo(2));

                // Rendering exercises the native crowd/weather lease without advancing
                // a race. Shutdown below must restore the complete shared render state.
                Assert.That(firstStanding, Is.Not.Null);
                camera.transform.position = firstStanding.transform.TransformPoint(new Vector3(0, 3, -15));
                camera.transform.LookAt(firstStanding.transform.TransformPoint(new Vector3(0, 1, 0)));
                environment.Render(camera, "high", 0);
                int visible = 0;
                for (int i = 0; i < environment.CrowdCount; i++) visible += environment.Crowd(i).Visible;
                Assert.That(visible, Is.GreaterThan(0), "Actual instantiated anchors must submit native crowd instances from the course-side camera.");
                Assert.That(firstStanding.Visible, Is.GreaterThan(0), "The real roadside standing crowd must be submitted by the native renderer.");
                Assert.That(standingMesh.bounds, Is.EqualTo(standingBoundsBefore));
                CollectionAssert.AreEqual(standingVerticesBefore, standingMesh.vertices, "Foot projection must not alter native shared standing mesh vertices.");
                CollectionAssert.AreEqual(standingFacesBefore, standingMesh.triangles, "Foot projection must not alter native shared standing mesh faces.");
                foreach (var placement in recipe.assets)
                    if (sourceStandingAnchors.TryGetValue(placement.resource, out var originalAnchors))
                        CollectionAssert.AreEqual(originalAnchors, Resources.Load<GameObject>(placement.resource).GetComponentInChildren<SpectatorSeating>().CopyAnchors(), placement.id + ": native rendering must retain the immutable source prefab anchors.");
                Assert.That(Sources(plan), Is.EqualTo(sourceBefore)); Assert.That(recipe.ToJson(), Is.EqualTo(recipeBefore));
                for (int loop = 0; loop < course.LoopCount; loop++)
                {
                    string id = course.LoopId(loop); var original = nativeBefore[id];
                    // Course.Initialize already applies the established closed-seam
                    // grooming. Capture that real input before adding any dressing;
                    // comparing to an ungroomed builder is a different contract.
                    CollectionAssert.AreEqual(original.Vertices, course.NativeLoopMesh(id).vertices, id + ": dressing keeps the exact initialized course vertices.");
                    CollectionAssert.AreEqual(original.Faces, course.NativeLoopMesh(id).triangles, id + ": dressing keeps the exact initialized course faces.");
                }
                TestContext.WriteLine("{\"native_dressing\":true,\"race_advanced\":false,\"banners\":6,\"actual_seated\":" + actualSeated + ",\"actual_standing\":" + actualStanding +
                    ",\"terrain_projected_standing_anchors\":" + projectedStandingAnchors + ",\"uphill_standing_groups\":" + uphillStandingGroups +
                    ",\"maximum_native_standing_foot_surface_gap_m\":" + maximumStandingFootSurfaceGap.ToString("R", CultureInfo.InvariantCulture) +
                    ",\"tape_prefabs\":" + tapes + ",\"flag_stelas\":" + flags + ",\"billboards\":" + billboards + ",\"checked_nonportal_meshes\":" + checkedMeshes + ",\"footing_overlay_vertices\":" + overlayVertices +
                    ",\"maximum_banner_embedding_m\":" + maximumEmbedding.ToString("R", CultureInfo.InvariantCulture) + ",\"minimum_paid_corridor_clearance_m\":" + minimumClearance.ToString("R", CultureInfo.InvariantCulture) + "}");
                course.Shutdown(); environment.Shutdown(); renderBefore.AssertRestored();
                yield return null;
                Assert.That(root.transform.childCount, Is.EqualTo(1), "Only this fixture's detached camera survives scenery shutdown.");
            }
            finally { course?.Shutdown(); environment?.Shutdown(); Object.Destroy(root); }
        }

        private static string Sources(VenueAuthoringPlan plan) => plan.Manifest.ToJson() + plan.Track.ToJson() + plan.Visual.ToJson() + plan.Paths.ToJson() +
            plan.Formats.ToJson() + plan.StartApproach.ToJson() + plan.StartContinuation.ToJson() + plan.Junctions.ToJson() + plan.Connections.ToJson() + plan.Occupancy.ToJson() + plan.SharedApron.ToJson();

        private static List<ProductionMeshPlacement> MeshParts(ProductionEnvironmentDefinition recipe, string id, int expected)
        {
            var parts = new List<ProductionMeshPlacement>();
            foreach (var value in recipe.mesh_assets) if (value.id.StartsWith(id + "_", StringComparison.Ordinal)) parts.Add(value);
            Assert.That(parts.Count, Is.EqualTo(expected), id); return parts;
        }
        private static Transform NativePrefabFrame(ProductionVenueEnvironment environment, ProductionPrefabPlacement placement, GameObject source)
        {
            Assert.That(placement.scale, Is.EqualTo(1), placement.id);
            var sourceFilters = source.GetComponentsInChildren<MeshFilter>(true);
            bool expectsCrowd = source.GetComponentInChildren<SpectatorSeating>() != null;
            Transform found = null;
            foreach (var candidate in environment.GetComponentsInChildren<Transform>(true))
            {
                // Instances live directly under an environment-owned generated
                // container. Match their actual pose and component/mesh identity;
                // object names do not identify gameplay or prove a native asset.
                if (candidate.parent == null || candidate.parent.parent != environment.transform ||
                    (candidate.position - placement.position).sqrMagnitude >= 1e-8f ||
                    Quaternion.Angle(candidate.rotation, Quaternion.Euler(0, placement.yaw_deg, 0)) >= .001f ||
                    (candidate.GetComponent<SpectatorCrowd>() != null) != expectsCrowd) continue;
                var filters = candidate.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length != sourceFilters.Length) continue;
                var meshes = new List<Mesh>(); foreach (var filter in filters) meshes.Add(filter.sharedMesh);
                bool same = true;
                foreach (var filter in sourceFilters) if (!meshes.Remove(filter.sharedMesh)) { same = false; break; }
                if (!same || meshes.Count != 0) continue;
                Assert.That(found, Is.Null, placement.id + ": native prefab identity/pose must match exactly one instance."); found = candidate;
            }
            Assert.That(found, Is.Not.Null, placement.id + ": the actual imported prefab must be instantiated at its authored pose.");
            RequireUnitHierarchy(found);
            var actualRenderers = found.GetComponentsInChildren<MeshRenderer>(true);
            var sourceRenderers = source.GetComponentsInChildren<MeshRenderer>(true);
            Assert.That(actualRenderers.Length, Is.EqualTo(sourceRenderers.Length), placement.id);
            var sourceMaterials = new List<Material>();
            foreach (var renderer in sourceRenderers) sourceMaterials.AddRange(renderer.sharedMaterials);
            var actualMaterials = new List<Material>();
            foreach (var renderer in actualRenderers) actualMaterials.AddRange(renderer.sharedMaterials);
            CollectionAssert.AreEquivalent(sourceMaterials, actualMaterials, placement.id + ": every native renderer must use the imported materials.");
            return found;
        }
        private static Bounds StandingBounds(Transform frame, SpectatorSeating seating)
        {
            var mesh = Resources.Load<Mesh>("Biathlon/Crowd/StandingSpectatorLOD0");
            Assert.That(mesh, Is.Not.Null, "Standing acceptance requires actual imported standing geometry.");
            var local = mesh.bounds; var merged = new Bounds(); bool first = true;
            var relative = frame.worldToLocalMatrix * seating.transform.localToWorldMatrix;
            if (seating.transform.localToWorldMatrix == frame.localToWorldMatrix) relative = Matrix4x4.identity;
            foreach (var anchor in seating.CopyAnchors())
            {
                var matrix = relative * Matrix4x4.Translate(anchor);
                for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
                {
                    var point = matrix.MultiplyPoint3x4(new Vector3(x == 0 ? local.min.x : local.max.x, y == 0 ? local.min.y : local.max.y, z == 0 ? local.min.z : local.max.z));
                    if (first) { merged = new Bounds(point, Vector3.zero); first = false; } else merged.Encapsulate(point);
                }
            }
            Assert.That(first, Is.False, "The native standing footprint must cover actual populated anchors.");
            return merged;
        }
        private static List<Point> NativeMeshFootprint(List<ProductionMeshPlacement> parts, MeshFilter[] filters, out double bottom)
        {
            var native = new List<MeshFilter>(); Transform frame = null;
            foreach (var part in parts)
            {
                Assert.That(part.scale, Is.EqualTo(1), part.id); MeshFilter found = null;
                var mesh = Resources.Load<Mesh>(part.mesh_resource); var material = Resources.Load<Material>(part.material_resource);
                Assert.That(mesh, Is.Not.Null); Assert.That(material, Is.Not.Null);
                foreach (var filter in filters) if (filter.sharedMesh == mesh && (filter.transform.position - part.position).sqrMagnitude < 1e-8f) found = filter;
                Assert.That(found, Is.Not.Null, part.id + ": native renderer must use the imported mesh at its actual placement.");
                RequireUnitHierarchy(found.transform);
                Assert.That(Quaternion.Angle(found.transform.rotation, Quaternion.Euler(0, part.yaw_deg, 0)), Is.LessThan(.001f));
                Assert.That(found.GetComponent<MeshRenderer>().sharedMaterial, Is.SameAs(material));
                if (frame == null) frame = found.transform;
                else Assert.That(found.transform.localToWorldMatrix, Is.EqualTo(frame.localToWorldMatrix), "Paired landmark meshes require exactly one rigid pose.");
                native.Add(found);
            }
            var bounds = LocalBounds(frame, native.ToArray());
            bottom = frame.TransformPoint(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z)).y;
            return Footprint(bounds, frame.localToWorldMatrix);
        }
        private static void RequireUnitHierarchy(Transform frame)
        {
            // Exact authored scales are the contract. Unity derives lossyScale from
            // float rotation matrices; a pure unit-scale yaw can differ by one ULP.
            for (var parent = frame; parent != null; parent = parent.parent)
                Assert.That(parent.localScale, Is.EqualTo(Vector3.one));
            Assert.That((frame.lossyScale - Vector3.one).sqrMagnitude, Is.LessThanOrEqualTo(1e-12f));
        }
        private static Bounds LocalBounds(Transform frame, MeshFilter[] filters)
        {
            bool first = true; var merged = new Bounds();
            foreach (var filter in filters)
            {
                var bounds = filter.sharedMesh.bounds; var relative = frame.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                // Exact identity is used directly: multiplying a large world position
                // by an inverse float matrix must not invent a native hierarchy offset.
                if (filter.transform.localToWorldMatrix == frame.localToWorldMatrix) relative = Matrix4x4.identity;
                for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++) for (int z = 0; z < 2; z++)
                {
                    var point = relative.MultiplyPoint3x4(new Vector3(x == 0 ? bounds.min.x : bounds.max.x, y == 0 ? bounds.min.y : bounds.max.y, z == 0 ? bounds.min.z : bounds.max.z));
                    if (first) { merged = new Bounds(point, Vector3.zero); first = false; } else merged.Encapsulate(point);
                }
            }
            Assert.That(first, Is.False); return merged;
        }
        private static List<Point> Footprint(Bounds bounds, Matrix4x4 frame) => new List<Point> {
            new Point(frame.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.min.y, bounds.min.z))),
            new Point(frame.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.min.y, bounds.min.z))),
            new Point(frame.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.min.y, bounds.max.z))),
            new Point(frame.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.min.y, bounds.max.z)))
        };
        private static double Cross(Point a, Point b, Point c) => (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);
        private static List<Point> Clip(Triangle terrain, List<Point> footprint)
        {
            var polygon = new List<Point> { new Point(terrain.A), new Point(terrain.B), new Point(terrain.C) };
            for (int edge = 0; edge < footprint.Count && polygon.Count > 0; edge++)
            {
                var a = footprint[edge]; var b = footprint[(edge + 1) % footprint.Count];
                var output = new List<Point>(); var prior = polygon[polygon.Count - 1]; double previous = Cross(a, b, prior);
                foreach (var current in polygon)
                {
                    double side = Cross(a, b, current);
                    if ((previous >= 0) != (side >= 0))
                    { double t = previous / (previous - side); output.Add(new Point(prior.X + (current.X - prior.X) * t, prior.Z + (current.Z - prior.Z) * t)); }
                    if (side >= 0) output.Add(current); prior = current; previous = side;
                }
                polygon = output;
            }
            return polygon;
        }
        private static void Extrema(NativeTerrainWorld world, List<Point> footprint, out double low, out double high, out int witnesses)
        {
            low = double.PositiveInfinity; high = double.NegativeInfinity; witnesses = 0;
            double minX = double.PositiveInfinity, minZ = minX, maxX = double.NegativeInfinity, maxZ = maxX, covered = 0;
            foreach (var p in footprint) { minX = Math.Min(minX, p.X); maxX = Math.Max(maxX, p.X); minZ = Math.Min(minZ, p.Z); maxZ = Math.Max(maxZ, p.Z); }
            foreach (var triangle in world.Over(new Vector3((float)minX, 0, (float)minZ), new Vector3((float)maxX, 0, (float)maxZ), new Vector3((float)maxX, 0, (float)minZ)))
            {
                var polygon = Clip(triangle, footprint);
                for (int i = 1; i < polygon.Count - 1; i++) covered += Math.Abs(Cross(polygon[0], polygon[i], polygon[i + 1])) * .5;
                foreach (var p in polygon) { double y = triangle.Height(p); low = Math.Min(low, y); high = Math.Max(high, y); witnesses++; }
            }
            double area = (Cross(footprint[0], footprint[1], footprint[2]) + Cross(footprint[0], footprint[2], footprint[3])) * .5;
            Assert.That(area, Is.GreaterThan(0)); Assert.That(covered, Is.EqualTo(area).Within(1e-5), "Every actual footing face needs complete native terrain coverage.");
            Assert.That(witnesses, Is.GreaterThan(0));
        }
        private static double PaidClearance(ProductionEnvironmentDefinition recipe, List<Point> polygon, string id)
        {
            double minimum = double.PositiveInfinity;
            foreach (var corridor in recipe.route_corridors) for (int i = 0; i < corridor.knots.Length - 1; i++)
            {
                var a = new Point(corridor.knots[i].x, corridor.knots[i].z); var b = new Point(corridor.knots[i + 1].x, corridor.knots[i + 1].z);
                double gap = SegmentPolygon(a, b, polygon) - Math.Max(corridor.widths_m[i], corridor.widths_m[i + 1]) * .5;
                minimum = Math.Min(minimum, gap);
            }
            Assert.That(minimum, Is.GreaterThanOrEqualTo(3 - NativeFloatTolerance), id + ": full native footprint must clear every actual paid corridor edge by at least 3m."); return minimum;
        }
        private static double SegmentPolygon(Point a, Point b, List<Point> polygon)
        {
            bool insideA = true, insideB = true; double minimum = double.PositiveInfinity;
            for (int i = 0; i < polygon.Count; i++)
            {
                var c = polygon[i]; var d = polygon[(i + 1) % polygon.Count];
                insideA &= Cross(c, d, a) >= 0; insideB &= Cross(c, d, b) >= 0;
                if (SegmentsIntersect(a, b, c, d)) return 0;
                minimum = Math.Min(minimum, Math.Min(Math.Min(PointSegment(a, c, d), PointSegment(b, c, d)), Math.Min(PointSegment(c, a, b), PointSegment(d, a, b))));
            }
            return insideA || insideB ? 0 : minimum;
        }
        private static bool SegmentsIntersect(Point a, Point b, Point c, Point d)
        {
            double ac = Cross(a, b, c), ad = Cross(a, b, d), ca = Cross(c, d, a), cb = Cross(c, d, b);
            if ((ac < 0 && ad > 0 || ac > 0 && ad < 0) && (ca < 0 && cb > 0 || ca > 0 && cb < 0)) return true;
            bool On(Point p, Point first, Point last) => p.X >= Math.Min(first.X, last.X) && p.X <= Math.Max(first.X, last.X) && p.Z >= Math.Min(first.Z, last.Z) && p.Z <= Math.Max(first.Z, last.Z);
            return ac == 0 && On(c, a, b) || ad == 0 && On(d, a, b) || ca == 0 && On(a, c, d) || cb == 0 && On(b, c, d);
        }
        private static double PointSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X, dz = b.Z - a.Z, length = dx * dx + dz * dz;
            double t = length == 0 ? 0 : Math.Max(0, Math.Min(1, ((p.X - a.X) * dx + (p.Z - a.Z) * dz) / length));
            dx = p.X - a.X - t * dx; dz = p.Z - a.Z - t * dz; return Math.Sqrt(dx * dx + dz * dz);
        }
        private sealed class NativeTerrainWorld
        {
            private readonly double spacing;
            private readonly Vector2 origin;
            private readonly Dictionary<ProductionTerrainContactTests.CellKey, ProductionTerrainContactTests.TerrainCell> cells =
                new Dictionary<ProductionTerrainContactTests.CellKey, ProductionTerrainContactTests.TerrainCell>();

            internal NativeTerrainWorld(ProductionVenueEnvironment environment)
            {
                spacing = environment.TerrainGridSpacing; origin = environment.TerrainGridOrigin;
                var filters = environment.GetComponentsInChildren<MeshFilter>(true);
                for (int tile = 0; tile < environment.TerrainTileCount; tile++)
                {
                    var mesh = environment.TileMesh(tile); MeshRenderer renderer = null;
                    foreach (var filter in filters) if (filter.sharedMesh == mesh) renderer = filter.GetComponent<MeshRenderer>();
                    Assert.That(renderer, Is.Not.Null, "Every environment-owned terrain buffer needs its actual renderer.");
                    Assert.That(renderer.localToWorldMatrix, Is.EqualTo(Matrix4x4.identity), "This detached fixture checks the actual authored world grid.");
                    var vertices = mesh.vertices; var faces = mesh.triangles; var frame = renderer.localToWorldMatrix;
                    for (int vertex = 0; vertex < vertices.Length; vertex++) vertices[vertex] = frame.MultiplyPoint3x4(vertices[vertex]);
                    for (int face = 0; face < faces.Length; face += 3)
                    {
                        var triangle = new Triangle(vertices[faces[face]], vertices[faces[face + 1]], vertices[faces[face + 2]]);
                        Assert.That(Math.Abs(triangle.Area), Is.GreaterThan(0), "Native terrain triangles must have nonzero horizontal area.");
                        var key = new ProductionTerrainContactTests.CellKey(
                            (int)Math.Round((Math.Min(triangle.A.x, Math.Min(triangle.B.x, triangle.C.x)) - origin.x) / spacing),
                            (int)Math.Round((Math.Min(triangle.A.z, Math.Min(triangle.B.z, triangle.C.z)) - origin.y) / spacing));
                        cells.TryGetValue(key, out var cell); Assert.That(cell.Count, Is.LessThan(2), "Actual terrain tiles must not overlap.");
                        if (cell.Count == 0) cell.First = triangle; else cell.Second = triangle;
                        cell.Count++; cells[key] = cell;
                    }
                }
                foreach (var cell in cells.Values) Assert.That(cell.Count, Is.EqualTo(2), "Every actual terrain cell has its two rendered triangle faces.");
            }
            internal IEnumerable<Triangle> Over(Vector3 a, Vector3 b, Vector3 c)
            {
                int X(double x) => (int)Math.Floor((x - origin.x) / spacing);
                int Z(double z) => (int)Math.Floor((z - origin.y) / spacing);
                for (int z = Z(Math.Min(a.z, Math.Min(b.z, c.z))) - 1; z <= Z(Math.Max(a.z, Math.Max(b.z, c.z))) + 1; z++)
                    for (int x = X(Math.Min(a.x, Math.Min(b.x, c.x))) - 1; x <= X(Math.Max(a.x, Math.Max(b.x, c.x))) + 1; x++)
                        if (cells.TryGetValue(new ProductionTerrainContactTests.CellKey(x, z), out var cell))
                        { yield return cell.First; yield return cell.Second; }
            }
            internal double Height(Vector3 position)
            {
                var point = new Point(position);
                foreach (var triangle in Over(position, position, position))
                {
                    var a = new Point(triangle.A); var b = new Point(triangle.B); var c = new Point(triangle.C);
                    double area = Cross(a, b, c);
                    if (Cross(a, b, point) / area >= -1e-9 && Cross(b, c, point) / area >= -1e-9 && Cross(c, a, point) / area >= -1e-9)
                        return triangle.Height(point);
                }
                Assert.Fail("Actual standing foot lies outside all native rendered terrain triangle faces."); return double.NaN;
            }
        }
        private sealed class RenderLeaseSnapshot
        {
            private readonly bool fog = RenderSettings.fog;
            private readonly FogMode fogMode = RenderSettings.fogMode;
            private readonly float start = RenderSettings.fogStartDistance, end = RenderSettings.fogEndDistance, density = RenderSettings.fogDensity;
            private readonly Color fogColor = RenderSettings.fogColor, sky = RenderSettings.ambientSkyColor, equator = RenderSettings.ambientEquatorColor, ground = RenderSettings.ambientGroundColor;
            private readonly AmbientMode mode = RenderSettings.ambientMode;
            private readonly Material skybox = RenderSettings.skybox;
            internal void AssertRestored()
            {
                Assert.That(RenderSettings.fog, Is.EqualTo(fog)); Assert.That(RenderSettings.fogMode, Is.EqualTo(fogMode));
                Assert.That(RenderSettings.fogStartDistance, Is.EqualTo(start)); Assert.That(RenderSettings.fogEndDistance, Is.EqualTo(end)); Assert.That(RenderSettings.fogDensity, Is.EqualTo(density));
                Assert.That(RenderSettings.fogColor, Is.EqualTo(fogColor)); Assert.That(RenderSettings.ambientMode, Is.EqualTo(mode));
                Assert.That(RenderSettings.ambientSkyColor, Is.EqualTo(sky)); Assert.That(RenderSettings.ambientEquatorColor, Is.EqualTo(equator)); Assert.That(RenderSettings.ambientGroundColor, Is.EqualTo(ground));
                Assert.That(RenderSettings.skybox, Is.SameAs(skybox));
            }
        }
    }
}

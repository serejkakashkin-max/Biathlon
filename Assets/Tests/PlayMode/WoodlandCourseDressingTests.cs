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
        public IEnumerator ActualDressingKeepsGroundedBannerFootprintsPaidClearanceAndRealSeatedCrowds()
        {
            var plan = new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(Prefix + "manifest")), ProductionVenueHost.ReadResource);
            var recipe = ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(Prefix + "environment"));
            string sourceBefore = Sources(plan), recipeBefore = recipe.ToJson();
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
                Assert.That(environment.TerrainTileCount, Is.EqualTo(99));
                Assert.That(environment.ForestGroupCount, Is.EqualTo(90));
                Assert.That(environment.TreeCount, Is.EqualTo(26554));
                Assert.That(environment.CrowdCount, Is.EqualTo(11));
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

                foreach (var item in new[] { ("course_plateau_boundary", 3), ("course_return_boundary", 3), ("course_plateau_support_hut", 8) })
                {
                    var footprint = NativeMeshFootprint(MeshParts(recipe, item.Item1, item.Item2), filters, out _);
                    minimumClearance = Math.Min(minimumClearance, PaidClearance(recipe, footprint, item.Item1));
                }
                int actualNewSeats = 0;
                foreach (string id in new[] { "course_plateau_spectators", "course_return_spectators" })
                {
                    ProductionPrefabPlacement placement = null;
                    foreach (var value in recipe.assets) if (value.id == id) placement = value;
                    Assert.That(placement, Is.Not.Null, id); Assert.That(placement.scale, Is.EqualTo(1));
                    SpectatorCrowd crowd = null;
                    for (int i = 0; i < environment.CrowdCount; i++)
                        if ((environment.Crowd(i).transform.position - placement.position).sqrMagnitude < 1e-8f) crowd = environment.Crowd(i);
                    Assert.That(crowd, Is.Not.Null, id + ": actual instantiated prefab must own its crowd.");
                    Assert.That(crowd.Total, Is.EqualTo(168)); RequireUnitHierarchy(crowd.transform);
                    var seating = crowd.GetComponentInChildren<SpectatorSeating>();
                    Assert.That(seating, Is.Not.Null); Assert.That(seating.Count, Is.EqualTo(168)); actualNewSeats += crowd.Total;
                    var bounds = LocalBounds(crowd.transform, crowd.GetComponentsInChildren<MeshFilter>(true));
                    minimumClearance = Math.Min(minimumClearance, PaidClearance(recipe, Footprint(bounds, crowd.transform.localToWorldMatrix), id));
                }
                Assert.That(actualNewSeats, Is.EqualTo(336));

                // Rendering exercises the native crowd/weather lease without advancing
                // a race. Shutdown below must restore the complete shared render state.
                camera.transform.position = new Vector3(136, 72, -670);
                environment.Render(camera, "high", 0);
                int visible = 0;
                for (int i = 0; i < environment.CrowdCount; i++) visible += environment.Crowd(i).Visible;
                Assert.That(visible, Is.GreaterThan(0), "Actual instantiated seat anchors must submit native crowd instances from the course-side camera.");
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
                TestContext.WriteLine("{\"native_dressing\":true,\"race_advanced\":false,\"banners\":6,\"actual_new_seats\":336,\"footing_overlay_vertices\":" + overlayVertices +
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

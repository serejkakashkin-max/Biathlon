using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    /// <summary>Cold geometric acceptance of actual rendered meshes, including their lateral edges.</summary>
    public class ProductionTerrainContactTests
    {
        private readonly struct CellKey : IEquatable<CellKey>
        {
            public readonly int X, Z;
            public CellKey(int x, int z) { X = x; Z = z; }
            public bool Equals(CellKey other) => X == other.X && Z == other.Z;
            public override bool Equals(object value) => value is CellKey other && Equals(other);
            public override int GetHashCode() => unchecked(X * 397) ^ Z;
        }

        private readonly struct Point
        {
            public readonly double X, Z;
            public Point(double x, double z) { X = x; Z = z; }
            public Point(Vector3 value) { X = value.x; Z = value.z; }
        }

        private readonly struct Triangle
        {
            public readonly Vector3 A, B, C;
            public Triangle(Vector3 a, Vector3 b, Vector3 c) { A = a; B = b; C = c; }
            public double Area => Cross(new Point(A), new Point(B), new Point(C));
            public double Height(Point p)
            {
                double bx = (double)B.x - A.x, bz = (double)B.z - A.z;
                double cx = (double)C.x - A.x, cz = (double)C.z - A.z;
                double u = ((p.X - A.x) * cz - (p.Z - A.z) * cx) / Area;
                double v = (bx * (p.Z - A.z) - bz * (p.X - A.x)) / Area;
                return A.y + u * ((double)B.y - A.y) + v * ((double)C.y - A.y);
            }
        }

        private struct TerrainCell
        {
            public Triangle First, Second;
            public int Count;
        }

        private static double Cross(Point a, Point b, Point c) =>
            (b.X - a.X) * (c.Z - a.Z) - (b.Z - a.Z) * (c.X - a.X);

        private static List<Point> Overlay(Triangle ribbon, Triangle terrain)
        {
            var polygon = new List<Point>(6) { new Point(ribbon.A), new Point(ribbon.B), new Point(ribbon.C) };
            var edges = new[] { new Point(terrain.A), new Point(terrain.B), new Point(terrain.C) };
            double sign = terrain.Area >= 0 ? 1 : -1;
            for (int edge = 0; edge < 3 && polygon.Count > 0; edge++)
            {
                var a = edges[edge]; var b = edges[(edge + 1) % 3];
                var result = new List<Point>(6); var prior = polygon[polygon.Count - 1];
                double previous = sign * Cross(a, b, prior);
                foreach (var current in polygon)
                {
                    double side = sign * Cross(a, b, current);
                    if ((previous >= -1e-9) != (side >= -1e-9))
                    {
                        double fraction = previous / (previous - side);
                        result.Add(new Point(prior.X + (current.X - prior.X) * fraction,
                            prior.Z + (current.Z - prior.Z) * fraction));
                    }
                    if (side >= -1e-9) result.Add(current);
                    prior = current; previous = side;
                }
                polygon = result;
            }
            return polygon;
        }

        private static int Cell(double coordinate, double spacing) => (int)Math.Floor(coordinate / spacing);

        [UnityTest]
        public IEnumerator ActualRenderedTerrainRemainsBeneathEveryProductionRibbonTriangle()
        {
            const string prefix = "Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)
            {
                var asset = Resources.Load<TextAsset>(id);
                return asset != null ? asset.text : throw new FileNotFoundException(id);
            }
            var plan = new VenueAuthoringPlan(VenueManifest.FromJson(Read(prefix + "manifest")), Read);
            var recipe = ProductionEnvironmentDefinition.FromJson(Read(prefix + "environment"));
            string trackBefore = plan.Track.ToJson(), visualBefore = plan.Visual.ToJson(), pathsBefore = plan.Paths.ToJson(), recipeBefore = recipe.ToJson();
            var root = new GameObject("Production complete terrain contact acceptance");
            ProductionVenueEnvironment view = null;
            ProductionCoursePresentation course = null;
            try
            {
                var cameraObject = new GameObject("Acceptance camera"); cameraObject.transform.SetParent(root.transform, false);
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                var feed = root.AddComponent<RacePresentationFeed>(); view = root.AddComponent<ProductionVenueEnvironment>();
                view.Initialize(plan, recipe, Read, feed, camera);
                course = root.AddComponent<ProductionCoursePresentation>(); course.Initialize(plan);
                double spacing = recipe.terrain.grid_spacing_m;
                var cells = new Dictionary<CellKey, TerrainCell>();
                var sharedHeights = new Dictionary<CellKey, float>(); int sharedBorderChecks = 0;
                for (int tileIndex = 0; tileIndex < view.TerrainTileCount; tileIndex++)
                {
                    var vertices = view.TileMesh(tileIndex).vertices; var indices = view.TileMesh(tileIndex).triangles;
                    Vector3 center = recipe.terrain.tiles[tileIndex].center;
                    for (int i = 0; i < vertices.Length; i++)
                    {
                        vertices[i] += center;
                        var key = new CellKey((int)Math.Round(vertices[i].x / spacing), (int)Math.Round(vertices[i].z / spacing));
                        if (Math.Abs(vertices[i].x - key.X * spacing) >= 1e-5 || Math.Abs(vertices[i].z - key.Z * spacing) >= 1e-5)
                            Assert.Fail("Actual terrain vertex does not use the declared shared world grid.");
                        if (sharedHeights.TryGetValue(key, out float prior))
                        { Assert.That(vertices[i].y, Is.EqualTo(prior).Within(1e-5)); sharedBorderChecks++; }
                        else sharedHeights.Add(key, vertices[i].y);
                    }
                    // Index the renderer's actual triangles rather than assuming a diagonal or
                    // reconstructing the generator's height cap.
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        var triangle = new Triangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
                        var key = new CellKey(Cell(Math.Min(triangle.A.x, Math.Min(triangle.B.x, triangle.C.x)), spacing),
                            Cell(Math.Min(triangle.A.z, Math.Min(triangle.B.z, triangle.C.z)), spacing));
                        cells.TryGetValue(key, out var cell);
                        if (cell.Count >= 2) Assert.Fail("Terrain cells must not overlap between tiles.");
                        if (cell.Count == 0) cell.First = triangle; else cell.Second = triangle;
                        cell.Count++; cells[key] = cell;
                    }
                }
                Assert.That(sharedBorderChecks, Is.GreaterThan(0));
                foreach (var cell in cells.Values) if (cell.Count != 2) Assert.Fail("Every terrain grid cell requires two actual renderer triangles.");
                double minimum = double.PositiveInfinity, maximum = double.NegativeInfinity;
                long overlayVertices = 0; int ribbonTriangles = 0; string minimumAt = null, maximumAt = null;
                for (int loopIndex = 0; loopIndex < plan.Visual.Count; loopIndex++)
                {
                    string loopId = plan.Visual.Get(loopIndex).LoopId;
                    // Inspect the actual course renderer, including its joined closed-loop edge.
                    var mesh = course.LoopMesh(loopId);
                    {
                        var vertices = mesh.vertices; var indices = mesh.triangles;
                        for (int i = 0; i < indices.Length; i += 3)
                        {
                            var ribbon = new Triangle(vertices[indices[i]], vertices[indices[i + 1]], vertices[indices[i + 2]]);
                            if (Math.Abs(ribbon.Area) < 1e-12) continue;
                            ribbonTriangles++; long before = overlayVertices;
                            int x0 = Cell(Math.Min(ribbon.A.x, Math.Min(ribbon.B.x, ribbon.C.x)), spacing);
                            int x1 = Cell(Math.Max(ribbon.A.x, Math.Max(ribbon.B.x, ribbon.C.x)), spacing);
                            int z0 = Cell(Math.Min(ribbon.A.z, Math.Min(ribbon.B.z, ribbon.C.z)), spacing);
                            int z1 = Cell(Math.Max(ribbon.A.z, Math.Max(ribbon.B.z, ribbon.C.z)), spacing);
                            for (int z = z0; z <= z1; z++) for (int x = x0; x <= x1; x++)
                            {
                                if (!cells.TryGetValue(new CellKey(x, z), out var cell))
                                    Assert.Fail("Terrain does not cover the complete ribbon triangle bounds: " + loopId);
                                for (int side = 0; side < 2; side++)
                                {
                                    var terrain = side == 0 ? cell.First : cell.Second;
                                    foreach (var point in Overlay(ribbon, terrain))
                                    {
                                        double gap = ribbon.Height(point) - terrain.Height(point); overlayVertices++;
                                        if (gap < minimum) { minimum = gap; minimumAt = loopId + " triangle " + (i / 3) + " at " + point.X + "," + point.Z; }
                                        if (gap > maximum) { maximum = gap; maximumAt = loopId + " triangle " + (i / 3) + " at " + point.X + "," + point.Z; }
                                    }
                                }
                            }
                            if (overlayVertices <= before) Assert.Fail("Every nondegenerate ribbon triangle requires actual terrain coverage: " + loopId);
                        }
                    }
                }
                Assert.That(ribbonTriangles, Is.GreaterThan(0));
                TestContext.WriteLine("{\"ribbon_triangles\":" + ribbonTriangles + ",\"overlay_vertices\":" + overlayVertices +
                    ",\"shared_border_checks\":" + sharedBorderChecks + ",\"minimum_ribbon_gap_m\":" + minimum.ToString("R", CultureInfo.InvariantCulture) +
                    ",\"maximum_ribbon_gap_m\":" + maximum.ToString("R", CultureInfo.InvariantCulture) + "}");
                Assert.That(minimum, Is.GreaterThanOrEqualTo(.025), "Terrain protrudes into the rendered ribbon: " + minimumAt);
                Assert.That(double.IsFinite(maximum), Is.True, "Maximum gap must remain observable: " + maximumAt);
                Assert.That(plan.Track.ToJson(), Is.EqualTo(trackBefore)); Assert.That(plan.Visual.ToJson(), Is.EqualTo(visualBefore));
                Assert.That(plan.Paths.ToJson(), Is.EqualTo(pathsBefore)); Assert.That(recipe.ToJson(), Is.EqualTo(recipeBefore));
                course.Shutdown(); view.Shutdown(); yield return null;
            }
            finally { course?.Shutdown(); view?.Shutdown(); Object.Destroy(root); }
        }
    }
}

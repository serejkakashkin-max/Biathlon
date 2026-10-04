using System;
using System.Collections.Generic;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Draft optional cold presentation, not physical admission authority.
    /// Emits a packed continuous top and clips duplicate coplanar draw triangles only.
    /// The caller owns all returned meshes and must join the resulting external boundary.
    /// This file is outside Assets until the compact-range bundle integrates it.</summary>
    public sealed class RangeApronDrawPlan
    {
        public const int SchemaVersion = 1;
        public string Id { get; }
        public string TrackSnapshotJson { get; }
        public string VisualSnapshotJson { get; }
        public string PathsSnapshotJson { get; }
        public string ManifestSnapshotJson { get; }
        public string SharedApronSnapshotJson { get; private set; }
        public string JunctionSnapshotJson { get; }
        public string ConnectionSnapshotJson { get; }
        public string OccupancySnapshotJson { get; }
        public float MinX { get; }
        public float MaxX { get; }
        public float MinZ { get; }
        public float MaxZ { get; }
        public float Height { get; }
        public float EdgeWidth { get; }
        public Mesh Top { get; }
        private readonly HashSet<string> privateLoops;
        private readonly HashSet<string> knownLoops;

        private RangeApronDrawPlan(string id, VenueAuthoringPlan plan, IDictionary<string, Mesh> paidDrawMeshes,
            float edgeWidth)
        {
            if (plan == null || !plan.UsesPhysicalPaths || paidDrawMeshes == null)
                throw new ArgumentException("Typed physical range and readonly paid meshes required.");
            VenueManifest.ResourceId(id);
            if (id.IndexOf('/') >= 0 || !float.IsFinite(edgeWidth) || edgeWidth < .1f || edgeWidth > 3)
                throw new ArgumentException("Stable apron identity and existing snow edge width required.");
            // This one-venue version supports its actual cardinal front only; no general rotated polygon contract.
            var range = plan.Manifest.Range;
            if (range.Forward.X != 0 || range.Forward.Y != 0 || range.Forward.Z != 1)
                throw new ArgumentException("This bounded apron requires the authored +Z firing direction.");
            Id = id; EdgeWidth = edgeWidth; Height = (float)range.Origin.Y;
            TrackSnapshotJson = plan.Track.ToJson(); VisualSnapshotJson = plan.Visual.ToJson(); PathsSnapshotJson = plan.Paths.ToJson(); ManifestSnapshotJson = plan.Manifest.ToJson();
            JunctionSnapshotJson = plan.Junctions?.ToJson(); ConnectionSnapshotJson = plan.Connections?.ToJson(); OccupancySnapshotJson = plan.Occupancy?.ToJson();
            privateLoops = new HashSet<string>(StringComparer.Ordinal);
            knownLoops = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < plan.Visual.Count; i++) knownLoops.Add(plan.Visual.Get(i).LoopId);
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minZ = (float)(range.Origin.Z - range.ApronDepth), maxZ = (float)range.Origin.Z;
            for (int lane = 0; lane < plan.Paths.Count; lane++)
            {
                string loopId = plan.Paths.GetLane(lane).MatAnchor.LoopId;
                if (!privateLoops.Add(loopId) || !paidDrawMeshes.TryGetValue(loopId, out var mesh) || mesh == null)
                    throw new ArgumentException("Each typed mat requires its distinct paid private draw mesh.");
                if (plan.Track.GetLoop(loopId).Closed) throw new ArgumentException("Private apron loops must be open.");
                var vertices = mesh.vertices; var triangles = mesh.triangles;
                if (triangles.Length == 0) throw new ArgumentException("A paid private path cannot disappear before apron coverage.");
                foreach (int index in triangles)
                {
                    if (index < 0 || index >= vertices.Length) throw new ArgumentException("Invalid private triangle index.");
                    var p = vertices[index];
                    if (!Finite(p) || p.y != Height) throw new ArgumentException("Every paid private point must match the exact original apron plane.");
                    minX = Math.Min(minX, p.x); maxX = Math.Max(maxX, p.x);
                    minZ = Math.Min(minZ, p.z); maxZ = Math.Max(maxZ, p.z);
                }
            }
            if (!(minX < maxX && minZ < maxZ)) throw new ArgumentException("Nonempty physical apron footprint required.");
            MinX = minX; MaxX = maxX; MinZ = minZ; MaxZ = maxZ;
            Top = BuildTop();
        }

        public static RangeApronDrawPlan Create(string id, VenueAuthoringPlan plan,
            IDictionary<string, Mesh> paidDrawMeshes, float edgeWidth) => new RangeApronDrawPlan(id, plan, paidDrawMeshes, edgeWidth);

        public static RangeApronDrawPlan Create(PhysicalSharedApronDefinition sharedApron, VenueAuthoringPlan plan,
            IDictionary<string, Mesh> paidDrawMeshes, float edgeWidth)
        {
            if (sharedApron == null || plan == null || !plan.UsesPhysicalPaths || plan.Junctions == null || plan.Connections == null || plan.Occupancy == null ||
                sharedApron.TrackSnapshotJson != plan.Track.ToJson() || sharedApron.VisualSnapshotJson != plan.Visual.ToJson() ||
                sharedApron.RangePathsSnapshotJson != plan.Paths.ToJson() || sharedApron.JunctionSnapshotJson != plan.Junctions.ToJson() ||
                sharedApron.ConnectionSnapshotJson != plan.Connections.ToJson() || sharedApron.OccupancySnapshotJson != plan.Occupancy.ToJson())
                throw new ArgumentException("Exact typed physical shared apron family required.");
            var value = new RangeApronDrawPlan(sharedApron.Id, plan, paidDrawMeshes, edgeWidth);
            value.SharedApronSnapshotJson = sharedApron.ToJson(); return value;
        }

        public void RequireBinding(VenueAuthoringPlan plan)
        {
            if (plan == null || !plan.UsesPhysicalPaths || plan.Track.ToJson() != TrackSnapshotJson ||
                plan.Visual.ToJson() != VisualSnapshotJson || plan.Paths.ToJson() != PathsSnapshotJson || plan.Manifest.ToJson() != ManifestSnapshotJson ||
                plan.Junctions?.ToJson() != JunctionSnapshotJson || plan.Connections?.ToJson() != ConnectionSnapshotJson || plan.Occupancy?.ToJson() != OccupancySnapshotJson ||
                SharedApronSnapshotJson != null && plan.SharedApron?.ToJson() != SharedApronSnapshotJson)
                throw new ArgumentException("Apron draw plan cannot move to a changed same-ID physical family.");
        }

        public bool Contains(Vector3 p) => p.x >= MinX && p.x <= MaxX && p.z >= MinZ && p.z <= MaxZ;
        public bool ContainsPaidTop(Vector3 p) => p.y == Height && Contains(p);

        /// <summary>Uses the same footprint as the composite top. No height/index/normal changes.
        /// Interior is uniformly packed; only the outside boundary has a data-driven feather.</summary>
        public Color TerrainWeights(Vector3 p, Color original)
        {
            if (!Finite(p)) throw new ArgumentException("Finite terrain point required.");
            float distance = Math.Max(Math.Max(MinX - p.x, p.x - MaxX), Math.Max(MinZ - p.z, p.z - MaxZ));
            float t = Mathf.Clamp01(Math.Max(0, distance) / EdgeWidth); float packed = 1 - t * t * (3 - 2 * t);
            return new Color(original.r * (1 - packed), original.g * (1 - packed) + packed,
                original.b * (1 - packed), original.a * (1 - packed));
        }

        /// <summary>Complete original paid triangles on the apron plane are replaced by the same
        /// continuously rendered plane. Other faces are retained; crossing coplanar faces are split
        /// only at the four outer bounds. It does not toggle a renderer or mutate the source mesh.</summary>
        public Mesh OutsideDrawMesh(string loopId, Mesh source)
            => OutsideMesh(loopId, source, false);

        /// <summary>The continuous top covers internal original curtains below its plane.
        /// Retain and split only their exterior portions; the apron owns the replacement perimeter.</summary>
        public Mesh OutsideJoinMesh(string loopId, Mesh source)
            => OutsideMesh(loopId, source, true);

        private Mesh OutsideMesh(string loopId, Mesh source, bool join)
        {
            if (string.IsNullOrWhiteSpace(loopId) || !knownLoops.Contains(loopId) || source == null) throw new ArgumentException("Known typed draw source required.");
            var points = source.vertices; var normals = source.normals; var uv = source.uv; var colors = source.colors;
            if (normals.Length != points.Length || uv.Length != points.Length || colors.Length != points.Length)
                throw new ArgumentException("Complete original draw channels required.");
            var outPoints = new List<Vector3>(); var outNormals = new List<Vector3>();
            var outUv = new List<Vector2>(); var outColors = new List<Color>(); var outIndices = new List<int>();
            var indices = source.triangles;
            if (indices.Length % 3 != 0) throw new ArgumentException("Complete triangle list required.");
            for (int i = 0; i < indices.Length; i += 3)
            {
                var triangle = new List<Vertex>(3);
                for (int j = 0; j < 3; j++)
                {
                    int index = indices[i + j]; if (index < 0 || index >= points.Length) throw new ArgumentException("Triangle index out of range.");
                    triangle.Add(new Vertex(points[index], normals[index], uv[index], colors[index]));
                }
                bool coplanar = triangle[0].Point.y == Height && triangle[1].Point.y == Height && triangle[2].Point.y == Height;
                bool below = triangle[0].Point.y <= Height && triangle[1].Point.y <= Height && triangle[2].Point.y <= Height;
                if (!coplanar && !(join && below))
                {
                    if (!join && privateLoops.Contains(loopId)) throw new ArgumentException("Private paid path must remain exactly coplanar.");
                    if (join) foreach (var v in triangle) if (Contains(v.Point))
                        throw new ArgumentException("A raised original join cannot be covered by the unchanged apron plane.");
                    Emit(triangle); continue;
                }
                if (!join && privateLoops.Contains(loopId))
                    foreach (var v in triangle) if (!ContainsPaidTop(v.Point)) throw new ArgumentException("Apron cannot discard an uncovered private paid triangle.");
                if (join && Contains(triangle[0].Point) && Contains(triangle[1].Point) && Contains(triangle[2].Point)) continue;
                // Disjoint decomposition of triangle-minus-rectangle. Each rejected half is emitted
                // once; its accepted half is passed to the next bound. Remaining inside is covered by Top.
                var remaining = triangle;
                for (int side = 0; side < 4 && remaining.Count > 0; side++)
                {
                    Emit(Clip(remaining, side, false));
                    remaining = Clip(remaining, side, true);
                }
            }
            var result = new Mesh { name = Id + " outside paid draw " + loopId, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            result.SetVertices(outPoints); result.SetNormals(outNormals); result.SetUVs(0, outUv); result.SetColors(outColors);
            result.SetTriangles(outIndices, 0); result.RecalculateBounds(); return result;

            void Emit(List<Vertex> polygon)
            {
                for (int v = 1; v + 1 < polygon.Count; v++)
                {
                    var a = polygon[0]; var b = polygon[v]; var c = polygon[v + 1];
                    var cross = Vector3.Cross(b.Point - a.Point, c.Point - a.Point);
                    // Exact zero coverage can be omitted; a finite but tiny retained face is rejected.
                    if (cross.sqrMagnitude == 0) continue;
                    if (!Finite(cross) || cross.sqrMagnitude < 1e-16f) throw new ArgumentException("Projected clipping must not silently lose a tiny paid face.");
                    int first = outPoints.Count;
                    foreach (var q in new[] { a, b, c }) { outPoints.Add(q.Point); outNormals.Add(q.Normal); outUv.Add(q.Uv); outColors.Add(q.Color); }
                    outIndices.Add(first); outIndices.Add(first + 1); outIndices.Add(first + 2);
                }
            }
        }

        private Mesh BuildTop()
        {
            // A two-row native ribbon shape permits the existing bounded external GroundJoin code
            // to join this top once at its perimeter. Row direction +X, sides +Z/-Z.
            var points = new[] { new Vector3(MinX, Height, MaxZ), new Vector3(MinX, Height, MinZ),
                new Vector3(MaxX, Height, MaxZ), new Vector3(MaxX, Height, MinZ) };
            var mesh = new Mesh { name = Id + " continuous paid rear apron", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32,
                vertices = points, triangles = new[] { 0, 2, 1, 1, 2, 3 },
                normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                colors = new[] { new Color(0,1,0,0), new Color(0,1,0,0), new Color(0,1,0,0), new Color(0,1,0,0) },
                uv = new[] { new Vector2(0,0),new Vector2(MaxZ-MinZ,0),new Vector2(0,MaxX-MinX),new Vector2(MaxZ-MinZ,MaxX-MinX) } };
            mesh.RecalculateBounds(); return mesh;
        }
        private float Side(Vector3 p, int side) => side == 0 ? p.x-MinX : side == 1 ? MaxX-p.x : side == 2 ? p.z-MinZ : MaxZ-p.z;
        private List<Vertex> Clip(List<Vertex> polygon, int side, bool inside)
        {
            var output = new List<Vertex>(); if (polygon.Count == 0) return output;
            var prior = polygon[polygon.Count-1]; float priorSide = Side(prior.Point, side) * (inside ? 1 : -1);
            foreach (var current in polygon)
            {
                float currentSide = Side(current.Point, side) * (inside ? 1 : -1);
                bool priorAccepted = inside ? priorSide >= 0 : priorSide > 0;
                bool currentAccepted = inside ? currentSide >= 0 : currentSide > 0;
                if (priorAccepted != currentAccepted)
                {
                    float fraction = priorSide/(priorSide-currentSide); var crossing = Vertex.Lerp(prior,current,fraction);
                    var point = crossing.Point;
                    if (side == 0) point.x=MinX; else if (side == 1) point.x=MaxX; else if (side == 2) point.z=MinZ; else point.z=MaxZ;
                    output.Add(new Vertex(point,crossing.Normal,crossing.Uv,crossing.Color));
                }
                if (currentAccepted) output.Add(current);
                prior=current; priorSide=currentSide;
            }
            return output;
        }
        private static bool Finite(Vector3 p) => float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        private readonly struct Vertex
        {
            internal readonly Vector3 Point,Normal; internal readonly Vector2 Uv; internal readonly Color Color;
            internal Vertex(Vector3 p,Vector3 n,Vector2 uv,Color c) { Point=p;Normal=n;Uv=uv;Color=c; }
            internal static Vertex Lerp(Vertex a,Vertex b,float t) => new Vertex(Vector3.Lerp(a.Point,b.Point,t),Vector3.Lerp(a.Normal,b.Normal,t),
                Vector2.Lerp(a.Uv,b.Uv,t),UnityEngine.Color.Lerp(a.Color,b.Color,t));
        }
    }
}

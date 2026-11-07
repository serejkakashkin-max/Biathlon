using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Interpolate the head-local surfaces baked for the isolated hero.</summary>
    internal sealed class Biathlete167GlassesRetainer : IDisposable
    {
        [Serializable] private sealed class Fit
        {
            public int schema_version, segments, lift_samples;
            public string id;
            public Frame[] frames;
        }

        [Serializable] private sealed class Frame
        {
            public string headwear;
            public float amount;
            public Vector3[] points;
        }

        private readonly MeshFilter filter;
        private readonly Mesh original, mesh;
        private readonly Transform head;
        private readonly Biathlete167Appearance appearance;
        private readonly Fit fit;
        private readonly Vector3[] vertices;
        private readonly Vector3 originalPosition;
        private readonly Quaternion originalRotation;
        private int lastOption = -1;
        private float lastLift = -1;
        private static readonly string[] Options = { "cap", "headband_crew", "headband_short_swept", "none_crew", "none_short_swept" };

        public Biathlete167GlassesRetainer(Biathlete167Rig rig, Transform head)
        {
            filter = rig.GlassesRetainer;
            this.head = head;
            appearance = rig.GetComponent<Biathlete167Appearance>();
            fit = JsonUtility.FromJson<Fit>(rig.GlassesRetainerFit.text);
            if (fit == null || fit.schema_version != 1 || fit.id != "m167_fitted_glasses_strap_v1" ||
                fit.segments < 8 || fit.segments > 128 || fit.lift_samples < 2 || fit.lift_samples > 65 ||
                fit.frames == null || fit.frames.Length != Options.Length * fit.lift_samples)
                throw new ArgumentException("Invalid fitted glasses retainer data.");
            vertices = new Vector3[(fit.segments + 1) * 4];
            for (int index = 0; index < fit.frames.Length; index++)
            {
                var frame = fit.frames[index];
                string option = Options[index / fit.lift_samples];
                float lift = (index % fit.lift_samples) / (float)(fit.lift_samples - 1);
                if (frame == null || frame.headwear != option || Mathf.Abs(frame.amount - lift) > .00001f ||
                    frame.points == null || frame.points.Length != vertices.Length)
                    throw new ArgumentException("Missing or misordered glasses retainer fit.");
                for (int vertex = 0; vertex < frame.points.Length; vertex++)
                {
                    var point = frame.points[vertex];
                    if (!float.IsFinite(point.sqrMagnitude)) throw new ArgumentException("Non-finite glasses retainer vertex.");
                    // FBX reflects Blender X and maps Z-up to Unity Y-up.
                    frame.points[vertex] = head.InverseTransformPoint(rig.transform.TransformPoint(new Vector3(-point.x, point.z, -point.y)));
                }
            }

            original = filter.sharedMesh;
            originalPosition = filter.transform.localPosition;
            originalRotation = filter.transform.localRotation;
            mesh = new Mesh { name = "Fitted glasses retainer", hideFlags = HideFlags.DontSave };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            var uv = new Vector2[vertices.Length];
            var triangles = new int[fit.segments * 24 + 12];
            int cursor = 0;
            void Quad(int a, int b, int c, int d)
            {
                triangles[cursor++] = a; triangles[cursor++] = b; triangles[cursor++] = c;
                triangles[cursor++] = a; triangles[cursor++] = c; triangles[cursor++] = d;
            }
            for (int row = 0; row <= fit.segments; row++)
                for (int corner = 0; corner < 4; corner++)
                {
                    int a = row * 4 + corner;
                    uv[a] = new Vector2(row / (float)fit.segments, corner / 3f);
                    if (row == fit.segments) continue;
                    int b = row * 4 + (corner + 1) % 4;
                    Quad(a, b, b + 4, a + 4);
                }
            Quad(3, 2, 1, 0);
            int end = fit.segments * 4;
            Quad(end, end + 1, end + 2, end + 3);
            mesh.uv = uv;
            mesh.triangles = triangles;
            filter.sharedMesh = mesh;
        }

        public void Apply(float lift)
        {
            filter.transform.SetPositionAndRotation(head.position, head.rotation);
            var profile = appearance?.Current;
            int haircut = profile?.HairStyleId == "short_swept" ? 1 : 0;
            int option = profile?.HeadwearId switch { "headband" => 1 + haircut, "none" => 3 + haircut, _ => 0 };
            if (option == lastOption && lift == lastLift) return;
            lastOption = option;
            lastLift = lift;
            float sample = Mathf.Clamp01(lift) * (fit.lift_samples - 1);
            int lower = Mathf.Min(Mathf.FloorToInt(sample), fit.lift_samples - 2);
            int offset = option * fit.lift_samples + lower;
            var from = fit.frames[offset].points;
            var to = fit.frames[offset + 1].points;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = Vector3.LerpUnclamped(from[i], to[i], sample - lower);
            mesh.SetVertices(vertices);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }

        public void Dispose()
        {
            if (filter != null)
            {
                filter.sharedMesh = original;
                filter.transform.SetLocalPositionAndRotation(originalPosition, originalRotation);
            }
            if (Application.isPlaying) Object.Destroy(mesh); else Object.DestroyImmediate(mesh);
        }
    }
}

using System;
using System.Diagnostics;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable]
    public sealed class CrowdPoseMeasurement
    {
        public int schema_version = 1;
        public string id, unity_version, gpu, cpu;
        public int athletes, frames, near_vertices, far_vertices, near_triangles, far_triangles;
        public double mean_pose_ms, maximum_pose_ms;
        public long allocated_bytes;
    }

    /// <summary>Repeatable presentation benchmark using a detached real-race snapshot on a fixed
    /// grid. Its clock moves poses only, not race positions or simulation time.</summary>
    public sealed class AthleteCrowdValidation : MonoBehaviour
    {
        private RacePresentationSnapshot snapshot;
        private AthleteAnimationPresentation[] adapters;
        private Camera observer;
        public bool Frozen;
        public double SampleTime;
        public int Count => adapters == null ? 0 : adapters.Length;
        public void Initialize(RacePresentationSnapshot source, Camera camera)
        {
            if (source == null || camera == null || adapters != null) throw new ArgumentException("Fresh fixture and snapshot/camera required.");
            snapshot = source; observer = camera; adapters = new AthleteAnimationPresentation[source.Count];
            for (int i = 0; i < adapters.Length; i++)
            {
                var pivot = new GameObject("Validation athlete " + i); pivot.transform.SetParent(transform, false);
                pivot.transform.localPosition = new Vector3((i % 6 - 2.5f) * 1.6f, 0, -(i / 6) * 2.8f);
                var rig = OriginalAthleteRigBuilder.Create(pivot.transform);
                adapters[i] = pivot.AddComponent<AthleteAnimationPresentation>(); adapters[i].Initialize(rig);
            }
        }
        public void RenderAt(double time, float overrideDistance = -1)
        {
            if (adapters == null) return;
            SampleTime = time;
            long tick = (long)Math.Floor(time * 60) + 1;
            double alpha = time * 60 - Math.Floor(time * 60);
            for (int i = 0; i < adapters.Length; i++)
            {
                float distance = overrideDistance >= 0 ? overrideDistance : Vector3.Distance(observer.transform.position, adapters[i].transform.position);
                adapters[i].Render(new AthleteAnimationFrame(snapshot.Get(i), tick, 60, alpha, 0, 0), distance);
            }
        }
        public CrowdPoseMeasurement Measure(int frames, bool distant)
        {
            if (frames < 60 || frames > 10000 || adapters == null) throw new ArgumentOutOfRangeException(nameof(frames));
            float distance = distant ? 100 : 5;
            for (int i = 0; i < 60; i++) RenderAt(i / 60.0, distance);
            var watch = new Stopwatch(); double total = 0, maximum = 0;
            long allocation = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 60; i < frames + 60; i++)
            {
                watch.Restart(); RenderAt(i / 60.0, distance); watch.Stop();
                double elapsed = watch.Elapsed.TotalMilliseconds; total += elapsed; maximum = Math.Max(maximum, elapsed);
            }
            allocation = GC.GetAllocatedBytesForCurrentThread() - allocation;
            var near = Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBody");
            var far = Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBodyDistant");
            return new CrowdPoseMeasurement { id = distant ? "wp150-far-pose" : "wp150-near-pose", unity_version = Application.unityVersion,
                gpu = SystemInfo.graphicsDeviceName, cpu = SystemInfo.processorType, athletes = Count, frames = frames,
                mean_pose_ms = total / frames, maximum_pose_ms = maximum, allocated_bytes = allocation,
                near_vertices = near.vertexCount, far_vertices = far.vertexCount, near_triangles = near.triangles.Length / 3, far_triangles = far.triangles.Length / 3 };
        }
        private void Update() { if (adapters != null) RenderAt(Frozen ? SampleTime : Time.timeAsDouble); }
    }
}

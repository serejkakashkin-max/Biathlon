using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Local venue reflection, refreshed only when the benchmark lighting changes.
    /// A fixed capture point makes seeking camera/athlete poses independent of capture history.</summary>
    public sealed class BenchmarkReflectionPresentation : MonoBehaviour
    {
        private ReflectionProbe probe;
        private string weatherId, lightingId;
        private int renderId = -1;
        public int CaptureCount { get; private set; }
        public bool Ready => renderId >= 0 && probe != null && probe.IsFinishedRendering(renderId);
        public ReflectionProbe Probe => probe;

        public void Initialize()
        {
            if (probe != null) throw new InvalidOperationException("Reflection already initialized.");
            var go = new GameObject("Benchmark venue reflection");go.transform.SetParent(transform,false);
            go.transform.position = new Vector3(-8,2,8);
            probe = go.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 256;probe.hdr = true;
            probe.size = new Vector3(1000,400,1000);probe.blendDistance = 0;
            probe.nearClipPlane = .15f;probe.farClipPlane = 600;
            probe.shadowDistance = 80;probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        }

        public void Render(string weather, string lighting)
        {
            if (probe == null || string.IsNullOrWhiteSpace(weather) || string.IsNullOrWhiteSpace(lighting))
                throw new ArgumentException("Initialized reflection and weather/lighting IDs required.");
            if (weather == weatherId && lighting == lightingId) return;
            weatherId = weather;lightingId = lighting;
            renderId = probe.RenderProbe();CaptureCount++;
        }
    }
}

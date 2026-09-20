using System;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public struct BenchmarkFrameSample
    {
        public int index;
        public double presentation_time,wall_ms,cpu_frame_ms,cpu_main_ms,cpu_render_ms,cpu_wait_ms,gpu_ms,gpu_camera_ms;
        public ulong timing_timestamp;
        public long draw_calls,batches,setpass_calls,triangles,gc_bytes,used_memory_bytes;
    }
    /// <summary>Explicit profiler lease. Unsupported/stale timings and counters are -1, never zero.</summary>
    public sealed class BenchmarkFrameCounters : IDisposable
    {
        private readonly FrameTiming[] timings=new FrameTiming[1];
        private ProfilerRecorder draws,batches,passes,triangles,gc,memory;
        private ProfilerRecorder[] drawParts;
        private UnityEngine.Profiling.Recorder cameraRecorder;
        private bool cameraRecorderWasEnabled;
        private ulong previousTimestamp;
        private bool disposed;
        public bool TimingEnabled=>FrameTimingManager.IsFeatureEnabled();
        public BenchmarkFrameCounters()
        {
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            var names=new HashSet<string>();foreach(var h in handles){var d=ProfilerRecorderHandle.GetDescription(h);if(d.Category==ProfilerCategory.Render)names.Add(d.Name);}
            if(names.Contains("Draw Calls Count"))draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1);
            else
            {
                string[] parts={"Standard Draw Calls Count","Standard Indirect Draw Calls Count","Standard Instanced Draw Calls Count","SRP Batcher Draw Calls Count","BRG Draw Calls Count","BRG Indirect Draw Calls Count","Null Geometry Draw Calls Count","Null Geometry Indirect Draw Calls Count"};
                drawParts=new ProfilerRecorder[parts.Length];
                for(int i=0;i<parts.Length;i++)if(names.Contains(parts[i]))drawParts[i]=ProfilerRecorder.StartNew(ProfilerCategory.Render,parts[i],1);
            }
            if(names.Contains("Batches Count"))batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count",1);
            passes=ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count",1);
            triangles=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1);
            gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1);
            memory=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory",1);
        }
        public void ObserveCamera(Camera camera)
        {
            if(disposed)throw new ObjectDisposedException(nameof(BenchmarkFrameCounters));
            if(camera==null)throw new ArgumentNullException(nameof(camera));
            RestoreCameraRecorder();
            cameraRecorder=UnityEngine.Profiling.Recorder.Get("UniversalRenderPipeline.RenderSingleCameraInternal: "+camera.name);
            if(cameraRecorder.isValid){cameraRecorderWasEnabled=cameraRecorder.enabled;cameraRecorder.enabled=true;}
        }
        public void Request(){if(disposed)throw new ObjectDisposedException(nameof(BenchmarkFrameCounters));FrameTimingManager.CaptureFrameTimings();}
        public BenchmarkFrameSample Read(int index,double presentationTime,double wallMs)
        {
            if(disposed)throw new ObjectDisposedException(nameof(BenchmarkFrameCounters));
            if(index<0||!double.IsFinite(presentationTime)||presentationTime<0||!double.IsFinite(wallMs)||wallMs<0)throw new ArgumentOutOfRangeException();
            var s=new BenchmarkFrameSample{index=index,presentation_time=presentationTime,wall_ms=wallMs,cpu_frame_ms=-1,cpu_main_ms=-1,cpu_render_ms=-1,cpu_wait_ms=-1,gpu_ms=-1,
                draw_calls=DrawCount(),batches=Value(batches),setpass_calls=Value(passes),triangles=Value(triangles),gc_bytes=Value(gc),used_memory_bytes=Value(memory),
                gpu_camera_ms=cameraRecorder!=null&&cameraRecorder.isValid&&cameraRecorder.gpuSampleBlockCount>0?Positive(cameraRecorder.gpuElapsedNanoseconds/1000000.0):-1};
            if(FrameTimingManager.GetLatestTimings(1,timings)>0)
            {
                var t=timings[0];
                if(t.frameStartTimestamp!=0&&t.frameStartTimestamp!=previousTimestamp)
                {
                    previousTimestamp=t.frameStartTimestamp;s.timing_timestamp=t.frameStartTimestamp;
                    s.cpu_frame_ms=Positive(t.cpuFrameTime);s.cpu_main_ms=Positive(t.cpuMainThreadFrameTime);s.cpu_render_ms=Positive(t.cpuRenderThreadFrameTime);s.gpu_ms=Positive(t.gpuFrameTime);
                    s.cpu_wait_ms=double.IsFinite(t.cpuMainThreadPresentWaitTime)&&t.cpuMainThreadPresentWaitTime>=0?t.cpuMainThreadPresentWaitTime:-1;
                }
            }
            return s;
        }
        private static double Positive(double v)=>double.IsFinite(v)&&v>0?v:-1;
        private long DrawCount(){if(drawParts==null)return Value(draws);long sum=0;foreach(var r in drawParts){long v=Value(r);if(v<0)return -1;sum+=v;}return sum;}
        private static long Value(ProfilerRecorder r)=>r.Valid&&r.Count>0?r.LastValue:-1;
        private void RestoreCameraRecorder(){if(cameraRecorder!=null&&cameraRecorder.isValid)cameraRecorder.enabled=cameraRecorderWasEnabled;cameraRecorder=null;}
        public void Dispose(){if(disposed)return;disposed=true;draws.Dispose();if(drawParts!=null)foreach(var r in drawParts)r.Dispose();RestoreCameraRecorder();batches.Dispose();passes.Dispose();triangles.Dispose();gc.Dispose();memory.Dispose();}
    }
}

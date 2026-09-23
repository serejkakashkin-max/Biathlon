using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in development-player profiler, absent from ordinary launches.</summary>
    public sealed class VisualBenchmarkProfiler : MonoBehaviour
    {
        private BenchmarkFrameCounters counters;
        private BenchmarkRenderTarget target;
        private bool logFailure;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonProfile")<0||FindAnyObjectByType<VisualBenchmarkProfiler>()!=null)return;
            var go=new GameObject("Visual benchmark profiling");DontDestroyOnLoad(go);go.AddComponent<VisualBenchmarkProfiler>();
        }
        private void OnEnable()=>Application.logMessageReceived+=Observe;
        private void OnDisable(){Application.logMessageReceived-=Observe;counters?.Dispose();target?.Dispose();}
        private void Observe(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Assert||type==LogType.Exception)logFailure=true;}
        private IEnumerator Start()
        {
            var run=Run();
            while(true)
            {
                object current;
                try{if(!run.MoveNext())break;current=run.Current;}
                catch(Exception ex){Debug.LogException(ex);counters?.Dispose();Application.Quit(1);yield break;}
                yield return current;
            }
        }
        private IEnumerator Run()
        {
            var settings=BenchmarkProfileDefinition.Load();string output=null;var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-biathlonProfileOutput")output=args[i+1];
            if(string.IsNullOrWhiteSpace(output))throw new ArgumentException("Explicit -biathlonProfileOutput directory required.");Directory.CreateDirectory(output);
            var probe=BenchmarkProfileProbe.Parse(args,settings);
            Application.runInBackground=true;Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            Screen.SetResolution(settings.width,settings.height,FullScreenMode.Windowed);
            yield return SceneManager.LoadSceneAsync("VisualBenchmark");yield return null;
            var p=FindAnyObjectByType<VisualBenchmarkPresentation>();if(p==null||!p.Ready)throw new InvalidOperationException("Benchmark initialization failed.");
            p.ShowControls=false;p.Animate=false;p.enabled=false;p.View.enabled=false;
            // CPU vegetation/crowd culling must use the target frustum too, even when
            // the OS constrains the hidden window independently of the offscreen image.
            p.View.aspect=(float)settings.width/settings.height;
            UnityEngine.Profiling.Profiler.enabled=true;
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Rendering,true);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU,true);
            counters=new BenchmarkFrameCounters();
            target=new BenchmarkRenderTarget(settings.width,settings.height);
            foreach(var c in settings.cases)
            {
                if(!probe.Includes(c.id))continue;
                p.Tier=c.tier;p.Shot=c.shot;p.WeatherPreset=c.weather;p.LightingPreset=c.lighting;p.Seek(settings.start_time);
                probe.Apply(p);
                // Pipeline/shader warmup plus eight frames beyond the documented delayed timing window.
                for(int i=0;i<settings.warmup_frames+8;i++){p.RenderFrame(settings.start_time+i*settings.step_seconds);target.Render(p.View);counters.Request();yield return null;}
                counters.ObserveCamera(p.View);
                var samples=new BenchmarkFrameSample[settings.sample_frames];
                for(int i=0;i<samples.Length;i++)
                {
                    double time=settings.start_time+(settings.warmup_frames+8+i)*settings.step_seconds;p.RenderFrame(time);target.Render(p.View);counters.Request();yield return null;
                    samples[i]=counters.Read(i,time,Time.unscaledDeltaTime*1000.0);
                }
                if(target.Width!=settings.width||target.Height!=settings.height)throw new InvalidOperationException("Actual offscreen target differs from profiling definition.");
                var evidence=new CaseEvidence{case_id=c.id,definition_id=settings.id,tier=c.tier,shot=c.shot,weather=c.weather,lighting=c.lighting,width=target.Width,height=target.Height,window_width=Screen.width,window_height=Screen.height,camera_aspect=p.View.aspect,
                    unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,api=SystemInfo.graphicsDeviceType.ToString(),ram_mb=SystemInfo.systemMemorySize,vram_mb=SystemInfo.graphicsMemorySize,
                    warmup_frames=settings.warmup_frames+8,frame_timing_enabled=counters.TimingEnabled,source_tick=p.Athletes.SourceTick,athletes=p.Athletes.Count,visible_trees=p.VisibleTrees,visible_spectators=p.VisibleSpectators,samples=samples,
                    diagnostic_only=probe.IsDiagnostic,diagnostic_probe=probe.Name};
                File.WriteAllText(Path.Combine(output,c.id+".json"),JsonUtility.ToJson(evidence,true));Debug.Log("BIATHLON_PROFILE_CASE "+c.id);
                target.SavePng(Path.Combine(output,c.id+".png"));
                foreach(var sample in samples)if(sample.draw_calls<=0||sample.triangles<=0)throw new InvalidOperationException("Profiling requires real rendered frames; batch/minimized/unsupported capture is invalid.");
            }
            counters.Dispose();counters=null;target.Dispose();target=null;yield return null;
            Debug.Log(logFailure?"BIATHLON_PROFILE_FAIL":"BIATHLON_PROFILE_PASS");Application.Quit(logFailure?1:0);
        }
        [Serializable] private sealed class CaseEvidence
        {
            public int schema_version=1;public string id="visual_benchmark_frame_samples_v1",case_id,definition_id,tier,shot,weather,lighting,unity,gpu,cpu,api;
            public int width,height,window_width,window_height,ram_mb,vram_mb,warmup_frames,athletes,visible_trees,visible_spectators;public bool frame_timing_enabled;public long source_tick;
            public float camera_aspect;
            public bool diagnostic_only;public string diagnostic_probe;
            public string render_mode="explicit URP SingleCameraRequest to offscreen RenderTexture; hidden window, no onscreen presentation/HUD cost; Rendering/GPU profiler enabled";
            public string timing_note="FrameTimingManager is delayed; counters/wall time are recent frames. -1 means unavailable or repeated timing timestamp. Timing includes waits; no deep profiling.";
            public BenchmarkFrameSample[] samples;
        }
    }
}

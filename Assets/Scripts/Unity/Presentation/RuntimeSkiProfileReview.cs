using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Unity.Profiling;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in real production race diagnostic. Never instantiated on normal launches.</summary>
    public sealed class RuntimeSkiProfileReview:MonoBehaviour
    {
        [Serializable] public sealed class Sample
        {
            public int frame,mode;public long tick;public double wall_ms,step_ms;
            public long step_alloc_bytes;public double player_speed,player_x,player_z,start_approach_meters;
            public double tick_ms,snapshot_ms,athletes_ms,environment_ms,hud_ms;
            public long tick_calls,snapshot_calls,athlete_calls,environment_calls,hud_calls;
            public BenchmarkFrameSample rendering;
        }
        [Serializable] public sealed class Report
        {
            public int schema_version=1;public string id="production_ski_runtime_profile_v1",unity,cpu,gpu;
            public string mode="controlled production ManualStepper30, original scene, explicit camera RT; not an onscreen FPS claim";
            public int frames,width=1280,height=720,athletes=30;
            public bool automatic;
            public bool thread_allocation_counter_supported;
            public string[] errors;public Sample[] samples;
            public long final_tick;public double final_player_distance;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonSkiProfile")<0)return;
            new GameObject("Explicit ski profiling diagnostic").AddComponent<RuntimeSkiProfileReview>();
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;string[] args=Environment.GetCommandLineArgs();
            int index=Array.IndexOf(args,"-biathlonSkiProfileOutput");
            string folder=Path.GetFullPath(index>=0&&index+1<args.Length?args[index+1]:Path.Combine(Application.dataPath,"../SkiProfile"));
            var errors=new List<string>();Application.LogCallback log=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);};Application.logMessageReceived+=log;
            var iter=Collect(folder,360,Array.IndexOf(args,"-biathlonSkiProfileAuto")>=0);Exception failure=null;
            while(true)
            {
                bool more=false;object next=null;
                try{more=iter.MoveNext();if(more)next=iter.Current;}catch(Exception e){failure=e;}
                if(failure!=null||!more)break;yield return next;
            }
            (iter as IDisposable)?.Dispose();Application.logMessageReceived-=log;
            if(failure!=null)errors.Add(failure.ToString());
            Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"completion.json"),JsonUtility.ToJson(new Completion{success=errors.Count==0,errors=errors.ToArray()},true));
            Application.Quit(errors.Count==0?0:1);
        }
        [Serializable] private sealed class Completion{public bool success;public string[] errors;}
        public static IEnumerator Collect(string folder,int frames,bool automatic=false)
        {
            if(frames<60||frames>3600)throw new ArgumentOutOfRangeException(nameof(frames));
            Directory.CreateDirectory(folder);var root=new GameObject("Controlled ski performance case");
            BenchmarkRenderTarget target=null;BenchmarkFrameCounters counters=null;
            var disabled=new List<GameObject>();var watches=new ProfilerRecorder[5];
            var watchNames=new[]{RuntimeFrameProfile.TickName,RuntimeFrameProfile.SnapshotName,RuntimeFrameProfile.AthletesName,RuntimeFrameProfile.EnvironmentName,RuntimeFrameProfile.HudName};
            try
            {
                foreach(var menu in UnityEngine.Object.FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))if(menu.gameObject.activeSelf){disabled.Add(menu.gameObject);menu.gameObject.SetActive(false);}
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource);
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=true;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/mass_start_profile")),RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/mass_start_route")),RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:30,movingOvertakeVersion:0);
                host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment")));
                var camera=host.ChaseCamera;camera.enabled=false;camera.GetComponent<AthleteChaseCamera>().enabled=false;
                host.GetComponent<RenderQualityPresentation>().Apply("high");host.Environment.AutomaticRendering=true;
                host.Proxies.SetViewCamera(camera);
                target=new BenchmarkRenderTarget(1280,720);counters=new BenchmarkFrameCounters();counters.ObserveCamera(camera);
                for(int i=0;i<watches.Length;i++)watches[i]=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,watchNames[i],1);
                var report=new Report{unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,frames=frames,automatic=automatic,samples=new Sample[frames],errors=Array.Empty<string>()};
                if(automatic)
                {
                    report.mode="controlled production ManualStepper30, AI controls, 2000-tick pre-roll, actual player follow camera, explicit RT; not onscreen FPS";
                    host.Environment.AutomaticRendering=false;host.Proxies.enabled=false;
                    for(int t=0;t<2000;t++){host.Runner.Step();if(t%100==99)yield return null;}
                    host.Environment.AutomaticRendering=true;host.Proxies.enabled=true;
                }
                for(int i=0;i<frames;i++)report.samples[i]=new Sample();
                var ms=new double[5];var calls=new long[5];
                long allocationControl=GC.GetAllocatedBytesForCurrentThread();var allocationProbe=new byte[1024];GC.KeepAlive(allocationProbe);
                report.thread_allocation_counter_supported=GC.GetAllocatedBytesForCurrentThread()>allocationControl;
                var timer=new System.Diagnostics.Stopwatch();double previous=Time.realtimeSinceStartupAsDouble;
                // Fixed input; simulation is stepped exactly once per measured frame.
                // Camera path is independent of variable host wall time and follows original track geometry.
                var main=venue.Visual.Get(venue.StartApproach.RouteEntryLoopId);
                for(int f=-90;f<frames;f++)
                {
                    var field=host.Runner.Field;var tick=field.Tick.Next();var id=field.GetAthlete(host.PlayerIndex).AthleteId;
                    long allocated=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
                    if(automatic)
                    {
                        using(var profile=RuntimeFrameProfile.Tick.Auto())
                        {
                            host.Runner.Step();using(var snapshot=RuntimeFrameProfile.Snapshot.Auto())host.Feed.Publish(RacePresentationSnapshot.Capture(field));
                        }
                    }
                    else host.Step(new ManualSkiCommand(tick,id,700,0),new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));
                    timer.Stop();double step=timer.Elapsed.TotalMilliseconds;allocated=GC.GetAllocatedBytesForCurrentThread()-allocated;
                    host.Feed.SetInterpolationAlpha(1);
                    double distance=f<frames/2?Math.Max(0,f+90)*.25:150+(f-frames/2)*.45;
                    var point=main.Sample(distance);var forward=new Vector3((float)point.ForwardX,0,(float)point.ForwardZ);
                    var center=new Vector3((float)point.X,(float)point.Y,(float)point.Z);
                    camera.transform.position=center+Vector3.up*2.2f-forward*8;camera.transform.LookAt(center+forward*10+Vector3.up);
                    if(automatic)
                    {
                        var actor=host.ManualStepper.World.GetActor(host.PlayerIndex);forward=new Vector3((float)Math.Sin(actor.Pose.HeadingRadians),0,(float)Math.Cos(actor.Pose.HeadingRadians));
                        center=new Vector3((float)actor.Pose.X,(float)actor.Observation.HeightMeters,(float)actor.Pose.Z);
                        camera.transform.position=center+Vector3.up*2.2f-forward*8;camera.transform.LookAt(center+forward*10+Vector3.up);
                    }
                    counters.Request();target.Render(camera);yield return null;
                    double now=Time.realtimeSinceStartupAsDouble;
                    if(f>=0)
                    {
                        var s=report.samples[f];s.frame=f;s.mode=f<frames/2?0:1;s.tick=field.Tick.Value;s.wall_ms=(now-previous)*1000;s.step_ms=step;
                        var actor=field.GetAthlete(host.PlayerIndex);var pose=host.ManualStepper.World.GetActor(host.PlayerIndex).Pose;
                        s.step_alloc_bytes=report.thread_allocation_counter_supported?allocated:-1;s.player_speed=actor.Motion.SpeedMetersPerSecond;s.player_x=pose.X;s.player_z=pose.Z;s.start_approach_meters=actor.RemainingStartApproachNanometers/(double)SkiState.NanometersPerMeter;
                        Array.Clear(ms,0,ms.Length);Array.Clear(calls,0,calls.Length);
                        for(int i=0;i<5;i++)if(watches[i].Valid&&watches[i].Count>0){var value=watches[i].GetSample(0);ms[i]=value.Value/1000000d;calls[i]=value.Count;}
                        s.tick_ms=ms[0];s.snapshot_ms=ms[1];s.athletes_ms=ms[2];s.environment_ms=ms[3];s.hud_ms=ms[4];
                        s.tick_calls=calls[0];s.snapshot_calls=calls[1];s.athlete_calls=calls[2];s.environment_calls=calls[3];s.hud_calls=calls[4];
                        s.rendering=counters.Read(f,Math.Max(0,f)/60d,s.wall_ms);report.samples[f]=s;
                    }
                    previous=now;
                }
                report.final_tick=host.Runner.Field.Tick.Value;report.final_player_distance=host.Runner.Field.GetAthlete(host.PlayerIndex).RouteNanometers/(double)SkiState.NanometersPerMeter;
                target.SavePng(Path.Combine(folder,"last-frame.png"));
                File.WriteAllText(Path.Combine(folder,"profile.json"),JsonUtility.ToJson(report,true));
            }
            finally
            {
                for(int i=0;i<watches.Length;i++)watches[i].Dispose();counters?.Dispose();target?.Dispose();
                root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();UnityEngine.Object.DestroyImmediate(root);
                if(BiathlonAudio.Instance!=null)UnityEngine.Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);
                foreach(var menu in disabled)if(menu!=null)menu.SetActive(true);
            }
        }
    }
}

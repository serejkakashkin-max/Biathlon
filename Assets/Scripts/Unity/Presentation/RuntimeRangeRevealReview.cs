using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
namespace Biathlon.Unity.Presentation
{
    // Explicit review harness, absent in normal play. No altered outcomes/saves.
    public sealed class RuntimeRangeRevealReview:MonoBehaviour
    {
        [Serializable] public sealed class Frame{public long tick;public double route,wall_ms,step_ms,publish_ms,ui_ms,layout_ms,render_ms;public bool visible;public int rows;}
        [Serializable] public sealed class Report{public int schema_version=1;public string id="first_range_reveal_profile_v1",unity;public bool success;public int first_visible=-1,geometry_events;public Frame[] frames;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Launch()
        {
            if(!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonRangeRevealProfile")<0)return;
            new GameObject("Range reveal review").AddComponent<RuntimeRangeRevealReview>();
        }
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-biathlonRangeRevealOutput");
            string folder=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.dataPath,"../RangeReveal");
            var iter=Collect(folder);Exception failed=null;
            while(true){bool more=false;object next=null;try{more=iter.MoveNext();if(more)next=iter.Current;}catch(Exception e){failed=e;}
                if(failed!=null||!more)break;yield return next;}
            (iter as IDisposable)?.Dispose();if(failed!=null)Debug.LogException(failed);Application.Quit(failed==null?0:1);
        }
        public static IEnumerator Collect(string folder)
        {
            Directory.CreateDirectory(folder);var root=new GameObject("First actual range board reveal");var disabled=new List<GameObject>();
            BenchmarkRenderTarget target=null;ProfilerRecorder ui=default,layout=default,render=default;
            var report=new Report{unity=Application.unityVersion,frames=new Frame[600]};
            try
            {
                foreach(var m in UnityEngine.Object.FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))if(m.gameObject.activeSelf){disabled.Add(m.gameObject);m.gameObject.SetActive(false);}
                var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource);
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/mass_start_profile")),RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/mass_start_route")),RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:30);
                host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment")));
                var camera=host.ChaseCamera;camera.enabled=false;camera.GetComponent<AthleteChaseCamera>().enabled=false;
                host.GetComponent<RenderQualityPresentation>().Apply("high");host.Proxies.SetViewCamera(camera);
                target=new BenchmarkRenderTarget(1280,720);
                host.Proxies.enabled=false;host.Environment.AutomaticRendering=false;
                while(host.Runner.Field.GetAthlete(0).RouteNanometers<440*SkiState.NanometersPerMeter){host.Runner.Step();if(host.Runner.Field.Tick.Value%100==0)yield return null;}
                host.Proxies.enabled=true;host.Environment.AutomaticRendering=true;
                var hud=host.GameplayHud;host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));hud.Refresh();
                var board=hud.Root.Q("range_broadcast");board.RegisterCallback<GeometryChangedEvent>(_=>report.geometry_events++);
                for(int i=0;i<30;i++){target.Render(camera);yield return null;}
                ui=ProfilerRecorder.StartNew(ProfilerCategory.Gui,"UIElements.UpdateRenderData",1);
                layout=ProfilerRecorder.StartNew(ProfilerCategory.Gui,"UIElements.UpdateLayout",1);
                render=ProfilerRecorder.StartNew(ProfilerCategory.Gui,"UIR.GenerateEntries",1);
                var clock=new System.Diagnostics.Stopwatch();double previous=Time.realtimeSinceStartupAsDouble;
                for(int i=0;i<report.frames.Length;i++)
                {
                    clock.Restart();host.Runner.Step();clock.Stop();double step=clock.Elapsed.TotalMilliseconds;
                    clock.Restart();host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));hud.Refresh();clock.Stop();double publish=clock.Elapsed.TotalMilliseconds;
                    host.Feed.SetInterpolationAlpha(1);var a=host.ManualStepper.World.GetActor(0);var forward=new Vector3((float)Math.Sin(a.Pose.HeadingRadians),0,(float)Math.Cos(a.Pose.HeadingRadians));
                    var p=new Vector3((float)a.Pose.X,(float)a.Observation.HeightMeters,(float)a.Pose.Z);camera.transform.position=p+Vector3.up*2.2f-forward*8;camera.transform.LookAt(p+forward*10+Vector3.up);
                    target.Render(camera);yield return null;double now=Time.realtimeSinceStartupAsDouble;
                    report.frames[i]=new Frame{tick=host.Runner.Field.Tick.Value,route=host.Runner.Field.GetAthlete(0).RouteNanometers/(double)SkiState.NanometersPerMeter,wall_ms=(now-previous)*1000,step_ms=step,publish_ms=publish,visible=hud.RangeBroadcastVisible,rows=hud.RangeBroadcastRows,ui_ms=Ms(ui),layout_ms=Ms(layout),render_ms=Ms(render)};
                    if(report.first_visible<0&&hud.RangeBroadcastVisible)report.first_visible=i;previous=now;
                }
                report.success=report.first_visible>=0;File.WriteAllText(Path.Combine(folder,"profile.json"),JsonUtility.ToJson(report,true));
                if(!report.success)throw new InvalidOperationException("First range reveal was not reached.");
                double Ms(ProfilerRecorder recorder)=>recorder.Valid&&recorder.Count>0?recorder.GetSample(0).Value/1000000d:-1;
            }
            finally
            {
                ui.Dispose();layout.Dispose();render.Dispose();target?.Dispose();root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();UnityEngine.Object.DestroyImmediate(root);
                if(BiathlonAudio.Instance!=null)UnityEngine.Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);
                foreach(var m in disabled)if(m!=null)m.SetActive(true);
            }
        }
    }
}

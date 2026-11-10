#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Biathlon.Presentation;
using Biathlon.Replay;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit copy-only, realtime career diagnostic including the live UI,
    /// audio and replay observer. Submits one camera to a UI-backed render texture.</summary>
    public sealed class RuntimeCareerHitchReview:MonoBehaviour
    {
        [Serializable] public struct Sample
        {
            public int frame,phase,collections;public long tick,forest_triangles;
            public double wall_ms,simulation_ms,player_distance,player_speed,planning_ms,physics_ms;
            public double prepare_ms,drives_ms,contacts_ms,sweep_ms,commit_ms,min_paid_s,min_force_s;
            public int intervals,contact_pairs,course_responses,force_refreshes,force_refinements,range_rows;
            public bool shooting,countdown,range_board;public BenchmarkFrameSample rendering;
        }
        [Serializable] public sealed class Report
        {
            public int schema_version=1,width,height,frames,marker_count;public string id="m168_full_career_hitch_review",cpu,gpu,unity,variant,tier;
            public string mode="Realtime copied career with UI updates, audio and replay recording. AI owner input, single offscreen camera render, no swapchain. Markers describe the previously completed frame.";
            public string[] markers,errors;public long[] marker_nanoseconds;public Sample[] samples;
            public double seconds;public long final_tick;public bool career_unchanged,rendering_observed;
        }
        private string output,profiles,variant;private double duration;private int startTick;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            var args=Environment.GetCommandLineArgs();if(Application.isEditor||Array.IndexOf(args,"-biathlonCareerHitches")<0)return;
            string Arg(string key){int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
            var review=new GameObject("Explicit copied-career hitch diagnostic").AddComponent<RuntimeCareerHitchReview>();
            review.profiles=Arg("-biathlonHitchProfiles");review.output=Arg("-biathlonHitchOutput");review.variant=Arg("-biathlonHitchVariant")??"normal";
            review.duration=double.TryParse(Arg("-biathlonHitchSeconds"),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)?value:180;
            review.startTick=int.TryParse(Arg("-biathlonHitchStartTick"),out int tick)?tick:0;
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;var errors=new List<string>();
            Application.LogCallback listener=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);};Application.logMessageReceived+=listener;
            var routine=Run(errors);Exception failure=null;
            while(true)
            {
                bool more=false;object next=null;try{more=routine.MoveNext();if(more)next=routine.Current;}catch(Exception e){failure=e;}
                if(failure!=null||!more)break;yield return next;
            }
            (routine as IDisposable)?.Dispose();Application.logMessageReceived-=listener;if(failure!=null)errors.Add(failure.ToString());
            if(!string.IsNullOrEmpty(output))
            {Directory.CreateDirectory(output);File.WriteAllText(Path.Combine(output,"completion.json"),JsonUtility.ToJson(new Completion{success=errors.Count==0,errors=errors.ToArray()},true));}
            Application.Quit(errors.Count==0?0:1);
        }
        [Serializable] private sealed class Completion{public bool success;public string[] errors;}
        private IEnumerator Run(List<string> errors)
        {
            if(string.IsNullOrEmpty(profiles)||!File.Exists(Path.Combine(profiles,"M167_TEST_COPY.marker"))||string.IsNullOrEmpty(output)||duration<30||duration>600)
                throw new ArgumentException("An explicitly marked career copy, output and 30–600 seconds are required.");
            output=Path.GetFullPath(output);Directory.CreateDirectory(output);
            foreach(var existing in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))existing.gameObject.SetActive(false);
            var root=new GameObject("Full copied-career hitch case");var menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;
            var watches=new List<ProfilerRecorder>();BenchmarkRenderTarget target=null;HitchCameraRender capture=null;HitchCameraRestore restore=null;
            try
            {
                menu.Initialize(new FileCareerProfileStore(profiles),new RaceReplayLibrary(Path.Combine(output,"replays")));
                menu.Session.ContinueCareer();string before=menu.Session.Career.ToJson();menu.Session.OpenPreRace();menu.Race.Launch(menu);
                double deadline=Time.realtimeSinceStartupAsDouble+180;
                while(menu.Race.Loading){if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Career load deadline.");yield return null;}
                var host=menu.Race.Host;if(host==null)throw new InvalidOperationException("No career race: "+menu.LastMessage);
                host.DiagnosticAiDrive=true;
                if(variant=="no-forest")host.Environment.DrawForestForReview=false;
                else if(variant=="no-range-board")
                    host.GameplayHud.Root.Q("range_broadcast").style.display=DisplayStyle.None;
                else if(variant=="no-range-text")
                    host.GameplayHud.Root.Q("range_broadcast").Query<Label>().ForEach(label=>label.style.display=DisplayStyle.None);
                else if(variant!="normal")throw new ArgumentException("Unknown diagnostic variant.");
                var ai=(Biathlon.AI.ManualScopedAiMovement)typeof(Biathlon.AI.PackAiRaceRunner).GetField("manualCandidate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(host.Runner);
                ai.MeasureWork=true;
                host.ManualStepper.MeasureWork=true;
                if(startTick>0)
                {
                    host.enabled=false;host.Proxies.enabled=false;host.Environment.AutomaticRendering=false;
                    for(int frame=0;frame<100&&host.CountdownRemaining>0;frame++)host.AdvanceSimulationFrame(.05);
                    while(host.Runner.Field.Tick.Value<startTick){host.Runner.Step();if(host.Runner.Field.Tick.Value%100==0)yield return null;}
                    host.Feed.Publish(Biathlon.Simulation.RacePresentationSnapshot.Capture(host.Runner.Field));host.Feed.SetInterpolationAlpha(1);
                    host.Proxies.enabled=true;host.Environment.AutomaticRendering=true;host.enabled=true;
                }
                target=new BenchmarkRenderTarget(Screen.width,Screen.height);
                foreach(var doc in FindObjectsByType<UIDocument>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                    if(doc.panelSettings!=null){doc.panelSettings.targetTexture=target.Texture;doc.panelSettings.clearColor=false;}
                capture=root.AddComponent<HitchCameraRender>();capture.Initialize(host.ChaseCamera,host.GetComponent<RangePresentation>().AimCamera,target);
                restore=root.AddComponent<HitchCameraRestore>();restore.Capture=capture;
                var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);var names=new List<string>();
                foreach(var handle in handles)
                {
                    var d=ProfilerRecorderHandle.GetDescription(handle);string n=d.Name;
                    bool wanted=n.StartsWith("Biathlon.",StringComparison.Ordinal)||n=="PlayerLoop"||n=="GC.Collect"||n.Contains("CollectIncremental")||
                        n.Contains("Shader.Create")||n.Contains("WaitForPresent")||n.Contains("WaitForRenderThread")||n.Contains("UpdatePreloading")||
                        n.Contains("AsyncResourceUpload")||n.StartsWith("UIElements.",StringComparison.Ordinal)||n=="UIR.RenderChain.Repaint"||n=="RenderPipelineManager.DoRenderLoop_Internal"||
                        n.StartsWith("WaitFor",StringComparison.Ordinal)||n=="Gfx.PresentFrame"||n=="Semaphore.WaitForSignal";
                    wanted|=n.Contains("FontAsset")||n.Contains("TextGenerator")||n.Contains("DynamicAtlas")||n=="Gfx.WaitForGfxCommandsFromMainThread";
                    if(!wanted||d.UnitType!=ProfilerMarkerDataUnit.TimeNanoseconds||names.Contains(n))continue;
                    names.Add(n);watches.Add(ProfilerRecorder.StartNew(d.Category,n,1));
                }
                const int capacity=65000;var samples=new Sample[capacity];var markerData=new long[capacity*watches.Count];
                using(var counters=new BenchmarkFrameCounters())
                {
                    int count=0,drawn=0;double start=Time.realtimeSinceStartupAsDouble,previous=start;
                    while(Time.realtimeSinceStartupAsDouble-start<duration&&!host.Runner.Field.IsComplete)
                    {
                        yield return null;
                        if(host.HasSimulationFault)throw new InvalidOperationException("The full career host rejected a race tick.");
                        double now=Time.realtimeSinceStartupAsDouble,wall=(now-previous)*1000;previous=now;
                        var player=host.Runner.Field.GetAthlete(host.PlayerIndex);long tick=host.Runner.Field.Tick.Value;
                        var rendering=counters.Read(count,tick/(double)host.Runner.Field.TickFrequency,wall);if(rendering.triangles>0)drawn++;
                        if(count==capacity)throw new InvalidOperationException("Capture sample cap.");
                        samples[count]=new Sample{frame=Time.frameCount,tick=tick,wall_ms=wall,simulation_ms=host.LastSimulationFrameMilliseconds,
                            phase=(int)(player.ActiveShooting?.Phase??Biathlon.Simulation.ShootingPhase.Outside),shooting=player.ActiveShooting!=null,
                            countdown=host.WaitingForPlayerStart,player_distance=player.RouteNanometers/(double)Biathlon.Domain.SkiState.NanometersPerMeter,
                            player_speed=player.Motion.SpeedMetersPerSecond,forest_triangles=host.Environment.SubmittedForestTriangles,
                            collections=GC.CollectionCount(0),rendering=rendering,range_board=host.GameplayHud.RangeBroadcastVisible,range_rows=host.GameplayHud.RangeBroadcastRows};
                        samples[count].planning_ms=ai.LastPlanningTimeTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
                        samples[count].physics_ms=ai.LastPhysicsTimeTicks*1000.0/System.Diagnostics.Stopwatch.Frequency;
                        double ms=1000.0/System.Diagnostics.Stopwatch.Frequency;var stepper=host.ManualStepper;
                        samples[count].prepare_ms=stepper.PreparationTimeTicks*ms;samples[count].drives_ms=stepper.DrivesTimeTicks*ms;
                        samples[count].contacts_ms=stepper.ContactsTimeTicks*ms;samples[count].sweep_ms=stepper.SweepTimeTicks*ms;samples[count].commit_ms=stepper.CommitTimeTicks*ms;
                        samples[count].intervals=stepper.LastStep.Intervals;
                        samples[count].contact_pairs=stepper.PeakContacts;samples[count].course_responses=stepper.World.CourseEdgeResponses;
                        samples[count].force_refreshes=stepper.ForceRefreshes;samples[count].force_refinements=stepper.ForceRefinements;
                        samples[count].min_paid_s=stepper.MinimumPaidSeconds;samples[count].min_force_s=stepper.MinimumForceSeconds;
                        for(int m=0;m<watches.Count;m++)markerData[count*watches.Count+m]=watches[m].Valid?watches[m].LastValue:-1;
                        counters.Request();count++;
                    }
                    Array.Resize(ref samples,count);Array.Resize(ref markerData,count*watches.Count);
                    bool visible=drawn>count*.95;
                    if(!visible)errors.Add("The single-camera diagnostic must draw production geometry in at least 95% of captured frames.");
                    File.WriteAllText(Path.Combine(output,"profile.json"),JsonUtility.ToJson(new Report{width=Screen.width,height=Screen.height,frames=count,marker_count=watches.Count,
                        cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,unity=Application.unityVersion,variant=variant,tier=host.GetComponent<RenderQualityPresentation>().TierId,
                        markers=names.ToArray(),marker_nanoseconds=markerData,samples=samples,seconds=Time.realtimeSinceStartupAsDouble-start,
                        final_tick=host.Runner.Field.Tick.Value,career_unchanged=before==menu.Session.Career.ToJson(),rendering_observed=visible,errors=errors.ToArray()},true));
                    yield return new WaitForEndOfFrame();target.SavePng(Path.Combine(output,"last-frame.png"));yield return null;
                }
            }
            finally{if(capture!=null)capture.enabled=false;if(restore!=null)restore.enabled=false;foreach(var watch in watches)watch.Dispose();target?.Dispose();if(menu.Race.Host!=null)menu.Race.Abandon();Destroy(root);}
        }
    }
    [DefaultExecutionOrder(31000)] public sealed class HitchCameraRender:MonoBehaviour
    {
        private Camera chase,aim;private bool chaseEnabled,aimEnabled;private BenchmarkRenderTarget target;
        internal void Initialize(Camera chase,Camera aim,BenchmarkRenderTarget target)
        {this.chase=chase;this.aim=aim;this.target=target;chaseEnabled=chase.enabled;aimEnabled=aim.enabled;}
        internal void Restore(){if(chase!=null)chase.enabled=chaseEnabled;if(aim!=null)aim.enabled=aimEnabled;}
        private void LateUpdate()
        {
            if(target==null)return;chaseEnabled=chase.enabled;aimEnabled=aim.enabled;
            target.Render(aim.isActiveAndEnabled?aim:chase);chase.enabled=false;aim.enabled=false;
        }
    }
    [DefaultExecutionOrder(-31000)] public sealed class HitchCameraRestore:MonoBehaviour
    {
        internal HitchCameraRender Capture;
        private void Update(){if(Capture!=null)Capture.Restore();}
    }
}
#endif

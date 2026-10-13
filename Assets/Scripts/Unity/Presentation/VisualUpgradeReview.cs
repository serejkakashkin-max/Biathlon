using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Biathlon.Domain;
using Biathlon.Simulation;
using Unity.Profiling;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit development-player before/after probe. No instance in normal gameplay.</summary>
    public sealed class VisualUpgradeReview:MonoBehaviour
    {
        [Serializable]private sealed class Receipt
        {
            public int schema_version=1;public string id="visual_upgrade_native_review",phase,unity,cpu,gpu,api;
            public int width,height,athletes,trees,texture_bytes=-1,animation_samples=-1;public bool success;
            public string render_mode="1920x1080 URP SingleCameraRequest on actual production field; hidden window; no HUD/swapchain cost";
            public string[] errors,captures;public BenchmarkFrameSample[] samples;
            public long[] animation_ns;
        }
        private readonly List<string> errors=new List<string>(),captures=new List<string>();
        private Receipt receipt;private ProductionVenueHost host;private string output;
        private BenchmarkFrameCounters counters;private BenchmarkRenderTarget target;
        private ProfilerRecorder animationRecorder;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonVisualUpgradeReview")<0)return;
            new GameObject("Explicit visual upgrade review").AddComponent<VisualUpgradeReview>();
        }
        private void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
        private IEnumerator Start()
        {
            Application.runInBackground=true;Application.logMessageReceived+=Log;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-biathlonVisualUpgradeOutput");
            if(at<0||at+1>=args.Length){Application.Quit(1);yield break;}
            output=Path.GetFullPath(args[at+1]);Directory.CreateDirectory(output);
            int phase=Array.IndexOf(args,"-biathlonVisualUpgradePhase");
            receipt=new Receipt{phase=phase>=0?args[phase+1]:"before",unity=Application.unityVersion,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString()};
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object current=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child)stack.Push(child);else yield return current;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            counters?.Dispose();animationRecorder.Dispose();target?.Dispose();receipt.errors=errors.ToArray();receipt.captures=captures.ToArray();receipt.success=errors.Count==0&&captures.Count>=10&&receipt.samples!=null;
            File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(receipt,true));Application.logMessageReceived-=Log;Application.Quit(receipt.success?0:1);
        }
        private IEnumerator Run()
        {
            yield return null;foreach(var menu in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))menu.gameObject.SetActive(false);
            var root=new GameObject("Visual upgrade production field");host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:30);
            host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment")));
            if(receipt.phase.StartsWith("before",StringComparison.Ordinal))
                for(int i=0;i<host.Runner.Field.Count;i++)host.Proxies.GetRig(host.Runner.Field.GetAthlete(i).AthleteId.Value).GetComponent<AthleteHybridLocomotionDriver>().UseOriginalMotion=true;
            host.GetComponent<RenderQualityPresentation>().Apply("high");host.GameplayHud.enabled=false;
            for(int i=0;i<608;i++)host.Runner.Step();Publish(1);host.Proxies.enabled=false;yield return null;
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            UnityEngine.Profiling.Profiler.enabled=true;UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Rendering,true);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU,true);
            counters=new BenchmarkFrameCounters();counters.ObserveCamera(host.ChaseCamera);
            animationRecorder=ProfilerRecorder.StartNew(ProfilerCategory.Scripts,"Biathlon.Presentation.AthletePose",1);
            target=new BenchmarkRenderTarget(1920,1080);host.ChaseCamera.enabled=false;
            receipt.width=target.Width;receipt.height=target.Height;receipt.athletes=host.Runner.Field.Count;receipt.trees=host.Environment.TreeCount;
            for(int frame=0;frame<120;frame++){Advance(frame);target.Render(host.ChaseCamera);counters.Request();yield return null;}
            receipt.samples=new BenchmarkFrameSample[240];
            receipt.animation_ns=new long[receipt.samples.Length];
            for(int frame=0;frame<receipt.samples.Length;frame++)
            {Advance(frame);target.Render(host.ChaseCamera);counters.Request();yield return null;receipt.samples[frame]=counters.Read(frame,host.Feed.Current.Tick/(double)host.Feed.Current.TickFrequency,Time.unscaledDeltaTime*1000);receipt.animation_ns[frame]=animationRecorder.Valid&&animationRecorder.Count>0?animationRecorder.LastValue:-1;}
            bool realRendered=false;foreach(var s in receipt.samples)realRendered|=s.triangles>0&&s.draw_calls>0;if(!realRendered)throw new InvalidOperationException("No positive rendering counters; performance capture invalid.");
            counters.Dispose();counters=null;
            host.ChaseCamera.GetComponent<AthleteChaseCamera>().enabled=false;
            long textureBytes=0;foreach(var texture in Resources.FindObjectsOfTypeAll<Texture>())textureBytes+=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);receipt.texture_bytes=(int)Math.Min(int.MaxValue,textureBytes);
            var rig=host.Proxies.GetRig(host.Runner.Field.GetAthlete(0).AthleteId.Value);var camera=host.ChaseCamera;
            yield return Capture("09-forest-course");
            var world=rig.ModelRoot.position;camera.fieldOfView=52;camera.transform.position=world+new Vector3(65,48,-70);camera.transform.LookAt(world+Vector3.up*2);yield return Capture("10-race-environment");
            // Matched pose/gallery cases; same phase, rig, FOV and lighting in both builds.
            var driver=rig.GetComponent<AthleteLocomotionDriver>();var contact=rig.GetComponent<AthleteGroundContact>();var grip=rig.GetComponent<AthleteGripPresentation>();
            var hybrid=rig.GetComponent<AthleteHybridLocomotionDriver>();
            rig.GetComponent<AthleteSkinDetailBinding>().Prepare(0,true,true);
            // SingleCameraRequest is not a normal enabled camera for skin culling.
            // Update this gallery rig while the review camera is explicitly disabled.
            // Set after timing so it cannot change the performance workload.
            rig.GetComponentInChildren<SkinnedMeshRenderer>().updateWhenOffscreen=true;
            rig.GetComponentInChildren<SkinnedMeshRenderer>().forceMatrixRecalculationPerRender=true;
            for(int shot=0;shot<8;shot++)
            {
                float grade=shot==4?.10f:0,effort=shot==6?1.25f:1,speed=shot==6?9:6,curve=shot==5?.035f:0;
                var input=new LocomotionInput(speed,grade,effort,false,shot==6,true,curve);
                for(int i=0;i<120;i++)
                {
                    double time=20+i/60.0;
                    if(hybrid!=null)hybrid.Sample(rig,input,time);else driver.Sample(rig,input,time);
                    contact.SampleMoving(rig,grade,hybrid!=null&&hybrid.Active?hybrid.PoleCycle:driver.PoleCycle,hybrid!=null&&hybrid.Active?hybrid.ContactState:driver.State,0,time);
                    if(hybrid!=null)hybrid.ApplySkiEdges(rig);grip.Render(rig);
                }
                Vector3 focus=rig.ModelRoot.position+Vector3.up*(shot==7?.1f:1.0f);
                Vector3 offset=shot==0?new Vector3(0,.35f,2.7f):shot==1?new Vector3(2.6f,.3f,0):shot==2?new Vector3(0,.3f,-2.7f):shot==7?new Vector3(1.7f,.6f,-1.5f):new Vector3(3.3f,.55f,-3.8f);
                camera.fieldOfView=shot<3?38:48;camera.transform.position=focus+rig.ModelRoot.TransformDirection(offset);camera.transform.LookAt(focus);
                yield return Capture(new[]{"01-hero-front","02-hero-side","03-hero-back","04-flat-skate","05-uphill-skate","06-fast-corner","07-finish-sprint","08-skis-and-tracks"}[shot]);
            }
            // Five atmospheric looks on the same production camera; gameplay weather is unchanged.
            foreach(string atmosphere in receipt.phase.StartsWith("before",StringComparison.Ordinal)?new[]{"clear","overcast","snowfall"}:new[]{"clear","overcast","light_snow","snowfall","winter_haze"})
            {host.Environment.SetAtmosphereForReview(atmosphere);yield return Capture("atmosphere-"+atmosphere);}
            host.Environment.SetAtmosphereForReview(null);
            void Advance(int frame){if(frame%3==0)host.Runner.Step();Publish((frame%3+1)/3.0);}
        }
        private void Publish(double alpha)
        {
            if(host.Feed.Current==null||host.Runner.Field.Tick.Value>host.Feed.Current.Tick)
                host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));
            host.Feed.SetInterpolationAlpha(alpha);host.Proxies.Render(alpha);host.GetComponent<RangePresentation>().Refresh();
        }
        private IEnumerator Capture(string name)
        {for(int i=0;i<4;i++){target.Render(host.ChaseCamera);yield return null;}target.SavePng(Path.Combine(output,name+".png"));captures.Add(name);}
    }
}

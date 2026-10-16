using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit development-player qualification. Absent from ordinary play.</summary>
    public sealed class WinterVenuePlayerReview:MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public int schema_version=1;public string id="m16_1_player_review",phase,unity,gpu,cpu,api;
            public string scope="1920x1080 offscreen production camera; 30 real athletes; no menu/HUD/swapchain cost; same compiled binary, legacy or natural presentation policy";
            public bool success;public int trees,excluded_trees,flags,tapered_panels,vertical_panels,join_vertices,covered_curtain_faces,rim_clearance_adjustments;
            public string[] errors;public List<Case> cases=new List<Case>();public List<Capture> captures=new List<Capture>();
        }
        [Serializable] private sealed class Capture{public string id;public long tick;public Vector3 position,forward;public float field_of_view;}
        [Serializable] private sealed class Case
        {
            public string id,tier,weather="clear";public Vector3 camera,target;
            public long tick;public BenchmarkFrameSample[] samples;
        }
        private readonly List<string> errors=new List<string>();
        private Receipt receipt;private string output;private ProductionVenueHost host;
        private BenchmarkRenderTarget target;private BenchmarkFrameCounters counters;
        private Camera camera;private string tier;private float visualTime;
        private const string Prefix="Biathlon/Venues/woodland_basin_v1/";
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonWinterVenueReview")<0)return;
            new GameObject("Explicit winter venue player review").AddComponent<WinterVenuePlayerReview>();
        }
        private void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-biathlonWinterVenueOutput"),phase=Array.IndexOf(args,"-biathlonWinterVenuePhase");
            if(at<0||at+1>=args.Length){Application.Quit(1);yield break;}
            output=Path.GetFullPath(args[at+1]);Directory.CreateDirectory(output);
            receipt=new Receipt{phase=phase>=0&&phase+1<args.Length?args[phase+1]:"after",unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,api=SystemInfo.graphicsDeviceType.ToString()};
            if(receipt.phase!="before"&&receipt.phase!="after"){Application.Quit(1);yield break;}
            Application.runInBackground=true;Application.logMessageReceived+=Log;
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
            counters?.Dispose();target?.Dispose();receipt.errors=errors.ToArray();receipt.success=errors.Count==0&&receipt.cases.Count==6;
            File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(receipt,true));
            Application.logMessageReceived-=Log;Application.Quit(receipt.success?0:1);
        }
        private IEnumerator Run()
        {
            yield return null;foreach(var menu in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))menu.gameObject.SetActive(false);
            var root=new GameObject("Winter venue production field");host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(Prefix+"manifest")),ProductionVenueHost.ReadResource);
            host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(Prefix+"mass_start_profile")),RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(Prefix+"mass_start_route")),RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),30);
            var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(Prefix+"environment"));
            if(receipt.phase=="before")
            {recipe.range.flags_resource=null;recipe.terrain.snow_shoulders_resource=Prefix+"snow_shoulders_before_m16_1";}
            host.InitializeEnvironment(recipe);host.Environment.AutomaticRendering=false;
            host.Environment.SetAtmosphereForReview("clear");host.GameplayHud.enabled=false;host.Proxies.enabled=false;
            host.GetComponent<RangePresentation>().enabled=false;
            host.GetComponent<RangePresentation>().AimCamera.enabled=false;
            camera=host.ChaseCamera;camera.GetComponent<AthleteChaseCamera>().enabled=false;camera.enabled=false;
            camera.fieldOfView=58;camera.nearClipPlane=.05f;camera.farClipPlane=650;
            host.Environment.SetViewCamera(camera);
            target=new BenchmarkRenderTarget(1920,1080);
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            UnityEngine.Profiling.Profiler.enabled=true;
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.Rendering,true);
            UnityEngine.Profiling.Profiler.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.GPU,true);
            receipt.trees=host.Environment.TreeCount;receipt.excluded_trees=host.Environment.FlagClearanceExcludedTrees;receipt.flags=host.Environment.RangeFlags?.Count??0;
            receipt.tapered_panels=host.Course.TaperedSnowPanelCount;receipt.vertical_panels=host.Course.VerticalSnowPanelCount;
            receipt.covered_curtain_faces=host.Course.CoveredCurtainFaces;receipt.rim_clearance_adjustments=host.Course.RimClearanceAdjustments;
            for(int i=0;i<host.Course.LoopCount;i++)receipt.join_vertices+=host.Course.SkirtMesh(host.Course.LoopId(i)).vertexCount;
            for(int i=0;i<608;i++)host.Runner.Step();Publish();
            var p=venue.Visual.Get("main").Sample(55);var center=new Vector3((float)p.X,(float)p.Y,(float)p.Z);var direction=new Vector3((float)p.ForwardX,0,(float)p.ForwardZ);
            foreach(string quality in new[]{"high","balanced","low"})
                yield return Measure("a-stadium-bend",quality,center-direction*10+Vector3.up*6,center+direction*18+Vector3.up*.8f);
            bool aiming=false,wide=false,approach=false;
            for(int i=0;i<15000;i++)
            {
                host.Runner.Step();
                var point=host.Runner.Field.GetPhysicalPoint(host.PlayerIndex);double distance=point.LocalNanometers/(double)SkiState.NanometersPerMeter;
                if(!wide&&point.LoopId=="main"&&distance>=480&&distance<500){Publish();yield return CaptureLive("p-live-field",true);wide=true;}
                if(!approach&&point.LoopId=="main"&&distance>=710&&distance<730){Publish();yield return CaptureLive("q-live-range-approach");approach=true;}
                if(host.Runner.Field.GetAthlete(host.PlayerIndex).ActiveShooting?.Phase==ShootingPhase.Aiming){aiming=true;break;}
                if(i%400==0)yield return null;
            }
            if(!aiming)throw new InvalidOperationException("Production field did not reach actual aiming.");
            Publish();host.GetComponent<RangePresentation>().enabled=false;
            camera.enabled=false;host.GetComponent<RangePresentation>().AimCamera.enabled=false;
            var bankCenter=(recipe.range.banks[0].position+recipe.range.banks[recipe.range.banks.Length-1].position)*.5f;
            foreach(string quality in new[]{"high","balanced","low"})
                yield return Measure("e-range",quality,bankCenter-recipe.range.forward*66+Vector3.up*5,bankCenter+Vector3.up*3);
            yield return CaptureLive("r-live-aim-camera",aiming:true);
            if(!wide||!approach)throw new InvalidOperationException("Actual field/approach observation was not reached.");
        }
        private void Publish()
        {
            host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));host.Feed.SetInterpolationAlpha(1);
            host.Proxies.Render(1);
        }
        private IEnumerator Measure(string id,string quality,Vector3 position,Vector3 focus)
        {
            tier=quality;host.GetComponent<RenderQualityPresentation>().Apply(tier);
            Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            camera.transform.position=position;camera.transform.LookAt(focus);visualTime=12;
            counters=new BenchmarkFrameCounters();counters.ObserveCamera(camera);
            for(int i=0;i<90;i++){DrawFrame();counters.Request();yield return null;}
            var result=new Case{id=id,tier=tier,camera=position,target=focus,tick=host.Runner.Field.Tick.Value,samples=new BenchmarkFrameSample[240]};
            for(int i=0;i<result.samples.Length;i++)
            {DrawFrame();counters.Request();yield return null;result.samples[i]=counters.Read(i,visualTime,Time.unscaledDeltaTime*1000);}
            bool actual=false;foreach(var sample in result.samples)actual|=sample.triangles>0&&sample.draw_calls>0;
            if(!actual)throw new InvalidOperationException("Missing actual draw/triangle counters: "+id+"/"+tier);
            receipt.cases.Add(result);target.SavePng(Path.Combine(output,id+"-"+tier+".png"));counters.Dispose();counters=null;
        }
        private void DrawFrame()
        {
            // Frozen accepted authority; the same bounded visual wind/time in both runs.
            // Character meshes remain those of the real 30-athlete field.
            host.Proxies.Render(1);host.Environment.Render(camera,tier,visualTime);target.Render(camera);
        }
        private IEnumerator CaptureLive(string id,bool wide=false,bool aiming=false)
        {
            host.GetComponent<RenderQualityPresentation>().Apply("high");Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            host.ChaseCamera.GetComponent<AthleteChaseCamera>().SnapToTarget();var actual=host.ChaseCamera;
            if(wide){var forward=actual.transform.forward;actual.transform.position-=Vector3.ProjectOnPlane(forward,Vector3.up).normalized*6;actual.transform.position+=Vector3.up*3;}
            if(aiming){var range=host.GetComponent<RangePresentation>();range.Render(1);actual=range.AimCamera;actual.enabled=false;}
            host.Environment.SetViewCamera(actual);
            for(int i=0;i<16;i++){host.Proxies.Render(1);host.Environment.Render(actual,"high",12);target.Render(actual);yield return null;}
            receipt.captures.Add(new Capture{id=id,tick=host.Runner.Field.Tick.Value,position=actual.transform.position,forward=actual.transform.forward,field_of_view=actual.fieldOfView});
            target.SavePng(Path.Combine(output,id+".png"));host.Environment.SetViewCamera(camera);camera.enabled=false;
        }
    }
}

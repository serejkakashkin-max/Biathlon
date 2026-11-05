using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    // Explicit Development-player diagnostic only. Reads a copy of a save;
    // never connects a profile store or publishes a career result.
    public sealed class RuntimeStartContactReview:MonoBehaviour
    {
        [Serializable] public sealed class Report
        {
            public int schema_version=1;public string id="occupied_start_owner_review_v1";
            public bool success,save_unchanged;public string[] errors;
            public int start_version,player,held_count;public long player_scheduled_start,accepted_tick,world_tick;
            public int[] held_at_1500;public long[] actual_release_ticks;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Launch()
        {
            if(Debug.isDebugBuild&&Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonStartContactReview")>=0)
                new GameObject("Occupied start review").AddComponent<RuntimeStartContactReview>();
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;var args=Environment.GetCommandLineArgs();string Get(string key){int at=Array.IndexOf(args,key);return at>=0&&at+1<args.Length?args[at+1]:null;}
            string folder=Get("-biathlonStartContactOutput")??Path.Combine(Application.dataPath,"../StartContactReview");Directory.CreateDirectory(folder);
            var report=new Report();var errors=new List<string>();Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);};Application.logMessageReceived+=log;
            var iter=Collect(Get("-biathlonStartContactSave"),folder,report);Exception failure=null;
            while(true){bool more=false;object next=null;try{more=iter.MoveNext();if(more)next=iter.Current;}catch(Exception e){failure=e;}if(failure!=null||!more)break;yield return next;}
            (iter as IDisposable)?.Dispose();Application.logMessageReceived-=log;if(failure!=null)errors.Add(failure.ToString());
            report.errors=errors.ToArray();report.success=failure==null&&errors.Count==0&&report.held_count>0&&report.save_unchanged&&report.accepted_tick==2300;
            File.WriteAllText(Path.Combine(folder,"review.json"),JsonUtility.ToJson(report,true));Application.Quit(report.success?0:1);
        }
        public static IEnumerator Collect(string savePath,string folder,Report report)
        {
            string json=File.ReadAllText(savePath);var save=CareerSave.FromJson(json);
            string Read(string name)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(name));
            var definitions=CareerDefinitions.ForSave(Read,save);
            var week=CareerSeasonEngine.PrepareWeek(save,definitions,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));
            var prepared=new CareerRaceProvider(Read,definitions,ProductionVenueHost.ReadResource).Prepare(CareerSave.FromJson(week.RaceSnapshotJson),week.Event,week.RaceSeed);
            var root=new GameObject("Actual owner start case");BenchmarkRenderTarget target=null;var disabled=new List<GameObject>();
            try
            {
                foreach(var menu in UnityEngine.Object.FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))if(menu.gameObject.activeSelf){disabled.Add(menu.gameObject);menu.gameObject.SetActive(false);}
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.InitializeCareer(prepared);
                host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment")));
                host.GameplayHud.SetBroadcastRoster(save);var camera=host.ChaseCamera;camera.enabled=false;camera.GetComponent<AthleteChaseCamera>().enabled=false;
                host.GetComponent<RenderQualityPresentation>().Apply("high");host.Proxies.SetViewCamera(camera);target=new BenchmarkRenderTarget(1280,720);
                var runner=host.Runner;var world=host.ManualStepper.World;report.player=host.PlayerIndex;report.start_version=runner.Field.StartContinuationVersion;
                report.player_scheduled_start=runner.Field.GetAthlete(host.PlayerIndex).ScheduledStart.Value;
                host.Proxies.enabled=false;host.Environment.AutomaticRendering=false;
                for(int t=0;t<2300;t++)
                {
                    var a=runner.Field.GetAthlete(host.PlayerIndex);var tick=runner.Field.Tick.Next();
                    runner.StepWithManualPlayer(host.PlayerIndex,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,t<1500?0:650),1,1,0),new WeaponAimCommand(tick,a.AthleteId,0,0)),0,t>=1500);
                    if(t==1499)
                    {
                        var held=new List<int>();for(int i=0;i<runner.Field.Count;i++)if(world.IsStartReleaseHeld(i))held.Add(i);
                        report.held_at_1500=held.ToArray();report.held_count=held.Count;
                    }
                    if(t<1499){if(t%100==99)yield return null;continue;}
                    host.Proxies.enabled=true;host.Environment.AutomaticRendering=true;
                    host.Feed.Publish(RacePresentationSnapshot.Capture(runner.Field));host.Feed.SetInterpolationAlpha(1);
                    var body=world.GetActor(host.PlayerIndex);var f=new Vector3((float)Math.Sin(body.Pose.HeadingRadians),0,(float)Math.Cos(body.Pose.HeadingRadians));var p=new Vector3((float)body.Pose.X,(float)body.Observation.HeightMeters,(float)body.Pose.Z);
                    camera.transform.position=p+Vector3.up*2.2f-f*8;camera.transform.LookAt(p+f*10+Vector3.up);target.Render(camera);yield return null;
                    if(t==1499)
                    {
                        for(int warm=0;warm<4;warm++){target.Render(camera);yield return null;}
                        target.SavePng(Path.Combine(folder,"occupied-start.png"));
                    }
                }
                report.accepted_tick=runner.Field.Tick.Value;report.world_tick=world.Tick.Value;
                report.actual_release_ticks=new long[report.held_count];
                for(int i=0;i<report.held_count;i++)
                {
                    int actor=report.held_at_1500[i];report.actual_release_ticks[i]=world.GetStartReleaseTick(actor);
                    if(world.IsStartReleaseHeld(actor)||report.actual_release_ticks[i]<=1500||runner.Field.GetAthlete(actor).Motion.DistanceNanometers<=SkiState.NanometersPerMeter)
                        throw new InvalidOperationException("Held entrant did not resume after the player cleared the start.");
                }
                report.save_unchanged=File.ReadAllText(savePath)==json;target.SavePng(Path.Combine(folder,"released-start.png"));
            }
            finally
            {
                target?.Dispose();root.GetComponent<ProductionVenueEnvironment>()?.Shutdown();UnityEngine.Object.DestroyImmediate(root);
                if(BiathlonAudio.Instance!=null)UnityEngine.Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);foreach(var menu in disabled)if(menu!=null)menu.SetActive(true);
            }
        }
    }
}

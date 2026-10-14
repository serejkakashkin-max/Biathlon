using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit development-player regression footage of the real 30-athlete field.
    /// Ordinary games never create this component or change their camera/skin update policy.</summary>
    public sealed class PoleMotionReview : MonoBehaviour
    {
        [Serializable] private sealed class Receipt
        {
            public int schema_version=1;public string id="m15_5_owner_pole_review",unity;
            public int athletes=30,fps=30,frames_per_clip=150;public bool success;
            public long final_tick;public string[] errors,captures;
            public float waiting_hand_height_m,waiting_pole_down_dot,first_turn_curvature,uphill_grade;
            public string waiting_motion_state;
            public string source="Actual production Mass Start, AI control, detached snapshots; no race tuning changes";
        }
        private ProductionVenueHost host;
        private AthleteRigBinding lead;
        private BenchmarkRenderTarget target;
        private string output;
        private float waitingHeight,waitingPoleDown,turnCurvature,climbGrade;
        private string waitingState;
        private readonly List<string> errors=new List<string>(),captures=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonPoleReview")<0)return;
            new GameObject("Explicit pole motion regression review").AddComponent<PoleMotionReview>();
        }
        private void Log(string message,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
        private IEnumerator Start()
        {
            Application.runInBackground=true;Application.logMessageReceived+=Log;
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-biathlonPoleReviewOutput");
            if(at<0||at+1>=args.Length){Application.Quit(1);yield break;}
            output=Path.GetFullPath(args[at+1]);Directory.CreateDirectory(output);
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                bool more=false;object current=null;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child)stack.Push(child);else yield return current;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();target?.Dispose();
            var receipt=new Receipt{unity=Application.unityVersion,final_tick=host!=null?host.Runner.Field.Tick.Value:0,
                waiting_hand_height_m=waitingHeight,waiting_pole_down_dot=waitingPoleDown,waiting_motion_state=waitingState,
                first_turn_curvature=turnCurvature,uphill_grade=climbGrade,
                captures=captures.ToArray(),errors=errors.ToArray(),success=errors.Count==0&&captures.Count==4};
            File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(receipt,true));
            Application.logMessageReceived-=Log;Application.Quit(receipt.success?0:1);
        }
        private IEnumerator Run()
        {
            yield return null;foreach(var menu in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))menu.gameObject.SetActive(false);
            host=new GameObject("Actual Mass Start animation review").AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),
                RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:30);
            host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment")));
            host.GetComponent<RenderQualityPresentation>().Apply("high");host.GameplayHud.enabled=false;
            host.Proxies.enabled=false;host.ChaseCamera.enabled=false;host.ChaseCamera.GetComponent<AthleteChaseCamera>().enabled=false;
            lead=host.Proxies.GetRig(host.Runner.Field.GetAthlete(0).AthleteId.Value);
            // Explicit offscreen camera captures require matrix refresh. This is review only.
            foreach(var skin in lead.GetComponentsInChildren<SkinnedMeshRenderer>())
            {skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;}
            target=new BenchmarkRenderTarget(1280,720);Publish();CameraPose();
            waitingHeight=lead.ModelRoot.InverseTransformPoint(lead.Bone(AthleteBone.LeftHand).position).y;
            waitingPoleDown=Vector3.Dot(-lead.GetComponent<AthleteEquipmentBinding>().LeftPole.up,Vector3.down);
            waitingState=lead.GetComponent<AthleteHybridLocomotionDriver>().MotionState.ToString();
            if(waitingPoleDown<=.7f||waitingState!=SkiMotionState.SlowStop.ToString())throw new InvalidOperationException("Actual start poles are not ready/braced.");
            for(int i=0;i<4;i++){target.Render(host.ChaseCamera);yield return null;}
            target.SavePng(Path.Combine(output,"waiting-mass-start.png"));captures.Add("waiting-mass-start");
            yield return Clip("start-acceleration");
            yield return FindTerrain(true);yield return Clip("first-turn");
            yield return FindTerrain(false);yield return Clip("uphill");
        }
        private void Publish()
        {
            if(host.Feed.Current==null||host.Feed.Current.Tick<host.Runner.Field.Tick.Value)
                host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));
            host.Feed.SetInterpolationAlpha(1);host.Proxies.Render(1);host.GetComponent<RangePresentation>().Refresh();
        }
        private void CameraPose()
        {
            var camera=host.ChaseCamera;var focus=lead.ModelRoot.position+Vector3.up;
            camera.fieldOfView=42;camera.transform.position=focus+lead.ModelRoot.TransformDirection(new Vector3(4,1.3f,7));
            camera.transform.LookAt(focus);
        }
        private IEnumerator FindTerrain(bool corner)
        {
            for(int i=0;i<6000;i++)
            {
                var f=lead.GetComponentInParent<AthleteAnimationPresentation>().LastFrame;
                if(corner?Math.Abs(f.PresentationCurvature)>.012:f.PresentationGrade>.035)
                {if(corner)turnCurvature=(float)f.PresentationCurvature;else climbGrade=(float)f.PresentationGrade;yield break;}
                host.Runner.Step();Publish();if(i%120==0)yield return null;
            }
            throw new InvalidOperationException("Actual field did not reach the requested "+(corner?"turn":"climb")+" within 100 seconds.");
        }
        private IEnumerator Clip(string name)
        {
            string folder=Path.Combine(output,name);Directory.CreateDirectory(folder);
            for(int i=0;i<150;i++)
            {
                host.Runner.Step();host.Runner.Step();Publish();CameraPose();target.Render(host.ChaseCamera);
                yield return null;target.SavePng(Path.Combine(folder,i.ToString("D4")+".png"));
            }
            captures.Add(name);
        }
    }
}

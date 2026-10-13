using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Review scene only. Two identical rigs receive identical synthetic observations.
    /// Left is the retained original path; right is original authored hybrid motion.</summary>
    public sealed class SkiMotionComparison:MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonMotionLab")<0)return;
            UnityEngine.SceneManagement.SceneManager.LoadScene("SkiMotionComparison");
        }
        public bool AutoCycle=true;
        public SkiMotionState State=SkiMotionState.Flat;
        public Camera ReviewCamera {get;private set;}
        public AthleteRigBinding Original {get;private set;}
        public AthleteRigBinding Candidate {get;private set;}
        private WeatherPresentation weather;
        private double previous=-1;
        private GameObject generated;
        private bool capturing;
        public void Initialize()
        {
            if(generated!=null)return;
            generated=new GameObject("Motion comparison fixture");generated.transform.SetParent(transform,false);
            Original=AthleteRigFactory.Create(generated.transform);Candidate=AthleteRigFactory.Create(generated.transform);
            // Facing +Z toward the front camera: world +X projects to screen left.
            Original.transform.position=new Vector3(1.3f,0,0);Candidate.transform.position=new Vector3(-1.3f,0,0);
            Original.GetComponent<AthleteHybridLocomotionDriver>().UseOriginalMotion=true;
            foreach(var rig in new[]{Original,Candidate}){rig.GetComponent<AthleteSkinDetailBinding>().Prepare(0,true,true);rig.GetComponent<AthleteAppearancePresentation>().ApplyNation("NOR");}
            var surface=GameObject.CreatePrimitive(PrimitiveType.Plane);surface.transform.SetParent(generated.transform,false);surface.transform.localScale=new Vector3(3,1,3);
            var material=Resources.Load<Material>("Biathlon/Snow/cold");
            if(material!=null)surface.GetComponent<MeshRenderer>().sharedMaterial=material;
            var cam=new GameObject("Matched review camera");cam.transform.SetParent(generated.transform,false);ReviewCamera=cam.AddComponent<Camera>();
            ReviewCamera.transform.position=new Vector3(0,2.3f,6.5f);ReviewCamera.transform.LookAt(new Vector3(0,1,0));ReviewCamera.fieldOfView=38;
            var lightObject=new GameObject("Review key");lightObject.transform.SetParent(generated.transform,false);var key=lightObject.AddComponent<Light>();
            key.type=LightType.Directional;key.shadows=LightShadows.Soft;lightObject.transform.rotation=Quaternion.Euler(32,-30,0);
            weather=generated.AddComponent<WeatherPresentation>();weather.Initialize(ReviewCamera,key);
        }
        public static LocomotionInput Input(SkiMotionState state)
        {
            switch(state)
            {
                case SkiMotionState.Uphill:return new LocomotionInput(4,.08f,.9f);
                case SkiMotionState.SteepUphill:return new LocomotionInput(3,.15f,1);
                case SkiMotionState.FastSkate:return new LocomotionInput(8,0,1);
                case SkiMotionState.MaxSprint:return new LocomotionInput(9,0,1.25f,sprint:true);
                case SkiMotionState.NoPole:return new LocomotionInput(8,0,.1f);
                case SkiMotionState.DoublePole:return new LocomotionInput(9,-.07f,.9f);
                case SkiMotionState.Tuck:return new LocomotionInput(12,-.1f,.7f,tuck:true);
                case SkiMotionState.CornerLeft:return new LocomotionInput(8,0,.9f,curvature:-.03f);
                case SkiMotionState.CornerRight:return new LocomotionInput(8,0,.9f,curvature:.03f);
                case SkiMotionState.SlowStop:return new LocomotionInput(0,0,0,travelling:false);
                default:return new LocomotionInput(6,0,.9f);
            }
        }
        public void Sample(SkiMotionState state,double time)
        {
            Initialize();State=state;
            var input=Input(state);var effort=state==SkiMotionState.Fatigued?new ExertionInput(.7f,.2f,185):new ExertionInput(0,1,140);
            Pose(Original);Pose(Candidate);weather.Render("clear","high",time,Vector2.zero,0);previous=time;
            void Pose(AthleteRigBinding rig)
            {
                var driver=rig.GetComponent<AthleteHybridLocomotionDriver>();var rifle=rig.GetComponent<AthleteRiflePresentation>();var contact=rig.GetComponent<AthleteGroundContact>();
                driver.Sample(rig,input,time,effort);rifle.Sample(rig,0);
                var old=rig.GetComponent<AthleteLocomotionDriver>();contact.SampleMoving(rig,input.Grade,driver.Active?driver.PoleCycle:old.PoleCycle,driver.Active?driver.ContactState:old.State,0,time);
                driver.ApplySkiEdges(rig);rig.GetComponent<AthleteGripPresentation>().Render(rig);
            }
        }
        private void Start()
        {
            Initialize();var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-biathlonMotionCaptureOutput");
            if(!Application.isEditor&&Debug.isDebugBuild&&index>=0&&index+1<args.Length){capturing=true;StartCoroutine(Capture(args[index+1]));}
        }
        private void Update(){if(capturing)return;if(AutoCycle)State=(SkiMotionState)((int)(Time.time/8)%12);Sample(State,Time.timeAsDouble);}
        private IEnumerator Capture(string directory)
        {
            directory=Path.GetFullPath(directory);Directory.CreateDirectory(directory);Application.runInBackground=true;Application.targetFrameRate=-1;
            Original.GetComponentInChildren<SkinnedMeshRenderer>().updateWhenOffscreen=true;Candidate.GetComponentInChildren<SkinnedMeshRenderer>().updateWhenOffscreen=true;
            Original.GetComponentInChildren<SkinnedMeshRenderer>().forceMatrixRecalculationPerRender=true;Candidate.GetComponentInChildren<SkinnedMeshRenderer>().forceMatrixRecalculationPerRender=true;
            ReviewCamera.enabled=false;
            using(var target=new BenchmarkRenderTarget(1280,720))
            {
                foreach(var state in new[]{SkiMotionState.Flat,SkiMotionState.Uphill,SkiMotionState.MaxSprint})
                {
                    string folder=Path.Combine(directory,state.ToString());Directory.CreateDirectory(folder);
                    for(int i=0;i<90;i++)
                    {Sample(state,i/30.0);target.Render(ReviewCamera);yield return null;target.SavePng(Path.Combine(folder,i.ToString("D4")+".png"));}
                }
            }
            File.WriteAllText(Path.Combine(directory,"capture.json"),"{\"schema_version\":1,\"id\":\"m15_5_motion_comparison\",\"fps\":30,\"frames_per_state\":90,\"width\":1280,\"height\":720,\"left\":\"original\",\"right\":\"authored_hybrid\"}");Application.Quit(0);
        }
        private void OnGUI()
        {
            GUI.Box(new Rect(20,20,530,90),"M15.5 MOTION REVIEW | "+State+"\nLEFT: original | RIGHT: authored hybrid\nIdentical speed, grade, effort and time. No race simulation changes.");
            if(GUI.Button(new Rect(20,115,150,32),AutoCycle?"Pause state cycling":"Cycle 12 states"))AutoCycle=!AutoCycle;
            if(GUI.Button(new Rect(180,115,100,32),"Next state")){AutoCycle=false;State=(SkiMotionState)(((int)State+1)%12);}
        }
        private void OnDestroy(){weather?.Shutdown();if(generated!=null)Destroy(generated);}
    }
}

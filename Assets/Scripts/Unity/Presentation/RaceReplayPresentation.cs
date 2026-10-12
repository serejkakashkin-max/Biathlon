using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Replay;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Independent playback authority. Never calls AI Step, career completion or profile writes.</summary>
    public sealed partial class RaceReplayPresentation:MonoBehaviour
    {
        public bool LoadEnvironment=true;
        public bool Loading {get;private set;}
        public bool Active=>Host!=null;
        public bool Paused {get;private set;}
        public bool Seeking=>seekTarget>=0;
        public ProductionVenueHost Host {get;private set;}
        public RaceReplayInfo Info {get;private set;}
        public long Tick=>reader?.TickCount??0;
        public int SelectedIndex {get;private set;}
        public string CameraMode {get;private set;}="auto";
        public float Speed {get;private set;}=1;
        public string LastFailure {get;private set;}
        private FrontEndMenu menu;
        private GameObject replayRoot,uiRoot;
        private PackAiRaceRunner authority;
        private ManualScopedFieldStepper stepper;
        private Stream stream;
        private ManualRaceJournalReader reader;
        private ReplayPresentationSettings settings;
        private BroadcastCameraDirector director;
        private Camera camera;
        private Transform subject;
        private CareerSave snapshot;
        private CareerDefinitions definitions;
        private CareerCalendarEvent raceEvent;
        private long seekTarget=-1;
        private double backlog,nextTv;
        private int highlightIndex;
        private string automaticShot="track";
        private long shotUntil;
        private RaceBroadcastCutPolicy cuts;
        private readonly System.Diagnostics.Stopwatch work=new System.Diagnostics.Stopwatch();
        private static string ReadCore(string name)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(name));
        public void Launch(FrontEndMenu owner,RaceReplayInfo info,long moment=0)
        {
            if(Loading||Active||owner==null||owner.Session.Screen!=FrontEndScreen.Replays)throw new InvalidOperationException("Replay list and idle viewer required.");
            menu=owner;Info=RaceReplayInfo.FromJson(info.ToJson());settings=ReplayPresentationSettings.Load();cuts=new RaceBroadcastCutPolicy(Info.TickFrequency,settings.shot_hold_seconds);Loading=true;LastFailure=null;
            menu.RefreshFrontEnd();StartCoroutine(Create(moment));
        }
        private CareerPreparedRace Prepare()
            =>new CareerRaceProvider(ReadCore,definitions,ProductionVenueHost.ReadResource).Prepare(CareerSave.FromJson(Info.CareerSnapshotJson),raceEvent,Info.Seed);
        private IEnumerator Create(long moment)
        {
            yield return null;Exception failure=null;
            try
            {
                snapshot=CareerSave.FromJson(Info.CareerSnapshotJson);definitions=CareerDefinitions.ForSave(ReadCore,snapshot);
                for(int i=0;i<definitions.Calendar.Count;i++)if(definitions.Calendar.Get(i).Id==Info.EventId)raceEvent=definitions.Calendar.Get(i);
                if(raceEvent==null||raceEvent.VenueId!=Info.VenueId||(int)raceEvent.Format!=Info.Format||snapshot.PlayerId!=Info.PlayerId)throw new InvalidDataException("Replay event differs.");
                var prepared=Prepare();authority=prepared.Runner;replayRoot=new GameObject("Recorded race");replayRoot.transform.SetParent(transform,false);
                Host=replayRoot.AddComponent<ProductionVenueHost>();Host.AutoStart=false;Host.enabled=false;Host.InitializeCareer(prepared);stepper=Host.ManualStepper;
                stream=menu.ReplayLibrary.OpenVerified(Info);reader=new ManualRaceJournalReader(stream,stepper,Info.TickCount);
                var view=new GameObject("Replay broadcast camera",typeof(Camera));view.transform.SetParent(replayRoot.transform,false);camera=view.GetComponent<Camera>();camera.enabled=false;camera.depth=300;
                subject=new GameObject("Broadcast subject binding").transform;subject.SetParent(replayRoot.transform,false);
                director=replayRoot.AddComponent<BroadcastCameraDirector>();director.Initialize(camera,BroadcastShotDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/replay_cameras_v1")));director.Bind("subject",subject);
                SelectedIndex=Host.PlayerIndex;CameraMode="auto";Speed=1;Paused=false;
            }
            catch(Exception e){failure=e;}
            if(failure==null)
            {
                yield return null;
                try
                {
                    if(LoadEnvironment)Host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/"+Info.VenueId+"/environment")));
                    Host.UseReplayView(camera);menu.SetRaceVisible(true);BuildControls();RefreshTv();
                    Loading=false;
                    if(moment>0)Seek(Math.Max(0,moment-(long)(settings.highlight_lead_seconds*Info.TickFrequency)));
                }
                catch(Exception e){failure=e;}
            }
            Loading=false;
            if(failure!=null){LastFailure=failure.Message;Close();menu.ShowFrontEndError("replay_open_error");menu.RefreshFrontEnd();Debug.LogWarning("Replay could not open: "+failure.Message);}
        }
        public void TogglePause(){if(Active&&!Loading)Paused=!Paused;}
        public void SetSpeed(float value){if(value!=1&&value!=2&&value!=4)throw new ArgumentException("Supported replay speeds are 1, 2 and 4.");Speed=value;}
        public void SetCamera(string mode)
        {if(mode!="auto"&&mode!="track"&&mode!="wide"&&mode!="range"&&mode!="finish")throw new ArgumentException("Unknown replay camera.");CameraMode=mode;}
        public void SelectAthlete(int index){if(!Active||index<0||index>=authority.Field.Count)throw new ArgumentOutOfRangeException(nameof(index));SelectedIndex=index;CameraMode="track";}
        public void Seek(long tick)
        {
            if(!Active||Loading)return;seekTarget=Math.Max(0,Math.Min(Info.TickCount,tick));backlog=0;
            if(seekTarget<Tick)ResetAuthority();
            if(seekTarget==Tick)seekTarget=-1;
        }
        private void ResetAuthority()
        {
            reader.Dispose();stream.Dispose();var prepared=Prepare();authority=prepared.Runner;
            stepper=ManualProductionFieldComposition.Create(authority.Field,prepared.Venue,ReadCore);
            authority.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(ReadCore("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(ReadCore("manual_ski_steering_v2.json")),PlayerCourseAssistanceDefinition.FromJson(ReadCore("player_course_assistance_v1.json")));
            stream=menu.ReplayLibrary.OpenVerified(Info);reader=new ManualRaceJournalReader(stream,stepper,Info.TickCount);
            Host.RebindReplayAuthority(authority,stepper);
            Host.Feed.ResetRace();Host.Feed.Publish(RacePresentationSnapshot.Capture(authority.Field));Host.Feed.SetInterpolationAlpha(1);
            Host.GetComponent<SnowTrackPresentation>()?.Clear();Host.GetComponent<SnowSprayPresentation>()?.Clear();Host.GetComponent<BreathVaporPresentation>()?.Clear();
            highlightIndex=0;shotUntil=0;automaticShot="track";cuts.Reset();resultVerified=false;
        }
        private void Update()
        {
            if(!Active||Loading)return;
            if(Keyboard.current?.escapeKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true){Close();return;}
            if(LastFailure!=null)return;
            if(Keyboard.current?.spaceKey.wasPressedThisFrame==true)TogglePause();
            if(Keyboard.current?.leftArrowKey.wasPressedThisFrame==true)Seek(Tick-(long)(settings.seek_seconds*Info.TickFrequency));
            if(Keyboard.current?.rightArrowKey.wasPressedThisFrame==true)Seek(Tick+(long)(settings.seek_seconds*Info.TickFrequency));
            if(Keyboard.current?.cKey.wasPressedThisFrame==true)SetCamera(CameraMode=="auto"?"track":CameraMode=="track"?"wide":"auto");
            if(!Paused&&!Seeking)backlog=Math.Min(1,backlog+Time.unscaledDeltaTime*Speed);
            int count=Seeking?settings.seek_step_limit:Math.Min(settings.seek_step_limit,(int)(backlog*Info.TickFrequency));work.Restart();
            try
            {
                for(int i=0;i<count&&reader.HasNext;i++)
                {
                    reader.Step();Host.Feed.Publish(RacePresentationSnapshot.Capture(authority.Field));
                    if(!Seeking)backlog=Math.Max(0,backlog-1.0/Info.TickFrequency);
                    if(Seeking&&Tick>=seekTarget){seekTarget=-1;break;}
                    if(work.Elapsed.TotalMilliseconds>=settings.work_budget_ms)break;
                }
                Host.Feed.SetInterpolationAlpha(Seeking||Paused?1:Math.Min(1,backlog*Info.TickFrequency));
                if(!reader.HasNext){seekTarget=-1;Paused=true;VerifyResult();}
            }
            catch(Exception e){Paused=true;seekTarget=-1;LastFailure=e.Message;status.text=T("replay_open_error");Debug.LogWarning("Replay stopped: "+e.Message);}
            if(Time.unscaledTimeAsDouble>=nextTv){nextTv=Time.unscaledTimeAsDouble+settings.tv_refresh_seconds;RefreshTv();}
        }
        private bool resultVerified;
        private void VerifyResult()
        {
            if(resultVerified)return;
            var f=authority.Field;string result=f.Profile.Format==CompetitionFormat.Sprint?f.Result.ToJson():f.Profile.Format==CompetitionFormat.Pursuit?f.PursuitResult.ToJson():f.MassStartResult.ToJson();
            if(!f.IsComplete||BalanceRegression.Hash(result)!=Info.ResultSha256)throw new InvalidDataException("Replay result differs from the recorded finish.");resultVerified=true;
        }
        private void LateUpdate()
        {
            if(!Active||Loading||camera==null)return;
            while(highlightIndex<Info.Highlights.Length&&Info.Highlights[highlightIndex].Tick<=Tick)
            {
                var h=Info.Highlights[highlightIndex++];if(CameraMode!="auto")continue;
                if(!cuts.TrySelect(h))continue;
                for(int i=0;i<authority.Field.Count;i++)if(authority.Field.GetAthlete(i).AthleteId.Value==h.AthleteId)SelectedIndex=i;
                automaticShot=cuts.ShotId;shotUntil=cuts.UntilTick;
            }
            Host.Proxies.Render(Host.Feed.InterpolationAlpha??1);
            subject.position=Host.Proxies.GetProxy(authority.Field.GetAthlete(SelectedIndex).AthleteId.Value).position;
            string shot=CameraMode=="auto"?(Tick<=shotUntil?automaticShot:Host.Feed.Current.Get(SelectedIndex).ShootingActive?"range":"track"):CameraMode;
            director.Render(shot,Tick/(double)Info.TickFrequency);
        }
        public void Close()
        {
            reader?.Dispose();reader=null;stream?.Dispose();stream=null;director?.Release();director=null;
            if(uiRoot!=null){uiRoot.SetActive(false);Destroy(uiRoot);}if(replayRoot!=null){replayRoot.SetActive(false);Destroy(replayRoot);}
            if(panel!=null)Destroy(panel);panel=null;document=null;uiRoot=null;replayRoot=null;Host=null;authority=null;stepper=null;Loading=false;seekTarget=-1;backlog=0;highlightIndex=0;resultVerified=false;raceEvent=null;camera=null;subject=null;
            if(menu!=null&&menu.Root!=null){menu.SetRaceVisible(false);menu.RefreshFrontEnd();}
        }
        private void OnDestroy(){menu=null;Close();}
    }
}

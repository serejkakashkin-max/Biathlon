using System;
using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Domain;
using Biathlon.Replay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Owns one live career race and returns its authority result to the menu.</summary>
    public sealed class CareerRacePresentation : MonoBehaviour
    {
        public ProductionVenueHost Host { get; private set; }
        public bool LoadEnvironment=true;
        public bool Loading { get; private set; }
        public bool AwaitingSave { get; private set; }
        public bool Paused { get; private set; }
        private int pauseFrame=-1;
        private FrontEndMenu menu;
        private CareerPreparedRace prepared;
        private GameObject raceRoot;
        private CareerReplayCapture replayCapture;
        public LiveRaceSpectator Spectator {get;private set;}
        private static string ReadCore(string name)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(name));
        public void Launch(FrontEndMenu owner)
        {
            if(Loading||Host!=null)throw new InvalidOperationException("A race is already active.");
            if(owner==null||owner.Session.Screen!=FrontEndScreen.PreRace)throw new InvalidOperationException("Prepare a career event first.");
            menu=owner;Loading=true;menu.RefreshFrontEnd();StartCoroutine(CreateRace());
        }
        private IEnumerator CreateRace()
        {
            // Let the loading label paint before one-off composition work.
            yield return null;
            Exception failure=null;
            try
            {
                var week=menu.Session.PreparedWeek;
                var replayPolicy=ReplayPresentationSettings.Load();
                string format=week.Event.Format==CompetitionFormat.Sprint?"sprint":week.Event.Format==CompetitionFormat.Pursuit?"pursuit":"mass_start";
                var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/"+week.Event.VenueId+"/"+format+"_profile"));
                int frequency=CareerRaceDefinition.FromJson(ReadCore("career_race_v1.json")).TickFrequency;
                var highlights=new RaceHighlightDetector(profile.BoutCount,frequency,replayPolicy.close_finish_seconds,replayPolicy.highlight_cooldown_seconds,week.Event.Format==CompetitionFormat.Sprint);
                prepared=new CareerRaceProvider(ReadCore,menu.Session.Definitions,ProductionVenueHost.ReadResource)
                    .Prepare(CareerSave.FromJson(week.RaceSnapshotJson),week.Event,week.RaceSeed,highlights);
                raceRoot=new GameObject("Career race");raceRoot.transform.SetParent(transform,false);
                Host=raceRoot.AddComponent<ProductionVenueHost>();Host.AutoStart=false;Host.enabled=false;
                Host.InitializeCareer(prepared);
                Host.GameplayHud.SetPreferences(menu.Locale,menu.TextSizeIndex,menu.Session.Player.Name);
                Host.GameplayHud.SetAccessibility(menu.HighContrast);
                Host.GameplayHud.SetBroadcastRoster(menu.Session.Career);
                try
                {
                    var info=new RaceReplayInfo{Id="race_"+Guid.NewGuid().ToString("N"),RecordedUtc=DateTime.UtcNow.ToString("O"),CareerSnapshotJson=week.RaceSnapshotJson,
                        EventId=week.Event.Id,VenueId=week.Event.VenueId,Format=(int)week.Event.Format,Seed=week.RaceSeed,PlayerId=menu.Session.Career.PlayerId,PlayerName=menu.Session.Player.Name,TickFrequency=Host.Runner.Field.TickFrequency};
                    replayCapture=new CareerReplayCapture(menu.ReplayLibrary,Host.ManualStepper,info,highlights,replayPolicy);
                }
                catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){Debug.LogWarning("Replay capture unavailable: "+e.Message);}
            }
            catch(Exception e){failure=e;}
            if(failure==null)
            {
                yield return null;
                try
                {
                    if(LoadEnvironment)Host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(
                        ProductionVenueHost.ReadResource("Biathlon/Venues/"+prepared.Event.VenueId+"/environment")));
                    menu.Session.BeginRace();Host.enabled=true;menu.SetRaceVisible(true);
                }
                catch(Exception e){failure=e;}
            }
            Loading=false;
            if(failure!=null){ReleaseRace();menu.ShowFrontEndError("race_load_error");Debug.LogWarning("Career race load failed: "+failure);}
            menu.RefreshFrontEnd();
        }
        private void Update()
        {
            if(Loading||Host==null||AwaitingSave)return;
            if((Keyboard.current?.escapeKey.wasPressedThisFrame==true||Gamepad.current?.startButton.wasPressedThisFrame==true)&&pauseFrame!=Time.frameCount)
            {if(Paused)Resume();else Pause();}
            if(Paused)return;
            if(Host.HasSimulationFault)
            {Host.enabled=false;raceRoot.SetActive(false);AwaitingSave=true;menu.ShowRaceFailure();return;}
            if(Host.Runner.Field.IsComplete)SaveResult();
            else if(Spectator==null&&Host.Runner.Field.GetAthlete(Host.PlayerIndex).HasFinished)
            {Spectator=raceRoot.AddComponent<LiveRaceSpectator>();Spectator.Initialize(Host,menu);}
        }
        public void Pause()
        {
            if(Host==null||Loading||AwaitingSave)return;
            Paused=true;pauseFrame=Time.frameCount;raceRoot.SetActive(false);menu.SetRaceVisible(false);menu.RefreshFrontEnd();
        }
        public void Resume()
        {
            if(Host==null||!Paused||AwaitingSave||pauseFrame==Time.frameCount)return;
            Paused=false;pauseFrame=Time.frameCount;raceRoot.SetActive(true);menu.SetRaceVisible(true);
        }
        public void SaveResult()
        {
            if(Host==null||!Host.Runner.Field.IsComplete)throw new InvalidOperationException("Finish the complete field before saving its result.");
            Host.enabled=false;
            try
            {
                var result=prepared.CollectResult();menu.Session.FinishRace(result);
                try{replayCapture?.Publish(result);}catch(Exception e){menu.ShowFrontEndError("replay_save_error");Debug.LogWarning("Career saved, replay unavailable: "+e.Message);}
            }
            catch(Exception e) when(e is IOException||e is UnauthorizedAccessException)
            {AwaitingSave=true;raceRoot.SetActive(false);menu.SetRaceVisible(false);menu.ShowFrontEndError("result_save_error");menu.RefreshFrontEnd();return;}
            catch(Exception failure)
            {AwaitingSave=true;raceRoot.SetActive(false);Debug.LogException(failure,this);menu.ShowRaceFailure();return;}
            AwaitingSave=false;ReleaseRace();menu.SetRaceVisible(false);menu.RefreshFrontEnd();
        }
        public void Abandon()
        {
            if(Loading)throw new InvalidOperationException("Wait for loading to finish.");
            if(menu.Session.Screen==FrontEndScreen.Race)menu.Session.AbandonRace();
            AwaitingSave=false;ReleaseRace();menu.SetRaceVisible(false);menu.RefreshFrontEnd();
        }
        private void ReleaseRace()
        {replayCapture?.Dispose();replayCapture=null;if(raceRoot!=null){raceRoot.SetActive(false);Destroy(raceRoot);}raceRoot=null;Host=null;Spectator=null;prepared=null;Paused=false;}
        private void OnDestroy()=>ReleaseRace();
    }
}

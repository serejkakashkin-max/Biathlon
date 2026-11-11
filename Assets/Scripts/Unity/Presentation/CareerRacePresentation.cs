using System;
using System.Collections;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biathlon.Unity.Presentation
{
    public enum CareerRaceLoadingStage { Preparation=0, Athletes=1, Venue=2, Ready=3 }
    /// <summary>Owns one live career race and returns its authority result to the menu.</summary>
    public sealed class CareerRacePresentation : MonoBehaviour
    {
        public ProductionVenueHost Host { get; private set; }
        public bool LoadEnvironment=true;
        public bool Loading { get; private set; }
        public CareerRaceLoadingStage LoadingStage {get;private set;}
        public bool AwaitingSave { get; private set; }
        public bool Paused { get; private set; }
        private int pauseFrame=-1;
        private FrontEndMenu menu;
        private CareerPreparedRace prepared;
        private GameObject raceRoot;
        private CareerReplayCapture replayCapture;
        private static bool movementRuntimeReady;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMovementRuntime()=>movementRuntimeReady=false;
        public LiveRaceSpectator Spectator {get;private set;}
        private static string ReadCore(string name)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(name));
        public void Launch(FrontEndMenu owner)
        {
            if(Loading||Host!=null)throw new InvalidOperationException("A race is already active.");
            if(owner==null||owner.Session.Screen!=FrontEndScreen.PreRace)throw new InvalidOperationException("Prepare a career event first.");
            menu=owner;Loading=true;LoadingStage=CareerRaceLoadingStage.Preparation;menu.RefreshFrontEnd();StartCoroutine(CreateRace());
        }
        private IEnumerator CreateRace()
        {
            // UI layout and rendering must both run before each synchronous Unity
            // initialization phase. The race clock/countdown is not armed yet.
            yield return null;yield return null;
            Exception failure=null;
            var week=menu.Session.PreparedWeek;
            ReplayPresentationSettings replayPolicy=null;RaceHighlightDetector highlights=null;
            try
            {
                replayPolicy=ReplayPresentationSettings.Load();
                string format=week.Event.Format==CompetitionFormat.Sprint?"sprint":week.Event.Format==CompetitionFormat.Pursuit?"pursuit":"mass_start";
                var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/"+week.Event.VenueId+"/"+format+"_profile"));
                int frequency=CareerRaceDefinition.FromJson(ReadCore("career_race_v1.json")).TickFrequency;
                highlights=new RaceHighlightDetector(profile.BoutCount,frequency,replayPolicy.close_finish_seconds,replayPolicy.highlight_cooldown_seconds,week.Event.Format==CompetitionFormat.Sprint);
                var provider=new CareerRaceProvider(ReadCore,menu.Session.Definitions,ProductionVenueHost.ReadResource);
                prepared=provider.Prepare(CareerSave.FromJson(week.RaceSnapshotJson),week.Event,week.RaceSeed,highlights,
                    broadcastCheckpointVersion:CareerRaceProvider.CurrentBroadcastCheckpointVersion);
                if(!movementRuntimeReady&&profile.Format==CompetitionFormat.MassStart)
                {
                    // Mono compiles the first movement/commit paths on first use.
                    // Two disposable skiers cover the shared field and pair paths.
                    // It has its own RNG and event sink, with no replay observer.
                    var snow=RaceSnowConditions.FromJson(ReadCore("race_snow_neutral_v1.json"));
                    var warm=ProductionVenueRaceFactory.Create(prepared.Venue,profile,RaceRoutePlan.FromJson(prepared.Runner.Field.Route.PlanSnapshotJson),
                        prepared.Weather?.SnowConditions(snow)??snow,ReadCore,new BalanceEventAudit(5),new AiDecisionCounts(),count:2,frequency:frequency,seed:week.RaceSeed);
                    if(prepared.Timeline!=null)warm.BindWeather(prepared.Timeline,prepared.Venue.Visual);
                    var source=ManualProductionFieldComposition.Create(warm.Field,prepared.Venue,ReadCore,prepared.CourseReactionPolicy);
                    warm.BindManualScopedCandidate(source,ManualAiSteeringDefinition.FromJson(ReadCore(ManualAiSteeringDefinition.ResourceForVersion(prepared.MovingOvertakeVersion))),
                        ManualSkiSteeringDefinition.FromJson(ReadCore("manual_ski_steering_v2.json")),
                        PlayerCourseAssistanceDefinition.FromJson(ReadCore("player_course_assistance_v1.json")),prepared.AiSnowSearchVersion==1);
                    warm.Step();warm.Step();
                    movementRuntimeReady=true;
                }
            }
            catch(Exception e){failure=e;}
            if(failure==null)
            {
                SetLoadingStage(CareerRaceLoadingStage.Athletes);yield return null;yield return null;
                try
                {
                    raceRoot=new GameObject("Career race");raceRoot.transform.SetParent(transform,false);
                    Host=raceRoot.AddComponent<ProductionVenueHost>();Host.AutoStart=false;Host.enabled=false;
                    Host.InputPreferences=menu.Settings.Clone(PlayerSettingsPreferences.Catalog);Host.UseGamepad=menu.Settings.Device=="gamepad";
                    Host.InitializeCareer(prepared);
                    Host.Proxies.ConfigureCareerAppearance(CareerSave.FromJson(week.RaceSnapshotJson),menu.Session.Definitions.Calendar,menu.Session.Definitions.Scoring,week.Event);
                    Host.GameplayHud.SetPreferences(menu.Locale,menu.TextSizeIndex,menu.Session.Player.Name);
                    Host.GameplayHud.SetAccessibility(menu.HighContrast);
                    Host.GameplayHud.SetBroadcastRoster(menu.Session.Career);
                    // This independent UIDocument sorts above the menu. Detach it until
                    // the loader hands over, including the Venue and Ready frames.
                    Host.GameplayHud.enabled=false;
                    try
                    {
                        var info=new RaceReplayInfo{Id="race_"+Guid.NewGuid().ToString("N"),RecordedUtc=DateTime.UtcNow.ToString("O"),CareerSnapshotJson=week.RaceSnapshotJson,
                            EventId=week.Event.Id,VenueId=week.Event.VenueId,Format=(int)week.Event.Format,Seed=week.RaceSeed,PlayerId=menu.Session.Career.PlayerId,PlayerName=menu.Session.Player.Name,TickFrequency=Host.Runner.Field.TickFrequency,
                            RaceWeatherJson=prepared.Weather?.ToJson(),StartOrderVersion=CareerRaceProvider.CurrentStartOrderVersion,
                            StartContinuationVersion=prepared.Runner.Field.StartContinuationVersion,WeatherTimelineVersion=prepared.Timeline==null?0:CareerRaceProvider.CurrentWeatherTimelineVersion,WeatherTimelineJson=prepared.Timeline?.ToJson(),EquipmentVersion=CareerRaceProvider.CurrentEquipmentVersion,EquipmentSnapshotJson=prepared.Equipment.ToJson()};
                        info.AiSnowSearchVersion=prepared.Runner.AiSnowSearchVersion;
                        info.MovingOvertakeVersion=prepared.Runner.MovingOvertakeVersion;
                        info.BroadcastCheckpointVersion=CareerRaceProvider.CurrentBroadcastCheckpointVersion;
                        replayCapture=new CareerReplayCapture(menu.ReplayLibrary,Host.ManualStepper,info,highlights,replayPolicy);
                    }
                    catch(Exception e)when(e is IOException||e is UnauthorizedAccessException){Debug.LogWarning("Replay capture unavailable: "+e.Message);}
                }
                catch(Exception e){failure=e;}
            }
            if(failure==null)
            {
                SetLoadingStage(CareerRaceLoadingStage.Venue);yield return null;yield return null;
                try
                {
                    if(LoadEnvironment)Host.InitializeEnvironment(CareerWeatherPresentation.Bind(ProductionEnvironmentDefinition.FromJson(
                        ProductionVenueHost.ReadResource("Biathlon/Venues/"+prepared.Event.VenueId+"/environment")),prepared.Weather));
                }
                catch(Exception e){failure=e;}
            }
            if(failure==null)
            {
                // Build text geometry and upload the actual HUD while the loading
                // panel covers it. Keep the retained tree at the countdown handoff.
                Host.GameplayHud.PrepareBehindLoading(menu.GetComponent<UnityEngine.UIElements.UIDocument>().panelSettings);
                SetLoadingStage(CareerRaceLoadingStage.Ready);yield return null;yield return null;
                try
                {
                    menu.Session.BeginRace();Host.ArmRaceStartCountdown();Host.GameplayHud.FinishLoading();menu.SetRaceVisible(true);Host.enabled=true;
                }
                catch(Exception e){failure=e;}
            }
            Loading=false;
            if(failure!=null){ReleaseRace();menu.ShowFrontEndError("race_load_error");Debug.LogWarning("Career race load failed: "+failure);}
            menu.RefreshFrontEnd();
        }
        private void SetLoadingStage(CareerRaceLoadingStage stage){LoadingStage=stage;menu.RefreshFrontEnd();}
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

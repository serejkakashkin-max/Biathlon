using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit dev-player evidence only. No instance, camera or loop in ordinary play.</summary>
    public sealed class LiveShootingPlayerReview:MonoBehaviour
    {
        private sealed class Store:ICareerProfileStore
        {private string json;public bool HasProfile=>json!=null;public string ReadLatest()=>json;public void WriteNew(string id,string value)=>json=value;public void Replace(string id,string expected,string value){if(json!=expected)throw new IOException();json=value;}}
        [Serializable] private sealed class Evidence
        {
            public int schema_version=1;public string id="m16_2_native_live_board",unity;
            public string method="Actual production career MassStart,30athletes. Accepted snapshots; no edited outcomes. Main probe uses AI controls; optional miss probe uses normal player effort/aim/trigger/bolt commands with existing AI steering for reproducible off-target input. Native world plus native UI composited in GPU;20fps video advances one accepted20Hz tick per frame.";
            public bool manual_player_control;
            public bool audio_review;public int audio_hits,audio_misses,audio_played,audio_other_shots,audio_sources,audio_peak_voices,audio_samples,audio_channels,audio_rate;
            public long audio_pcm_bytes;public float audio_peak,audio_rms;
            public bool success,player_natural_order,player_miss,clean_series,queue_replacement,tab_preserved;
            public int maximum_active,maximum_overflow,distinct_displayed,frames;public long tick;
            public string[] errors;public List<Event> events=new List<Event>();public List<string> captures=new List<string>();
        }
        [Serializable] private sealed class Event
        {public long tick;public string kind,athlete;public int slot,shots,mask,marks;}
        private ProductionVenueHost host;private FrontEndMenu menu;private Evidence receipt;
        private string output;private readonly List<string> errors=new List<string>();
        private readonly HashSet<int> seen=new HashSet<int>();private readonly int[] previous=new int[30];
        private BenchmarkRenderTarget world;private RenderTexture worldTexture,overlay,combined;private Texture2D pixels;private Material composite;
        private PanelSettings panel;private Camera camera;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Boot()
        {if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonLiveShootingReview")<0)return;new GameObject("Explicit live shooting review").AddComponent<LiveShootingPlayerReview>();}
        private void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
        private IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-biathlonLiveShootingOutput");if(at<0||at+1>=args.Length){Application.Quit(1);yield break;}
            output=Path.GetFullPath(args[at+1]);Directory.CreateDirectory(output);Application.runInBackground=true;
            receipt=new Evidence{unity=Application.unityVersion,manual_player_control=Array.IndexOf(args,"-biathlonLiveShootingMissReview")>=0,audio_review=Array.IndexOf(args,"-biathlonLiveShootingAudioReview")>=0};Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Run());
            while(stack.Count>0)
            {
                object value=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(value is IEnumerator child)stack.Push(child);else yield return value;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();ReleaseTargets();if(composite!=null)Destroy(composite);
            receipt.errors=errors.ToArray();receipt.tick=host?.Runner.Field.Tick.Value??0;receipt.distinct_displayed=seen.Count;
            receipt.success=errors.Count==0&&(receipt.audio_review?
                receipt.audio_hits+receipt.audio_misses==5&&receipt.audio_played==5&&receipt.audio_other_shots>20&&receipt.audio_sources==22&&receipt.audio_peak_voices<=22&&receipt.audio_samples>10000&&receipt.audio_peak>.01f&&receipt.audio_peak<.98f&&receipt.audio_rms>.001f&&
                (receipt.manual_player_control?receipt.audio_misses==5:receipt.audio_hits==5):
                receipt.frames>100&&receipt.tab_preserved&&(receipt.manual_player_control?receipt.player_miss:receipt.maximum_active>=25&&receipt.maximum_overflow>0&&receipt.player_natural_order&&receipt.clean_series&&receipt.queue_replacement));
            File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(receipt,true));Application.logMessageReceived-=Log;Application.Quit(receipt.success?0:1);
        }
        private static string Read(string name)=>ProductionVenueHost.ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(name));
        private IEnumerator Run()
        {
            yield return null;foreach(var old in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))old.gameObject.SetActive(false);
            var root=new GameObject("Isolated review career");menu=root.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("live_board_review","Алекс Северин","NOR",11);for(int i=0;i<5;i++)menu.Session.RestWeek();menu.Session.OpenPreRace();var week=menu.Session.PreparedWeek;
            CareerCalendarEvent mass=null;for(int w=1;w<=52;w++){var e=menu.Session.Definitions.Calendar.AtWeek(w);if(e?.Format==CompetitionFormat.MassStart){mass=e;break;}}
            var prepared=new CareerRaceProvider(Read,menu.Session.Definitions,ProductionVenueHost.ReadResource).Prepare(CareerSave.FromJson(week.RaceSnapshotJson),mass,week.RaceSeed);
            var race=new GameObject("Native live broadcast production field");host=race.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.InitializeCareer(prepared);
            host.GameplayHud.SetBroadcastRoster(menu.Session.Career);host.Proxies.ConfigureCareerAppearance(menu.Session.Career,menu.Session.Definitions.Calendar,menu.Session.Definitions.Scoring,mass);
            host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment")));
            host.Environment.AutomaticRendering=false;host.Proxies.enabled=false;menu.gameObject.SetActive(false);
            var range=host.GetComponent<RangePresentation>();range.enabled=false;range.AimCamera.enabled=false;camera=host.ChaseCamera;camera.enabled=false;camera.GetComponent<AthleteChaseCamera>().enabled=false;
            var center=(host.Environment.Bank(0).transform.position+host.Environment.Bank(29).transform.position)*.5f;
            // Actual venue-local stadium overview; never writes an athlete pose.
            camera.transform.position=center+new Vector3(15,18,-88);camera.transform.LookAt(center+new Vector3(0,0,-30));camera.fieldOfView=62;
            host.Environment.SetViewCamera(camera);host.GetComponent<RenderQualityPresentation>().Apply("high");Application.targetFrameRate=-1;QualitySettings.vSyncCount=0;
            panel=(PanelSettings)typeof(GameplayHud).GetField("panel",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(host.GameplayHud);
            composite=new Material(Resources.Load<Shader>("Biathlon/UI/LiveShootingReviewComposite"));
            if(receipt.audio_review){yield return RunAudio();yield break;}
            for(int i=0;i<previous.Length;i++)previous[i]=-1;
            bool started=false,screens=false;int recording=0,missRecording=0,aimingFrames=0,frameLimit=receipt.manual_player_control?300:620;
            for(int step=0;step<16000;step++)
            {
                Advance(ref aimingFrames);
                var snapshot=RacePresentationSnapshot.Capture(host.Runner.Field);host.Feed.Publish(snapshot);host.Feed.SetInterpolationAlpha(1);host.GameplayHud.Refresh();
                var hud=host.GameplayHud;var board=hud.RangeBroadcast;
                if(!receipt.tab_preserved&&hud.RangeBroadcastVisible&&!snapshot.Get(host.PlayerIndex).ShootingActive)
                {hud.SetCheckpointTableVisible(true);Require(hud.CheckpointTableVisible&&!hud.RangeBroadcastVisible,"TAB overlay did not cover live board.");hud.SetCheckpointTableVisible(false);receipt.tab_preserved=true;}
                receipt.maximum_active=Math.Max(receipt.maximum_active,board.ActiveCount);receipt.maximum_overflow=Math.Max(receipt.maximum_overflow,board.OverflowCount);
                if(!started&&hud.RangeBroadcastVisible&&(receipt.manual_player_control?snapshot.Get(host.PlayerIndex).ShootingActive:board.ActiveCount>=25))
                {started=true;Resize(1920,1080);Directory.CreateDirectory(Path.Combine(output,"frames"));for(int warm=0;warm<4;warm++){Draw();yield return null;}}
                var ownState=board.Get(host.PlayerIndex);bool needMissClip=started&&recording>=frameLimit&&ownState.LastMissTick==snapshot.Tick;
                if(needMissClip&&!receipt.player_miss){missRecording=1;Directory.CreateDirectory(Path.Combine(output,"player-miss-frames"));}
                if(started&&(recording<frameLimit||missRecording>0))
                {
                    Draw();yield return null;
                    if(recording<frameLimit){Save("frames/"+recording.ToString("D5")+".png",false);recording++;}
                    else {Save("player-miss-frames/"+(missRecording-1).ToString("D5")+".png",false);missRecording++;}
                    receipt.frames=recording+Math.Max(0,missRecording-1);
                    bool ownMiss=false,clean=false,replace=false;int naturalSlot=-1;
                    var player=snapshot.Get(host.PlayerIndex);int visit=board.Window(player.RouteMeters,player.OnCourse,player.Phase);
                    if(visit<0)throw new InvalidOperationException("Recorded live board left its actual context.");
                    for(int slot=0;slot<hud.RangeBroadcastRows;slot++)
                    {
                        int athlete=board.Displayed(slot);var row=board.Get(athlete,visit);seen.Add(athlete);
                        if(athlete==host.PlayerIndex&&row.Active){naturalSlot=slot;if(slot>0)receipt.player_natural_order=true;}
                        if(athlete==host.PlayerIndex&&row.LastMissTick==snapshot.Tick)ownMiss=true;
                        if(row.CompletedTick==snapshot.Tick&&RangeBroadcastRow.Hits(row.ClosedMask)==5)clean=true;
                        if(previous[slot]>=0&&previous[slot]!=athlete&&!WasDisplayed(athlete))replace=true;
                        if(row.LastMissTick==snapshot.Tick||row.CompletedTick==snapshot.Tick)
                            receipt.events.Add(new Event{tick=snapshot.Tick,kind=row.CompletedTick==snapshot.Tick?"completed":"miss",athlete=row.AthleteId,slot=slot,shots=row.Shots,mask=row.ClosedMask,marks=row.ShotMarks});
                    }
                    if(!screens)
                    {
                        foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720)})
                        foreach(string locale in new[]{"ru","en"})
                        {Resize(resolution.x,resolution.y);hud.SetPreferences(locale,0);for(int warm=0;warm<4;warm++){Draw();yield return null;}Save("mass30-"+resolution.x+"-"+locale+".png");}
                        Resize(1920,1080);hud.SetPreferences("ru",0);for(int warm=0;warm<4;warm++){Draw();yield return null;}screens=true;
                    }
                    if(ownMiss&&!receipt.player_miss){receipt.player_miss=true;Save("player-miss.png");}
                    if(naturalSlot>0&&!File.Exists(Path.Combine(output,"player-natural-order.png")))Save("player-natural-order.png");
                    if(clean&&!receipt.clean_series){receipt.clean_series=true;Save("clean-five.png");}
                    if(replace&&!receipt.queue_replacement){receipt.queue_replacement=true;Save("queue-replacement.png");}
                    for(int i=0;i<previous.Length;i++)previous[i]=i<hud.RangeBroadcastRows?board.Displayed(i):-1;
                    if(recording>=frameLimit&&(!receipt.manual_player_control||receipt.player_miss&&missRecording==0||missRecording>120))break;
                }
                else if(step%200==0)yield return null;
            }
            Require(started,"Actual30-athlete range not reached.");
        }
        private void Advance(ref int aimingFrames)
        {
            if(!receipt.manual_player_control){host.Runner.Step();return;}
            var actor=host.Runner.Field.GetAthlete(host.PlayerIndex);var next=host.Runner.Field.Tick.Next();var shooting=actor.ActiveShooting;
            aimingFrames=shooting?.Phase==ShootingPhase.Aiming?aimingFrames+1:0;
            var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,actor.AthleteId,1000),1,1,0),
                new WeaponAimCommand(next,actor.AthleteId,0,1000),aimingFrames>=40,false,host.Runner.RaceWind,cycleBolt:shooting?.NeedsBoltCycle==true,manualBoltCycle:true);
            host.Runner.StepWithManualPlayer(host.PlayerIndex,input,0,true);
        }
        private IEnumerator RunAudio()
        {
            receipt.id="m16_2_native_player_crowd_audio";
            receipt.method="Actual30-athlete production career MassStart. Native AudioListener mixer capture at ordinary accepted20Hz steps. Camera audio selection deliberately follows bot1; crowd reactions remain tied to Host.PlayerIndex. Optional miss uses ordinary off-target aim/trigger/bolt input. No edited outcomes or synthesized audio.";
            var audio=BiathlonAudio.Instance;audio.SetPreferences(new BiathlonAudioPreferences{mute_unfocused=false},false);audio.SetMenu(false);audio.StopRaceVoices();
            var director=host.GetComponent<RaceAudioPresentation>();director.ReplayContext(camera,1,true);
            var capture=audio.gameObject.AddComponent<M15AudioCapture>();Resize(960,540);
            int aiming=0;bool recording=false;
            for(int step=0;step<16000;step++)
            {
                Advance(ref aiming);var snapshot=RacePresentationSnapshot.Capture(host.Runner.Field);
                if(!recording&&snapshot.Get(host.PlayerIndex).ShootingActive)
                {capture.Begin(35);recording=true;director.ReplayContext(camera,1,false);director.RenderAudio();}
                host.Feed.Publish(snapshot);host.Feed.SetInterpolationAlpha(1);host.GameplayHud.Refresh();
                if(recording)
                {
                    Draw();yield return new WaitForSecondsRealtime(1f/snapshot.TickFrequency);
                    if(director.PlayerHitReactions+director.PlayerMissReactions==5){Save("audio-final-shot.png");break;}
                }
                else if(step%200==0)yield return null;
            }
            Require(recording,"Actual player's shooting not reached for native audio.");
            yield return new WaitForSecondsRealtime(3);capture.End();yield return null;
            receipt.audio_hits=director.PlayerHitReactions;receipt.audio_misses=director.PlayerMissReactions;
            receipt.audio_played=audio.PlayedCue(AudioCue.CrowdHit)+audio.PlayedCue(AudioCue.CrowdMiss);
            receipt.audio_other_shots=director.AcceptedShots-5;receipt.audio_sources=audio.SourceCount;receipt.audio_peak_voices=audio.PeakVoices;receipt.audio_pcm_bytes=audio.PcmBytes;
            receipt.audio_samples=capture.Count;receipt.audio_channels=capture.Channels;receipt.audio_rate=AudioSettings.outputSampleRate;receipt.audio_peak=capture.Peak();receipt.audio_rms=capture.Rms();
            capture.Save(Path.Combine(output,"native-player-reactions.wav"));
        }
        private bool WasDisplayed(int athlete){foreach(int prior in previous)if(prior==athlete)return true;return false;}
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        private void Resize(int width,int height)
        {
            ReleaseTargets();world=new BenchmarkRenderTarget(width,height);
            worldTexture=(RenderTexture)typeof(BenchmarkRenderTarget).GetField("target",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(world);
            overlay=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);overlay.Create();combined=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32);combined.Create();pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            var prior=RenderTexture.active;RenderTexture.active=overlay;GL.Clear(true,true,Color.clear);RenderTexture.active=prior;ConfigureCapturePanel(panel,overlay);composite.SetTexture("_Overlay",overlay);
        }
        /// <summary>Offscreen QA panels own their color buffer; the ordinary screen panel does not.</summary>
        public static void ConfigureCapturePanel(PanelSettings settings,RenderTexture target)
        {
            // Screen-space panels normally inherit a camera-cleared buffer. A dedicated
            // UI target has no camera and must erase removed rows before each redraw.
            settings.clearColor=true;
            settings.colorClearValue=Color.clear;
            settings.targetTexture=target;
        }
        private void Draw(){host.Proxies.Render(1);host.Environment.Render(camera,"high",12);world.Render(camera);}
        private void Save(string path,bool remember=true)
        {
            Graphics.Blit(worldTexture,combined,composite);var prior=RenderTexture.active;RenderTexture.active=combined;pixels.ReadPixels(new Rect(0,0,pixels.width,pixels.height),0,0);pixels.Apply();RenderTexture.active=prior;
            File.WriteAllBytes(Path.Combine(output,path),pixels.EncodeToPNG());if(remember)receipt.captures.Add(path);
        }
        private void ReleaseTargets()
        {
            if(panel!=null)panel.targetTexture=null;world?.Dispose();world=null;worldTexture=null;
            foreach(var texture in new[]{overlay,combined})if(texture!=null){if(RenderTexture.active==texture)RenderTexture.active=null;texture.Release();Destroy(texture);}overlay=combined=null;if(pixels!=null)Destroy(pixels);pixels=null;
        }
    }
}

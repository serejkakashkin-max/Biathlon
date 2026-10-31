using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Biathlon.Presentation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in development-player acceptance; never instantiated during normal gameplay.</summary>
    public sealed partial class M15_6PlayerReview:MonoBehaviour
    {
        private sealed class Store:ICareerProfileStore
        {
            private string json;public bool HasProfile=>json!=null;
            public string ReadLatest()=>json??throw new FileNotFoundException();
            public void WriteNew(string id,string value){if(json!=null)throw new IOException();json=value;}
            public void Replace(string id,string expected,string value){if(json!=expected)throw new IOException();json=value;}
        }
        [Serializable]private sealed class Receipt
        {public int schema_version=1;public string id="m15_6_standalone_review",unity,fullscreen_mode,display_check;public bool success,owner_profiles_unchanged,fullscreen,created,appearance_restored,race_loaded,countdown_started,backup_recovered,diagnostic_exported,bindings_applied,settings_reloaded,checkpoint_verified,start_wait_skipped;public int width,height,display_width,display_height,player_start_ordinal,player_bib;public long race_tick,player_start_tick;public int[] countdown_numbers;public string[] errors,captures;public NavigationReceipt navigation;}
        [Serializable]private sealed class LoadingReceipt
        {public bool enabled,immediate,clock_held=true,countdown_unarmed=true;public int painted_stages;public List<string> captures=new List<string>();}
        private LoadingReceipt loadingReceipt;
        private readonly List<string> errors=new List<string>(),captures=new List<string>();
        private Receipt receipt;private string folder,profiles;private FrontEndMenu menu;private RenderTexture target,raceOverlay,raceCombined;private Material raceComposite;private double began;private bool finished,m16;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Boot()
        {
            var args=Environment.GetCommandLineArgs();if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(args,"-biathlonM156Review")<0&&Array.IndexOf(args,"-biathlonM16Review")<0)return;
            foreach(var old in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None)){old.AutoStart=false;old.PersistPreferences=false;}
            new GameObject("M15.6 explicit acceptance").AddComponent<M15_6PlayerReview>();
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;began=Time.realtimeSinceStartupAsDouble;var args=Environment.GetCommandLineArgs();m16=Array.IndexOf(args,"-biathlonM16Review")>=0;int at=Array.IndexOf(args,m16?"-biathlonM16ReviewOutput":"-biathlonM156ReviewOutput");
            folder=Path.GetFullPath(at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.dataPath,m16?"../M16Review":"../M15_6Review"));Directory.CreateDirectory(folder);
            receipt=new Receipt{unity=Application.unityVersion,id=m16?"m16_standalone_reliability":"m15_6_standalone_review"};profiles=Fingerprint();Application.logMessageReceived+=Log;
            var stack=new Stack<IEnumerator>();stack.Push(Review());
            while(stack.Count>0&&!finished)
            {
                object value=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}if(value is IEnumerator child)stack.Push(child);else yield return value;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();
        }
        private void Update()
        {
            if(finished||receipt==null)return;
            if(Time.realtimeSinceStartupAsDouble-began>180){errors.Add("Acceptance exceeded180s.");StopAllCoroutines();Finish();return;}
            // Hidden Windows sessions can suspend automatic camera presentation.
            // Request the same native production cameras into explicit surfaces.
            var studio=menu?.CreatorStudio;
            if(studio!=null&&studio.PreviewCamera!=null)Render(studio.PreviewCamera,studio.Texture);
            if(menu?.Race.Host!=null&&!menu.Race.Loading&&target!=null)Render(menu.Race.Host.ChaseCamera,target);
        }
        private static void Render(Camera camera,RenderTexture destination)
        {if(camera!=null&&camera.isActiveAndEnabled)UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=destination});}
        private void Log(string text,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)errors.Add(text+"\n"+stack);}
        private IEnumerator Review()
        {
            for(int i=0;i<8;i++)yield return null;
            receipt.width=Screen.width;receipt.height=Screen.height;receipt.display_width=Display.main.systemWidth;receipt.display_height=Display.main.systemHeight;receipt.fullscreen=Screen.fullScreen;receipt.fullscreen_mode=Screen.fullScreenMode.ToString();
            bool ordinaryDisplay=StandaloneDisplayStartup.UseDesktopFullscreen(Environment.GetCommandLineArgs());
            receipt.display_check=ordinaryDisplay?"ordinary_fullscreen":"explicit_windowed_QA";
            if(ordinaryDisplay)Require(receipt.fullscreen&&receipt.width==receipt.display_width&&receipt.height==receipt.display_height,"Ordinary launch must use the full desktop display.");
            foreach(var old in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))old.gameObject.SetActive(false);
            var obj=new GameObject("Isolated acceptance menu");menu=obj.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;
            var disk=m16?new FileCareerProfileStore(Path.Combine(folder,"ProfileReview")):null;
            menu.Initialize(disk??(ICareerProfileStore)new Store(),m16?new Biathlon.Replay.RaceReplayLibrary(Path.Combine(folder,"ReviewReplays")):null);menu.ApplyPreferences("ru",0);
            if(m16){menu.SetDiagnosticDirectoryForReview(Path.Combine(folder,"Reports"));menu.SetSettingsDirectoryForReview(Path.Combine(folder,"SettingsReview"));}
            target=new RenderTexture(Screen.width,Screen.height,24);target.Create();obj.GetComponent<UIDocument>().panelSettings.targetTexture=target;
            yield return Capture("01-home");Submit("new_career");yield return Capture("02-career-path");Submit("creator_mode_own");yield return Capture("03-creator");
            Require(menu.CreatorStudio!=null&&menu.CreatorStudio.Texture.IsCreated(),"Actual preview surface missing.");
            menu.Root.Q<TextField>("athlete_name").value="M15.6 native review";Submit("creator_step_1");yield return Capture("04-live-look");
            Submit("creator_step_2");yield return Capture("05-ratings");string appearance=menu.CreationDraft.Appearance.ToJson();Submit("create");yield return Capture("06-confirm");Submit("confirm_create");
            receipt.created=menu.Session.Screen==FrontEndScreen.Career;Require(receipt.created,"Actual wizard did not publish a career.");
            menu.Back();menu.Session.ContinueCareer();menu.RefreshFrontEnd();receipt.appearance_restored=menu.Session.Player.Appearance.ToJson()==appearance;Require(receipt.appearance_restored,"Saved appearance changed on Continue.");yield return Capture("07-career");
            if(m16)
            {
                menu.Session.RestWeek();string savePath=Path.Combine(folder,"ProfileReview",menu.Session.Career.Id+".json");menu.Back();File.WriteAllText(savePath,"{interrupted");Submit("continue");
                receipt.backup_recovered=disk.LastRead?.WasRecovered==true&&menu.Session.Career.Week==1;Require(receipt.backup_recovered&&File.ReadAllText(savePath)=="{interrupted","Same-career recovery failed or mutated original bytes.");
                yield return Capture("07a-recovered");menu.Session.RestWeek();
                menu.Session.OpenSettings();menu.RefreshFrontEnd();Submit("settings_page_2");menu.BeginRebind("attack","keyboard");Require(menu.CompleteRebind("<Keyboard>/r"),"Actual binding change failed.");
                var reloaded=new FilePlayerSettingsStore(Path.Combine(folder,"SettingsReview"),PlayerSettingsPreferences.Catalog).Load(new PlayerSettings());receipt.settings_reloaded=reloaded.Settings.Binding(PlayerSettingsPreferences.Catalog,"attack","keyboard")=="<Keyboard>/r";Require(receipt.settings_reloaded,"Actual settings file did not retain the accepted binding.");
                yield return Capture("07b-controls");Submit("settings_page_4");Submit("export_diagnostic");receipt.diagnostic_exported=File.Exists(menu.LastDiagnosticExport);Require(receipt.diagnostic_exported,"Local diagnostic report missing.");yield return Capture("07c-support");menu.Back();
            }
            while(menu.Session.Definitions.Calendar.AtWeek(menu.Session.Career.Week)==null)menu.Session.RestWeek();
            menu.Session.OpenPreRace();menu.RefreshFrontEnd();menu.Race.Launch(menu);double deadline=Time.realtimeSinceStartupAsDouble+80;
            bool loadingReview=Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonLoadingReview")>=0;
            if(loadingReview)loadingReceipt=new LoadingReceipt{enabled=true,immediate=menu.Root.Q("race_loading_screen")!=null&&menu.Race.Host==null};
            int loadingStage=-1,loadingFrame=-1;
            while(menu.Race.Loading&&Time.realtimeSinceStartupAsDouble<deadline)
            {
                if(loadingReview)
                {
                    int current=(int)menu.Race.LoadingStage;
                    if(menu.Race.Host!=null){loadingReceipt.clock_held&=menu.Race.Host.Runner.Field.Tick.Value==0&&!menu.Race.Host.enabled;loadingReceipt.countdown_unarmed&=!menu.Race.Host.WaitingForPlayerStart;}
                    if(current!=loadingStage){loadingStage=current;loadingFrame=Time.frameCount;}
                    else if(Time.frameCount>loadingFrame&&(loadingReceipt.painted_stages&(1<<current))==0)
                    {
                        string name="07d-loading-"+current+".png";var prior=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                        try
                        {
                            RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name),pixels.EncodeToPNG());
                            var bounds=menu.Root.Q<Label>("race_loading_title").worldBound;var rootBounds=menu.Root.worldBound;
                            int ink=0;float scaleX=target.width/rootBounds.width,scaleY=target.height/rootBounds.height;
                            for(int y=Math.Max(0,(int)(bounds.yMin*scaleY));y<Math.Min(target.height,bounds.yMax*scaleY);y+=3)
                            for(int x=Math.Max(0,(int)(bounds.xMin*scaleX));x<Math.Min(target.width,bounds.xMax*scaleX);x+=3)
                            {var color=pixels.GetPixel(x,target.height-1-y);if(color.r>.8f&&color.g>.85f&&color.b>.85f)ink++;}
                            Require(ink>20,"Compiled loading capture must contain its rendered title, not an overwritten world camera: "+current);
                        }
                        finally{RenderTexture.active=prior;Destroy(pixels);}
                        loadingReceipt.painted_stages|=1<<current;loadingReceipt.captures.Add(name);
                    }
                }
                yield return null;
            }
            if(loadingReview){File.WriteAllText(Path.Combine(folder,"loading-review.json"),JsonUtility.ToJson(loadingReceipt,true));Require(loadingReceipt.immediate&&loadingReceipt.clock_held&&loadingReceipt.countdown_unarmed&&loadingReceipt.painted_stages==15,"Actual loader did not paint all preparation stages before the race clock.");}
            var host=menu.Race.Host;Require(host!=null&&!menu.Race.Loading,"Actual production race load failed: "+menu.LastMessage);receipt.race_loaded=true;
            if(m16)
            {
                receipt.bindings_applied=host.InputPreferences.Binding(PlayerSettingsPreferences.Catalog,"attack","keyboard")=="<Keyboard>/r";Require(receipt.bindings_applied,"Host did not bind the accepted input snapshot.");
                string checkpoint=Path.Combine(folder,"ProfileReview",menu.Session.Career.Id+".json.checkpoint");receipt.checkpoint_verified=File.Exists(checkpoint)&&Biathlon.Career.CareerSave.FromJson(File.ReadAllText(checkpoint)).ToJson()==menu.Session.Career.ToJson();Require(receipt.checkpoint_verified,"Exact pre-race checkpoint missing.");
            }
            // Explicit QA only: camera and retained UI need separate buffers. A manual
            // camera render otherwise clears the UI just before a screenshot is read.
            raceOverlay=new RenderTexture(target.width,target.height,24,RenderTextureFormat.ARGB32);raceOverlay.Create();
            raceCombined=new RenderTexture(target.width,target.height,0,RenderTextureFormat.ARGB32);raceCombined.Create();
            raceComposite=new Material(Resources.Load<Shader>("Biathlon/UI/LiveShootingReviewComposite"));raceComposite.SetTexture("_Overlay",raceOverlay);
            host.ChaseCamera.targetTexture=target;foreach(var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))if(doc.panelSettings!=null)LiveShootingPlayerReview.ConfigureCapturePanel(doc.panelSettings,raceOverlay);
            bool historyReview=Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonM162HistoryReview")>=0;
            if(historyReview){receipt.navigation=new NavigationReceipt();yield return VerifyCursorMenus(host);}
            var player=host.Runner.Field.GetAthlete(host.PlayerIndex);receipt.player_start_ordinal=host.PlayerIndex+1;receipt.player_start_tick=player.ScheduledStart.Value;
            receipt.player_bib=host.Proxies.GetRig(player.AthleteId.Value).GetComponent<AthleteBibPresentation>().Number;
            Require(player.AthleteId.Value==menu.Session.Career.PlayerId&&receipt.player_bib==receipt.player_start_ordinal,"Drawn player's identity/bib binding differs.");
            if(host.CanSkipStartWait)
            {
                yield return Capture("08a-drawn-start-wait");Require(host.RequestSkipStartWait(),"Delayed Sprint could not skip its wait.");
                deadline=Time.realtimeSinceStartupAsDouble+90;
                while(host.StartWaitFastForwarding&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
                Require(!host.StartWaitFastForwarding&&!host.HasSimulationFault,"Bounded start skip failed.");receipt.start_wait_skipped=true;
            }
            receipt.countdown_started=host.WaitingForPlayerStart&&host.CountdownNumber==3;Require(receipt.countdown_started,"Personal three-second preparation countdown missing.");
            host.GameplayHud.Refresh();Require(host.GameplayHud.Root.Q("start_countdown").style.display.value==DisplayStyle.Flex&&host.GameplayHud.Root.Q<Label>("countdown_number").text=="3","Native HUD did not display the personal countdown.");
            var numbers=new List<int>{host.CountdownNumber};
            yield return Capture("08-countdown");deadline=Time.realtimeSinceStartupAsDouble+15;
            while(host.WaitingForPlayerStart&&Time.realtimeSinceStartupAsDouble<deadline){if(numbers[numbers.Count-1]!=host.CountdownNumber)numbers.Add(host.CountdownNumber);yield return null;}
            if(numbers[numbers.Count-1]!=host.CountdownNumber)numbers.Add(host.CountdownNumber);receipt.countdown_numbers=numbers.ToArray();
            Require(!host.HasSimulationFault&&!host.WaitingForPlayerStart,"Race did not leave countdown cleanly.");
            Require(numbers.Count==4&&numbers[0]==3&&numbers[1]==2&&numbers[2]==1&&numbers[3]==0,"Personal countdown skipped a displayed number.");
            for(int i=0;i<12;i++)yield return null;receipt.race_tick=host.Runner.Field.Tick.Value;Require(receipt.race_tick>0,"Shared race clock did not advance.");yield return Capture("09-race");
            if(historyReview)yield return VerifyCheckpointHistory(host);
            menu.Race.Abandon();if(historyReview)Require(UnityEngine.Cursor.visible&&UnityEngine.Cursor.lockState==CursorLockMode.None,"Returning to the menu must release the cursor.");
        }
        private void Submit(string id){var b=menu.Root.Q<Button>(id);Require(b!=null&&b.enabledInHierarchy,"Missing or disabled action: "+id);b.Focus();using(var e=NavigationSubmitEvent.GetPooled())b.SendEvent(e);}
        private IEnumerator Capture(string name)
        {
            // A disconnected/minimized desktop may never present an end-of-frame
            // backbuffer. Native panels render to the explicit target; sample the
            // following update, without depending on swapchain presentation.
            yield return new WaitForSecondsRealtime(.35f);for(int i=0;i<10;i++)yield return null;var before=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{if(raceCombined!=null)Graphics.Blit(target,raceCombined,raceComposite);RenderTexture.active=raceCombined!=null?raceCombined:target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();float low=1,high=0;for(int y=0;y<pixels.height;y+=32)for(int x=0;x<pixels.width;x+=32){float l=pixels.GetPixel(x,y).grayscale;low=Mathf.Min(low,l);high=Mathf.Max(high,l);}Require(high-low>.05f,"Native capture must contain visible UI/scene: "+name);File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());captures.Add(name+".png");}
            finally{RenderTexture.active=before;Destroy(pixels);}
        }
        private static string Fingerprint()
        {
            string directory=Path.Combine(Application.persistentDataPath,"Profiles");if(!Directory.Exists(directory))return "absent";
            var files=Directory.GetFiles(directory,"*",SearchOption.AllDirectories);Array.Sort(files,StringComparer.Ordinal);var text=new System.Text.StringBuilder();using(var hash=SHA256.Create())foreach(string file in files)text.Append(file).Append(':').Append(Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(file)))).Append('\n');return text.ToString();
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private void Finish()
        {
            if(finished)return;finished=true;Application.logMessageReceived-=Log;
            receipt.owner_profiles_unchanged=profiles==Fingerprint();if(!receipt.owner_profiles_unchanged)errors.Add("Owner profile directory changed.");receipt.errors=errors.ToArray();receipt.captures=captures.ToArray();receipt.success=errors.Count==0&&receipt.race_tick>0;
            File.WriteAllText(Path.Combine(folder,"review.json"),JsonUtility.ToJson(receipt,true));Application.Quit(receipt.success?0:1);
        }
    }
}

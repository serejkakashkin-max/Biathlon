using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Replay;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Explicit development-player review. No instance, update or allocation exists without its command-line flag.</summary>
    public sealed class FrostlineStandaloneReview : MonoBehaviour
    {
        private const string Flag="-biathlonFrostlineReview";
        [Serializable] private sealed class Evidence
        {
            public int schema_version=1;
            public string id="frostline_standalone_native_review",unity,output,results_source="isolated recorded-result boundary fixture";
            public bool success,career_preserved,isolated_profile_store=true,real_race_loaded,real_race_started;
            public bool menu_art_only,menu_ux_only,pulse_review,fullscreen;
            public string game_title_ru,game_title_en,storage_product;
            public int artwork_checked;
            public string fullscreen_mode;
            public int width,height,transition_frames;
            public long race_tick;
            public string[] screenshots,errors,navigation;
        }
        private sealed class MemoryStore : ICareerProfileStore
        {
            private string json;
            public bool HasProfile=>json!=null;
            public string ReadLatest()=>json??throw new FileNotFoundException();
            public void WriteNew(string id,string value)=>json=value;
            public void Replace(string id,string expected,string value)
            {if(json!=expected)throw new IOException("Review profile changed unexpectedly.");json=value;}
        }
        private readonly List<string> errors=new List<string>(),screens=new List<string>(),navigation=new List<string>();
        private readonly List<GameObject> disabledMenus=new List<GameObject>();
        private FrontEndMenu menu;
        private GameObject reviewRoot;
        private Evidence evidence;
        private string folder,ownerProfilesBefore;
        private bool finished,recordFrames;
        private double began;
        private RenderTexture captureTarget;
        private readonly Dictionary<Camera,RenderTexture> cameraTargets=new Dictionary<Camera,RenderTexture>();
        private readonly Dictionary<PanelSettings,RenderTexture> panelTargets=new Dictionary<PanelSettings,RenderTexture>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void StartIfRequested()
        {
            if(Application.isEditor||!Debug.isDebugBuild||(Array.IndexOf(Environment.GetCommandLineArgs(),Flag)<0&&Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonMenuArtReview")<0&&Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonMenuUxReview")<0&&Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonPulseReview")<0))return;
            new GameObject("Frostline explicit standalone review").AddComponent<FrostlineStandaloneReview>();
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;began=Time.realtimeSinceStartupAsDouble;
            string[] args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-biathlonFrostlineReviewOutput");
            folder=Path.GetFullPath(output>=0&&output+1<args.Length?args[output+1]:Path.Combine(Application.dataPath,"../FrostlineReview"));
            recordFrames=Array.IndexOf(args,"-biathlonFrostlineReviewFrames")>=0;
            evidence=new Evidence{unity=Application.unityVersion,output=folder,width=Screen.width,height=Screen.height};
            evidence.pulse_review=Array.IndexOf(args,"-biathlonPulseReview")>=0;
            evidence.menu_ux_only=evidence.pulse_review||Array.IndexOf(args,"-biathlonMenuUxReview")>=0;
            evidence.menu_art_only=evidence.menu_ux_only||Array.IndexOf(args,"-biathlonMenuArtReview")>=0;
            if(evidence.menu_art_only){evidence.id="m166_menu_art_standalone_review";evidence.results_source="isolated menu-only fixture; no race result";}
            if(evidence.menu_ux_only)evidence.id="m166_menu_ux_standalone_review";
            if(evidence.pulse_review)evidence.id="m166_pulse_standalone_review";
            Application.logMessageReceived+=OnLog;
            // Explicit stack permits errors in nested native-capture coroutines to produce a structured failure receipt.
            var stack=new Stack<IEnumerator>();stack.Push(Review());
            while(stack.Count>0&&!finished)
            {
                object current=null;bool more=false;Exception failure=null;
                try{more=stack.Peek().MoveNext();if(more)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){errors.Add(failure.ToString());break;}
                if(!more){(stack.Pop() as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator child)stack.Push(child);else yield return current;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();
            Finish();
        }
        private void Update()
        {
            if(evidence!=null&&!finished&&Time.realtimeSinceStartupAsDouble-began>300){errors.Add("Review exceeded the 300 second deadline.");StopAllCoroutines();Finish();}
            if(!finished&&captureTarget!=null&&menu?.Race.Host!=null)
            {
                var host=menu.Race.Host;var range=host.GetComponent<RangePresentation>();
                var camera=range!=null&&range.Visible?range.AimCamera:host.ChaseCamera;
                if(camera!=null&&camera.isActiveAndEnabled)
                    UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=captureTarget});
            }
        }
        private void OnLog(string condition,string trace,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(condition+"\n"+trace);}
        private IEnumerator Review()
        {
            Directory.CreateDirectory(folder);ownerProfilesBefore=ProfileFingerprint();
            for(int i=0;i<4;i++)yield return null;
            foreach(var existing in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))
            {if(existing.gameObject.activeSelf){disabledMenus.Add(existing.gameObject);existing.gameObject.SetActive(false);}}
            reviewRoot=new GameObject("Isolated Frostline review menu");menu=reviewRoot.AddComponent<FrontEndMenu>();
            menu.AutoStart=false;menu.PersistPreferences=false;
            menu.Initialize(new MemoryStore(),new RaceReplayLibrary(Path.Combine(folder,"ReviewReplays")));
            menu.ApplyPreferences("ru",0);menu.ApplyAccessibility(false);
            if(evidence.menu_art_only){yield return ReviewMenuArt();yield break;}
            yield return Capture("01-home");
            if(recordFrames)yield return TransitionFrames();
            else {Submit("new_career");yield return Settle();}
            Require(menu.Session.Screen==FrontEndScreen.NewCareer,"New career button did not navigate.");
            yield return Capture("02-new-career");
            var name=menu.Root.Q<TextField>("athlete_name");Require(name!=null,"Name control missing.");name.value="Александр Северин";
            Submit("create");yield return Settle();
            Require(menu.Session.Screen==FrontEndScreen.Career&&menu.Session.Player.Name=="Александр Северин","Creation did not use actual form values.");
            string career=menu.Session.Career.ToJson();yield return Capture("03-dashboard");
            Submit("career_training");yield return Capture("04-training");
            Require(menu.Session.Screen==FrontEndScreen.Training,"Training navigation failed.");
            Submit("back");yield return Settle();Submit("career_equipment");yield return Capture("05-equipment");
            Require(menu.Session.Screen==FrontEndScreen.Equipment,"Equipment navigation failed.");
            Submit("back");yield return Capture("06-calendar");
            Require(menu.Session.Career.ToJson()==career,"Read-only menu exploration mutated the fixture career.");
            for(int i=0;i<5;i++){Submit("rest_week");yield return Settle();}
            Submit("prepare");yield return Capture("07-pre-race");
            Require(menu.Session.Screen==FrontEndScreen.PreRace,"Preparation navigation failed.");
            string preparedCareer=menu.Session.Career.ToJson();
            Submit("start_race");
            double deadline=Time.realtimeSinceStartupAsDouble+60;
            while(menu.Race.Loading&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Require(!menu.Race.Loading&&menu.Race.Host!=null&&menu.Session.Screen==FrontEndScreen.Race,"Actual race loading failed: "+menu.LastMessage);
            var host=menu.Race.Host;evidence.real_race_loaded=host.Environment!=null;
            Require(evidence.real_race_loaded,"Production environment was not loaded.");
            if(host.CanSkipStartWait)host.RequestSkipStartWait();
            yield return Capture("08-race-countdown");
            deadline=Time.realtimeSinceStartupAsDouble+30;
            while(host.WaitingForPlayerStart&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Require(!host.WaitingForPlayerStart&&!host.HasSimulationFault,"Personal countdown failed to release player.");
            evidence.real_race_started=true;evidence.race_tick=host.Runner.Field.Tick.Value;
            yield return Capture("09-race-hud");
            if(recordFrames)yield return HudFrames(30);
            host.GameplayHud.SetBroadcastVisible(true);yield return Capture("10-race-broadcast");host.GameplayHud.SetBroadcastVisible(false);
            host.enabled=false;host.UseManualHeading=false;
            bool aiming=false;
            for(int step=0;step<30000;step++)
            {
                var tick=host.Runner.Field.Tick.Next();var actor=host.Runner.Field.GetAthlete(host.PlayerIndex);
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,actor.AthleteId,750),1,1,0),
                    new WeaponAimCommand(tick,actor.AthleteId,0,0),false);
                // Paid shared route assistance drives this isolated visual probe; never bypass the
                // bound player's command requirement or mutate/teleport authoritative state.
                host.Runner.StepWithManualPlayer(host.PlayerIndex,input,0,true);
                if(step%20==0)
                {
                    var observation=RacePresentationSnapshot.Capture(host.Runner.Field);
                    if(observation.Get(host.PlayerIndex).ShootingPhase==ShootingPhase.Aiming)
                    {host.Feed.Publish(observation);aiming=true;break;}
                }
                if(step%100==0)yield return null;
            }
            Require(aiming,"Actual field did not reach shooting during bounded review.");
            host.Proxies.Render(1);host.GetComponent<RangePresentation>().Refresh();host.GameplayHud.Refresh();
            yield return Capture("11-shooting-hud");
            if(recordFrames)yield return HudFrames(30);
            // Leave through the live-race adapter before showing the documented results boundary fixture.
            menu.Race.Abandon();yield return Settle();
            Require(menu.Session.Career.ToJson()==preparedCareer,"Abandoning the probe race changed the career week.");
            FinishResultFixture();menu.RefreshFrontEnd();yield return Capture("11-results");
            Submit("career_return");yield return Settle();Require(menu.Session.Screen==FrontEndScreen.Career,"Results return navigation failed.");
        }
        private void Submit(string id)
        {
            var button=menu.Root.Q<Button>(id);
            Require(button!=null&&button.enabledInHierarchy,"Missing or disabled button: "+id);
            for(var element=(VisualElement)button;element!=null;element=element.parent)
                Require(element.resolvedStyle.display!=DisplayStyle.None,"Hidden button: "+id);
            button.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(button);button.Focus();
            using(var action=NavigationSubmitEvent.GetPooled())button.SendEvent(action);
            navigation.Add(id+" → "+menu.Session.Screen);
        }
        private IEnumerator ReviewMenuArt()
        {
            evidence.width=Screen.width;evidence.height=Screen.height;evidence.fullscreen=Screen.fullScreen;evidence.fullscreen_mode=Screen.fullScreenMode.ToString();
            Require(evidence.fullscreen,"Fullscreen menu review required.");
            if(evidence.menu_ux_only)yield return Capture("00-home");
            if(evidence.pulse_review)
            {
                evidence.game_title_ru=menu.Root.Q<Label>("game_title").text;
                Require(evidence.game_title_ru==GameBranding.RussianTitle&&GameBranding.WindowTitle==GameBranding.RussianTitle,"Russian title not applied to menu/window.");
                menu.ApplyPreferences("en",0);yield return Capture("00-home-en");
                evidence.game_title_en=menu.Root.Q<Label>("game_title").text;
                Require(evidence.game_title_en==GameBranding.EnglishTitle&&GameBranding.WindowTitle==GameBranding.EnglishTitle,"English title not applied to menu/window.");
                evidence.storage_product=Application.productName;
                Require(evidence.storage_product=="Biathlon","Legacy persistence identity must remain stable.");
                menu.ApplyPreferences("ru",0);yield return Settle();
            }
            menu.Session.OpenNewCareer();menu.Session.CreateCareer("career_menu_art_review","Александр Северин","NOR",11);
            string before=menu.Session.Career.ToJson();
            if(evidence.menu_ux_only){menu.RefreshFrontEnd();yield return Capture("00-dashboard");}
            menu.Session.OpenCareerPage(FrontEndScreen.Training);menu.RefreshFrontEnd();yield return Settle();
            for(int i=0;i<6;i++){CheckMenuArt("training_art_"+i,"training_"+i);}
            menu.Root.Q<ScrollView>("page_content").ScrollTo(menu.Root.Q("training_card_0"));yield return Capture("01-training");
            if(!evidence.menu_ux_only){Submit("back");yield return Settle();}
            Submit("career_equipment");yield return Settle();
            CheckProducts();yield return Capture("02-skis");Submit("equipment_rifles");yield return Settle();CheckProducts();yield return Capture("03-rifles");
            Require(evidence.artwork_checked==18,"All eighteen shipping resources must bind.");
            Require(menu.Session.Career.ToJson()==before,"Menu-only exploration changed career state.");
        }
        private void CheckProducts()
        {
            foreach(var card in menu.Root.Q("equipment_tiles").Children())
            {
                string id=card.name.Substring("equipment_card_".Length);
                CheckMenuArt((id.StartsWith("rifle_")?"rifle_art_":"ski_art_")+id,id);
            }
        }
        private void CheckMenuArt(string name,string id)
        {
            var art=menu.Root.Q<Image>(name);
            Require(art!=null&&art.image!=null&&art.image==Resources.Load<Texture2D>(CareerMenuArt.ResourceRoot+id),"Shipping artwork missing: "+id);
            evidence.artwork_checked++;
        }
        private static IEnumerator Settle(){for(int i=0;i<8;i++)yield return null;yield return new WaitForSecondsRealtime(.3f);}
        private IEnumerator Capture(string name)
        {
            BindCaptureSurface();
            yield return Settle();yield return new WaitForEndOfFrame();
            var image=ReadCaptureSurface();
            try
            {
                Require(image!=null&&image.width>100,"Native screen capture failed: "+name);
                float lo=1,hi=0;
                for(int y=0;y<image.height;y+=32)for(int x=0;x<image.width;x+=32)
                {float value=image.GetPixel(x,y).grayscale;lo=Math.Min(lo,value);hi=Math.Max(hi,value);}
                Require(hi-lo>.15f,"Native screenshot is blank: "+name);
                File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());screens.Add(name+".png");
            }
            finally{if(image!=null)Destroy(image);}
        }
        private IEnumerator TransitionFrames()
        {
            BindCaptureSurface();
            string directory=Path.Combine(folder,"transition");Directory.CreateDirectory(directory);
            double next=Time.realtimeSinceStartupAsDouble;
            for(int i=0;i<48;i++)
            {
                if(i==16)Submit("new_career");
                while(Time.realtimeSinceStartupAsDouble<next)yield return null;
                yield return new WaitForEndOfFrame();var image=ReadCaptureSurface();
                try{File.WriteAllBytes(Path.Combine(directory,"frame-"+i.ToString("D4")+".png"),image.EncodeToPNG());}
                finally{if(image!=null)Destroy(image);}
                evidence.transition_frames++;next+=1.0/15;
            }
        }
        private IEnumerator HudFrames(int count)
        {
            BindCaptureSurface();
            string directory=Path.Combine(folder,"transition");Directory.CreateDirectory(directory);
            for(int i=0;i<count;i++)
            {
                yield return new WaitForSecondsRealtime(1f/15);yield return new WaitForEndOfFrame();
                var frame=ReadCaptureSurface();
                try{File.WriteAllBytes(Path.Combine(directory,"frame-"+evidence.transition_frames.ToString("D4")+".png"),frame.EncodeToPNG());}
                finally{if(frame!=null)Destroy(frame);}
                evidence.transition_frames++;
            }
        }
        private void BindCaptureSurface()
        {
            // A hidden Windows window can stop presenting its backbuffer. Render the same native
            // cameras and UI into an explicit stencil-enabled surface, without changing quality.
            if(captureTarget==null){captureTarget=new RenderTexture(Screen.width,Screen.height,24);captureTarget.Create();}
            foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if(!cameraTargets.ContainsKey(camera))cameraTargets.Add(camera,camera.targetTexture);
                camera.targetTexture=captureTarget;
            }
            foreach(var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var settings=document.panelSettings;if(settings==null)continue;
                if(!panelTargets.ContainsKey(settings))panelTargets.Add(settings,settings.targetTexture);
                settings.targetTexture=captureTarget;
            }
        }
        private Texture2D ReadCaptureSurface()
        {
            var previous=RenderTexture.active;RenderTexture.active=captureTarget;
            var frame=new Texture2D(captureTarget.width,captureTarget.height,TextureFormat.RGBA32,false);
            frame.ReadPixels(new Rect(0,0,frame.width,frame.height),0,0);frame.Apply();RenderTexture.active=previous;return frame;
        }
        private void FinishResultFixture()
        {
            var week=menu.Session.PreparedWeek;var saved=CareerSave.FromJson(week.RaceSnapshotJson);var rows=new CareerFinish[saved.AthleteCount];
            for(int i=0;i<rows.Length;i++)
            {
                var a=saved.Athlete((i+23)%rows.Length);
                var detail=new CareerResultDetails(a.Id,new[]{31,27},new[]{ShootingStance.Prone,ShootingStance.Standing},
                    new[]{new CareerResultSplit("split_1","lap_1",75000,75000),new CareerResultSplit("split_2","lap_2",210000,210000)});
                rows[i]=new CareerFinish(a.Id,300000+i*1523,300000+i*1523,1,details:detail);
            }
            var result=new CareerRaceRecord(saved.SeasonId+"_"+week.Event.Id,saved.SeasonId,week.Event.Id,week.Event.Format,week.RaceSeed,new string('a',64),rows);
            menu.Session.BeginRace();menu.Session.FinishRace(result);
        }
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private static string ProfileFingerprint()
        {
            string directory=Path.Combine(Application.persistentDataPath,"Profiles");if(!Directory.Exists(directory))return "absent";
            var files=Directory.GetFiles(directory,"*",SearchOption.AllDirectories);Array.Sort(files,StringComparer.Ordinal);
            var digest=new System.Text.StringBuilder();using(var hash=SHA256.Create())foreach(string file in files)
                digest.Append(file).Append(':').Append(Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(file)))).Append('\n');
            return digest.ToString();
        }
        private void Finish()
        {
            if(finished)return;finished=true;Application.logMessageReceived-=OnLog;
            try
            {
                if(menu!=null&&menu.Race.Host!=null)menu.Race.Abandon();
                evidence.career_preserved=ownerProfilesBefore!=null&&ownerProfilesBefore==ProfileFingerprint();
                if(!evidence.career_preserved)errors.Add("Owner profile directory fingerprint changed during review.");
                evidence.screenshots=screens.ToArray();evidence.errors=errors.ToArray();evidence.navigation=navigation.ToArray();
                evidence.success=errors.Count==0&&evidence.career_preserved&&(evidence.menu_art_only?evidence.artwork_checked==18&&screens.Count==(evidence.pulse_review?6:evidence.menu_ux_only?5:3):evidence.real_race_started&&screens.Count>=11);
                Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,"standalone-review.json"),JsonUtility.ToJson(evidence,true));
            }
            catch(Exception e){Debug.LogException(e);evidence.success=false;}
            if(reviewRoot!=null)Destroy(reviewRoot);
            foreach(var pair in cameraTargets)if(pair.Key!=null)pair.Key.targetTexture=pair.Value;
            foreach(var pair in panelTargets)if(pair.Key!=null)pair.Key.targetTexture=pair.Value;
            if(captureTarget!=null)Destroy(captureTarget);
            foreach(var previous in disabledMenus)if(previous!=null)previous.SetActive(true);
            Application.Quit(evidence.success?0:1);
        }
    }
}

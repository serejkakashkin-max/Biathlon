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
    public sealed class M15_6PlayerReview:MonoBehaviour
    {
        private sealed class Store:ICareerProfileStore
        {
            private string json;public bool HasProfile=>json!=null;
            public string ReadLatest()=>json??throw new FileNotFoundException();
            public void WriteNew(string id,string value){if(json!=null)throw new IOException();json=value;}
            public void Replace(string id,string expected,string value){if(json!=expected)throw new IOException();json=value;}
        }
        [Serializable]private sealed class Receipt
        {public int schema_version=1;public string id="m15_6_standalone_review",unity,fullscreen_mode,display_check;public bool success,owner_profiles_unchanged,fullscreen,created,appearance_restored,race_loaded,countdown_started;public int width,height,display_width,display_height;public long race_tick;public string[] errors,captures;}
        private readonly List<string> errors=new List<string>(),captures=new List<string>();
        private Receipt receipt;private string folder,profiles;private FrontEndMenu menu;private RenderTexture target;private double began;private bool finished;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonM156Review")<0)return;
            new GameObject("M15.6 explicit acceptance").AddComponent<M15_6PlayerReview>();
        }
        private IEnumerator Start()
        {
            Application.runInBackground=true;began=Time.realtimeSinceStartupAsDouble;var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-biathlonM156ReviewOutput");
            folder=Path.GetFullPath(at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.dataPath,"../M15_6Review"));Directory.CreateDirectory(folder);
            receipt=new Receipt{unity=Application.unityVersion};profiles=Fingerprint();Application.logMessageReceived+=Log;
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
        private void Update(){if(!finished&&receipt!=null&&Time.realtimeSinceStartupAsDouble-began>180){errors.Add("Acceptance exceeded180s.");StopAllCoroutines();Finish();}}
        private void Log(string text,string stack,LogType kind){if(kind==LogType.Error||kind==LogType.Exception||kind==LogType.Assert)errors.Add(text+"\n"+stack);}
        private IEnumerator Review()
        {
            for(int i=0;i<8;i++)yield return null;
            receipt.width=Screen.width;receipt.height=Screen.height;receipt.display_width=Display.main.systemWidth;receipt.display_height=Display.main.systemHeight;receipt.fullscreen=Screen.fullScreen;receipt.fullscreen_mode=Screen.fullScreenMode.ToString();
            bool ordinaryDisplay=StandaloneDisplayStartup.UseDesktopFullscreen(Environment.GetCommandLineArgs());
            receipt.display_check=ordinaryDisplay?"ordinary_fullscreen":"explicit_windowed_QA";
            if(ordinaryDisplay)Require(receipt.fullscreen&&receipt.width==receipt.display_width&&receipt.height==receipt.display_height,"Ordinary launch must use the full desktop display.");
            foreach(var old in FindObjectsByType<FrontEndMenu>(FindObjectsSortMode.None))old.gameObject.SetActive(false);
            var obj=new GameObject("Isolated M15.6 acceptance menu");menu=obj.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;menu.Initialize(new Store());menu.ApplyPreferences("ru",0);
            target=new RenderTexture(Screen.width,Screen.height,24);target.Create();obj.GetComponent<UIDocument>().panelSettings.targetTexture=target;
            yield return Capture("01-home");Submit("new_career");yield return Capture("02-career-path");Submit("creator_mode_own");yield return Capture("03-creator");
            menu.Root.Q<TextField>("athlete_name").value="M15.6 native review";Submit("creator_step_1");yield return Capture("04-live-look");
            Submit("creator_step_2");yield return Capture("05-ratings");string appearance=menu.CreationDraft.Appearance.ToJson();Submit("create");yield return Capture("06-confirm");Submit("confirm_create");
            receipt.created=menu.Session.Screen==FrontEndScreen.Career;Require(receipt.created,"Actual wizard did not publish a career.");
            menu.Back();menu.Session.ContinueCareer();menu.RefreshFrontEnd();receipt.appearance_restored=menu.Session.Player.Appearance.ToJson()==appearance;Require(receipt.appearance_restored,"Saved appearance changed on Continue.");yield return Capture("07-career");
            while(menu.Session.Definitions.Calendar.AtWeek(menu.Session.Career.Week)==null)menu.Session.RestWeek();
            menu.Session.OpenPreRace();menu.RefreshFrontEnd();menu.Race.Launch(menu);double deadline=Time.realtimeSinceStartupAsDouble+80;
            while(menu.Race.Loading&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            var host=menu.Race.Host;Require(host!=null&&!menu.Race.Loading,"Actual production race load failed: "+menu.LastMessage);receipt.race_loaded=true;
            receipt.countdown_started=host.WaitingForPlayerStart;Require(receipt.countdown_started,"Personal preparation countdown missing.");
            host.ChaseCamera.targetTexture=target;foreach(var doc in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))if(doc.panelSettings!=null)doc.panelSettings.targetTexture=target;
            yield return Capture("08-countdown");deadline=Time.realtimeSinceStartupAsDouble+15;
            while(host.WaitingForPlayerStart&&Time.realtimeSinceStartupAsDouble<deadline)yield return null;
            Require(!host.HasSimulationFault&&!host.WaitingForPlayerStart,"Race did not leave countdown cleanly.");
            for(int i=0;i<12;i++)yield return null;receipt.race_tick=host.Runner.Field.Tick.Value;Require(receipt.race_tick>0,"Shared race clock did not advance.");yield return Capture("09-race");menu.Race.Abandon();
        }
        private void Submit(string id){var b=menu.Root.Q<Button>(id);Require(b!=null&&b.enabledInHierarchy,"Missing or disabled action: "+id);b.Focus();using(var e=NavigationSubmitEvent.GetPooled())b.SendEvent(e);}
        private IEnumerator Capture(string name)
        {
            for(int i=0;i<8;i++)yield return null;yield return new WaitForEndOfFrame();var before=RenderTexture.active;var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),pixels.EncodeToPNG());captures.Add(name+".png");}
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

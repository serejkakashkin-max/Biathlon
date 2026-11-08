#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Presentation;
using Biathlon.Replay;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in player/editor smoke on explicitly marked career copies.</summary>
    public sealed class Biathlete167PlayerReview:MonoBehaviour
    {
        [Serializable] private sealed class Evidence
        {
            public int schema_version=1,field_count,upgraded_athletes,save_version;
            public string id="m167_player_career_review",failure,nation,ski,headwear,boots,gloves;
            public string approach_control="separate fresh production AI race after a paid manual startup review";
            public long tick,first_range_tick;
            public bool passed,career_unchanged,range_reached,sight_body_hidden,overview_body_restored;
        }
        private string profiles,output;
        private bool quit;
        private FrontEndMenu menu;
        private GameObject menuRoot;
        private readonly List<GameObject> hidden=new List<GameObject>();
        private readonly Evidence evidence=new Evidence();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"-biathlonM167Smoke")<0)return;
            string Arg(string name){int i=Array.IndexOf(args,name);return i>=0&&i+1<args.Length?args[i+1]:null;}
            StartReview(Arg("-biathlonM167Profiles"),Arg("-biathlonM167Output"),true);
        }
        public static string StartReview(string profiles,string output,bool quit=false)
        {
            if(string.IsNullOrEmpty(profiles)||!File.Exists(Path.Combine(profiles,"M167_TEST_COPY.marker")))
                throw new ArgumentException("An explicitly marked career-copy directory is required.");
            if(string.IsNullOrEmpty(output))throw new ArgumentException("A review output directory is required.");
            Application.runInBackground=true;
#if UNITY_EDITOR
            var window=UnityEditor.EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));window.Show();window.Focus();
#endif
            var root=new GameObject("M16.7 copied-career review");DontDestroyOnLoad(root);
            var review=root.AddComponent<Biathlete167PlayerReview>();
            review.profiles=Path.GetFullPath(profiles);review.output=Path.GetFullPath(output);review.quit=quit;
            Directory.CreateDirectory(review.output);review.StartCoroutine(review.Run());
            return review.output;
        }
        private IEnumerator Run()
        {
            var routine=Execute();
            while(true)
            {
                bool more;
                try{more=routine.MoveNext();}
                catch(Exception error){evidence.failure=error.ToString();break;}
                if(!more)break;
                yield return routine.Current;
            }
            (routine as IDisposable)?.Dispose();
            if(menu!=null&&menu.Race!=null&&menu.Race.Host!=null)menu.Race.Abandon();
            if(menuRoot!=null)Object.Destroy(menuRoot);
            foreach(var root in hidden)if(root!=null)root.SetActive(true);
            evidence.passed=evidence.failure==null;
            File.WriteAllText(Path.Combine(output,"player-review.json"),JsonUtility.ToJson(evidence,true));
            Debug.Log("BIATHLON_M167_REVIEW_"+(evidence.passed?"PASS ":"FAIL ")+JsonUtility.ToJson(evidence));
            if(quit)Application.Quit(evidence.passed?0:1);
            Object.Destroy(gameObject);
        }
        private IEnumerator Execute()
        {
            yield return null;
            foreach(var studio in Object.FindObjectsByType<Biathlete167Studio>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            {hidden.Add(studio.gameObject);studio.gameObject.SetActive(false);}
            foreach(var existing in Object.FindObjectsByType<FrontEndMenu>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            {hidden.Add(existing.gameObject);existing.gameObject.SetActive(false);}
            menuRoot=new GameObject("Copied-career front end");
            menu=menuRoot.AddComponent<FrontEndMenu>();menu.AutoStart=false;menu.PersistPreferences=false;
            menu.Initialize(new FileCareerProfileStore(profiles),new RaceReplayLibrary(Path.Combine(output,"replays")));
            menu.Session.ContinueCareer();string before=menu.Session.Career.ToJson();
            evidence.save_version=menu.Session.Career.SaveVersion;
            var player=menu.Session.Player;
            evidence.nation=player.Nation;evidence.ski=player.SelectedSki;
            evidence.headwear=player.Appearance.HeadwearId;evidence.boots=player.Appearance.BootsId;evidence.gloves=player.Appearance.GlovesId;
            menu.Session.OpenPreRace();menu.Race.Launch(menu);
            double deadline=Time.realtimeSinceStartupAsDouble+180;
            while(menu.Race.Loading)
            {if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("Copied career race loading timed out.");yield return null;}
            var host=menu.Race.Host;
            if(host==null)throw new InvalidOperationException("The copied career did not create a race: "+menu.LastMessage);
            host.enabled=false;menu.Race.enabled=false;
            int ticks=60+(int)Math.Ceiling(host.PlayerStartWaitSeconds*host.Runner.Field.TickFrequency);
            if(ticks>50000)throw new InvalidOperationException("The review start wait exceeds its bounded paid prefix.");
            ProductionPlayerBuildSmoke.AdvanceAndValidate(host,ticks);
            if(host.HasSimulationFault)throw new InvalidOperationException("The real career host reported a simulation fault.");
            evidence.tick=host.Runner.Field.Tick.Value;evidence.field_count=host.Runner.Field.Count;
            foreach(var visual in host.Proxies.GetComponentsInChildren<Biathlete167Rig>(true))evidence.upgraded_athletes++;
            if(evidence.upgraded_athletes!=evidence.field_count)throw new InvalidOperationException("The career field has missing upgraded athletes.");
            var source=host.Proxies.GetRig(player.Id);
            var upgraded=source.GetComponent<Biathlete167RacePresentation>();
            if(upgraded==null||upgraded.SelectedSkiId!=player.SelectedSki||upgraded.Rig.GetComponent<Biathlete167Outfit>().Nation!=player.Nation)
                throw new InvalidOperationException("The player's saved equipment was not applied.");
            yield return null;yield return new WaitForEndOfFrame();
            var batches=host.Proxies.GetComponent<AthleteEquipmentBatches>();
            if(File.Exists(Path.Combine(output,"capture-unbatched.flag")))batches.enabled=false;
            var boundsReport=new System.Text.StringBuilder();
            boundsReport.AppendLine("source="+source.transform.position+" target="+upgraded.Rig.transform.position+" hips="+upgraded.Rig.Bone(HumanBodyBones.Hips).position+
                " foot="+upgraded.Rig.Bone(HumanBodyBones.LeftFoot).position+" ground="+host.Environment.SampleGround(upgraded.Rig.transform.position));
            foreach(var renderer in upgraded.Rig.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mesh=renderer.GetComponent<MeshFilter>();if(mesh==null)continue;
                var expected=renderer.localToWorldMatrix.MultiplyPoint3x4(mesh.sharedMesh.bounds.center);
                boundsReport.AppendLine(renderer.name+" active="+renderer.gameObject.activeInHierarchy+" enabled="+renderer.enabled+" forced="+renderer.forceRenderingOff+
                    " actual="+renderer.bounds.center+" size="+renderer.bounds.size+" expected="+expected+" error="+Vector3.Distance(renderer.bounds.center,expected));
            }
            File.WriteAllText(Path.Combine(output,"equipment-bounds.txt"),boundsReport.ToString());
            foreach(var skin in upgraded.Rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {skin.updateWhenOffscreen=true;skin.forceMatrixRecalculationPerRender=true;}
            using(var target=new BenchmarkRenderTarget(1920,1080))
            {
                var camera=host.ChaseCamera;
                host.Environment.Render(camera,"high",host.Runner.Field.Tick.Value/(float)host.Runner.Field.TickFrequency);
                if(batches.enabled)batches.Render();target.Render(camera);yield return new WaitForEndOfFrame();
                if(batches.enabled)batches.Render();target.Render(camera);yield return new WaitForEndOfFrame();
                target.SavePng(Path.Combine(output,"in-game-career.png"));
                var chase=camera.GetComponent<AthleteChaseCamera>();if(chase!=null)chase.enabled=false;
                var focus=upgraded.Rig.transform.position+Vector3.up*.95f;
                var offset=upgraded.Rig.transform.TransformDirection(new Vector3(1.1f,.15f,3.4f));
                camera.transform.SetPositionAndRotation(focus+offset,Quaternion.LookRotation(-offset,Vector3.up));
                host.Environment.Render(camera,"high",host.Runner.Field.Tick.Value/(float)host.Runner.Field.TickFrequency);
                if(batches.enabled)batches.Render();target.Render(camera);yield return new WaitForEndOfFrame();
                if(batches.enabled)batches.Render();target.Render(camera);yield return new WaitForEndOfFrame();
                target.SavePng(Path.Combine(output,"in-game-player-front.png"));
            }
            while(File.Exists(Path.Combine(output,"hold-for-review.flag")))yield return null;
            // Release the manual start review before qualifying the range through
            // a fresh, unbound AI start. The runner never changes control identity
            // mid-race and still receives its ordinary production decisions.
            menu.Race.Abandon();yield return null;
            menu.Race.enabled=true;menu.Race.Launch(menu);
            deadline=Time.realtimeSinceStartupAsDouble+180;
            while(menu.Race.Loading)
            {if(Time.realtimeSinceStartupAsDouble>deadline)throw new TimeoutException("AI range review loading timed out.");yield return null;}
            host=menu.Race.Host;
            if(host==null||host.Runner.Field.Tick.Value!=0)throw new InvalidOperationException("The autonomous range review requires a fresh race.");
            host.enabled=false;menu.Race.enabled=false;
            source=host.Proxies.GetRig(player.Id);upgraded=source.GetComponent<Biathlete167RacePresentation>();
            batches=host.Proxies.GetComponent<AthleteEquipmentBatches>();
            var range=host.GetComponent<RangePresentation>();
            for(int attempt=0;attempt<12000&&!range.Visible;attempt++)
            {
                // An opt-in visual review uses the same production AI controls to
                // follow the whole course, rather than holding a straight input
                // through every bend. No pose teleport or race clock shortcut.
                host.Runner.Step();host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));range.Refresh();
                if(host.HasSimulationFault)throw new InvalidOperationException("The paid approach to the first range faulted.");
                if(attempt%120==0){host.Proxies.Render(1);yield return null;}
            }
            host.Proxies.Render(1);range.Refresh();
            evidence.range_reached=range.Visible;
            evidence.first_range_tick=host.Runner.Field.Tick.Value;
            evidence.sight_body_hidden=!upgraded.Rig.gameObject.activeSelf;
            if(!evidence.range_reached||!evidence.sight_body_hidden)throw new InvalidOperationException("The first real aiming view did not hide the upgraded body.");
            using(var target=new BenchmarkRenderTarget(1920,1080))
            {
                host.Environment.Render(range.AimCamera,"high",host.Runner.Field.Tick.Value/(float)host.Runner.Field.TickFrequency);
                if(batches.enabled)batches.Render();target.Render(range.AimCamera);yield return new WaitForEndOfFrame();
                yield return null;
                host.Environment.Render(range.AimCamera,"high",host.Runner.Field.Tick.Value/(float)host.Runner.Field.TickFrequency);
                if(batches.enabled)batches.Render();target.Render(range.AimCamera);yield return new WaitForEndOfFrame();
                target.SavePng(Path.Combine(output,"in-game-first-range.png"));
            }
            range.OverviewRequested=true;yield return null;range.Refresh();
            evidence.overview_body_restored=upgraded.Rig.gameObject.activeSelf;
            if(!evidence.overview_body_restored)throw new InvalidOperationException("Range overview did not restore the upgraded body.");
            menu.Race.Abandon();
            evidence.career_unchanged=menu.Session.Career.ToJson()==before;
            if(!evidence.career_unchanged)throw new InvalidOperationException("The visual review changed career state.");
        }
    }
}
#endif

using System;
using System.Collections;
using Biathlon.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in development-player check; ordinary launches keep normal controls.</summary>
    public sealed class PlayerBuildSmoke : MonoBehaviour
    {
        private bool failed;
        private BenchmarkFrameCounters renderCounters;
        private BenchmarkRenderTarget renderTarget;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Application.isEditor || !Debug.isDebugBuild ||
                Array.IndexOf(Environment.GetCommandLineArgs(), "-biathlonSmoke") < 0) return;
            var go = new GameObject("Player build smoke");
            DontDestroyOnLoad(go);
            go.AddComponent<PlayerBuildSmoke>();
        }
        private void OnEnable(){Application.logMessageReceived += ObserveLog;renderCounters=new BenchmarkFrameCounters();renderTarget=new BenchmarkRenderTarget(1920,1080);Application.runInBackground=true;}
        private void OnDisable(){Application.logMessageReceived -= ObserveLog;renderCounters?.Dispose();renderTarget?.Dispose();}
        private void VerifyRendered(string scene,string tier)
        {
            var sample=renderCounters.Read(0,0,0);
            if(sample.draw_calls<=0){failed=true;Debug.LogError("Smoke requires actual drawing: "+scene+" "+tier);}
            else Debug.Log("BIATHLON_SMOKE_RENDER "+scene+" "+tier+" "+sample.draw_calls);
            var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-biathlonSmokeOutput")renderTarget.SavePng(System.IO.Path.Combine(args[i+1],scene+"-"+tier+".png"));
        }
        private IEnumerator RenderAndVerify(Camera camera,string scene,string tier)
        {
            for(int i=0;i<8;i++){renderTarget.Render(camera);yield return null;}
            VerifyRendered(scene,tier);
        }
        private void ObserveLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) failed = true;
        }
        public static void AdvanceAndValidate(PlayableSprintHost host, int ticks)
        {
            if (host == null || host.Runner == null) throw new InvalidOperationException("Scene host failed to initialize.");
            if (ticks < 1) throw new ArgumentOutOfRangeException(nameof(ticks));
            host.enabled = false;
            var field = host.Runner.Field;
            long start = field.Tick.Value;
            var id = field.GetAthlete(host.PlayerIndex).AthleteId;
            for (int i = 0; i < ticks; i++)
            {
                var tick = field.Tick.Next();
                host.Step(new PlayerCommand(tick, id, 800, 0),
                    new ShootingDeviceCommand(new WeaponAimCommand(tick, id, 0, 0), false, false));
            }
            if (field.Tick.Value != start + ticks || host.Feed.Current.Tick != field.Tick.Value ||
                field.GetAthlete(host.PlayerIndex).Motion.DistanceNanometers <= 0)
                throw new InvalidOperationException("Player simulation did not advance.");
            if (host.GetComponent<AthleteProxyPresentation>().GetProxy(id.Value) == null ||
                host.GetComponentInChildren<Camera>() == null)
                throw new InvalidOperationException("Player presentation is missing.");
            var proxies = host.GetComponent<AthleteProxyPresentation>(); proxies.Render(1);
            var rig = proxies.GetRig(id.Value); rig.Validate();
            if (rig.GetComponent<AthleteGroundContact>() == null || rig.GetComponent<AthleteRiflePresentation>() == null ||
                rig.GetComponent<AthleteShootingPose>() == null || Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBodyDistant") == null)
                throw new InvalidOperationException("M11 athlete presentation assets are missing in the player.");
        }
        private IEnumerator Start()
        {
            yield return null;
            string startup=SceneManager.GetActiveScene().name;
            if(startup=="VisualBenchmark")
            {
                var preview=FindAnyObjectByType<VisualBenchmarkPresentation>();
                if(preview==null||!preview.Ready||!preview.View.isActiveAndEnabled||preview.View.targetTexture!=null||!preview.ShowControls)
                {failed=true;Debug.LogError("Visual review startup requires an active window camera and review controls.");}
                else Debug.Log("BIATHLON_SMOKE_STARTUP VisualBenchmark CAMERA_ACTIVE CONTROLS_VISIBLE");
            }
            foreach (string scene in new[] { "PlayableSprint", "PlayableMassStart" })
            {
                if (SceneManager.GetActiveScene().name != scene) yield return SceneManager.LoadSceneAsync(scene);
                yield return null; // Let the scene composition root initialize.
                var host = FindAnyObjectByType<PlayableSprintHost>();
                try
                {
                    AdvanceAndValidate(host, 400);
                    Debug.Log("BIATHLON_SMOKE_SCENE " + JsonUtility.ToJson(new SceneEvidence {
                        scene = scene, tick = host.Feed.Current.Tick, athletes = host.Feed.Current.Count }));
                }
                catch (Exception ex) { failed = true; Debug.LogException(ex); }
                yield return null; // Exercise presentation with the resulting snapshot.
                foreach (string tier in new[] { "low", "balanced", "high" })
                {
                    try { host.GetComponent<RenderQualityPresentation>().Apply(tier); }
                    catch (Exception ex) { failed = true; Debug.LogException(ex); }
                    yield return null;
                    yield return RenderAndVerify(host.GetComponentInChildren<AthleteChaseCamera>().GetComponent<Camera>(),scene,tier);
                    Debug.Log("BIATHLON_SMOKE_QUALITY " + scene + " " + tier);
                }
            }
            yield return SceneManager.LoadSceneAsync("VisualBenchmark");yield return null;
            var benchmark=FindAnyObjectByType<VisualBenchmarkPresentation>();
            foreach(string tier in new[]{"low","balanced","high"})
            {
                try{if(benchmark==null||!benchmark.Ready||benchmark.Athletes.Count!=10)throw new InvalidOperationException("Integrated benchmark is missing.");benchmark.View.enabled=false;benchmark.Tier=tier;benchmark.ShowControls=false;benchmark.Shot="athlete";benchmark.Seek(8);}
                catch(Exception ex){failed=true;Debug.LogException(ex);}yield return null;yield return RenderAndVerify(benchmark.View,"VisualBenchmark",tier);Debug.Log("BIATHLON_SMOKE_BENCHMARK "+tier);
            }
            Debug.Log(failed ? "BIATHLON_SMOKE_FAIL" : "BIATHLON_SMOKE_PASS");
            Application.Quit(failed ? 1 : 0);
        }
        [Serializable] private sealed class SceneEvidence
        {
            public int schema_version = 1;
            public string id = "biathlon_player_smoke_v1", scene;
            public long tick;
            public int athletes;
        }
    }
}

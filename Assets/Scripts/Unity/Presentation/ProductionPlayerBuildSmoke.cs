using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Development-player content check. Hidden Windows players may suspend window rendering.</summary>
    public sealed class ProductionPlayerBuildSmoke : MonoBehaviour
    {
        private bool failed,previousBackground;
        private BenchmarkFrameCounters counters;
        private readonly Dictionary<Camera,string> firstTiers=new Dictionary<Camera,string>();
        private readonly Dictionary<Camera,int> renderedFrames=new Dictionary<Camera,int>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Boot()
        {
            var args=System.Environment.GetCommandLineArgs();
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(args,"-biathlonProductionSmoke")<0)return;
            var go=new GameObject("Production player content smoke");DontDestroyOnLoad(go);go.AddComponent<ProductionPlayerBuildSmoke>();
        }
        private void OnEnable()
        {
            previousBackground=Application.runInBackground;Application.runInBackground=true;
            Application.logMessageReceived+=ObserveLog;RenderPipelineManager.endCameraRendering+=ObserveCamera;counters=new BenchmarkFrameCounters();
        }
        private void OnDisable()
        {
            Application.logMessageReceived-=ObserveLog;RenderPipelineManager.endCameraRendering-=ObserveCamera;
            counters?.Dispose();counters=null;Application.runInBackground=previousBackground;
        }
        private void ObserveLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failed=true;}
        private void ObserveCamera(ScriptableRenderContext context,Camera camera)
        {
            var host=FindAnyObjectByType<ProductionVenueHost>();
            if(host==null||camera!=host.ChaseCamera||host.Runner==null)return;
            var key=camera;if(!firstTiers.ContainsKey(key))firstTiers.Add(key,host.GetComponent<RenderQualityPresentation>().TierId);
            renderedFrames.TryGetValue(key,out int frames);renderedFrames[key]=frames+1;
        }
        public static void AdvanceAndValidate(ProductionVenueHost host,int ticks)
        {
            if(host==null||host.Runner==null)throw new ArgumentException("Initialized production host required.");
            if(ticks<1)throw new ArgumentOutOfRangeException(nameof(ticks));
            host.enabled=false;var field=host.Runner.Field;long start=field.Tick.Value;
            long distance=field.GetAthlete(host.PlayerIndex).Motion.DistanceNanometers;
            var id=field.GetAthlete(host.PlayerIndex).AthleteId;
            for(int i=0;i<ticks;i++)
            {
                var tick=field.Tick.Next();var shooting=new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false);
                if(host.ManualStepper!=null)host.Step(new ManualSkiCommand(tick,id,800,0),shooting);
                else host.Step(new PlayerCommand(tick,id,800,0),shooting);
            }
            if(field.Tick.Value!=start+ticks||host.Feed.Current.Tick!=field.Tick.Value||host.Feed.Current.Count!=field.Count||
                field.GetAthlete(host.PlayerIndex).Motion.DistanceNanometers<=distance)throw new InvalidOperationException("Approved player commands did not advance the paid production prefix.");
            host.Proxies.Render(1);if(host.Proxies.Count!=field.Count)throw new InvalidOperationException("Every production athlete needs a proxy.");
            for(int i=0;i<field.Count;i++)host.Proxies.GetRig(field.GetAthlete(i).AthleteId.Value).Validate();
        }
        public static void ValidateInitialized(ProductionVenueHost host,ProductionReviewDefinition definition,ProductionReviewEntry entry)
        {
            if(host==null||host.Runner==null||host.Environment==null||host.Review==null||!host.Review.Initialized||
                host.Venue.Manifest.Id!=definition.venue_id||host.Runner.Field.Count!=entry.field_count||host.Review.EntryId!=entry.id||
                host.Review.TierId!=definition.initial_quality_tier||host.ChaseCamera==null||!host.ChaseCamera.isActiveAndEnabled||host.ChaseCamera.targetTexture!=null)
                throw new InvalidOperationException("Production review scene, roster, resource-owned initial quality or window camera did not initialize.");
            var environment=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(host.EnvironmentResource));
            int trees=0;foreach(var group in environment.forest.groups)trees+=group.count;
            if(!host.Course.GroundJoined||host.Course.PresentationRouteSnapshotJson!=host.Runner.Field.Route.PlanSnapshotJson||
                host.Course.PresentationRequirementsSnapshotJson!=host.Runner.Field.Route.RequirementsSnapshotJson||host.Course.LoopCount!=host.Venue.Visual.Count||
                host.Course.VisibleLoopCount<=0||host.Environment.VenueId!=host.Venue.Manifest.Id||host.Environment.RecipeId!=environment.id||
                host.Environment.TerrainTileCount!=environment.terrain.tiles.Length||host.Environment.ForestGroupCount!=environment.forest.groups.Length||host.Environment.TreeCount!=trees||
                host.Mats.Count!=host.Venue.LaneCount||host.Environment.BankCount!=host.Venue.LaneCount||host.Proxies.Count!=entry.field_count)
                throw new InvalidOperationException("Production terrain, route-selected course, forest, mats, targets or roster is incomplete.");
            foreach(var collider in host.GetComponentsInChildren<Collider>(true))if(collider.enabled)throw new InvalidOperationException("Presentation must not introduce an active gameplay collider.");
            if(Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBodyDistant")==null||Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend")?.shader?.name!="Biathlon/Snow")
                throw new InvalidOperationException("Native production athlete or snow asset is missing.");
        }
        private IEnumerator ObserveNormalRendering(ProductionVenueHost host,string entry,string tier)
        {
            var key=host.ChaseCamera;renderedFrames.TryGetValue(key,out int initial);
            for(int frame=0;frame<12;frame++)yield return null;
            var sample=counters.Read(0,0,0);renderedFrames.TryGetValue(key,out int final);
            if(host.ChaseCamera.targetTexture!=null||host.GetComponent<RenderQualityPresentation>().TierId!=tier)
            {failed=true;Debug.LogError("Window camera binding or requested quality changed.");yield break;}
            if(final==initial)
            {
                Debug.Log("BIATHLON_PRODUCTION_SMOKE_RENDER "+entry+" "+tier+" 0 HIDDEN_WINDOW_SUSPENDED draw_calls="+sample.draw_calls);
                yield break; // Initialization/paid controls remain checked; no visual or performance PASS is inferred.
            }
            if(sample.draw_calls==0)
            {failed=true;Debug.LogError("Production smoke requires actual normal window-camera frames; an available draw counter must be positive.");yield break;}
            // Hidden players can expose no render counters. Keep -1 as unavailable,
            // while normal camera events and the actual screenshot prove rendering.
            Debug.Log("BIATHLON_PRODUCTION_SMOKE_RENDER "+entry+" "+tier+" "+(final-initial)+" NORMAL_CAMERA draw_calls="+sample.draw_calls);
            string output=Argument("-biathlonProductionSmokeOutput");
            if(output!=null){Directory.CreateDirectory(output);ScreenCapture.CaptureScreenshot(Path.Combine(output,entry+"-"+tier+".png"));yield return null;}
        }
        private static string Argument(string key)
        {var args=System.Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==key)return args[i+1];return null;}
        private IEnumerator Start()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null){Debug.LogError("Production smoke requires the normal graphics player.");Application.Quit(1);yield break;}
            yield return null;
            var initialHost=FindAnyObjectByType<ProductionVenueHost>();ProductionReviewDefinition definition=null;
            try
            {
                if(initialHost==null||string.IsNullOrEmpty(initialHost.ReviewDefinitionResource))throw new InvalidOperationException("Production review startup host is missing.");
                definition=ProductionReviewDefinition.FromJson(ProductionVenueHost.ReadResource(initialHost.ReviewDefinitionResource));
                if(SceneManager.GetActiveScene().path!=definition.Get(definition.initial_entry_id).scene_path)throw new InvalidOperationException("Resource-owned startup entry was not used by this build.");
            }
            catch(Exception ex){Debug.LogException(ex);definition=null;}
            if(definition==null){Application.Quit(1);yield break;}
            foreach(var entry in definition.entries)
            {
                if(SceneManager.GetActiveScene().path!=entry.scene_path)
                {
                    var previous=FindAnyObjectByType<ProductionVenueHost>();bool started=false;
                    try
                    {
                        string priorEntry=previous.Review.EntryId;previous.Review.RequestEntry(entry.id);
                        if(!previous.Review.TransitionPending)throw new InvalidOperationException("Review controls did not start a transition.");
                        bool rejected=false;try{previous.Review.RequestEntry(entry.id);}catch(InvalidOperationException){rejected=true;}
                        if(!rejected)throw new InvalidOperationException("Review controls admitted two concurrent scene loads.");
                        Debug.Log("BIATHLON_PRODUCTION_SMOKE_TRANSITION "+priorEntry+" "+entry.id+" PENDING_REJECTED");started=true;
                    }
                    catch(Exception ex){Debug.LogException(ex);failed=true;}
                    if(!started)continue;
                    while(SceneManager.GetActiveScene().path!=entry.scene_path)yield return null;
                }
                yield return null;var host=FindAnyObjectByType<ProductionVenueHost>();
                try
                {
                    ValidateInitialized(host,definition,entry);host.enabled=false;
                    var key=host.ChaseCamera;if(firstTiers.TryGetValue(key,out string first)&&first!=definition.initial_quality_tier)throw new InvalidOperationException("Initial production camera frame did not use resource-owned quality.");
                    long before=host.Runner.Field.Tick.Value;AdvanceAndValidate(host,400);
                    Debug.Log("BIATHLON_PRODUCTION_SMOKE_SCENE "+JsonUtility.ToJson(new SceneEvidence{entry=entry.id,scene=entry.scene_path,athletes=host.Runner.Field.Count,start_tick=before,tick=host.Feed.Current.Tick,initial_quality=definition.initial_quality_tier,terrain_tiles=host.Environment.TerrainTileCount,forest_groups=host.Environment.ForestGroupCount,trees=host.Environment.TreeCount,mats=host.Mats.Count,banks=host.Environment.BankCount,visible_loops=host.Course.VisibleLoopCount,route_id=host.Course.PresentationRouteId}));
                }
                catch(Exception ex){Debug.LogException(ex);failed=true;continue;}
                foreach(string tier in new[]{"high","balanced","low"})
                {
                    try{host.Review.ApplyQuality(tier);}catch(Exception ex){Debug.LogException(ex);failed=true;continue;}
                    yield return ObserveNormalRendering(host,entry.id,tier);
                }
                if(firstTiers.TryGetValue(host.ChaseCamera,out string firstTier))
                {if(firstTier!=definition.initial_quality_tier){failed=true;Debug.LogError("Production first-frame quality differs from the resource.");}}
                else Debug.Log("BIATHLON_PRODUCTION_SMOKE_FIRST_FRAME_UNAVAILABLE "+entry.id+" HIDDEN_WINDOW_SUSPENDED");
            }
            Debug.Log(failed?"BIATHLON_PRODUCTION_SMOKE_FAIL":"BIATHLON_PRODUCTION_SMOKE_PASS");Application.Quit(failed?1:0);
        }
        [Serializable] private sealed class SceneEvidence
        {
            public int schema_version=1;public string id="production_player_content_smoke_v1",entry,scene,initial_quality,route_id;
            public long start_tick,tick;public int athletes,terrain_tiles,forest_groups,trees,mats,banks,visible_loops;
        }
    }
}

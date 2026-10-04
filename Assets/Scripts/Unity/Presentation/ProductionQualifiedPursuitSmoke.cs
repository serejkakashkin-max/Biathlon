using System;
using System.Collections;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Development-player-only qualified Pursuit content smoke. Ordinary review launches are unchanged.</summary>
    public sealed class ProductionQualifiedPursuitSmoke : MonoBehaviour
    {
        private bool failed, previousBackground;
        private ProductionVenueHost host;
        private BenchmarkFrameCounters counters;
        private int renderedFrames;
        private PursuitQualificationSnapshot qualification;
        private string sourceHash;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Boot()
        {
            var args=System.Environment.GetCommandLineArgs();
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(args,"-biathlonQualifiedPursuitSmoke")<0)return;
            if(Array.IndexOf(args,"-biathlonProductionSmoke")>=0||Array.IndexOf(args,"-biathlonSmoke")>=0)
                throw new ArgumentException("Qualified Pursuit smoke must run alone.");
            var go=new GameObject("Qualified production Pursuit content smoke");DontDestroyOnLoad(go);
            go.AddComponent<ProductionQualifiedPursuitSmoke>();
        }
        private void OnEnable()
        {
            previousBackground=Application.runInBackground;Application.runInBackground=true;
            Application.logMessageReceived+=ObserveLog;SceneManager.sceneLoaded+=SuppressBootstrap;
            RenderPipelineManager.endCameraRendering+=ObserveCamera;counters=new BenchmarkFrameCounters();
        }
        private void OnDisable()
        {
            Application.logMessageReceived-=ObserveLog;SceneManager.sceneLoaded-=SuppressBootstrap;
            RenderPipelineManager.endCameraRendering-=ObserveCamera;counters?.Dispose();counters=null;
            Application.runInBackground=previousBackground;
        }
        private void ObserveLog(string message,string stack,LogType type)
        {if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failed=true;}
        private void SuppressBootstrap(Scene scene,LoadSceneMode mode)
        {
            var hosts=FindObjectsByType<ProductionVenueHost>();
            if(hosts.Length!=1){failed=true;Debug.LogError("Exactly one production bootstrap required.");return;}
            host=hosts[0];host.AutoStart=false;host.enabled=false;
        }
        private void ObserveCamera(ScriptableRenderContext context,Camera camera)
        {if(host!=null&&host.Runner!=null&&camera==host.ChaseCamera)renderedFrames++;}
        private static string Argument(string key)
        {
            var args=System.Environment.GetCommandLineArgs();string value=null;
            for(int i=0;i<args.Length;i++)if(args[i]==key)
            {if(value!=null||i+1>=args.Length||args[i+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException("One value required for "+key);value=args[++i];}
            if(string.IsNullOrEmpty(value))throw new ArgumentException("Required argument: "+key);return value;
        }
        /// <summary>Consumes only the persisted completed actual-production Sprint report, never constructs results.</summary>
        public static PursuitQualificationSnapshot ReadQualification(string json,VenueAuthoringPlan venue,CompetitionProfile target)
        {
            if(venue==null||target==null)throw new ArgumentNullException();
            var report=JsonUtility.FromJson<SprintRecord>(json);
            if(report==null||report.schema_version!=1||report.athletes!=40||report.ticks<=0||string.IsNullOrEmpty(report.result_json)||
                !((report.id=="production_venue_stream_record_report_v1"&&report.status=="RACE_COMPLETE_REPLAY_NOT_YET_PROVED"&&report.stopped_reason=="RACE_COMPLETE")||
                  (report.id=="production_venue_race_probe_v1"&&report.status=="RACE_COMPLETE"&&report.venue==venue.Manifest.Id)))
                throw new ArgumentException("A completed actual production Sprint40 record is required.");
            var source=SprintResultSnapshot.FromJson(report.result_json);VenueFormatBinding sprint=null;
            for(int i=0;i<venue.Formats.Count;i++)if(venue.Formats.Get(i).Id=="sprint")sprint=venue.Formats.Get(i);
            if(sprint==null||source.Count!=40||source.TickFrequency!=20||source.CompetitionProfileJson!=sprint.Profile.ToJson()||
                target.Format!=CompetitionFormat.Pursuit||target.FieldCapacity!=40)
                throw new ArgumentException("Source Sprint40 profile, exact clock and current target capacity must match.");
            for(int i=0;i<source.Count;i++)if(source.Get(i).Status!=RaceResultStatus.Finished)
                throw new ArgumentException("All 40 actual Sprint entrants must have finished before the Pursuit40 smoke.");
            var result=PursuitQualificationSnapshot.Create(venue.Manifest.Id+"_persisted_sprint_qualification_v1",source,target);
            if(result.Count!=40)throw new ArgumentException("Exact qualified field required.");return result;
        }
        private void Initialize()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null||host==null||host.Runner!=null)
                throw new InvalidOperationException("Fresh production bootstrap and normal graphics player required.");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(host.VenueResource)),ProductionVenueHost.ReadResource);
            VenueFormatBinding pursuit=null;for(int i=0;i<venue.Formats.Count;i++)if(venue.Formats.Get(i).Id=="pursuit")pursuit=venue.Formats.Get(i);
            if(pursuit==null||venue.Manifest.Id!="woodland_basin_v1")throw new ArgumentException("Current Woodland production Pursuit binding required.");
            string sourcePath=Argument("-biathlonSprintRecord");string source=File.ReadAllText(sourcePath);
            qualification=ReadQualification(source,venue,pursuit.Profile);
            using(var sha=System.Security.Cryptography.SHA256.Create())using(var stream=File.OpenRead(sourcePath))sourceHash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
            host.ProfileResource=pursuit.ProfileResource;host.RouteResource=pursuit.RouteResource;host.FieldCount=40;
            host.Initialize(venue,pursuit.Profile,pursuit.Route,RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource(host.ConditionsResource)),40,0,qualification);
            host.GetComponent<RenderQualityPresentation>().Apply("high");
            host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(host.EnvironmentResource)));
            var field=host.Runner.Field;
            for(int i=0;i<field.Count;i++)
            {
                var row=qualification.Get(i);decimal offset=(decimal)row.StartGapUnits*field.TickFrequency/qualification.GapUnitsPerSecond;
                if(offset!=decimal.Truncate(offset)||field.GetAthlete(i).AthleteId.Value!=row.AthleteId||field.GetAthlete(i).ScheduledStart.Value!=checked(1+(long)offset))
                    throw new InvalidOperationException("Actual qualified identity/order/start gaps differ from the authoritative field.");
            }
            ValidateInitialized();
        }
        private void ValidateInitialized()
        {
            var environment=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(host.EnvironmentResource));int trees=0;
            foreach(var group in environment.forest.groups)trees+=group.count;
            if(host.Runner.Field.Count!=40||host.Runner.Field.Profile.Format!=CompetitionFormat.Pursuit||host.Proxies.Count!=40||
                !host.Course.GroundJoined||host.Course.PresentationRouteSnapshotJson!=host.Runner.Field.Route.PlanSnapshotJson||
                host.Course.PresentationRequirementsSnapshotJson!=host.Runner.Field.Route.RequirementsSnapshotJson||host.Course.VisibleLoopCount<=0||
                host.Environment.VenueId!=host.Venue.Manifest.Id||host.Environment.RecipeId!=environment.id||
                host.Environment.TerrainTileCount!=environment.terrain.tiles.Length||host.Environment.ForestGroupCount!=environment.forest.groups.Length||host.Environment.TreeCount!=trees||
                host.Mats.Count!=30||host.Environment.BankCount!=30||host.ChaseCamera==null||!host.ChaseCamera.isActiveAndEnabled||host.ChaseCamera.targetTexture!=null)
                throw new InvalidOperationException("Pursuit40 production environment, route, window camera, mats, targets or roster failed initialization.");
            foreach(var collider in host.GetComponentsInChildren<Collider>(true))if(collider.enabled)throw new InvalidOperationException("Presentation introduced an active gameplay collider.");
            if(Resources.Load<Mesh>("Biathlon/Athletes/Meshes/AthleteBodyDistant")==null||Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend")?.shader?.name!="Biathlon/Snow")
                throw new InvalidOperationException("Native production athlete or snow asset is missing.");
        }
        private void Advance()
        {
            var field=host.Runner.Field;long before=field.Tick.Value,distance=field.GetAthlete(0).Motion.DistanceNanometers;var id=field.GetAthlete(0).AthleteId;
            for(int i=0;i<400;i++){var tick=field.Tick.Next();host.Step(new PlayerCommand(tick,id,800,0),new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));}
            if(field.Tick.Value!=before+400||host.Feed.Current.Tick!=field.Tick.Value||host.Feed.Current.Count!=40||field.GetAthlete(0).Motion.DistanceNanometers<=distance)
                throw new InvalidOperationException("Paid normal-player Pursuit prefix failed.");
            host.Proxies.Render(1);for(int i=0;i<40;i++)host.Proxies.GetRig(field.GetAthlete(i).AthleteId.Value).Validate();
            Debug.Log("BIATHLON_QUALIFIED_PURSUIT_SMOKE_SCENE "+JsonUtility.ToJson(new Evidence{venue=host.Venue.Manifest.Id,profile=field.Profile.Id,route_id=host.Course.PresentationRouteId,athletes=40,mats=30,banks=30,start_tick=before,tick=field.Tick.Value,source_result_id=qualification.SourceResultId,source_sha256=sourceHash,qualified_count=qualification.Count,terrain_tiles=host.Environment.TerrainTileCount,forest_groups=host.Environment.ForestGroupCount,trees=host.Environment.TreeCount}));
        }
        private IEnumerator Start()
        {
            try{Initialize();Advance();}catch(Exception ex){failed=true;Debug.LogException(ex);}
            if(failed){Debug.Log("BIATHLON_QUALIFIED_PURSUIT_SMOKE_FAIL");Application.Quit(1);yield break;}
            foreach(string tier in new[]{"high","balanced","low"})
            {
                try{host.GetComponent<RenderQualityPresentation>().Apply(tier);}catch(Exception ex){failed=true;Debug.LogException(ex);continue;}
                int before=renderedFrames;for(int i=0;i<12;i++)yield return null;
                var sample=counters.Read(0,0,0);
                if(host.ChaseCamera.targetTexture!=null||host.GetComponent<RenderQualityPresentation>().TierId!=tier){failed=true;Debug.LogError("Pursuit camera binding or requested quality changed.");continue;}
                if(renderedFrames==before){Debug.Log("BIATHLON_QUALIFIED_PURSUIT_SMOKE_RENDER "+tier+" 0 HIDDEN_WINDOW_SUSPENDED draw_calls="+sample.draw_calls);continue;}
                if(sample.draw_calls==0){failed=true;Debug.LogError("An available draw counter reported zero for a rendered Pursuit camera.");continue;}
                Debug.Log("BIATHLON_QUALIFIED_PURSUIT_SMOKE_RENDER "+tier+" "+(renderedFrames-before)+" NORMAL_CAMERA draw_calls="+sample.draw_calls);
                string output=null;try{output=Argument("-biathlonQualifiedPursuitSmokeOutput");Directory.CreateDirectory(output);ScreenCapture.CaptureScreenshot(Path.Combine(output,"pursuit40-"+tier+".png"));}
                catch(Exception ex){failed=true;Debug.LogException(ex);}yield return null;
            }
            Debug.Log(failed?"BIATHLON_QUALIFIED_PURSUIT_SMOKE_FAIL":"BIATHLON_QUALIFIED_PURSUIT_SMOKE_PASS");Application.Quit(failed?1:0);
        }
        [Serializable] private sealed class SprintRecord
        {
            public int schema_version=0,athletes=0;public long ticks=0;
            public string id=null,status=null,stopped_reason=null,venue=null,result_json=null;
        }
        [Serializable] private sealed class Evidence
        {
            public int schema_version=1;public string id="qualified_pursuit40_player_content_smoke_v1",venue,profile,route_id,source_result_id,source_sha256;
            public int athletes,mats,banks,qualified_count,terrain_tiles,forest_groups,trees;public long start_tick,tick;
        }
    }
}

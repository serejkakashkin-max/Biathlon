using System;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Fixed-tick input/snapshot adapter for explicitly authored production courses.
    /// Environment dressing is a separate component. No Inspector setup enters simulation.</summary>
    public sealed class ProductionVenueHost : MonoBehaviour
    {
        public string VenueResource="Biathlon/Venues/woodland_basin_v1/manifest";
        public string ProfileResource="Biathlon/Venues/woodland_basin_v1/mass_start_profile";
        public string RouteResource="Biathlon/Venues/woodland_basin_v1/mass_start_route";
        public string ConditionsResource="Biathlon/race_snow_neutral_v1";
        public string EnvironmentResource="Biathlon/Venues/woodland_basin_v1/environment";
        public string ReviewDefinitionResource;
        public ProductionReviewPresentation Review {get;private set;}
        public bool AutoStart=true,UseGamepad;
        public int FieldCount=30;
        public PackAiRaceRunner Runner {get;private set;}
        public VenueAuthoringPlan Venue {get;private set;}
        public RacePresentationFeed Feed {get;private set;}
        public AthleteProxyPresentation Proxies {get;private set;}
        public RangeMatPresentation Mats {get;private set;}
        public ProductionCoursePresentation Course {get;private set;}
        public Camera ChaseCamera {get;private set;}
        public ProductionVenueEnvironment Environment {get;private set;}
        public int PlayerIndex {get;private set;}
        private PlayerDeviceInput ski;
        private ShootingDeviceInput shot;
        private EffortControlSystem effort;
        private RangePresentation range;
        private RaceHud hud;
        private SkiDebugHud debugHud;
        private PenaltyLoopPresentation penalty;
        private TrackSampler[] samplers;
        private OccupancyDefinition body;
        private double accumulator;
        private bool shootingContext;
        public static string ReadResource(string id)
        {
            VenueManifest.ResourceId(id);var text=Resources.Load<TextAsset>(id);
            if(text==null)throw new FileNotFoundException("Missing production race resource: "+id);return text.text;
        }
        private static string ReadCore(string name)=>ReadResource("Biathlon/"+Path.GetFileNameWithoutExtension(name));
        private void Start()
        {
            if(!AutoStart)return;
            var review=ReadOptionalReview(ReviewDefinitionResource);
            var entry=review?.ForScene(gameObject.scene.path);
            if(entry!=null&&entry.field_count!=FieldCount)throw new ArgumentException("Review entry must match the scene roster.");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ReadResource(VenueResource)),ReadResource);
            if(review!=null&&review.venue_id!=venue.Manifest.Id)throw new ArgumentException("Review venue mismatch.");
            var profile=CompetitionProfile.FromJson(ReadResource(ProfileResource));
            if(entry!=null&&(entry.format_id=="sprint"?CompetitionFormat.Sprint:CompetitionFormat.MassStart)!=profile.Format)throw new ArgumentException("Review format mismatch.");
            Initialize(venue,profile,RaceRoutePlan.FromJson(ReadResource(RouteResource)),
                RaceSnowConditions.FromJson(ReadResource(ConditionsResource)),FieldCount);
            if(review!=null)InitializeReview(review,entry.id);
            if(!string.IsNullOrEmpty(EnvironmentResource))InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ReadResource(EnvironmentResource)));
        }
        public static ProductionReviewDefinition ReadOptionalReview(string resource)
        {
            if(string.IsNullOrEmpty(resource))return null;
            var value=ProductionReviewDefinition.FromJson(ReadResource(resource));RenderQualityDefinition.Load().Get(value.initial_quality_tier);return value;
        }
        public void InitializeReview(ProductionReviewDefinition definition,string entryId)
        {
            if(Runner==null||Review!=null)throw new InvalidOperationException("Fresh initialized host review required.");
            if(!gameObject.activeInHierarchy)throw new InvalidOperationException("Active review host required.");
            if(definition==null)throw new ArgumentNullException(nameof(definition));
            var detached=ProductionReviewDefinition.FromJson(definition.ToJson());var entry=detached.Get(entryId);
            if(detached.venue_id!=Venue.Manifest.Id||entry.field_count!=Runner.Field.Count||(entry.format_id=="sprint"?CompetitionFormat.Sprint:CompetitionFormat.MassStart)!=Runner.Field.Profile.Format)throw new ArgumentException("Review source differs from this scene.");
            RenderQualityDefinition.Load().Get(detached.initial_quality_tier);
            var adapter=gameObject.AddComponent<ProductionReviewPresentation>();
            try{adapter.Initialize(this,detached,entryId);Review=adapter;}catch{Destroy(adapter);throw;}
        }
        public void InitializeEnvironment(ProductionEnvironmentDefinition definition)
        {
            if(Runner==null)throw new InvalidOperationException("Initialize the authored race before its environment.");
            if(Environment!=null)throw new InvalidOperationException("This host already owns an environment.");
            if(!string.IsNullOrEmpty(definition.terrain.snow_shoulders_resource))
                Course.ConfigureSnowBanks(Venue,ProductionSnowShoulderDefinition.FromJson(ReadResource(definition.terrain.snow_shoulders_resource),Venue));
            var environment=gameObject.AddComponent<ProductionVenueEnvironment>();
            try
            {
                environment.Initialize(Venue,definition,ReadResource,Feed,ChaseCamera,range.AimCamera,apron:Course.Apron);
                Course.JoinGround(environment,definition.terrain.grid_spacing_m,environment.TerrainGridOrigin);
                Environment=environment;
            }
            catch{Destroy(environment);throw;}
        }
        public void Initialize(VenueAuthoringPlan venue,CompetitionProfile profile,RaceRoutePlan route,
            RaceSnowConditions conditions,int count=30,int playerIndex=0,PursuitQualificationSnapshot qualification=null)
        {
            if(Runner!=null)throw new InvalidOperationException("A fresh host is required for another race.");
            if(playerIndex<0||playerIndex>=count)throw new ArgumentOutOfRangeException(nameof(playerIndex));
            var runner=ProductionVenueRaceFactory.Create(venue,profile,route,conditions,ReadCore,new BalanceEventAudit(5),new AiDecisionCounts(),qualification,count);
            Venue=venue;Runner=runner;PlayerIndex=playerIndex;body=venue.Occupancy;
            samplers=new TrackSampler[venue.Track.LoopCount];for(int i=0;i<samplers.Length;i++)samplers[i]=new TrackSampler(venue.Track.GetLoop(i));
            var field=Runner.Field;var id=field.GetAthlete(PlayerIndex).AthleteId;
            Feed=gameObject.AddComponent<RacePresentationFeed>();Feed.Publish(RacePresentationSnapshot.Capture(field));
            ski=gameObject.AddComponent<PlayerDeviceInput>();ski.enabled=false;ski.Initialize(id,field.TickFrequency);
            shot=gameObject.AddComponent<ShootingDeviceInput>();shot.enabled=false;shot.Initialize(id,field.TickFrequency);
            effort=new EffortControlSystem(EffortControlDefinition.FromJson(ReadCore("effort_control_provisional_v1.json")),id,field.TickFrequency);
            Mats=gameObject.AddComponent<RangeMatPresentation>();Mats.Initialize(venue);
            Course=gameObject.AddComponent<ProductionCoursePresentation>();Course.Initialize(venue,route:field.Route,sharedApron:venue.SharedApron);
            Proxies=gameObject.AddComponent<AthleteProxyPresentation>();Proxies.Initialize(Feed,venue);Proxies.Render(1);
            var cameraObject=new GameObject("Production chase camera");cameraObject.transform.SetParent(transform,false);
            ChaseCamera=cameraObject.AddComponent<Camera>();ChaseCamera.depth=100;
            cameraObject.AddComponent<AthleteChaseCamera>().Follow(Proxies.GetProxy(id.Value),ChaseCameraSettings.FromJson(ReadCore("chase_camera_v1.json")));
            Proxies.SetViewCamera(ChaseCamera);
            range=gameObject.AddComponent<RangePresentation>();range.Initialize(Feed,id.Value,TargetGeometryDefinition.FromJson(ReadCore("target_geometry_v1.json")));
            gameObject.AddComponent<RenderQualityPresentation>().Initialize(ChaseCamera,range.AimCamera);
            hud=gameObject.AddComponent<RaceHud>();hud.Initialize(Feed,id.Value);
            debugHud=gameObject.AddComponent<SkiDebugHud>();debugHud.Initialize(Feed,id.Value);
            penalty=gameObject.AddComponent<PenaltyLoopPresentation>();penalty.Initialize(Feed,id.Value,venue.Track,venue.Visual);
        }
        public double CurrentLateralExtent
        {
            get
            {
                if(Runner==null)throw new InvalidOperationException("Initialize the host first.");
                var point=Runner.Field.GetPhysicalPoint(PlayerIndex);
                for(int i=0;i<samplers.Length;i++)if(Venue.Track.GetLoop(i).Id==point.LoopId)
                    return samplers[i].Sample(new TrackDistance(point.LocalNanometers/SkiState.NanometersPerMillimeter)).WidthMeters*.5-body.EdgeMargin;
                throw new InvalidOperationException("Accepted physical loop has no authored sampler.");
            }
        }
        private void Update()
        {
            if(Runner==null||Runner.Field.IsComplete)return;
            bool shooting=Runner.Field.GetAthlete(PlayerIndex).ActiveShooting!=null;
            if(shooting!=shootingContext){ski.ReleaseHeldInput();shot.ReleaseHeldInput();shootingContext=shooting;}
            if(shooting){bool reviewMouse=Review!=null&&Mouse.current!=null&&Review.ContainsScreenPointer(Mouse.current.position.ReadValue());shot.Capture(UseGamepad||reviewMouse?null:Mouse.current,UseGamepad?null:Keyboard.current,UseGamepad?Gamepad.current:null);ski.ReleaseHeldInput();}
            else{ski.Capture(UseGamepad?null:Keyboard.current,UseGamepad?Gamepad.current:null);shot.ReleaseHeldInput();}
            accumulator+=Time.deltaTime;double dt=1.0/Runner.Field.TickFrequency;
            while(accumulator>=dt&&!Runner.Field.IsComplete)
            {
                var tick=Runner.Field.Tick.Next();
                Step(ski.BuildDirectional(tick,Runner.Field.GetLateral(PlayerIndex),CurrentLateralExtent),shot.Build(tick));accumulator-=dt;
            }
        }
        public void Step(PlayerCommand command,ShootingDeviceCommand shooting)
        {
            if(Runner==null)throw new InvalidOperationException("Initialize the host first.");
            var applied=effort.Step(command,true);
            var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(command.Tick,command.AthleteId,applied.AppliedControl),1,1,0,
                command.TuckHeld,applied.FinalSprintRequested),shooting.Aim,shooting.Trigger,shooting.HoldBreath);
            Runner.StepWithPlayer(PlayerIndex,input,CurrentLateralExtent*command.LineIntent/PlayerCommand.LineIntentUnits);
            Feed.Publish(RacePresentationSnapshot.Capture(Runner.Field));range.Refresh();hud.Refresh();debugHud.Refresh();penalty.Refresh();
        }
        private void OnApplicationFocus(bool value){if(ski!=null)ski.SetInputFocus(value);if(shot!=null)shot.SetInputFocus(value);}
        private void OnGUI()
        {
            if(Runner==null)return;
            GUI.Box(new Rect(12,Screen.height-30,1000,24),"SKI: W/S effort, A/D line, Space tuck | RANGE: mouse aim, LMB fire, RMB fine, Space breath");
        }
    }
}

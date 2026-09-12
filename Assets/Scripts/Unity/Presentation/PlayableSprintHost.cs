using System;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Greybox composition root. Fixed simulation clock is independent of render frames.</summary>
    public sealed class PlayableSprintHost : MonoBehaviour
    {
        public bool AutoStart=true;
        public bool UseGamepad;
        public PackAiRaceRunner Runner { get; private set; }
        public RacePresentationFeed Feed { get; private set; }
        private PlayerDeviceInput ski;
        private ShootingDeviceInput shot;
        private EffortControlSystem effort;
        private RangePresentation range;
        private AthleteProxyPresentation proxies;
        private RaceHud hud;
        private SkiDebugHud debugHud;
        private PenaltyLoopPresentation penalty;
        private double accumulator,extent;
        private bool shootingContext;
        private GameObject cameraObject;
        private static string Read(string name)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(name)).text;
        private void Start(){if(AutoStart)Initialize();}
        public void Initialize(int count=6)
        {
            if(Runner!=null)throw new InvalidOperationException("Create a new host to restart.");
            Runner=ChampionshipRaceFactory.Create(Read,n=>Resources.Load<TextAsset>("Biathlon/Tracks/"+Path.GetFileNameWithoutExtension(n)).text,CompetitionFormat.Sprint,new BalanceEventAudit(5),new AiDecisionCounts(),count:count);
            var field=Runner.Field;var id=field.GetAthlete(0).AthleteId;
            Feed=gameObject.AddComponent<RacePresentationFeed>();Feed.Publish(RacePresentationSnapshot.Capture(field));
            ski=gameObject.AddComponent<PlayerDeviceInput>();ski.enabled=false;ski.Initialize(id,field.TickFrequency);
            shot=gameObject.AddComponent<ShootingDeviceInput>();shot.enabled=false;shot.Initialize(id,field.TickFrequency);
            effort=new EffortControlSystem(EffortControlDefinition.FromJson(Read("effort_control_provisional_v1.json")),id,field.TickFrequency);
            double width=double.MaxValue;for(int i=0;i<field.Track.LoopCount;i++){var loop=field.Track.GetLoop(i);for(int j=0;j<loop.SegmentCount;j++)width=Math.Min(width,loop.GetSegment(j).WidthMeters);}
            extent=width/2-OccupancyDefinition.FromJson(Read("occupancy_provisional_v1.json")).EdgeMargin;
            var visual=TrackVisualDefinition.FromJson(Read("stadium_visual_v1.json"),field.Track);
            proxies=gameObject.AddComponent<AthleteProxyPresentation>();proxies.Initialize(Feed,visual);proxies.Render(1);
            cameraObject=new GameObject("Player chase camera");cameraObject.transform.SetParent(transform,false);cameraObject.AddComponent<Camera>().depth=100;
            cameraObject.AddComponent<AthleteChaseCamera>().Follow(proxies.GetProxy(id.Value),ChaseCameraSettings.FromJson(Read("chase_camera_v1.json")));
            range=gameObject.AddComponent<RangePresentation>();range.Initialize(Feed,id.Value,TargetGeometryDefinition.FromJson(Read("target_geometry_v1.json")));
            hud=gameObject.AddComponent<RaceHud>();hud.Initialize(Feed,id.Value);debugHud=gameObject.AddComponent<SkiDebugHud>();debugHud.Initialize(Feed,id.Value);
            penalty=gameObject.AddComponent<PenaltyLoopPresentation>();penalty.Initialize(Feed,id.Value,field.Track,visual);
        }
        private void Update()
        {
            if(Runner==null||Runner.Field.IsComplete)return;
            bool current=Runner.Field.GetAthlete(0).ActiveShooting!=null;
            if(current!=shootingContext){ski.ReleaseHeldInput();shot.ReleaseHeldInput();shootingContext=current;}
            if(current){shot.Capture(UseGamepad?null:Mouse.current,UseGamepad?null:Keyboard.current,UseGamepad?Gamepad.current:null);ski.ReleaseHeldInput();}
            else{ski.Capture(UseGamepad?null:Keyboard.current,UseGamepad?Gamepad.current:null);shot.ReleaseHeldInput();}
            accumulator+=Time.deltaTime;double dt=1.0/Runner.Field.TickFrequency;
            while(accumulator>=dt&&!Runner.Field.IsComplete){var t=Runner.Field.Tick.Next();Step(ski.Build(t),shot.Build(t));accumulator-=dt;}
        }
        public void Step(PlayerCommand command,ShootingDeviceCommand shooting)
        {
            var applied=effort.Step(command,true);
            var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(command.Tick,command.AthleteId,applied.AppliedControl),1,1,0,command.TuckHeld,applied.FinalSprintRequested),shooting.Aim,shooting.Trigger,shooting.HoldBreath);
            Runner.StepWithPlayer(0,input,extent*command.LineIntent/PlayerCommand.LineIntentUnits);
            Feed.Publish(RacePresentationSnapshot.Capture(Runner.Field));range.Refresh();hud.Refresh();debugHud.Refresh();penalty.Refresh();
        }
        private void OnApplicationFocus(bool value){if(ski!=null)ski.SetInputFocus(value);if(shot!=null)shot.SetInputFocus(value);}
        private void OnGUI(){GUI.Box(new Rect(12,Screen.height-30,1000,24),"SKI: W/S effort, A/D line, Space tuck | RANGE: mouse aim, LMB fire, RMB fine, Space breath | Gamepad via UseGamepad");}
    }
}

using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Lane-local target camera. Hit decisions belong exclusively to simulation.</summary>
    public sealed class RangePresentation : MonoBehaviour
    {
        private RacePresentationFeed feed;
        private string athleteId;
        private TargetGeometryDefinition geometry;
        private GameObject stage;
        private Camera aimCamera;
        private Camera chaseCamera;
        private bool chaseWasEnabled;
        private TargetBankArt bank;
        private VenueAuthoringPlan venue;
        private ProductionVenueEnvironment environment;
        private RifleSightSettings sightSettings;
        private Renderer[] playerRenderers;
        private bool[] playerWasEnabled;
        private GameObject upgradedPlayerVisual;
        private bool upgradedPlayerWasActive;
        private float targetHeight;
        private RifleSightPresentation rifleSight;
        private ShotImpactPresentation fallbackImpacts;
        private bool overview,capturedPlayerVisibility;
        public bool OverviewRequested {get;set;}
        public TargetBankArt Bank=>bank;
        private long observedTick=-1;
        private int observedBout=-1;
        private int selectedIndex=-1;
        private double publishedAt;
        public bool Visible { get; private set; }
        public int ClosedMask { get; private set; }
        public string Status { get; private set; }="";
        public bool DrawLegacyHud {get;set;}=true;
        public Camera AimCamera=>aimCamera;
        public WeaponAimState ObservedSight {get;private set;}
        public void Initialize(RacePresentationFeed source,string selectedAthleteId,TargetGeometryDefinition definition,Camera athleteCamera=null)
        {
            if(source==null||definition==null)throw new ArgumentNullException();
            if(string.IsNullOrWhiteSpace(selectedAthleteId))throw new ArgumentException("Stable athlete ID required.");
            Clear();feed=source;athleteId=selectedAthleteId;geometry=definition;
            chaseCamera=athleteCamera;chaseWasEnabled=athleteCamera!=null&&athleteCamera.enabled;
            // A camera-local stage avoids changing the venue or using its schematic target spacing.
            stage=new GameObject("Range target stage");stage.transform.SetParent(transform,false);
            stage.transform.localPosition=new Vector3(0,-10000,0);
            var template=Resources.Load<TargetBankArt>("Biathlon/Stadium/TargetBank");
            if(template==null)throw new InvalidOperationException("Generate authored target bank first.");
            bank=Instantiate(template,stage.transform,false);bank.transform.localPosition=new Vector3(0,0,(float)geometry.DistanceMeters);
            fallbackImpacts=stage.AddComponent<ShotImpactPresentation>();fallbackImpacts.Initialize(feed,geometry,new[]{bank},athleteId);
            var cameraObject=new GameObject("Range aim camera");cameraObject.transform.SetParent(stage.transform,false);
            aimCamera=cameraObject.AddComponent<Camera>();aimCamera.depth=200;aimCamera.nearClipPlane=.01f;
            aimCamera.farClipPlane=(float)geometry.DistanceMeters+1;aimCamera.clearFlags=CameraClearFlags.SolidColor;aimCamera.backgroundColor=new Color(.42f,.48f,.52f);
            // Frame the authored bank plus one spacing margin on both sides; vertical FOV follows aspect.
            aimCamera.fieldOfView=2*Mathf.Atan((float)(geometry.SpacingMeters*TargetGeometryDefinition.TargetCount/(2*geometry.DistanceMeters)))*Mathf.Rad2Deg;
            stage.SetActive(false);
        }
        public void BindVenue(VenueAuthoringPlan plan,ProductionVenueEnvironment world,AthleteRigBinding playerRig)
        {
            if(stage==null||venue!=null||plan==null||world==null||playerRig==null||world.BankCount!=plan.LaneCount)throw new ArgumentException("Initialized venue, world banks and player rig required.");
            venue=plan;environment=world;sightSettings=RifleSightSettings.Load();targetHeight=StadiumArtDefinition.Load().target_height_m;
            if(fallbackImpacts!=null){fallbackImpacts.Shutdown();Destroy(fallbackImpacts);fallbackImpacts=null;}
            bank.gameObject.SetActive(false);stage.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            aimCamera.farClipPlane=sightSettings.far_clip_m;aimCamera.fieldOfView=sightSettings.field_of_view_deg;aimCamera.clearFlags=CameraClearFlags.Skybox;
            rifleSight=aimCamera.gameObject.AddComponent<RifleSightPresentation>();rifleSight.Initialize(sightSettings);
            BindPlayerVisibility(playerRig);
        }
        public void BindPlayerVisibility(AthleteRigBinding playerRig)
        {
            if(stage==null||playerRig==null||playerRenderers!=null)throw new InvalidOperationException("Bind one player after range initialization.");
            playerRenderers=playerRig.GetComponentsInChildren<Renderer>(true);playerWasEnabled=new bool[playerRenderers.Length];
            upgradedPlayerVisual=playerRig.GetComponent<Biathlete167RacePresentation>()?.Rig.gameObject;
            upgradedPlayerWasActive=upgradedPlayerVisual!=null&&upgradedPlayerVisual.activeSelf;
        }
        public void Refresh()=>Render(0);
        public void Render(double alpha)
        {
            if(!double.IsFinite(alpha)||alpha<0||alpha>1)throw new ArgumentOutOfRangeException(nameof(alpha));
            var snapshot=feed==null?null:feed.Current;
            if(snapshot==null){SetVisible(false);observedTick=-1;observedBout=-1;selectedIndex=-1;ObservedSight=default;Status="";return;}
            if(snapshot.Tick!=observedTick)
            {
                observedTick=snapshot.Tick;publishedAt=Time.timeAsDouble;selectedIndex=-1;
                for(int i=0;i<snapshot.Count;i++)if(snapshot.Get(i).AthleteId==athleteId){selectedIndex=i;break;}
                if(selectedIndex<0){SetVisible(false);return;}
                var a=snapshot.Get(selectedIndex);
                // Keep the full athlete/equipment setup and recovery visible in the
                // chase view. The dedicated sight camera owns only actual aim/cycling.
                SetVisible(a.ShootingActive&&a.Phase==CompetitionPhase.Range&&
                    (a.ShootingPhase==ShootingPhase.Aiming||a.ShootingPhase==ShootingPhase.BoltCycle));
                if(!Visible)return;
                if(observedBout!=a.CompletedBouts){observedBout=a.CompletedBouts;Status="";}
                ClosedMask=a.ClosedTargetsMask;
                if(environment!=null){environment.RefreshBanks();bank=environment.Bank(a.RangeLane-1);}else bank.Apply(geometry,a.Stance,ClosedMask);
                if(a.ShotOutcome!=ShotOutcome.NoShot)Status=a.ShotOutcome.ToString();
                if(DrawLegacyHud)
                {
                    boltText=a.NeedsBoltCycle?"ПКМ — зарядить патрон":a.ShootingPhase==ShootingPhase.BoltCycle?"Перезарядка…":"ЛКМ — выстрел";
                    breathText=a.BreathBlocked?"Выдохните: отпустите Space":a.BreathHolding?(a.BreathSwayMultiplier>1?"Долгая задержка — выдохните":"Дыхание задержано"):(a.BreathDebt>.25?"Восстановление дыхания":"Space — задержать дыхание");
                    breathDebt=(float)a.BreathDebt;hrLoad=Mathf.InverseLerp(90,180,(float)a.Physiology.HeartRateBpm);
                    laneText="РУБЕЖ "+a.RangeLane+"  |  "+(a.Stance==ShootingStance.Prone?"ЛЁЖА":"СТОЯ")+"  |  "+a.ShotsFired+" / 5  |  ПУЛЬС "+a.Physiology.HeartRateBpm.ToString("F0")+"  |  "+boltText;
                    shootingText=laneText+"\n"+breathText+"  |  F — точный прицел  |  Tab — обзор";
                }
            }
            if(!Visible||selectedIndex<0)return;
            var current=snapshot.Get(selectedIndex);ObservedSight=RifleSightPresentation.SampleSight(feed.Previous,snapshot,selectedIndex,alpha);var ray=ObservedSight.Ray;
            if(venue!=null)
            {
                aimCamera.fieldOfView=overview?sightSettings.overview_field_of_view_deg:current.FineAim?sightSettings.fine_field_of_view_deg:sightSettings.field_of_view_deg;
                rifleSight.SetVisible(!overview);
                RifleSightPresentation.Observe(aimCamera,venue,current.RangeLane,geometry,ray,current.Stance,sightSettings,targetHeight,overview);
            }
            else aimCamera.transform.localRotation=Quaternion.LookRotation(new Vector3((float)ray.DirectionX,(float)ray.DirectionY,(float)ray.DirectionZ),Vector3.up);
            rifleSight?.AnimateCycle(current.ShootingPhase==ShootingPhase.BoltCycle&&current.ShootingPhaseDurationTicks>0?
                (float)Math.Min(1,(current.ShootingPhaseElapsedTicks+alpha)/current.ShootingPhaseDurationTicks):0);
        }
        private string laneText="",shootingText="";
        private string breathText="",boltText="";private float hrLoad,breathDebt;
        private void SetVisible(bool value)
        {
            if(value!=Visible&&playerRenderers!=null)
                for(int i=0;i<playerRenderers.Length;i++)
                {
                    if(playerRenderers[i]==null)continue;
                    if(value){playerWasEnabled[i]=playerRenderers[i].enabled;playerRenderers[i].enabled=overview&&playerWasEnabled[i];}
                    else playerRenderers[i].enabled=playerWasEnabled[i];
                }
            if(value)capturedPlayerVisibility=true;
            if(playerRenderers!=null&&capturedPlayerVisibility)
                for(int i=0;i<playerRenderers.Length;i++)if(playerRenderers[i]!=null)playerRenderers[i].enabled=playerWasEnabled[i]&&(!value||overview);
            // The upgraded athlete is a sibling of the compatibility rig. Hide its
            // whole root so animation cannot re-enable an instanced prop in the sight.
            if(upgradedPlayerVisual!=null)upgradedPlayerVisual.SetActive(upgradedPlayerWasActive&&(!value||overview));
            Visible=value;
            if(stage!=null&&stage.activeSelf!=value)stage.SetActive(value);
            if(chaseCamera!=null)chaseCamera.enabled=chaseWasEnabled&&!value;
        }
        private void LateUpdate()
        {
            double alpha=feed?.InterpolationAlpha??(feed?.Current!=null&&feed.Current.Tick==observedTick?
                Math.Min(1,(Time.timeAsDouble-publishedAt)*feed.Current.TickFrequency):0);
            Render(alpha);
        }
        private void Update()
        {
            if(overviewControls==null)
            {var p=GetComponent<ProductionVenueHost>()?.InputPreferences??PlayerSettingsPreferences.Current;overviewControls=new BoundDeviceControls(p,PlayerSettingsPreferences.Catalog);overviewGamepad=p.Device=="gamepad";}
            overviewControls.Resolve(Keyboard.current,Mouse.current,Gamepad.current);
            bool requested=OverviewRequested||overviewControls.Pressed("range_overview",false)||overviewGamepad&&overviewControls.Pressed("range_overview",true);
            if(requested!=overview){overview=requested;observedTick=-1;Refresh();}
        }
        private BoundDeviceControls overviewControls;
        private bool overviewGamepad;
        private void OnGUI()
        {
            if(!DrawLegacyHud)return;
            if(!Visible)return;
            GUI.Box(new Rect(Screen.width/2f-390,Screen.height-155,780,84),shootingText);
            var saved=GUI.color;GUI.color=Color.Lerp(new Color(.2f,.85f,.4f),new Color(1,.3f,.15f),hrLoad);
            GUI.DrawTexture(new Rect(Screen.width/2f-350,Screen.height-93,280*hrLoad,5),Texture2D.whiteTexture);
            GUI.color=Color.Lerp(new Color(.2f,.8f,1),Color.red,breathDebt);GUI.DrawTexture(new Rect(Screen.width/2f+70,Screen.height-93,280*breathDebt,5),Texture2D.whiteTexture);GUI.color=saved;
            // The open physical diopter owns aim feedback in the production venue.
            if(venue==null){var color=GUI.color;GUI.color=Color.red;
                GUI.DrawTexture(new Rect(Screen.width/2f-8,Screen.height/2f,16,1),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width/2f,Screen.height/2f-8,1,16),Texture2D.whiteTexture);GUI.color=color;}
        }
        private void Clear(){SetVisible(false);if(chaseCamera!=null)chaseCamera.enabled=chaseWasEnabled;chaseCamera=null;if(stage!=null)Destroy(stage);bank=null;venue=null;environment=null;playerRenderers=null;upgradedPlayerVisual=null;rifleSight=null;capturedPlayerVisibility=false;overview=false;OverviewRequested=false;observedTick=-1;observedBout=-1;selectedIndex=-1;publishedAt=0;ObservedSight=default;Visible=false;}
        private void OnDestroy()=>Clear();
    }
}

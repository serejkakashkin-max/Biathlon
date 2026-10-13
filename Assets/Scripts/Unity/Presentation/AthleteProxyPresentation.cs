using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Career;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public sealed class AthleteProxyPresentation : MonoBehaviour
    {
        private RacePresentationFeed feed;
        private TrackVisualDefinition visual;
        private readonly Dictionary<string,GameObject> proxies=new Dictionary<string,GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string,AthleteAnimationPresentation> animation=new Dictionary<string,AthleteAnimationPresentation>(StringComparer.Ordinal);
        private readonly HashSet<string> keep=new HashSet<string>(StringComparer.Ordinal);
        private readonly List<string> removed=new List<string>();
        private AthleteRigBinding rigPrefab;
        private double publishedAt;
        private long observedTick=-1;
        private AthleteLocomotionSettings motionSettings;
        private GroundContactSettings contactSettings;
        private Camera viewCamera;
        private AthleteChaseCamera chaseCamera;
        private SnowTrackPresentation snowTracks;
        private SnowSprayPresentation snowSpray;
        private BreathVaporPresentation breathVapor;
        private AthleteEquipmentBatches equipmentBatches;
        private FinishStagingSettings finishStaging;
        private Quaternion? shootingRotation;
        private bool preserveAcceptedShootingLine;
        private RangeTransitYieldSettings transitSettings;
        private readonly Dictionary<string,Vector3> transitOffsets=new Dictionary<string,Vector3>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> careerSkis=new Dictionary<string,string>(StringComparer.Ordinal);
        private readonly Dictionary<string,string> careerNations=new Dictionary<string,string>(StringComparer.Ordinal);
        private CareerLeaderBibs careerBibs;
        private string prestartAthleteId;
        /// <summary>Show the selected skier waiting at the accepted launch pose.
        /// Visibility never admits a body to the authoritative course early.</summary>
        public void ShowPrestartAthlete(string athleteId)
        {
            if(string.IsNullOrWhiteSpace(athleteId)||!proxies.ContainsKey(athleteId))throw new ArgumentException("A presented stable athlete ID is required.");
            prestartAthleteId=athleteId;Render(1);
        }
        public void ConfigureCareerAppearance(CareerSave save,CareerCalendar calendar,CareerScoringDefinition scoring,CareerCalendarEvent race)
        {
            var leaders=CareerLeaderBibs.Capture(save,calendar,scoring,race);
            careerSkis.Clear();careerNations.Clear();
            for(int i=0;i<save.AthleteCount;i++){var athlete=save.Athlete(i);CareerSkiAppearance.Catalog.Get(athlete.SelectedSki);careerSkis.Add(athlete.Id,athlete.SelectedSki);careerNations.Add(athlete.Id,athlete.Nation);}
            careerBibs=leaders;
            if(animation.Count==0)return;
            equipmentBatches.Clear();
            foreach(var entry in animation){ApplyCareerAppearance(entry.Key,entry.Value.Rig);equipmentBatches.Register(entry.Value.Rig);}
        }
        private void ApplyCareerAppearance(string athleteId,AthleteRigBinding rig)
        {
            if(!careerSkis.TryGetValue(athleteId,out var ski))return;
            rig.GetComponent<AthleteAppearancePresentation>()?.ApplyNation(careerNations[athleteId]);
            var appearance=rig.GetComponent<CareerSkiAppearance>();if(appearance==null)appearance=rig.gameObject.AddComponent<CareerSkiAppearance>();appearance.Apply(rig,ski);
            var bib=rig.GetComponent<AthleteBibPresentation>();if(bib!=null)bib.SetLeader(careerBibs.ForAthlete(athleteId));
        }
        public void SetVisualBreathEnvironment(BreathEnvironment? environment) { if(breathVapor==null)throw new InvalidOperationException("Initialize proxies first.");breathVapor.SetEnvironment(environment); }
        public void SetViewCamera(Camera camera)
        {viewCamera=camera;chaseCamera=camera!=null?camera.GetComponent<AthleteChaseCamera>():null;}
        public int Count=>proxies.Count;
        public Transform GetProxy(string athleteId)=>proxies[athleteId].transform;
        public AthleteRigBinding GetRig(string athleteId)=>animation[athleteId].Rig;
        public void Initialize(RacePresentationFeed source,VenueAuthoringPlan plan,AthleteRigBinding template=null)
        {
            if(plan==null)throw new ArgumentNullException(nameof(plan));
            var forward=plan.Manifest.Range.Forward;
            Initialize(source,plan.Visual,template,plan.UsesPhysicalPaths?(Vector3?)new Vector3((float)forward.X,(float)forward.Y,(float)forward.Z):null,
                preserveAcceptedLine:plan.Manifest.Range.MatDefinition!=null);
        }
        public void Initialize(RacePresentationFeed source,TrackVisualDefinition geometry,AthleteRigBinding template=null,Vector3? shootingDirection=null,bool preserveAcceptedLine=false)
        {
            if(source==null||geometry==null)throw new ArgumentNullException();
            Quaternion? direction=null;
            if(shootingDirection.HasValue)
            {
                var value=shootingDirection.Value;
                if(!float.IsFinite(value.x)||!float.IsFinite(value.y)||!float.IsFinite(value.z)||Mathf.Abs(value.y)>1e-6f||Mathf.Abs(value.sqrMagnitude-1)>1e-5f)
                    throw new ArgumentException("A finite horizontal unit shooting direction is required.",nameof(shootingDirection));
                direction=Quaternion.LookRotation(new Vector3(value.x,0,value.z).normalized,Vector3.up);
            }
            Clear();careerSkis.Clear();careerNations.Clear();careerBibs=null;prestartAthleteId=null;feed=source;visual=geometry;shootingRotation=direction;preserveAcceptedShootingLine=preserveAcceptedLine;
            rigPrefab=template;
            motionSettings=AthleteLocomotionSettings.Load();
            contactSettings=GroundContactSettings.Load();
            finishStaging=FinishStagingSettings.Load();
            transitSettings=RangeTransitYieldSettings.Load();
            snowTracks=GetComponent<SnowTrackPresentation>();if(snowTracks==null)snowTracks=gameObject.AddComponent<SnowTrackPresentation>();snowTracks.Initialize();
            snowSpray=GetComponent<SnowSprayPresentation>();if(snowSpray==null)snowSpray=gameObject.AddComponent<SnowSprayPresentation>();snowSpray.Initialize();
            breathVapor=GetComponent<BreathVaporPresentation>();if(breathVapor==null)breathVapor=gameObject.AddComponent<BreathVaporPresentation>();breathVapor.Initialize();
            equipmentBatches=GetComponent<AthleteEquipmentBatches>();if(equipmentBatches==null)equipmentBatches=gameObject.AddComponent<AthleteEquipmentBatches>();
        }
        public void Render(double alpha,double completedSeconds=0)
        {
            if(double.IsNaN(alpha)||double.IsInfinity(alpha)||alpha<0||alpha>1)throw new ArgumentOutOfRangeException(nameof(alpha));
            if(!double.IsFinite(completedSeconds)||completedSeconds<0||completedSeconds>86400)throw new ArgumentOutOfRangeException(nameof(completedSeconds));
            if(feed==null)return;if(feed.Current==null){Clear();return;}if(feed.Current.TrackId!=visual.TrackId)throw new InvalidOperationException("Proxy geometry does not match snapshot.");
            var current=feed.Current;keep.Clear();float lodScale=ProjectionAwareLod.Scale(viewCamera);
            for(int i=0;i<current.Count;i++)
            {
                var state=current.Get(i);keep.Add(state.AthleteId);
                if(!proxies.TryGetValue(state.AthleteId,out var go))
                {
                    go=new GameObject("Athlete presentation pivot");go.transform.SetParent(transform,false);
                    var rig=rigPrefab!=null?Instantiate(rigPrefab,go.transform,false):AthleteRigFactory.Create(go.transform);
                    var bib=rig.GetComponent<AthleteBibPresentation>();if(bib!=null)bib.SetNumber(i+1);
                    ApplyCareerAppearance(state.AthleteId,rig);
                    equipmentBatches.Register(rig);
                    rig.transform.localPosition=Vector3.down;
                    var adapter=go.AddComponent<AthleteAnimationPresentation>();adapter.Initialize(rig);
                    proxies.Add(state.AthleteId,go);animation.Add(state.AthleteId,adapter);
                }
                bool finished=state.Phase==CompetitionPhase.Finished;
                bool waiting=state.Phase==CompetitionPhase.Prestart&&state.AthleteId==prestartAthleteId;
                go.SetActive(state.OnCourse||finished||waiting);if(!state.OnCourse&&!finished&&!waiting)continue;var position=feed.Position(i,alpha);var route=visual.Get(position.LoopId);
                double sampleDistance=position.LocalMeters,lateral=position.LateralMeters;
                // Explicit physical mats preserve the accepted body center throughout setup,
                // shooting and recovery. Legacy schematic adapters keep their previous pose.
                float setupWeight=0;
                if(state.ShootingActive)
                {
                    float setup=AthleteRiflePresentation.Progress(state.ShootingPhase,state.ShootingPhaseElapsedTicks,state.ShootingPhaseDurationTicks,alpha);
                    setupWeight=Mathf.SmoothStep(0,1,setup);if(!preserveAcceptedShootingLine)lateral*=1-setupWeight;
                }
                if(finished&&!state.WorldPose.IsValid){var offset=finishStaging.Offset(i);sampleDistance=Math.Min(route.Get(route.Count-1).Distance,sampleDistance+offset.z);lateral+=offset.x;}
                var point=route.Sample(sampleDistance);
                go.transform.localPosition=new Vector3((float)(point.X+point.ForwardZ*lateral),(float)point.Y+1,(float)(point.Z-point.ForwardX*lateral));
                var heading=Quaternion.LookRotation(new Vector3((float)point.ForwardX,0,(float)point.ForwardZ),Vector3.up);
                var physicalPose=RacePresentationSampling.WorldPose(feed.Previous,current,i,alpha);
                if(physicalPose.IsValid)
                {
                    go.transform.localPosition=new Vector3((float)physicalPose.X,(float)point.Y+1,(float)physicalPose.Z);
                    Vector3 offset=Vector3.zero;
                    if(state.RangeTransitYielding&&!state.ShootingActive)
                    for(int other=0;other<current.Count;other++)
                    {
                        if(other==i)continue;var b=current.Get(other);if(!b.OnCourse||!b.WorldPose.IsValid)continue;
                        double dx=physicalPose.X-b.WorldPose.X,dz=physicalPose.Z-b.WorldPose.Z;
                        float proximity=(float)Math.Sqrt(dx*dx+dz*dz);
                        if(proximity>=transitSettings.separation_m)continue;
                        var away=proximity>.001f?new Vector3((float)dx/proximity,0,(float)dz/proximity):Vector3.right*(i<other?-1:1);
                        offset+=away*(transitSettings.separation_m-proximity)*(b.RangeTransitYielding ? .5f : 1);
                    }
                    offset=Vector3.ClampMagnitude(offset,transitSettings.maximum_offset_m);
                    transitOffsets.TryGetValue(state.AthleteId,out var priorOffset);
                    offset=Vector3.Lerp(priorOffset,offset,1-Mathf.Exp(-transitSettings.response_per_s*Time.deltaTime));
                    transitOffsets[state.AthleteId]=offset;go.transform.localPosition+=offset;
                    heading=Quaternion.Euler(0,(float)(physicalPose.HeadingRadians*180/Math.PI),0);
                }
                // The entry/exit tangent can be perpendicular to the target bank. Reuse the
                // observed rifle timing to turn only the visible body, including its return.
                go.transform.localRotation=shootingRotation.HasValue&&state.ShootingActive?Quaternion.Slerp(heading,shootingRotation.Value,setupWeight):heading;
                double curvature=physicalPose.IsValid?state.CourseCurvaturePerMeter:VisualRouteCurvature.Sample(route,position.LocalMeters,motionSettings.corner_sample_half_span_m);
                double grade=physicalPose.IsValid?state.CourseGrade:VisualRouteSurface.Grade(route,sampleDistance,contactSettings.sample_half_span_m);
                if(chaseCamera!=null&&chaseCamera.Target==go.transform)chaseCamera.SetTerrainGrade((float)grade);
                float distance=viewCamera!=null?Vector3.Distance(viewCamera.transform.position,go.transform.position):0;
                long poseTick=current.Tick;double poseAlpha=alpha;
                if(finished&&current.Complete){double ticks=completedSeconds*current.TickFrequency;poseTick+=(long)Math.Floor(ticks);poseAlpha=ticks-Math.Floor(ticks);}
                double lateralSpeed=0;
                var previous=feed.Previous;
                if(!finished&&!state.ShootingActive&&previous!=null&&i<previous.Count&&current.Tick>previous.Tick)
                {
                    var prior=previous.Get(i);
                    if(prior.AthleteId==state.AthleteId&&prior.OnCourse&&prior.LoopId==state.LoopId&&!prior.ShootingActive)
                        lateralSpeed=(state.LateralMeters-prior.LateralMeters)*current.TickFrequency/(current.Tick-previous.Tick);
                }
                var frame=new AthleteAnimationFrame(state,poseTick,current.TickFrequency,poseAlpha,finished?0:curvature,grade,lateralSpeed);
                animation[state.AthleteId].Render(frame,distance*lodScale);
                snowTracks.Observe(state.AthleteId,animation[state.AthleteId].Rig,frame,distance);
                snowSpray.Observe(state.AthleteId,animation[state.AthleteId].Rig,frame,distance);
                breathVapor.Observe(state.AthleteId,animation[state.AthleteId].Rig,frame,distance);
            }
            snowTracks.Render(Math.Max(0,current.Tick-1+alpha)/current.TickFrequency,viewCamera!=null?viewCamera.transform.position:transform.position);
            snowSpray.Render(Math.Max(0,current.Tick-1+alpha)/current.TickFrequency,viewCamera);
            breathVapor.Render(Math.Max(0,current.Tick-1+alpha)/current.TickFrequency,viewCamera);
            removed.Clear();foreach(var id in proxies.Keys)if(!keep.Contains(id))removed.Add(id);foreach(var id in removed){Destroy(proxies[id]);proxies.Remove(id);animation.Remove(id);}
        }
        private void LateUpdate()
        {
            if(feed==null)return;if(feed.Current==null){Clear();return;}
            if(observedTick!=feed.Current.Tick){observedTick=feed.Current.Tick;publishedAt=Time.timeAsDouble;}
            double elapsed=Time.timeAsDouble-publishedAt;
            Render(feed.Current.Complete?1:feed.InterpolationAlpha??Math.Min(1,elapsed*feed.Current.TickFrequency),feed.Current.Complete?Math.Min(86400,elapsed):0);
        }
        private void Clear(){if(equipmentBatches!=null)equipmentBatches.Clear();foreach(var go in proxies.Values)if(go!=null)Destroy(go);proxies.Clear();transitOffsets.Clear();animation.Clear();keep.Clear();removed.Clear();observedTick=-1;if(snowTracks!=null)snowTracks.Clear();if(snowSpray!=null)snowSpray.Clear();if(breathVapor!=null)breathVapor.Clear();}
        private void OnDestroy()=>Clear();
    }
}

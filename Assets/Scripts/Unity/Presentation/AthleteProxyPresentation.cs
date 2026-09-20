using System;
using System.Collections.Generic;
using Biathlon.Domain;
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
        private SnowTrackPresentation snowTracks;
        private SnowSprayPresentation snowSpray;
        private BreathVaporPresentation breathVapor;
        private FinishStagingSettings finishStaging;
        public void SetVisualBreathEnvironment(BreathEnvironment? environment) { if(breathVapor==null)throw new InvalidOperationException("Initialize proxies first.");breathVapor.SetEnvironment(environment); }
        public void SetViewCamera(Camera camera) => viewCamera = camera;
        public int Count=>proxies.Count;
        public Transform GetProxy(string athleteId)=>proxies[athleteId].transform;
        public AthleteRigBinding GetRig(string athleteId)=>animation[athleteId].Rig;
        public void Initialize(RacePresentationFeed source,TrackVisualDefinition geometry,AthleteRigBinding template=null)
        {
            if(source==null||geometry==null)throw new ArgumentNullException();Clear();feed=source;visual=geometry;
            rigPrefab=template;
            motionSettings=AthleteLocomotionSettings.Load();
            contactSettings=GroundContactSettings.Load();
            finishStaging=FinishStagingSettings.Load();
            snowTracks=GetComponent<SnowTrackPresentation>();if(snowTracks==null)snowTracks=gameObject.AddComponent<SnowTrackPresentation>();snowTracks.Initialize();
            snowSpray=GetComponent<SnowSprayPresentation>();if(snowSpray==null)snowSpray=gameObject.AddComponent<SnowSprayPresentation>();snowSpray.Initialize();
            breathVapor=GetComponent<BreathVaporPresentation>();if(breathVapor==null)breathVapor=gameObject.AddComponent<BreathVaporPresentation>();breathVapor.Initialize();
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
                    rig.transform.localPosition=Vector3.down;
                    var adapter=go.AddComponent<AthleteAnimationPresentation>();adapter.Initialize(rig);
                    proxies.Add(state.AthleteId,go);animation.Add(state.AthleteId,adapter);
                }
                bool finished=state.Phase==CompetitionPhase.Finished;
                go.SetActive(state.OnCourse||finished);if(!state.OnCourse&&!finished)continue;var position=feed.Position(i,alpha);var route=visual.Get(position.LoopId);
                double sampleDistance=position.LocalMeters,lateral=position.LateralMeters;
                if(finished){var offset=finishStaging.Offset(i);sampleDistance=Math.Min(route.Get(route.Count-1).Distance,sampleDistance+offset.z);lateral+=offset.x;}
                var point=route.Sample(sampleDistance);
                go.transform.localPosition=new Vector3((float)(point.X+point.ForwardZ*lateral),(float)point.Y+1,(float)(point.Z-point.ForwardX*lateral));
                go.transform.localRotation=Quaternion.LookRotation(new Vector3((float)point.ForwardX,0,(float)point.ForwardZ),Vector3.up);
                double curvature=VisualRouteCurvature.Sample(visual.Get(position.LoopId),position.LocalMeters,motionSettings.corner_sample_half_span_m);
                double grade=VisualRouteSurface.Grade(route,sampleDistance,contactSettings.sample_half_span_m);
                float distance=viewCamera!=null?Vector3.Distance(viewCamera.transform.position,go.transform.position):0;
                long poseTick=current.Tick;double poseAlpha=alpha;
                if(finished&&current.Complete){double ticks=completedSeconds*current.TickFrequency;poseTick+=(long)Math.Floor(ticks);poseAlpha=ticks-Math.Floor(ticks);}
                var frame=new AthleteAnimationFrame(state,poseTick,current.TickFrequency,poseAlpha,finished?0:curvature,grade);
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
            Render(Math.Min(1,elapsed*feed.Current.TickFrequency),feed.Current.Complete?Math.Min(86400,elapsed):0);
        }
        private void Clear(){foreach(var go in proxies.Values)if(go!=null)Destroy(go);proxies.Clear();animation.Clear();keep.Clear();removed.Clear();observedTick=-1;if(snowTracks!=null)snowTracks.Clear();if(snowSpray!=null)snowSpray.Clear();if(breathVapor!=null)breathVapor.Clear();}
        private void OnDestroy()=>Clear();
    }
}

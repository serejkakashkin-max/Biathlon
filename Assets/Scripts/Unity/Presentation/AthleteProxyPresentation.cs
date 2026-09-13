using System;
using System.Collections.Generic;
using Biathlon.Domain;
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
        public int Count=>proxies.Count;
        public Transform GetProxy(string athleteId)=>proxies[athleteId].transform;
        public AthleteRigBinding GetRig(string athleteId)=>animation[athleteId].Rig;
        public void Initialize(RacePresentationFeed source,TrackVisualDefinition geometry,AthleteRigBinding template=null)
        {
            if(source==null||geometry==null)throw new ArgumentNullException();Clear();feed=source;visual=geometry;
            rigPrefab=template;
            motionSettings=AthleteLocomotionSettings.Load();
            contactSettings=GroundContactSettings.Load();
        }
        public void Render(double alpha)
        {
            if(double.IsNaN(alpha)||double.IsInfinity(alpha)||alpha<0||alpha>1)throw new ArgumentOutOfRangeException(nameof(alpha));
            if(feed==null)return;if(feed.Current==null){Clear();return;}if(feed.Current.TrackId!=visual.TrackId)throw new InvalidOperationException("Proxy geometry does not match snapshot.");
            var current=feed.Current;keep.Clear();
            for(int i=0;i<current.Count;i++)
            {
                var state=current.Get(i);keep.Add(state.AthleteId);
                if(!proxies.TryGetValue(state.AthleteId,out var go))
                {
                    go=new GameObject("Athlete presentation pivot");go.transform.SetParent(transform,false);
                    var rig=rigPrefab!=null?Instantiate(rigPrefab,go.transform,false):OriginalAthleteRigBuilder.Create(go.transform);
                    rig.transform.localPosition=Vector3.down;
                    var adapter=go.AddComponent<AthleteAnimationPresentation>();adapter.Initialize(rig);
                    proxies.Add(state.AthleteId,go);animation.Add(state.AthleteId,adapter);
                }
                go.SetActive(state.OnCourse);if(!state.OnCourse)continue;var position=feed.Position(i,alpha);var point=visual.Get(position.LoopId).Sample(position.LocalMeters);
                go.transform.localPosition=new Vector3((float)(point.X+point.ForwardZ*position.LateralMeters),(float)point.Y+1,(float)(point.Z-point.ForwardX*position.LateralMeters));
                go.transform.localRotation=Quaternion.LookRotation(new Vector3((float)point.ForwardX,0,(float)point.ForwardZ),Vector3.up);
                double curvature=VisualRouteCurvature.Sample(visual.Get(position.LoopId),position.LocalMeters,motionSettings.corner_sample_half_span_m);
                double grade=VisualRouteSurface.Grade(visual.Get(position.LoopId),position.LocalMeters,contactSettings.sample_half_span_m);
                animation[state.AthleteId].Render(new AthleteAnimationFrame(state,current.Tick,current.TickFrequency,alpha,curvature,grade));
            }
            removed.Clear();foreach(var id in proxies.Keys)if(!keep.Contains(id))removed.Add(id);foreach(var id in removed){Destroy(proxies[id]);proxies.Remove(id);animation.Remove(id);}
        }
        private void LateUpdate()
        {
            if(feed==null)return;if(feed.Current==null){Clear();return;}
            if(observedTick!=feed.Current.Tick){observedTick=feed.Current.Tick;publishedAt=Time.timeAsDouble;}
            Render(Math.Min(1,(Time.timeAsDouble-publishedAt)*feed.Current.TickFrequency));
        }
        private void Clear(){foreach(var go in proxies.Values)if(go!=null)Destroy(go);proxies.Clear();animation.Clear();keep.Clear();removed.Clear();observedTick=-1;}
        private void OnDestroy()=>Clear();
    }
}

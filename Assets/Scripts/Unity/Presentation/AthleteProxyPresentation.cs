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
        private Material material;
        private double publishedAt;
        private long observedTick=-1;
        public int Count=>proxies.Count;
        public Transform GetProxy(string athleteId)=>proxies[athleteId].transform;
        public void Initialize(RacePresentationFeed source,TrackVisualDefinition geometry)
        {
            if(source==null||geometry==null)throw new ArgumentNullException();Clear();feed=source;visual=geometry;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit")){color=new Color(.85f,.15f,.12f)};
        }
        public void Render(double alpha)
        {
            if(feed==null||feed.Current==null)return;if(feed.Current.TrackId!=visual.TrackId)throw new InvalidOperationException("Proxy geometry does not match snapshot.");
            var current=feed.Current;var keep=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<current.Count;i++)
            {
                var state=current.Get(i);keep.Add(state.AthleteId);
                if(!proxies.TryGetValue(state.AthleteId,out var go))
                {
                    go=GameObject.CreatePrimitive(PrimitiveType.Capsule);go.name="Athlete proxy";go.transform.SetParent(transform,false);Destroy(go.GetComponent<Collider>());go.GetComponent<Renderer>().sharedMaterial=material;proxies.Add(state.AthleteId,go);
                }
                go.SetActive(state.OnCourse);if(!state.OnCourse)continue;var position=feed.Position(i,alpha);var point=visual.Get(position.LoopId).Sample(position.LocalMeters);
                go.transform.localPosition=new Vector3((float)(point.X+point.ForwardZ*position.LateralMeters),(float)point.Y+1,(float)(point.Z-point.ForwardX*position.LateralMeters));
                go.transform.localRotation=Quaternion.LookRotation(new Vector3((float)point.ForwardX,0,(float)point.ForwardZ),Vector3.up);
            }
            var removed=new List<string>();foreach(var id in proxies.Keys)if(!keep.Contains(id))removed.Add(id);foreach(var id in removed){Destroy(proxies[id]);proxies.Remove(id);}
        }
        private void LateUpdate()
        {
            if(feed==null||feed.Current==null)return;
            if(observedTick!=feed.Current.Tick){observedTick=feed.Current.Tick;publishedAt=Time.timeAsDouble;}
            Render(Math.Min(1,(Time.timeAsDouble-publishedAt)*feed.Current.TickFrequency));
        }
        private void Clear(){foreach(var go in proxies.Values)if(go!=null)Destroy(go);proxies.Clear();if(material!=null)Destroy(material);observedTick=-1;}
        private void OnDestroy()=>Clear();
    }
}

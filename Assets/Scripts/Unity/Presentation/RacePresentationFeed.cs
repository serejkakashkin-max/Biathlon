using System;
using Biathlon.Simulation;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    /// <summary>Presentation-owned handoff. The race host publishes detached snapshots;
    /// render consumers cannot obtain or advance the authoritative field through this component.</summary>
    public sealed class RacePresentationFeed : MonoBehaviour
    {
        public event Action<RacePresentationSnapshot> Published;
        public event Action RaceReset;
        public RacePresentationSnapshot Previous { get; private set; }
        public RacePresentationSnapshot Current { get; private set; }
        public double? InterpolationAlpha {get;private set;}
        public void SetInterpolationAlpha(double alpha)
        {if(!double.IsFinite(alpha)||alpha<0||alpha>1)throw new ArgumentOutOfRangeException(nameof(alpha));InterpolationAlpha=alpha;}
        public void Publish(RacePresentationSnapshot snapshot)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));
            if(Current!=null&&(snapshot.Tick<=Current.Tick||snapshot.TrackId!=Current.TrackId||snapshot.ProfileId!=Current.ProfileId||snapshot.TickFrequency!=Current.TickFrequency))throw new ArgumentException("Reset before publishing another race or a nonincreasing tick.");
            Previous=Current;Current=snapshot;
            Published?.Invoke(snapshot);
        }
        public void ResetRace(){Previous=null;Current=null;InterpolationAlpha=null;RaceReset?.Invoke();}
        public PresentationPosition Position(int index,double alpha)=>RacePresentationSampling.Position(Previous,Current,index,alpha);
    }
}

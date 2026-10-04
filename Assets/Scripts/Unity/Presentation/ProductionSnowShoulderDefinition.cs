using System;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Optional cold presentation bank policy. Width remains in the existing snow blend definition.</summary>
    [Serializable] public sealed class ProductionSnowShoulderDefinition
    {
        [Serializable] public sealed class Window {public double start_m,end_m;}
        public int schema_version;
        public string id,venue_id,track_id,visual_id,loop_id;
        public Window[] windows;
        [NonSerialized] public string TrackSnapshotJson,VisualSnapshotJson,SharedApronSnapshotJson;
        public static ProductionSnowShoulderDefinition FromJson(string json,VenueAuthoringPlan plan)
        {
            if(plan==null||plan.SharedApron==null)throw new ArgumentException("Banked presentation requires the explicit compact physical family.");
            var value=JsonUtility.FromJson<ProductionSnowShoulderDefinition>(json);
            if(value==null||value.schema_version!=1||value.venue_id!=plan.Manifest.Id||value.track_id!=plan.Track.Id||value.visual_id!=plan.Visual.Id||
                value.loop_id!="sprint_extension"||value.windows==null||value.windows.Length!=2)
                throw new ArgumentException("Explicit versioned two-fork Sprint presentation policy required.");
            VenueManifest.ResourceId(value.id);if(value.id.IndexOf('/')>=0)throw new ArgumentException("Single-component stable snow bank identity required.");
            var loop=plan.Track.GetLoop(value.loop_id);plan.Visual.Get(value.loop_id);
            if(loop.Closed)throw new ArgumentException("Actual open paid format branch required.");
            double previous=-1;
            foreach(var window in value.windows)
            {
                if(window==null||!double.IsFinite(window.start_m)||!double.IsFinite(window.end_m)||window.start_m<0||
                    window.end_m<=window.start_m||window.end_m>loop.Length.Meters||window.start_m<=previous)
                    throw new ArgumentException("Disjoint ordered windows inside the exact branch required.");
                previous=window.end_m;
            }
            if(value.windows[0].start_m!=0||value.windows[1].end_m!=loop.Length.Meters)
                throw new ArgumentException("The optional bank policy covers both true branch forks.");
            value.TrackSnapshotJson=plan.Track.ToJson();value.VisualSnapshotJson=plan.Visual.ToJson();value.SharedApronSnapshotJson=plan.SharedApron.ToJson();return value;
        }
        public void RequireBinding(VenueAuthoringPlan plan)
        {
            if(plan==null||plan.Track.ToJson()!=TrackSnapshotJson||plan.Visual.ToJson()!=VisualSnapshotJson||plan.SharedApron?.ToJson()!=SharedApronSnapshotJson)
                throw new ArgumentException("Snow bank policy cannot move to another same-ID physical family.");
        }
        public bool ContainsPanel(string loop,double start,double end)
        {
            if(loop!=loop_id)return false;
            foreach(var window in windows)if(start>=window.start_m&&end<=window.end_m)return true;
            return false;
        }
    }
}

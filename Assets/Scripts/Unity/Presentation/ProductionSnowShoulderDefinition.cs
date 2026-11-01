using System;
using Biathlon.Domain;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Optional cold presentation bank policy; absent dimensions retain the original blend width and flat profile.</summary>
    [Serializable] public sealed class ProductionSnowShoulderDefinition
    {
        [Serializable] public sealed class Window {public double start_m,end_m;}
        [Serializable] public sealed class RoadWindow {public string loop_id;public double start_m,end_m;}
        [Serializable] public sealed class NaturalProfile
        {
            public string[] exact_cut_formats;
            public int cross_sections;
            public bool all_routes;
            public float minimum_width_m, end_fade_m, width_variation;
            public float rim_direction_window_m,rim_maximum_displacement_m;
            public float rim_minimum_clearance_m;
            public bool UsesExactCuts(CompetitionFormat format)
            {if(exact_cut_formats!=null)foreach(string value in exact_cut_formats)if(Enum.Parse<CompetitionFormat>(value)==format)return true;return false;}
        }
        public NaturalProfile natural;
        public int schema_version;
        public string id,venue_id,track_id,visual_id,loop_id;
        public Window[] windows;
        public RoadWindow[] additional_windows;
        public float width_m,mound_height_m,mound_spacing_m;
        [NonSerialized] public string TrackSnapshotJson,VisualSnapshotJson,SharedApronSnapshotJson;
        public static ProductionSnowShoulderDefinition FromJson(string json,VenueAuthoringPlan plan)
        {
            if(plan==null||plan.SharedApron==null)throw new ArgumentException("Banked presentation requires the explicit compact physical family.");
            var value=JsonUtility.FromJson<ProductionSnowShoulderDefinition>(json);
            if(value==null||(value.schema_version!=1&&value.schema_version!=2)||value.venue_id!=plan.Manifest.Id||value.track_id!=plan.Track.Id||value.visual_id!=plan.Visual.Id||
                value.loop_id!="sprint_extension"||value.windows==null||value.windows.Length!=2)
                throw new ArgumentException("Explicit versioned two-fork Sprint presentation policy required.");
            VenueManifest.ResourceId(value.id);if(value.id.IndexOf('/')>=0)throw new ArgumentException("Single-component stable snow bank identity required.");
            var loop=plan.Track.GetLoop(value.loop_id);plan.Visual.Get(value.loop_id);
            if(loop.Closed)throw new ArgumentException("Actual open paid format branch required.");
            if(!float.IsFinite(value.width_m)||value.width_m<0||value.width_m>8||
                !float.IsFinite(value.mound_height_m)||value.mound_height_m<0||value.mound_height_m>1.5f||
                !float.IsFinite(value.mound_spacing_m)||value.mound_spacing_m<0||
                value.mound_height_m>0&&(value.mound_spacing_m<4||value.mound_spacing_m>100))
                throw new ArgumentException("Finite bounded presentation snow mound dimensions required.");
            if(value.schema_version==2&&(value.natural==null||value.natural.cross_sections<4||value.natural.cross_sections>16||
                !float.IsFinite(value.natural.minimum_width_m)||value.natural.minimum_width_m<.05f||value.natural.minimum_width_m>value.width_m||
                !float.IsFinite(value.natural.end_fade_m)||value.natural.end_fade_m<1||value.natural.end_fade_m>30||
                !float.IsFinite(value.natural.width_variation)||value.natural.width_variation<0||value.natural.width_variation>.35f||
                !float.IsFinite(value.natural.rim_direction_window_m)||value.natural.rim_direction_window_m<.1f||value.natural.rim_direction_window_m>5||
                !float.IsFinite(value.natural.rim_maximum_displacement_m)||value.natural.rim_maximum_displacement_m<.01f||value.natural.rim_maximum_displacement_m>1||
                !float.IsFinite(value.natural.rim_minimum_clearance_m)||value.natural.rim_minimum_clearance_m<.025f||value.natural.rim_minimum_clearance_m>.03f))
                throw new ArgumentException("Bounded natural snow profile required for schema 2.");
            if(value.schema_version==1&&value.natural!=null)
            {
                // JsonUtility materializes an omitted inline reference as an all-zero
                // object. Preserve old JSON semantics, but reject any real new tuning.
                var n=value.natural;
                if(n.cross_sections!=0||n.all_routes||n.minimum_width_m!=0||n.end_fade_m!=0||n.width_variation!=0||n.rim_direction_window_m!=0||n.rim_maximum_displacement_m!=0||n.rim_minimum_clearance_m!=0)
                    throw new ArgumentException("Natural profile requires explicit schema 2.");
                value.natural=null;
            }
            if(value.natural?.exact_cut_formats!=null)
            {
                var used=new System.Collections.Generic.HashSet<CompetitionFormat>();
                foreach(string format in value.natural.exact_cut_formats)
                    if(!Enum.TryParse<CompetitionFormat>(format,false,out var parsed)||!Enum.IsDefined(typeof(CompetitionFormat),parsed)||!used.Add(parsed))throw new ArgumentException("Unique existing competition formats required for exact visual cuts.");
            }
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
            if(value.additional_windows!=null)foreach(var w in value.additional_windows)
            {
                if(w==null||!double.IsFinite(w.start_m)||!double.IsFinite(w.end_m)||w.start_m<0||w.end_m<=w.start_m||
                    w.end_m>plan.Track.GetLoop(w.loop_id).Length.Meters)throw new ArgumentException("Snow shoulder window must belong to its actual road.");
                plan.Visual.Get(w.loop_id);
            }
            value.TrackSnapshotJson=plan.Track.ToJson();value.VisualSnapshotJson=plan.Visual.ToJson();value.SharedApronSnapshotJson=plan.SharedApron.ToJson();return value;
        }
        public void RequireBinding(VenueAuthoringPlan plan)
        {
            if(plan==null||plan.Track.ToJson()!=TrackSnapshotJson||plan.Visual.ToJson()!=VisualSnapshotJson||plan.SharedApron?.ToJson()!=SharedApronSnapshotJson)
                throw new ArgumentException("Snow bank policy cannot move to another same-ID physical family.");
        }
        public bool ContainsPanel(string loop,double start,double end)
        {
            if(natural?.all_routes==true)return true;
            if(additional_windows!=null)foreach(var w in additional_windows)
                if(loop==w.loop_id&&start>=w.start_m&&end<=w.end_m)return true;
            if(loop!=loop_id)return false;
            foreach(var window in windows)if(start>=window.start_m&&end<=window.end_m)return true;
            return false;
        }
    }
}

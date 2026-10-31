using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class RaceMiniMapSettings
    {
        public int schema_version,width,map_height,padding,gap,max_athletes;
        public string id;
        public float sample_spacing_m,rival_radius,player_radius,course_width;
        public static RaceMiniMapSettings Load()=>FromJson(ProductionVenueHost.ReadResource("Biathlon/UI/gameplay_minimap_v1"));
        public static RaceMiniMapSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<RaceMiniMapSettings>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.width<180||d.width>250||d.map_height<120||d.map_height>210||
                d.padding<8||d.padding>20||d.gap<8||d.gap>24||d.max_athletes<30||d.max_athletes>128)throw new ArgumentException("Bounded minimap definition required.");
            ProductionEnvironmentDefinition.Bound(d.sample_spacing_m,1,8);ProductionEnvironmentDefinition.Bound(d.rival_radius,1.5f,3);
            ProductionEnvironmentDefinition.Bound(d.player_radius,4,7);ProductionEnvironmentDefinition.Bound(d.course_width,1.5f,4);
            return d;
        }
    }
    public readonly struct RaceMiniMapLine
    {
        public readonly string LoopId;
        public readonly Vector2[] Points;
        public readonly bool Penalty;
        public RaceMiniMapLine(string id,Vector2[] points,bool penalty){LoopId=id;Points=points;Penalty=penalty;}
    }
    /// <summary>Cold, detached vector art. Uses the exact same selected slices as the rendered course.</summary>
    public sealed class RaceMiniMapGeometry
    {
        public string TrackId{get;}
        public string ProfileId{get;}
        public readonly RaceMiniMapLine[] Lines;
        public readonly Vector2 Min,Max,RangeLeft,RangeRight,Finish;
        private readonly Dictionary<string,TrackVisualLoop> loops=new Dictionary<string,TrackVisualLoop>(StringComparer.Ordinal);
        public RaceMiniMapGeometry(VenueAuthoringPlan venue,RaceRoute route,RaceMiniMapSettings settings)
        {
            if(venue==null||route==null||settings==null)throw new ArgumentNullException();
            TrackId=venue.Track.Id;ProfileId=route.CompetitionProfileId;
            for(int i=0;i<venue.Visual.Count;i++){var loop=venue.Visual.Get(i);loops.Add(loop.LoopId,loop);}
            var usage=ProductionCoursePresentation.RouteUsage(venue,route);var lines=new List<RaceMiniMapLine>();
            Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
            foreach(var item in usage)foreach(var interval in item.Value)
            {
                double start=interval.Item1/1000d,end=interval.Item2/1000d;int count=Mathf.CeilToInt((float)((end-start)/settings.sample_spacing_m));
                if(count<1||count>10000)throw new ArgumentException("Bounded positive map slice required.");
                var points=new Vector2[count+1];var loop=loops[item.Key];
                for(int i=0;i<=count;i++)
                {var p=loop.Sample(i==count?end:start+(end-start)*i/count);points[i]=new Vector2((float)p.X,(float)-p.Z);min=Vector2.Min(min,points[i]);max=Vector2.Max(max,points[i]);}
                lines.Add(new RaceMiniMapLine(item.Key,points,item.Key==venue.Track.Penalty.LoopId));
            }
            if(lines.Count==0)throw new ArgumentException("Selected route has no minimap geometry.");
            Lines=lines.ToArray();Min=min;Max=max;
            var left=venue.MatPoint(0);var right=venue.MatPoint(venue.LaneCount-1);
            RangeLeft=new Vector2((float)left.X,(float)-left.Z);RangeRight=new Vector2((float)right.X,(float)-right.Z);
            var final=route.GetSection(route.SectionCount-1);var finish=loops[final.LoopId].Sample(final.LoopEnd.Meters);
            Finish=new Vector2((float)finish.X,(float)-finish.Z);
        }
        public Vector2 Project(Vector2 mapPoint,Rect rect,float padding)
        {
            float scale=Mathf.Min(Mathf.Max(1,rect.width-2*padding)/Mathf.Max(1,Max.x-Min.x),Mathf.Max(1,rect.height-2*padding)/Mathf.Max(1,Max.y-Min.y));
            return rect.center+(mapPoint-(Min+Max)*.5f)*scale;
        }
        public void Sample(AthletePresentationState state,out Vector2 point,out Vector2 direction)
        {
            if(state.WorldPose.IsValid)
            {
                point=new Vector2((float)state.WorldPose.X,(float)-state.WorldPose.Z);
                direction=new Vector2((float)Math.Sin(state.WorldPose.HeadingRadians),(float)-Math.Cos(state.WorldPose.HeadingRadians));return;
            }
            if(!loops.TryGetValue(state.LoopId,out var loop))throw new ArgumentException("An observed actor must have a bound visual loop.");
            var p=loop.Sample(Math.Max(0,Math.Min(loop.Get(loop.Count-1).Distance,state.LocalNanometers/(double)SkiState.NanometersPerMeter)));
            point=new Vector2((float)(p.X+p.ForwardZ*state.LateralMeters),(float)-(p.Z-p.ForwardX*state.LateralMeters));
            direction=new Vector2((float)p.ForwardX,(float)-p.ForwardZ);
        }
    }
    public readonly struct RaceMiniMapMarker
    {
        public readonly Vector2 Point,Direction;
        public readonly bool Player,Waiting,Shooting,Visible;
        public RaceMiniMapMarker(Vector2 point,Vector2 direction,bool player,AthletePresentationState state)
        {Point=point;Direction=direction;Player=player;Waiting=state.Phase==CompetitionPhase.Prestart;Shooting=state.ShootingActive;Visible=state.Phase<CompetitionPhase.DidNotStart;}
    }
    /// <summary>Only accepted snapshots enter this bounded marker buffer. No authority or scene lookups.</summary>
    public sealed class RaceMiniMapState
    {
        private readonly RaceMiniMapMarker[] markers;
        private RacePresentationSnapshot observed;
        public int Count{get;private set;}
        public int Rivals{get;private set;}
        public int PlayerIndex{get;private set;}=-1;
        public int Observations{get;private set;}
        public RaceMiniMapMarker Get(int index)=>index>=0&&index<Count?markers[index]:throw new ArgumentOutOfRangeException(nameof(index));
        public RaceMiniMapState(int capacity){if(capacity<1||capacity>128)throw new ArgumentOutOfRangeException(nameof(capacity));markers=new RaceMiniMapMarker[capacity];}
        public bool Observe(RacePresentationSnapshot snapshot,RaceMiniMapGeometry geometry,int player)
        {
            if(snapshot==null){Clear();return true;}
            if(geometry==null||snapshot.TrackId!=geometry.TrackId||snapshot.ProfileId!=geometry.ProfileId||snapshot.Count>markers.Length||player<0||player>=snapshot.Count)throw new ArgumentException("Matching minimap observations required.");
            if(ReferenceEquals(observed,snapshot)&&PlayerIndex==player)return false;
            Count=snapshot.Count;PlayerIndex=player;Rivals=0;
            for(int i=0;i<Count;i++)
            {var actor=snapshot.Get(i);geometry.Sample(actor,out var point,out var direction);markers[i]=new RaceMiniMapMarker(point,direction,i==player,actor);if(i!=player&&markers[i].Visible)Rivals++;}
            observed=snapshot;Observations++;return true;
        }
        public void Clear(){observed=null;Count=Rivals=0;PlayerIndex=-1;}
    }
}

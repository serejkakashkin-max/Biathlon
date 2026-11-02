using System;
using System.Collections.Generic;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>Optional complete world-body proof for explicitly named range/penalty regions.
    /// The field supplies its already validated paid receipts and accepted incoming tails.
    /// Unknown geometry, a wrapped unsplit sweep or an absent separating axis stays blocked.</summary>
    public sealed class PhysicalRangeWorldTrafficMap
    {
        private readonly PhysicalTrackMap map;
        private readonly TrackDefinition track;
        private readonly TrackVisualDefinition visual;
        private readonly OccupancyDefinition body;
        private readonly PhysicalPassageCorridor[] corridors;
        private readonly PhysicalApronBodySupportDraft[,] projections,poseProjections;
        private readonly PhysicalApronSeamProjectionDraft[,] seamRotations,wrapRotations,seamPoseRotations,wrapPoseRotations;
        private readonly int representativeCorridor;
        private readonly bool[] applicable,penaltyRegions;
        private readonly bool[,] singleIncidentLoop;
        private readonly bool[] capturedBranchJunction;
        private readonly bool[] paidRouteLoops;
        private readonly double radius,margin;
        public string ContextJson {get;}
        public PhysicalRangeWorldTrafficMap(PhysicalTrackMap map,TrackDefinition track,RaceRoute route,
            TrackVisualDefinition visual,PhysicalRangeFieldLayout layout,PhysicalJunctionDefinition junctions,
            PhysicalConnectionDefinition connections,OccupancyDefinition body,PhysicalSharedApronDefinition definition,string capturedStartLoop=null)
        {
            if(map==null||track==null||route==null||visual==null||layout==null||junctions==null||connections==null||body==null||definition==null)throw new ArgumentNullException();
            if(definition.PenaltyWorldMergeCount==0||!map.IsBoundTo(track,route)||!layout.IsBoundTo(track,route)||
                definition.TrackSnapshotJson!=track.ToJson()||definition.VisualSnapshotJson!=visual.ToJson()||
                definition.JunctionSnapshotJson!=junctions.ToJson()||definition.ConnectionSnapshotJson!=connections.ToJson()||
                definition.OccupancySnapshotJson!=body.ToJson()||definition.RangePathsSnapshotJson!=layout.Definition.ToJson())
                throw new ArgumentException("Exact named range-world geometry and provenance required.");
            this.map=map;this.track=track;this.visual=visual;this.body=body;
            var loops=new HashSet<string>(StringComparer.Ordinal){track.Penalty.LoopId};
            if(capturedStartLoop!=null){track.GetLoop(capturedStartLoop);loops.Add(capturedStartLoop);}
            var paidLoops=new HashSet<string>(StringComparer.Ordinal);
            for(int s=0;s<route.SectionCount;s++){string loop=route.GetSection(s).LoopId;loops.Add(loop);paidLoops.Add(loop);}
            paidRouteLoops=new bool[track.LoopCount];
            for(int i=0;i<paidRouteLoops.Length;i++)paidRouteLoops[i]=paidLoops.Contains(track.GetLoop(i).Id);
            for(int p=0;p<layout.Definition.Count;p++)for(int s=0;s<layout.Definition.GetLane(p).SliceCount;s++)loops.Add(layout.Definition.GetLane(p).GetSlice(s).LoopId);
            corridors=new PhysicalPassageCorridor[track.LoopCount];projections=new PhysicalApronBodySupportDraft[track.LoopCount,track.LoopCount];
            poseProjections=new PhysicalApronBodySupportDraft[track.LoopCount,track.LoopCount];
            for(int i=0;i<corridors.Length;i++)if(loops.Contains(track.GetLoop(i).Id))
                corridors[i]=PhysicalPassageCorridor.CaptureFullLoop(visual.Get(track.GetLoop(i).Id),track,definition.ValidationPolicy);
            for(int i=0;i<corridors.Length;i++)if(corridors[i]!=null)for(int j=0;j<=i;j++)if(corridors[j]!=null)
            {
                projections[i,j]=projections[j,i]=new PhysicalApronBodySupportDraft(corridors[i],corridors[j],body,definition.ValidationPolicy,false,true);
                poseProjections[i,j]=poseProjections[j,i]=new PhysicalApronBodySupportDraft(corridors[i],corridors[j],body,definition.ValidationPolicy,true,true);
            }
            for(int i=0;i<corridors.Length;i++)if(corridors[i]!=null){representativeCorridor=i;break;}
            seamRotations=new PhysicalApronSeamProjectionDraft[connections.Count,corridors.Length];
            wrapRotations=new PhysicalApronSeamProjectionDraft[corridors.Length,corridors.Length];
            seamPoseRotations=new PhysicalApronSeamProjectionDraft[connections.Count,corridors.Length];
            wrapPoseRotations=new PhysicalApronSeamProjectionDraft[corridors.Length,corridors.Length];
            for(int c=0;c<connections.Count;c++)
            {
                var connection=connections.Get(c);PhysicalJunctionPort from=null,to=null;
                for(int j=0;j<junctions.Count;j++)if(junctions.Get(j).Id==connection.JunctionId)
                    for(int p=0;p<junctions.Get(j).PortCount;p++)
                    {var port=junctions.Get(j).GetPort(p);if(port.Id==connection.FromPortId)from=port;if(port.Id==connection.ToPortId)to=port;}
                var source=map.MapLoop(from.Anchor.LoopId,from.Anchor.Distance.Millimeters*SkiState.NanometersPerMillimeter);
                var target=map.MapLoop(to.Anchor.LoopId,to.Anchor.Distance.Millimeters*SkiState.NanometersPerMillimeter);
                if(corridors[source.LoopIndex]==null||corridors[target.LoopIndex]==null||map.SameLoop(source,target))continue;
                for(int i=0;i<corridors.Length;i++)if(corridors[i]!=null)
                {
                    seamRotations[c,i]=new PhysicalApronSeamProjectionDraft(map,body,source,target,corridors[source.LoopIndex],corridors[target.LoopIndex],corridors[i],definition.ValidationPolicy);
                    seamPoseRotations[c,i]=new PhysicalApronSeamProjectionDraft(map,body,source,target,corridors[source.LoopIndex],corridors[target.LoopIndex],corridors[i],definition.ValidationPolicy,false,true);
                }
            }
            for(int c=0;c<corridors.Length;c++)if(corridors[c]!=null&&track.GetLoop(c).Closed)
            {
                var point=map.MapLoop(track.GetLoop(c).Id,0);
                for(int i=0;i<corridors.Length;i++)if(corridors[i]!=null)
                {
                    wrapRotations[c,i]=new PhysicalApronSeamProjectionDraft(map,body,point,point,corridors[c],corridors[c],corridors[i],definition.ValidationPolicy,true);
                    wrapPoseRotations[c,i]=new PhysicalApronSeamProjectionDraft(map,body,point,point,corridors[c],corridors[c],corridors[i],definition.ValidationPolicy,true,true);
                }
            }
            applicable=new bool[junctions.Count];penaltyRegions=new bool[junctions.Count];
            singleIncidentLoop=new bool[junctions.Count,corridors.Length];
            capturedBranchJunction=new bool[junctions.Count];
            for(int j=0;j<junctions.Count;j++)
            {
                var incident=new HashSet<string>(StringComparer.Ordinal);bool complete=junctions.Get(j).PortCount>=2;
                for(int p=0;p<junctions.Get(j).PortCount;p++)
                {
                    string loop=junctions.Get(j).GetPort(p).Anchor.LoopId;
                    complete&=loops.Contains(loop)&&incident.Add(loop);
                }
                capturedBranchJunction[j]=complete;
            }
            for(int j=0;j<junctions.Count;j++)for(int c=0;c<corridors.Length;c++)if(corridors[c]!=null)
            {
                int ports=0;
                for(int p=0;p<junctions.Get(j).PortCount;p++)
                    if(junctions.Get(j).GetPort(p).Anchor.LoopId==track.GetLoop(c).Id)ports++;
                singleIncidentLoop[j,c]=ports==1;
            }
            for(int i=0;i<definition.RegionCount;i++)applicable[definition.GetRegion(i).JunctionIndex]=true;
            for(int i=0;i<definition.PenaltyWorldMergeCount;i++)for(int j=0;j<junctions.Count;j++)
                if(junctions.Get(j).Id==definition.GetPenaltyWorldMerge(i))applicable[j]=penaltyRegions[j]=true;
            radius=Math.Sqrt(Math.Pow(body.LongitudinalSeparation*.5,2)+Math.Pow(body.LateralSeparation*.5,2));
            margin=definition.ValidationPolicy.SeamTolerance;
            for(int i=0;i<corridors.Length;i++)for(int j=0;j<=i;j++)if(projections[i,j]!=null)
                margin=Math.Max(margin,projections[i,j].ProjectionMarginMeters);
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{definition.ToJson(),track.ToJson(),visual.ToJson(),route.PlanSnapshotJson,route.RequirementsSnapshotJson,
                connections.ToJson(),body.ToJson(),definition.ValidationPolicy.ToJson(),
                "named_range_penalty_world_bodies_v4;paid_route_loops_and_range_paths;full_paid_receipts;ordinary_ski_rectangle;all_body_headings_on_mat_and_mat_tails_only;whole_interval_and_knots;unsplit_wrap_exclusive;incoming_tail_exact_shortest_rotation;periodic_seam_rotation;single_incident_same_loop_junction_requires_complete_world_body_proof;captured_single_port_per_loop_branches_require_complete_world_body_proof;self_crossing_ports_exclusive;same_loop_occupancy_retained;ibu7_1_4_penalty_over_entering"});
            if(capturedStartLoop!=null)ContextJson=ShootingModelDefinitions.AppendContext(ContextJson,"captured_start_and_all_paid_corridors_world_union_v5",capturedStartLoop);
        }
        public bool Applies(int junction)=>applicable[junction];
        public bool IsPenaltyRegion(int junction)=>penaltyRegions[junction];
        public bool CanShareCapturedBranch(int junction)=>capturedBranchJunction[junction];
        public bool IsPaidRoutePoint(PhysicalTrackPoint point)=>Contains(point)&&paidRouteLoops[point.LoopIndex];
        public bool CanShareSingleLoopIncidentPort(int junction,PhysicalTrackPoint point)
            =>Contains(point)&&singleIncidentLoop[junction,point.LoopIndex];
        public bool Contains(PhysicalTrackPoint point)
        {
            try{map.Validate(point);}catch(ArgumentException){return false;}
            return corridors[point.LoopIndex]!=null;
        }
        /// <summary>Read-only centre observation in the self body's local forward/
        /// right frame. Different route stations are never compared across branches.
        /// This observation grants no movement or collision clearance.</summary>
        public bool TryObserveRelativeBody(PhysicalTrackPoint self,double selfLine,
            PhysicalTrackPoint other,double otherLine,out double forward,out double right)
        {
            forward=right=0;
            PhysiologyNumbers.RequireRange(selfLine,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(selfLine));
            PhysiologyNumbers.RequireRange(otherLine,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(otherLine));
            if(!Contains(self)||!Contains(other))return false;
            var a=visual.Get(self.LoopId).Sample(self.LocalNanometers/(double)SkiState.NanometersPerMeter);
            var b=visual.Get(other.LoopId).Sample(other.LocalNanometers/(double)SkiState.NanometersPerMeter);
            double dx=b.X+b.ForwardZ*otherLine-a.X-a.ForwardZ*selfLine;
            double dz=b.Z-b.ForwardX*otherLine-a.Z+a.ForwardX*selfLine;
            forward=PhysiologyNumbers.Quantize(dx*a.ForwardX+dz*a.ForwardZ);
            right=PhysiologyNumbers.Quantize(dx*a.ForwardZ-dz*a.ForwardX);
            return true;
        }
        private bool Covered(PhysicalOccupancySweep s)
        {
            try{map.Validate(s.Start);}catch(ArgumentException){return false;}
            int c=s.Start.LoopIndex;
            if(!s.AthleteId.IsValid||corridors[c]==null||s.TravelNanometers>s.Start.LoopLengthNanometers-s.Start.LocalNanometers)return false;
            // The accepting field separately validates the entire swept width envelope.
            // Reject impossible point offsets as well; this API grants no body-width exemption.
            double width=track.GetLoop(c).GetSegment(0).WidthMeters;
            for(int i=1;i<track.GetLoop(c).SegmentCount;i++)width=Math.Min(width,track.GetLoop(c).GetSegment(i).WidthMeters);
            double extent=width*.5-body.EdgeMargin;
            return Math.Abs(s.LateralBeforeMeters)<=extent&&Math.Abs(s.LateralAfterMeters)<=extent;
        }
        public bool Conflicts(PhysicalOccupancySweep a,PhysicalOccupancySweep b,bool allHeadingsA=false,bool allHeadingsB=false)
            =>a.AthleteId.Equals(b.AthleteId)||!Covered(a)||!Covered(b)||(allHeadingsA||allHeadingsB?poseProjections:projections)[a.Start.LoopIndex,b.Start.LoopIndex].Conflicts(a,b)||
                WrapMayContact(a,b,allHeadingsA||allHeadingsB)||WrapMayContact(b,a,allHeadingsA||allHeadingsB);
        private bool WrapMayContact(PhysicalOccupancySweep source,PhysicalOccupancySweep other,bool pose)
        {
            if(!source.Start.Closed)return false;
            long half=(long)Math.Ceiling(body.LongitudinalSeparation*.5*SkiState.NanometersPerMeter);
            if(source.Start.LocalNanometers>half&&source.Start.LocalNanometers+source.TravelNanometers<source.Start.LoopLengthNanometers-half)return false;
            var rotation=(pose?wrapPoseRotations:wrapRotations)[source.Start.LoopIndex,other.Start.LoopIndex];
            return rotation==null||rotation.Conflicts(source.LateralBeforeMeters,source.LateralAfterMeters,other);
        }
        public bool TailConflicts(PhysicalTrackPoint point,double lateral,PhysicalOccupancySweep other,int connection,bool pose=false)
        {
            try{map.Validate(point);}catch(ArgumentException){return true;}
            if(corridors[point.LoopIndex]==null||!Covered(other))return true;
            if(connection<0||connection>=seamRotations.GetLength(0))return true;
            var rotation=(pose?seamPoseRotations:seamRotations)[connection,other.Start.LoopIndex];
            return rotation==null||rotation.Conflicts(lateral,lateral,other);
        }
        public bool TailPairConflicts(PhysicalTrackPoint a,double al,int ac,PhysicalTrackPoint b,double bl,int bc,bool pose=false)
        {
            try{map.Validate(a);map.Validate(b);}catch(ArgumentException){return true;}
            if(corridors[a.LoopIndex]==null||corridors[b.LoopIndex]==null)return true;
            if(ac<0||bc<0||ac>=seamRotations.GetLength(0)||bc>=seamRotations.GetLength(0))return true;
            var cache=pose?seamPoseRotations:seamRotations;
            var first=cache[ac,representativeCorridor];var second=cache[bc,representativeCorridor];
            return first==null||second==null||first.Conflicts(al,al,second,bl,bl);
        }
    }
}

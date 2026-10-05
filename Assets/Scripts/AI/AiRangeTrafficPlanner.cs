using System;
using Biathlon.Domain;
using Biathlon.Simulation;
namespace Biathlon.AI
{
    public readonly struct AiRangeTrafficPlan
    {
        public bool Active { get; }
        public bool AlignmentBlocked { get; }
        public double DesiredLateralMeters { get; }
        public int? RequestedEffortCap { get; }
        internal AiRangeTrafficPlan(bool active,bool blocked,double line,int? effortCap=null)
        {Active=active;AlignmentBlocked=blocked;DesiredLateralMeters=line;RequestedEffortCap=effortCap;}
    }
    /// <summary>Observe prior-tick physical bodies before requesting a paid inward move.
    /// A blocked convergence retains the current line so parallel bodies can separate forward.
    /// This planner grants no movement, admission, reservation or capability modifier.</summary>
    public sealed class AiRangeTrafficPlanner
    {
        private readonly AiRangeTrafficPolicyDefinition definition;
        private readonly OccupancySystem occupancy;
        private readonly long lead;
        public AiRangeTrafficPlanner(AiRangeTrafficPolicyDefinition definition,OccupancyDefinition body)
        {
            this.definition=definition??throw new ArgumentNullException(nameof(definition));
            occupancy=new OccupancySystem(body);lead=TrackDistance.FromMeters(definition.AlignmentLeadMeters).Millimeters*SkiState.NanometersPerMillimeter;
        }
        public AiRangeTrafficPlan Evaluate(bool hasPhysicalPath,long? distanceToRangeEntry,RangeTraversalPhase? traversal,
            SkiPlacement self,TrackSample track,ReadOnlySpan<SkiPlacement> observed,bool onEntryAisle=false,
            bool onSharedExitCorridor=false,bool leavingPenalty=false)
        {
            bool active=definition.AlignPhysicalPaths&&hasPhysicalPath&&
                (distanceToRangeEntry.HasValue&&distanceToRangeEntry.Value<=lead||
                traversal==RangeTraversalPhase.Shooting||traversal==RangeTraversalPhase.RangeExit||
                traversal==RangeTraversalPhase.PenaltyApproach||traversal==RangeTraversalPhase.Rejoin||
                traversal==RangeTraversalPhase.PenaltyLoop&&definition.PenaltyLateralMeters.HasValue);
            if(!active)return new AiRangeTrafficPlan(false,false,self.LateralMeters);
            double desired=onEntryAisle?definition.EntryLateralMeters??0:0;
            if(traversal==RangeTraversalPhase.PenaltyLoop||traversal==RangeTraversalPhase.Rejoin&&leavingPenalty)
                desired=definition.PenaltyLateralMeters??0;
            else if(onSharedExitCorridor&&(traversal==RangeTraversalPhase.RangeExit||traversal==RangeTraversalPhase.PenaltyApproach||traversal==RangeTraversalPhase.Rejoin))
                desired=definition.ExitLateralMeters??0;
            desired=Math.Max(-track.WidthMeters*.5+occupancy.Definition.EdgeMargin,Math.Min(track.WidthMeters*.5-occupancy.Definition.EdgeMargin,desired));
            var destination=new SkiPlacement(self.AthleteId,self.CorridorId,self.Motion,desired);
            var query=occupancy.QueryMove(self,destination,track.WidthMeters,observed);
            bool blocked=query.Blocked;bool yield=false;
            if(blocked&&query.BlockerId.IsValid)
                for(int i=0;i<observed.Length;i++)if(observed[i].AthleteId.Equals(query.BlockerId))
                {
                    var other=observed[i];
                    // Only the trailing actor yields. Two parallel actors must not
                    // both remove drive while each waits for the other to advance.
                    yield=other.Motion.DistanceNanometers>self.Motion.DistanceNanometers||
                        other.Motion.DistanceNanometers==self.Motion.DistanceNanometers&&
                        string.CompareOrdinal(other.AthleteId.Value,self.AthleteId.Value)<0;
                    break;
                }
            return new AiRangeTrafficPlan(true,blocked,blocked?self.LateralMeters:desired,
                yield?definition.BlockedAlignmentEffort:null);
        }
    }
}

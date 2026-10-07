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
        private readonly long forkNeighborGap;
        public AiRangeTrafficPlanner(AiRangeTrafficPolicyDefinition definition,OccupancyDefinition body)
        {
            this.definition=definition??throw new ArgumentNullException(nameof(definition));
            occupancy=new OccupancySystem(body);lead=TrackDistance.FromMeters(definition.AlignmentLeadMeters).Millimeters*SkiState.NanometersPerMillimeter;
            forkNeighborGap=definition.ForkYieldNeighborGapMeters.HasValue?
                (long)Math.Round(definition.ForkYieldNeighborGapMeters.Value*SkiState.NanometersPerMeter):0;
        }
        /// <summary>An adjacent outer-line athlete must take an earlier private turn.
        /// Ease propulsion before the turn while both bodies are still on the common approach;
        /// the field remains responsible for every physical admission.</summary>
        public bool ShouldYieldAtFork(long ownFork,long otherFork,long ownRoute,long otherRoute,
            double ownLine,double otherLine)
            =>definition.ForkYieldEffort.HasValue&&ownRoute<ownFork&&otherRoute<otherFork&&
              otherFork<ownFork&&Math.Abs(otherRoute-ownRoute)<=forkNeighborGap&&
              (otherLine-ownLine)*definition.ForkYieldOuterSideSign.Value>definition.ForkYieldLateralGapMeters.Value;
        /// <summary>After the field proves a world-body conflict on the shared approach,
        /// retain the occupied line for this tick. The normal field step still decides
        /// whether the athlete may advance; this never grants passage or reduces effort.</summary>
        public static bool ShouldRetainPhysicalApproachLine(bool active,
            RangeTraversalPhase? traversal,bool onEntryRoute,bool worldBodyConflict,
            double currentLine,double requestedLine)
            =>active&&traversal==RangeTraversalPhase.Approach&&onEntryRoute&&
              worldBodyConflict&&requestedLine!=currentLine;
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
            // A shared departure has no assigned skiing lane. Keep each body's
            // current line; the traffic planner may choose a paid bypass when
            // required. Converging everyone onto one offset creates a queue.
            if(definition.RetainSharedExitLine&&onSharedExitCorridor&&
                (traversal==RangeTraversalPhase.RangeExit||traversal==RangeTraversalPhase.PenaltyApproach||traversal==RangeTraversalPhase.Rejoin))
                return new AiRangeTrafficPlan(true,false,self.LateralMeters);
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

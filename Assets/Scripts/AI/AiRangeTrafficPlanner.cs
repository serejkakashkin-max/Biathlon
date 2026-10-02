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
        internal AiRangeTrafficPlan(bool active,bool blocked,double line)
        {Active=active;AlignmentBlocked=blocked;DesiredLateralMeters=line;}
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
            SkiPlacement self,TrackSample track,ReadOnlySpan<SkiPlacement> observed)
        {
            bool active=definition.AlignPhysicalPaths&&hasPhysicalPath&&
                (distanceToRangeEntry.HasValue&&distanceToRangeEntry.Value<=lead||
                traversal==RangeTraversalPhase.Shooting||traversal==RangeTraversalPhase.RangeExit||
                traversal==RangeTraversalPhase.PenaltyApproach||traversal==RangeTraversalPhase.Rejoin);
            if(!active)return new AiRangeTrafficPlan(false,false,self.LateralMeters);
            var destination=new SkiPlacement(self.AthleteId,self.CorridorId,self.Motion,0);
            bool blocked=occupancy.QueryMove(self,destination,track.WidthMeters,observed).Blocked;
            return new AiRangeTrafficPlan(true,blocked,blocked?self.LateralMeters:0);
        }
    }
}

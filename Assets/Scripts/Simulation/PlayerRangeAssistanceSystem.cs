using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public enum PlayerRangeAssistMode { Manual, FinalApproach, InitialDeparture }
    public readonly struct PlayerRangeAssistPlan
    {
        public PlayerRangeAssistMode Mode {get;}
        public bool Active=>Mode!=PlayerRangeAssistMode.Manual;
        public int RequestedEffort {get;}
        public double RequestedLateral {get;}
        internal PlayerRangeAssistPlan(PlayerRangeAssistMode mode,int effort,double lateral)
        {Mode=mode;RequestedEffort=effort;RequestedLateral=lateral;}
    }
    /// <summary>Explicit short intent assistance only. It never advances a route, reserves a
    /// mat, changes speed, or overrides shared-body admission. Actual commands are replayable.</summary>
    public sealed class PlayerRangeAssistanceSystem
    {
        private readonly RangeGuidanceDefinition definition;
        private bool captured;
        private int capturedBout=-1;
        public string ContextJson=>definition.ToJson();
        public PlayerRangeAssistanceSystem(RangeGuidanceDefinition definition)
        {this.definition=definition??throw new ArgumentNullException(nameof(definition));}
        public PlayerRangeAssistPlan EvaluateManual(RangeTraversalPhase? phase,int bout,bool onArrow,bool departureTurnComplete,
            int effort,double lateral,double speed)
        {
            if(bout!=capturedBout){captured=false;capturedBout=bout;}
            if(phase==RangeTraversalPhase.Approach&&onArrow)captured=true;
            if(phase==RangeTraversalPhase.Approach&&captured)
                return new PlayerRangeAssistPlan(PlayerRangeAssistMode.FinalApproach,definition.ApproachEffort,0);
            if(phase==RangeTraversalPhase.RangeExit&&!departureTurnComplete)
                return new PlayerRangeAssistPlan(PlayerRangeAssistMode.InitialDeparture,definition.DepartureEffort,0);
            if(phase!=RangeTraversalPhase.Shooting&&phase!=RangeTraversalPhase.Approach)captured=false;
            return new PlayerRangeAssistPlan(PlayerRangeAssistMode.Manual,effort,lateral);
        }
        public PlayerRangeAssistPlan Evaluate(RangeTraversalPhase? phase,bool onAssignedPrivateMat,
            long distanceToFiringNanometers,int manualEffort,double manualLateral,double speed)
        {
            AthleteState.ValidateEffort(manualEffort);PhysiologyNumbers.RequireRange(speed,0,PhysiologyNumbers.MaximumMagnitude,"speed");
            PhysiologyNumbers.RequireRange(manualLateral,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,"line");
            double distance=distanceToFiringNanometers/(double)SkiState.NanometersPerMeter;
            if(onAssignedPrivateMat&&phase==RangeTraversalPhase.Approach&&distance>0&&distance<=definition.MatAssistLead)
                return new PlayerRangeAssistPlan(PlayerRangeAssistMode.FinalApproach,speed>definition.ApproachCoastAbove?0:definition.ApproachEffort,0);
            if(onAssignedPrivateMat&&phase==RangeTraversalPhase.RangeExit&&distance<=0&&distance>-definition.DepartureAssist)
                return new PlayerRangeAssistPlan(PlayerRangeAssistMode.InitialDeparture,definition.DepartureEffort,0);
            return new PlayerRangeAssistPlan(PlayerRangeAssistMode.Manual,manualEffort,manualLateral);
        }
    }
    public static class RaceRouteGuidance
    {
        /// <summary>Read-only next physical branch on the exact currently selected path.
        /// Terrain knots are skipped; format shortcuts and private mat forks are included.</summary>
        public static long NextBranch(PhysicalRangePathMap path,RaceRoute route,long at,long lookahead)
        {
            if(path==null||route==null||lookahead<0)throw new ArgumentException("Exact path/route and nonnegative preview required.");
            long end=Math.Min(checked(route.Length.Millimeters*SkiState.NanometersPerMillimeter),checked(at+lookahead));
            path.MapRoute(at);
            while(at<end)
            {
                long delta=path.DistanceToNextBoundary(at);if(delta<=0)return -1;
                long next=checked(at+delta);if(next>end)return -1;
                if(path.MapRoute(next,PhysicalBoundarySide.Incoming).LoopId!=path.MapRoute(next).LoopId)return next;
                at=next;
            }
            return -1;
        }
    }
}

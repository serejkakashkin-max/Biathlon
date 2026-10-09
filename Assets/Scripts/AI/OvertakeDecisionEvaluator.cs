using System;
using Biathlon.Domain;
using Biathlon.Simulation;
namespace Biathlon.AI
{
    public enum OvertakeDecisionReason { Ready=0, Terrain=1, Energy=2, RaceValue=3, NoTarget=4, Space=5, ClosingSpeed=6, Duration=7, PathEnds=8, StartGridHold=9, RangeTrafficHold=10, WorldHeadingPlanner=11 }
    public readonly struct OvertakeDecisionFrame
    {
        public bool Request => Reason == OvertakeDecisionReason.Ready;
        public OvertakeDecisionReason Reason { get; }
        public OvertakeOpportunity Opportunity { get; }
        public double EstimatedPassSeconds { get; }
        internal OvertakeDecisionFrame(OvertakeDecisionReason reason,OvertakeOpportunity opportunity=default,double seconds=0)
        { Reason=reason; Opportunity=opportunity; EstimatedPassSeconds=seconds; }
    }
    public sealed class OvertakeDecisionEvaluator
    {
        private readonly OvertakeDecisionDefinition definition;
        private readonly OvertakeDefinition movement;
        private readonly OccupancySystem occupancy;
        public OvertakeDecisionEvaluator(OvertakeDecisionDefinition definition,OvertakeDefinition movement,OccupancySystem occupancy)
        { this.definition=definition??throw new ArgumentNullException(nameof(definition)); this.movement=movement??throw new ArgumentNullException(nameof(movement)); this.occupancy=occupancy??throw new ArgumentNullException(nameof(occupancy)); }
        public OvertakeDecisionFrame Evaluate(SkiPlacement self,ReadOnlySpan<SkiPlacement> roster,PhysiologyState physiology,double raceValue,
            double minimumWidthMeters,bool overtakeAllowed,double maximumAbsGrade,double maximumAbsCurvature,TrackDistance clearPathDistance)
        {
            if(!self.IsValid || physiology.HeartRateBpm<=0) throw new ArgumentException("Valid physical state required.");
            PhysiologyNumbers.RequireRange(raceValue,0,1,nameof(raceValue));
            PhysiologyNumbers.RequireRange(maximumAbsGrade,0,1000000,nameof(maximumAbsGrade));
            PhysiologyNumbers.RequireRange(maximumAbsCurvature,0,1000000,nameof(maximumAbsCurvature));
            occupancy.ValidateRoster(roster);
            // Share geometry even for a declined tactical request; malformed path input is never hidden by an early policy return.
            var opportunity=OvertakeOpportunityQuery.Evaluate(movement,occupancy,self,minimumWidthMeters,overtakeAllowed,roster);
            if(!overtakeAllowed || maximumAbsGrade>definition.MaximumAbsGrade || maximumAbsCurvature>definition.MaximumAbsCurvature)
                return new OvertakeDecisionFrame(OvertakeDecisionReason.Terrain,opportunity);
            if(physiology.AnaerobicReserve<definition.MinimumReserve || physiology.MuscularFatigue>definition.MaximumFatigue)
                return new OvertakeDecisionFrame(OvertakeDecisionReason.Energy,opportunity);
            if(raceValue<definition.MinimumRaceValue) return new OvertakeDecisionFrame(OvertakeDecisionReason.RaceValue,opportunity);
            if(!opportunity.Target.IsValid) return new OvertakeDecisionFrame(OvertakeDecisionReason.NoTarget);
            if(!opportunity.Available) return new OvertakeDecisionFrame(OvertakeDecisionReason.Space,opportunity);
            double closing=self.Motion.SpeedMetersPerSecond-opportunity.Target.Motion.SpeedMetersPerSecond;
            if(closing<definition.MinimumClosingSpeed) return new OvertakeDecisionFrame(OvertakeDecisionReason.ClosingSpeed,opportunity);
            double gap=(opportunity.Target.Motion.DistanceNanometers-self.Motion.DistanceNanometers)/(double)SkiState.NanometersPerMeter;
            double lateralSeconds=2*Math.Abs(opportunity.PassingLaneMeters-self.LateralMeters)/movement.LateralSpeed;
            double seconds=lateralSeconds+(gap+opportunity.ClearanceDistanceNanometers/(double)SkiState.NanometersPerMeter)/closing;
            if(seconds>Math.Min(definition.MaximumPassSeconds,movement.TimeoutSeconds)) return new OvertakeDecisionFrame(OvertakeDecisionReason.Duration,opportunity,seconds);
            if(seconds*self.Motion.SpeedMetersPerSecond>clearPathDistance.Meters) return new OvertakeDecisionFrame(OvertakeDecisionReason.PathEnds,opportunity,seconds);
            return new OvertakeDecisionFrame(OvertakeDecisionReason.Ready,opportunity,seconds);
        }
    }
}

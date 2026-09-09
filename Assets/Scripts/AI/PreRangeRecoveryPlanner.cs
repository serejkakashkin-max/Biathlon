using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public enum PreRangeRecoveryReason { NotApproaching, OutsideWindow, HeartRateLow, Recover }
    public readonly struct PreRangeRecoveryPlan
    {
        public PreRangeRecoveryReason Reason { get; }
        public bool Requested => Reason == PreRangeRecoveryReason.Recover;
        public int EffortReduction { get; }
        public double Urgency { get; }
        internal PreRangeRecoveryPlan(PreRangeRecoveryReason reason, int reduction = 0, double urgency = 0)
        { Reason = reason; EffortReduction = reduction; Urgency = urgency; }
    }
    /// <summary>Pure cadenced request; never changes physiology or shooting outcomes directly.
    /// Subtract the reduction from the otherwise planned pace, then use normal control response.</summary>
    public sealed class PreRangeRecoveryPlanner
    {
        private readonly PreRangeRecoveryDefinition definition;
        public PreRangeRecoveryPlanner(PreRangeRecoveryDefinition definition)
        { this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public PreRangeRecoveryPlan Evaluate(AiSprintPerception observation) => Evaluate(
            observation.Phase == CompetitionPhase.Skiing, observation.NextRangeEntryRouteDistanceNanometers, observation.Physiology.HeartRateBpm);
        public PreRangeRecoveryPlan Evaluate(bool skiing, long? rangeDistanceNanometers, double heartRate)
        {
            PhysiologyNumbers.RequireRange(heartRate, 1, 300, nameof(heartRate));
            if (rangeDistanceNanometers < 0) throw new ArgumentOutOfRangeException(nameof(rangeDistanceNanometers));
            if (!skiing || !rangeDistanceNanometers.HasValue) return new PreRangeRecoveryPlan(PreRangeRecoveryReason.NotApproaching);
            double distance = rangeDistanceNanometers.Value / 1e9;
            if (distance >= definition.ApproachMeters) return new PreRangeRecoveryPlan(PreRangeRecoveryReason.OutsideWindow);
            if (heartRate <= definition.StartHeartRate) return new PreRangeRecoveryPlan(PreRangeRecoveryReason.HeartRateLow);
            double urgency = PhysiologyNumbers.Quantize((1 - distance / definition.ApproachMeters) *
                Math.Min(1, (heartRate - definition.StartHeartRate) / (definition.FullHeartRate - definition.StartHeartRate)));
            int reduction = (int)Math.Round(definition.MaximumReduction * urgency, MidpointRounding.ToEven);
            return new PreRangeRecoveryPlan(reduction > 0 ? PreRangeRecoveryReason.Recover : PreRangeRecoveryReason.HeartRateLow, reduction, urgency);
        }
    }
}

using System;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public readonly struct ShootingStrategyPlan
    {
        public string DefinitionId { get; }
        public double Risk { get; }
        public double FireTolerance { get; }
        public double StableSeconds { get; }
        public double MaximumAimSeconds { get; }
        internal ShootingStrategyPlan(ShootingStrategyDefinition d,double risk)
        {
            DefinitionId=d.Id; Risk=risk;
            FireTolerance=PhysiologyNumbers.Quantize(d.CautiousTolerance+(d.FastTolerance-d.CautiousTolerance)*risk);
            StableSeconds=PhysiologyNumbers.Quantize(d.CautiousStable+(d.FastStable-d.CautiousStable)*risk);
            MaximumAimSeconds=PhysiologyNumbers.Quantize(d.CautiousMaximumAim+(d.FastMaximumAim-d.CautiousMaximumAim)*risk);
        }
        internal void Validate()
        { if(string.IsNullOrEmpty(DefinitionId) || FireTolerance<=0 || StableSeconds<=0 || MaximumAimSeconds<StableSeconds) throw new ArgumentException("Valid shooting strategy required."); }
    }
    /// <summary>Decision thresholds only. Time urgency accepts risk; stability need moderates it.
    /// Call at tactical cadence and hold the returned plan until the next decision.</summary>
    public sealed class ShootingStrategyPlanner
    {
        private readonly ShootingStrategyDefinition definition;
        public ShootingStrategyPlanner(ShootingStrategyDefinition definition)
        { this.definition=definition??throw new ArgumentNullException(nameof(definition)); }
        public ShootingStrategyPlan Evaluate(double timeUrgency,double stabilityNeed)
        {
            PhysiologyNumbers.RequireRange(timeUrgency,0,1,nameof(timeUrgency));
            PhysiologyNumbers.RequireRange(stabilityNeed,0,1,nameof(stabilityNeed));
            return new ShootingStrategyPlan(definition,PhysiologyNumbers.Quantize(timeUrgency*(1-definition.StabilityWeight*stabilityNeed)));
        }
    }
}

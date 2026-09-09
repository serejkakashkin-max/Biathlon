using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SkiEquipmentOutput
    {
        public string SkiId { get; }
        public string SnowProfileId { get; }
        public double ConditionAffinity { get; }
        public double SnowResistanceMultiplier { get; }
        public double GripMultiplier { get; }
        public double StabilityMultiplier { get; }
        public double CorneringGripMultiplier => GripMultiplier * StabilityMultiplier;
        internal SkiEquipmentOutput(SkiEquipmentDefinition ski, SnowProfile snow)
        {
            SkiId = ski.Id; SnowProfileId = snow.Id; ConditionAffinity = ski.AffinityFor(snow.Id);
            SnowResistanceMultiplier = snow.ResistanceMultiplier / (ski.GlideMultiplier * ConditionAffinity);
            GripMultiplier = snow.GripMultiplier * ski.GripMultiplier; StabilityMultiplier = ski.StabilityMultiplier;
        }
    }
    public sealed class SkiEquipmentSystem
    {
        private readonly SkiEquipmentDefinition ski;
        private readonly SnowCatalog snowCatalog;
        public SkiEquipmentSystem(SkiEquipmentDefinition ski, SnowCatalog snowCatalog)
        {
            this.ski = ski ?? throw new ArgumentNullException(nameof(ski));
            this.snowCatalog = snowCatalog ?? throw new ArgumentNullException(nameof(snowCatalog));
            for (int i = 0; i < ski.AffinityCount; i++) snowCatalog.GetProfile(ski.GetAffinity(i).SnowProfileId);
        }
        public SkiEquipmentOutput Evaluate(string snowProfileId)
        {
            var result = new SkiEquipmentOutput(ski, snowCatalog.GetProfile(snowProfileId));
            PhysiologyNumbers.RequireRange(result.SnowResistanceMultiplier, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(snowProfileId));
            PhysiologyNumbers.RequireRange(result.CorneringGripMultiplier, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(snowProfileId));
            return result;
        }
    }
}

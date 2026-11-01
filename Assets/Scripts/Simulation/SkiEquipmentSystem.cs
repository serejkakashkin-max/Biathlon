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
        internal SkiEquipmentOutput(SkiEquipmentOutput a,SkiEquipmentOutput b,double blend)
        {
            SkiId=a.SkiId;SnowProfileId=blend<.5?a.SnowProfileId:b.SnowProfileId;
            ConditionAffinity=a.ConditionAffinity+(b.ConditionAffinity-a.ConditionAffinity)*blend;
            SnowResistanceMultiplier=a.SnowResistanceMultiplier+(b.SnowResistanceMultiplier-a.SnowResistanceMultiplier)*blend;
            GripMultiplier=a.GripMultiplier+(b.GripMultiplier-a.GripMultiplier)*blend;StabilityMultiplier=a.StabilityMultiplier;
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
        public SkiEquipmentOutput Evaluate(RaceWeatherFrame weather)
        {
            var a=Evaluate(weather.SnowFrom);if(weather.SnowBlend==0||weather.SnowFrom==weather.SnowTo)return a;
            var b=Evaluate(weather.SnowTo);return weather.SnowBlend==1?b:new SkiEquipmentOutput(a,b,weather.SnowBlend);
        }
    }
}

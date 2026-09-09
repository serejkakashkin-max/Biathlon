using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SkiEquipmentTests
    {
        public static SkiEquipmentCatalog Catalog() => SkiEquipmentCatalog.FromJson(PhysiologyContractTests.DefinitionJson("ski_equipment_provisional_v1.json"));
        [Test]
        public void NeutralPreservesSnowAndSpecialistsHaveLateralTradeoffs()
        {
            var snow = SnowConditionTests.Catalog(); var skis = Catalog();
            var neutral = new SkiEquipmentSystem(skis.GetSki("ski_neutral"), snow).Evaluate("fresh_soft");
            Assert.That(neutral.SnowResistanceMultiplier, Is.EqualTo(snow.GetProfile("fresh_soft").ResistanceMultiplier));
            Assert.That(neutral.CorneringGripMultiplier, Is.EqualTo(snow.GetProfile("fresh_soft").GripMultiplier));
            var fast = new SkiEquipmentSystem(skis.GetSki("ski_glide"), snow).Evaluate("packed_neutral");
            Assert.That(fast.SnowResistanceMultiplier, Is.LessThan(1));
            Assert.That(fast.CorneringGripMultiplier, Is.LessThan(1));
            var specialist = new SkiEquipmentSystem(skis.GetSki("ski_soft_specialist"), snow);
            Assert.That(specialist.Evaluate("fresh_soft").ConditionAffinity, Is.GreaterThan(1));
            Assert.That(specialist.Evaluate("icy_fast").ConditionAffinity, Is.LessThan(1));
            Assert.That(specialist.Evaluate("wet_slow").ConditionAffinity, Is.EqualTo(1));
        }
        [Test]
        public void EquipmentBenefitIsSmallerThanBroadTechniqueSkillGap()
        {
            var snow = SnowConditionTests.Catalog(); var skis = Catalog();
            var fast = new SkiEquipmentSystem(skis.GetSki("ski_glide"), snow).Evaluate("packed_neutral");
            var neutral = new SkiEquipmentSystem(skis.GetSki("ski_neutral"), snow).Evaluate("packed_neutral");
            var sample = new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(default);
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20);
            var technique = new SkiTechniqueSystem(SkiTechniqueTests.Definition());
            double novicePower = technique.Evaluate(new SkiTechniqueStats(0, 0), sample, 0, 350).PropulsivePowerWatts;
            double expertPower = technique.Evaluate(new SkiTechniqueStats(100, 100), sample, 0, 350).PropulsivePowerWatts;
            var baseState = default(SkiState); var fastState = default(SkiState); var expertState = default(SkiState);
            for (int i = 0; i < 3600; i++)
            {
                baseState = forces.Step(baseState, novicePower, sample, snowResistanceMultiplier: neutral.SnowResistanceMultiplier).State;
                fastState = forces.Step(fastState, novicePower, sample, snowResistanceMultiplier: fast.SnowResistanceMultiplier).State;
                expertState = forces.Step(expertState, expertPower, sample, snowResistanceMultiplier: neutral.SnowResistanceMultiplier).State;
            }
            Assert.That(fastState.DistanceNanometers, Is.GreaterThan(baseState.DistanceNanometers));
            Assert.That(expertState.DistanceNanometers, Is.GreaterThan(fastState.DistanceNanometers));
            Assert.That(fastState.DistanceMeters - baseState.DistanceMeters, Is.LessThan(expertState.DistanceMeters - baseState.DistanceMeters));
        }
        [Test]
        public void CatalogAndCombinedEffectsEnforceModestEnvelopeAndKnownIds()
        {
            string json = Catalog().ToJson();
            Assert.That(SkiEquipmentCatalog.FromJson(json).ToJson(), Is.EqualTo(json));
            Assert.Catch(() => SkiEquipmentCatalog.FromJson(json.Replace("ski_glide", "ski_neutral")));
            Assert.Catch(() => SkiEquipmentCatalog.FromJson(PhysiologyContractTests.ReplaceNumber(json, "glide_multiplier", "1.5")));
            Assert.Catch(() => SkiEquipmentCatalog.FromJson(PhysiologyContractTests.ReplaceNumber(json, "stability_multiplier", "1.08")), "Grip and stability cannot stack beyond the catalog envelope.");
            Assert.Catch(() => SkiEquipmentCatalog.FromJson(json.Replace("condition_affinities", "missing_affinities")));
            var invalid = SkiEquipmentCatalog.FromJson(json.Replace("fresh_soft", "unknown_snow"));
            Assert.Catch(() => new SkiEquipmentSystem(invalid.GetSki("ski_soft_specialist"), SnowConditionTests.Catalog()));
            Assert.Catch(() => Catalog().GetSki("unknown"));
            Assert.Catch(() => new SkiEquipmentSystem(Catalog().GetSki("ski_neutral"), SnowConditionTests.Catalog()).Evaluate("unknown"));
        }
        [Test]
        public void EquipmentAndSnowCompositionDoesNotAllocate()
        {
            var system = new SkiEquipmentSystem(Catalog().GetSki("ski_soft_specialist"), SnowConditionTests.Catalog());
            system.Evaluate("fresh_soft"); double sum = 0;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += system.Evaluate("fresh_soft").SnowResistanceMultiplier;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(sum, Is.GreaterThan(10000));
        }
    }
}

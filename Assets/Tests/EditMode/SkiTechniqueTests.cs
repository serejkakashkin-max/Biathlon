using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SkiTechniqueTests
    {
        public static SkiTechniqueDefinition Definition() => SkiTechniqueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_technique_provisional_v1.json"));
        private static TrackSample Sample(string id) => new TrackSampler(SyntheticTrackTests.Generated(id).GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
        [Test]
        public void AutomaticAndExplicitTechniquesRespectTerrainAndStableIds()
        {
            var system = new SkiTechniqueSystem(Definition());
            var stats = new SkiTechniqueStats(100, 100);
            var flat = Sample("synthetic_flat_v1");
            var climb = Sample("synthetic_climb_v1");
            Assert.That(system.Evaluate(stats, flat, 0, 350).TechniqueId, Is.EqualTo("skate_cruise"));
            var best = system.Evaluate(stats, climb, 0, 350);
            Assert.That(best.TechniqueId, Is.EqualTo("skate_climb"));
            Assert.That(best.PropulsivePowerWatts, Is.GreaterThan(system.Evaluate(stats, climb, 0, 350, "skate_cruise").PropulsivePowerWatts));
            Assert.That(system.Evaluate(stats, climb, 0, 0).TechniqueId, Is.EqualTo(best.TechniqueId));
            Assert.Catch(() => system.Evaluate(stats, flat, 0, 350, "unknown"));
            var tied = SkiTechniqueDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "flat_affinity", "1"));
            Assert.That(new SkiTechniqueSystem(tied).Evaluate(stats, flat, 0, 350).TechniqueId, Is.EqualTo(tied.GetProfile(0).Id));
        }
        [Test]
        public void SkillClimbingAndFatigueHaveObservableBoundedEffects()
        {
            var system = new SkiTechniqueSystem(Definition());
            var flat = Sample("synthetic_flat_v1");
            var climb = Sample("synthetic_climb_v1");
            var low = new SkiTechniqueStats(0, 0);
            var high = new SkiTechniqueStats(100, 100);
            Assert.That(system.Evaluate(high, flat, 0, 350).Efficiency, Is.EqualTo(1));
            Assert.That(system.Evaluate(low, flat, 0, 350).Efficiency, Is.EqualTo(0.65));
            Assert.That(system.Evaluate(new SkiTechniqueStats(100, 0), flat, 0, 350).Efficiency, Is.EqualTo(1));
            Assert.That(system.Evaluate(high, climb, 0, 350).Efficiency,
                Is.GreaterThan(system.Evaluate(new SkiTechniqueStats(100, 0), climb, 0, 350).Efficiency));
            var exhausted = system.Evaluate(high, flat, 1, 350);
            Assert.That(exhausted.FatigueModifier, Is.EqualTo(Definition().ExhaustedTechniqueModifier));
            Assert.That(exhausted.PropulsivePowerWatts, Is.LessThan(350));
        }
        [Test]
        public void SharedPhysiologyCostAndSkillDependentMovementAreAllocationFree()
        {
            var technique = new SkiTechniqueSystem(Definition());
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20);
            var physiology = PhysiologyIntegrationTests.Create(new RaceEventLog(100));
            var flat = Sample("synthetic_flat_v1");
            var low = default(SkiState); var high = default(SkiState);
            var lowStats = new SkiTechniqueStats(0, 0); var highStats = new SkiTechniqueStats(100, 100);
            var frame = physiology.Step(PhysiologyIntegrationTests.Input(physiology, 800));
            technique.Evaluate(lowStats, flat, 0, frame.Effort.DeliveredPowerWatts);
            forces.Step(low, 0, flat);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                frame = physiology.Step(PhysiologyIntegrationTests.Input(physiology, 800));
                // One physiological workload feeds both skill cases: technique cannot rebate exertion.
                double watts = frame.Effort.DeliveredPowerWatts;
                double fatigue = physiology.State.MuscularFatigue;
                low = forces.Step(low, technique.Evaluate(lowStats, flat, fatigue, watts).PropulsivePowerWatts, flat).State;
                high = forces.Step(high, technique.Evaluate(highStats, flat, fatigue, watts).PropulsivePowerWatts, flat).State;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(high.DistanceNanometers, Is.GreaterThan(low.DistanceNanometers));
            Assert.That(high.SpeedMetersPerSecond, Is.GreaterThan(low.SpeedMetersPerSecond));
        }
        [Test]
        public void ContractsRejectInvalidOrIncompleteDataAndInputs()
        {
            var json = Definition().ToJson();
            Assert.That(SkiTechniqueDefinition.FromJson(json).ToJson(), Is.EqualTo(json));
            Assert.Catch(() => SkiTechniqueDefinition.FromJson(json.Replace("schema_version", "missing_schema")));
            Assert.Catch(() => SkiTechniqueDefinition.FromJson(json.Replace("skate_climb", "skate_cruise")));
            foreach (var pair in new[] { ("full_climb_grade", "0"), ("minimum_skill_efficiency", "1.01"), ("exhausted_technique_modifier", "-0.1"), ("flat_affinity", "2") })
                Assert.Catch(() => SkiTechniqueDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair.Item1, pair.Item2)));
            Assert.Catch(() => new SkiTechniqueStats(-1, 100));
            Assert.Catch(() => new SkiTechniqueStats(0, 101));
            var system = new SkiTechniqueSystem(Definition());
            var sample = Sample("synthetic_flat_v1");
            Assert.Catch(() => system.Evaluate(default, sample, double.NaN, 350));
            Assert.Catch(() => system.Evaluate(default, sample, 0, double.PositiveInfinity));
            Assert.Catch(() => system.Evaluate(default, sample, 0, -1));
            Assert.Catch(() => system.Evaluate(default, default, 0, 350));
        }
    }
}

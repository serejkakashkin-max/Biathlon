using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SnowConditionTests
    {
        public static SnowCatalog Catalog() => SnowCatalog.FromJson(PhysiologyContractTests.DefinitionJson("snow_catalog_provisional_v1.json"));
        public static RaceSnowConditions Conditions(string profile = "packed_neutral") => RaceSnowConditions.FromJson(
            PhysiologyContractTests.DefinitionJson("race_snow_neutral_v1.json").Replace("packed_neutral", profile));
        [Test]
        public void NeutralBindingPreservesForcesAndConditionsAreExplicit()
        {
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var sample = new TrackSampler(track.GetLoop("main_long")).Sample(default);
            var snow = new SnowConditionSystem(Catalog(), Conditions(), track);
            Assert.That(snow.ConditionsId, Is.EqualTo(Conditions().Id));
            Assert.That(snow.Sample(sample).Id, Is.EqualTo("packed_neutral"));
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20); var state = new SkiState(0, 10);
            var a = forces.Step(state, 350, sample);
            var b = forces.Step(state, 350, sample, snowResistanceMultiplier: snow.Sample(sample).ResistanceMultiplier);
            Assert.That(a.State, Is.EqualTo(b.State)); Assert.That(a.NetNewtons, Is.EqualTo(b.NetNewtons));
            Assert.That(SnowCatalog.FromJson(Catalog().ToJson()).ToJson(), Is.EqualTo(Catalog().ToJson()));
            Assert.That(RaceSnowConditions.FromJson(Conditions().ToJson()).ToJson(), Is.EqualTo(Conditions().ToJson()));
        }
        [Test]
        public void SlowSnowCostsMotionAndIceTradesGlideForCornerGrip()
        {
            var catalog = Catalog(); var neutral = catalog.GetProfile("packed_neutral");
            var soft = catalog.GetProfile("fresh_soft"); var ice = catalog.GetProfile("icy_fast");
            var track = TuckTests.Descent(0.04); var state = new SkiState(0, 10);
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20);
            Assert.That(forces.Step(state, 350, track, snowResistanceMultiplier: soft.ResistanceMultiplier).SnowNewtons,
                Is.GreaterThan(forces.Step(state, 350, track).SnowNewtons));
            var corner = new CorneringSystem(CorneringTests.Definition(), SkiForceTests.Definition());
            Assert.That(corner.Evaluate(state, track, 100, 100, 1, 0, ice.GripMultiplier).SafeSpeedMetersPerSecond,
                Is.LessThan(corner.Evaluate(state, track, 100, 100, 1, 0, neutral.GripMultiplier).SafeSpeedMetersPerSecond));
            Assert.That(ice.ResistanceMultiplier, Is.LessThan(neutral.ResistanceMultiplier));
            var a = state; var b = state;
            for (int i = 0; i < 3600; i++)
            {
                a = forces.Step(a, 350, track).State;
                b = forces.Step(b, 350, track, snowResistanceMultiplier: soft.ResistanceMultiplier).State;
            }
            Assert.That(b.DistanceNanometers, Is.LessThan(a.DistanceNanometers));
        }
        [Test]
        public void UnknownReferencesAndUnsafeProfilesFailBeforeStepping()
        {
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1"); var catalog = Catalog();
            Assert.Catch(() => new SnowConditionSystem(catalog, Conditions("unknown"), track));
            Assert.Catch(() => new SnowConditionSystem(catalog, RaceSnowConditions.FromJson(Conditions().ToJson().Replace("snow_test_packed", "unknown_surface")), track));
            Assert.Catch(() => SnowCatalog.FromJson(catalog.ToJson().Replace("fresh_soft", "packed_neutral")));
            Assert.Catch(() => SnowCatalog.FromJson(PhysiologyContractTests.ReplaceNumber(catalog.ToJson(), "glide_multiplier", "0")));
            Assert.Catch(() => SnowCatalog.FromJson(catalog.ToJson().Replace("grip_multiplier", "missing_grip")));
            Assert.Catch(() => RaceSnowConditions.FromJson(PhysiologyContractTests.ReplaceNumber(Conditions().ToJson(), "schema_version", "2")));
            Assert.Catch(() => new SnowConditionSystem(catalog, Conditions(), track).Sample(default));
            Assert.Catch(() => new SkiForceSystem(SkiForceTests.Definition(), 20).Step(default, 0, TuckTests.Descent(), snowResistanceMultiplier: double.NaN));
        }
        [Test]
        public void ConditionLookupAndForceIntegrationAllocateNothingPerTick()
        {
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var sample = new TrackSampler(track.GetLoop("main_long")).Sample(default);
            var system = new SnowConditionSystem(Catalog(), Conditions("fresh_soft"), track);
            var forces = new SkiForceSystem(SkiForceTests.Definition(), 20); var state = default(SkiState);
            var snow = system.Sample(sample); forces.Step(state, 350, sample, snowResistanceMultiplier: snow.ResistanceMultiplier);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                snow = system.Sample(sample);
                state = forces.Step(state, 350, sample, snowResistanceMultiplier: snow.ResistanceMultiplier).State;
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(state.DistanceMeters, Is.GreaterThan(4000));
        }
    }
}

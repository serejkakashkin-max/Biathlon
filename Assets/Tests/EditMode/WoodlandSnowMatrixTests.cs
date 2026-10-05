using System;
using System.Collections.Generic;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    /// <summary>Actual venue binding and short component probes, not completed-race acceptance.</summary>
    public class WoodlandSnowMatrixTests
    {
        private const int ProbeFrequency = 20;
        private const int ProbeTicks = 1200;
        private const double ProbePowerWatts = 350;

        private static TrackDefinition Track() => TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile(
            "Assets/Resources/Biathlon/Venues/woodland_basin_v1/track.json"));
        private static SkiModelDefinitions Model() => new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);

        // Matches the WP175 generated RaceSnowConditions schema. Only stable IDs are
        // authored here; resistance and grip stay in the canonical snow catalog.
        private static RaceSnowConditions Conditions(TrackDefinition track, string profileId)
        {
            var surfaces = new SortedSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < track.LoopCount; i++)
                for (int j = 0; j < track.GetLoop(i).SegmentCount; j++)
                    surfaces.Add(track.GetLoop(i).GetSegment(j).SnowProfileId);
            var json = new StringBuilder("{\"schema_version\":1,\"id\":\"woodland_wp175_")
                .Append(profileId).Append("_v1\",\"surfaces\":[");
            bool first = true;
            foreach (string surface in surfaces)
            {
                if (!first) json.Append(',');
                first = false;
                json.Append("{\"surface_id\":\"").Append(surface)
                    .Append("\",\"profile_id\":\"").Append(profileId).Append("\"}");
            }
            return RaceSnowConditions.FromJson(json.Append("]}").ToString());
        }

        [TestCase("packed_neutral")]
        [TestCase("fresh_soft")]
        [TestCase("icy_fast")]
        [TestCase("wet_slow")]
        public void EveryActualLoopAndExactSegmentBoundaryResolveCanonicalProfile(string profileId)
        {
            var track = Track();
            var catalog = Model().Snow;
            var conditions = Conditions(track, profileId);
            var system = new SnowConditionSystem(catalog, conditions, track);
            var canonical = catalog.GetProfile(profileId);
            var encountered = new HashSet<string>(StringComparer.Ordinal);
            int sampleCount = 0;
            for (int i = 0; i < track.LoopCount; i++)
            {
                var loop = track.GetLoop(i);
                var sampler = new TrackSampler(loop);
                for (int j = 0; j < loop.SegmentCount; j++)
                {
                    var segment = loop.GetSegment(j);
                    long start = segment.StartDistance.Millimeters;
                    long end = checked(start + segment.Length.Millimeters);
                    foreach (long distance in new[] { start, start + segment.Length.Millimeters / 2, end - 1 })
                    {
                        var sample = sampler.Sample(new TrackDistance(distance));
                        Assert.That(sample.SegmentIndex, Is.EqualTo(j), loop.Id + "/" + segment.Id);
                        Assert.That(sample.SnowProfileId, Is.EqualTo(segment.SnowProfileId));
                        Assert.That(system.Sample(sample), Is.SameAs(canonical));
                        encountered.Add(sample.SnowProfileId);
                        sampleCount++;
                    }
                    var boundary = sampler.Sample(new TrackDistance(end));
                    Assert.That(boundary.SegmentIndex, Is.EqualTo(j + 1 < loop.SegmentCount ? j + 1 : j));
                    Assert.That(system.Sample(boundary), Is.SameAs(canonical));
                    sampleCount++;
                }
            }
            Assert.That(conditions.SurfaceCount, Is.EqualTo(encountered.Count));
            for (int i = 0; i < conditions.SurfaceCount; i++)
            {
                Assert.That(encountered.Contains(conditions.GetSurface(i).SurfaceId), Is.True);
                Assert.That(conditions.GetSurface(i).ProfileId, Is.EqualTo(profileId));
            }
            Assert.That(RaceSnowConditions.FromJson(conditions.ToJson()).ToJson(), Is.EqualTo(conditions.ToJson()));
            TestContext.WriteLine(FormattableString.Invariant(
                $"woodland_snow_binding={{\"profile_id\":\"{profileId}\",\"loops\":{track.LoopCount},\"samples\":{sampleCount},\"surfaces\":{encountered.Count}}}"));
        }

        [Test]
        public void MissingActualSurfaceAndUnknownCanonicalProfileFailBeforeStepping()
        {
            var track = Track();
            var catalog = Model().Snow;
            var valid = Conditions(track, "packed_neutral");
            string missingJson = valid.ToJson().Replace(valid.GetSurface(0).SurfaceId, "unbound_fixture_surface");
            Assert.Throws<ArgumentException>(() => new SnowConditionSystem(catalog,
                RaceSnowConditions.FromJson(missingJson), track));
            Assert.Throws<ArgumentException>(() => new SnowConditionSystem(catalog,
                Conditions(track, "unknown_fixture_profile"), track));
        }

        [Test]
        public void ActualTechnicalBendShowsIceGlideVersusGripAndSlowSnowResistance()
        {
            var track = Track();
            var model = Model();
            var sample = TechnicalBend(track);
            var force = new SkiForceSystem(model.Physics, ProbeFrequency);
            var corner = new CorneringSystem(model.Cornering, model.Physics);
            var neutral = new SnowConditionSystem(model.Snow, Conditions(track, "packed_neutral"), track).Sample(sample);
            var ice = new SnowConditionSystem(model.Snow, Conditions(track, "icy_fast"), track).Sample(sample);
            var soft = new SnowConditionSystem(model.Snow, Conditions(track, "fresh_soft"), track).Sample(sample);
            var wet = new SnowConditionSystem(model.Snow, Conditions(track, "wet_slow"), track).Sample(sample);
            var neutralLimit = corner.Evaluate(default, sample, 100, 100, 1, 0, neutral.GripMultiplier);
            var iceLimit = corner.Evaluate(default, sample, 100, 100, 1, 0, ice.GripMultiplier);
            Assert.That(iceLimit.SafeSpeedMetersPerSecond, Is.LessThan(neutralLimit.SafeSpeedMetersPerSecond));
            var state = new SkiState(0, (neutralLimit.SafeSpeedMetersPerSecond + iceLimit.SafeSpeedMetersPerSecond) / 2);
            var packedForce = force.Step(state, ProbePowerWatts, sample, snowResistanceMultiplier: neutral.ResistanceMultiplier);
            var iceForce = force.Step(state, ProbePowerWatts, sample, snowResistanceMultiplier: ice.ResistanceMultiplier);
            var softForce = force.Step(state, ProbePowerWatts, sample, snowResistanceMultiplier: soft.ResistanceMultiplier);
            var wetForce = force.Step(state, ProbePowerWatts, sample, snowResistanceMultiplier: wet.ResistanceMultiplier);
            Assert.That(iceForce.SnowNewtons, Is.LessThan(packedForce.SnowNewtons));
            Assert.That(iceForce.State.SpeedMetersPerSecond, Is.GreaterThan(packedForce.State.SpeedMetersPerSecond));
            Assert.That(softForce.SnowNewtons, Is.GreaterThan(packedForce.SnowNewtons));
            Assert.That(wetForce.SnowNewtons, Is.GreaterThan(packedForce.SnowNewtons));
            Assert.That(softForce.State.SpeedMetersPerSecond, Is.LessThan(packedForce.State.SpeedMetersPerSecond));
            Assert.That(wetForce.State.SpeedMetersPerSecond, Is.LessThan(packedForce.State.SpeedMetersPerSecond));
            Assert.That(corner.Evaluate(state, sample, 100, 100, 1, 0, wet.GripMultiplier).SafeSpeedMetersPerSecond,
                Is.GreaterThan(neutralLimit.SafeSpeedMetersPerSecond));
            var iceTurn = corner.Evaluate(state, sample, 100, 100, 1, 0, ice.GripMultiplier);
            Assert.That(iceTurn.BrakingNewtons, Is.GreaterThan(0));
            Assert.That(corner.Evaluate(state, sample, 100, 100, 1, 0, neutral.GripMultiplier).BrakingNewtons, Is.Zero);
            Assert.That(force.Step(state, ProbePowerWatts, sample).State, Is.EqualTo(packedForce.State));
            var brakedIce = force.Step(state, ProbePowerWatts, sample, brakingNewtons: iceTurn.BrakingNewtons,
                snowResistanceMultiplier: ice.ResistanceMultiplier);
            Assert.That(brakedIce.State.DistanceNanometers, Is.LessThan(iceForce.State.DistanceNanometers));
            TestContext.WriteLine(FormattableString.Invariant(
                $"woodland_snow_tradeoff={{\"loop_id\":\"{sample.LoopId}\",\"segment_id\":\"{sample.SegmentId}\",\"neutral_snow_n\":{packedForce.SnowNewtons:R},\"ice_snow_n\":{iceForce.SnowNewtons:R},\"soft_snow_n\":{softForce.SnowNewtons:R},\"wet_snow_n\":{wetForce.SnowNewtons:R},\"neutral_safe_mps\":{neutralLimit.SafeSpeedMetersPerSecond:R},\"ice_safe_mps\":{iceLimit.SafeSpeedMetersPerSecond:R},\"ice_braking_n\":{iceTurn.BrakingNewtons:R}}}"));
        }

        private static TrackSample TechnicalBend(TrackDefinition track)
        {
            var loop = track.GetLoop("main");
            var sampler = new TrackSampler(loop);
            for (int i = 0; i < loop.SegmentCount; i++)
            {
                var segment = loop.GetSegment(i);
                if (segment.SemanticZone == TrackSemanticZone.DescentTechnical && segment.CurvaturePerMeter != 0)
                    return sampler.Sample(new TrackDistance(segment.StartDistance.Millimeters + segment.Length.Millimeters / 2));
            }
            throw new InvalidOperationException("Actual Woodland main lacks an authored technical bend.");
        }

        [TestCase("packed_neutral")]
        [TestCase("fresh_soft")]
        [TestCase("icy_fast")]
        [TestCase("wet_slow")]
        public void ShortSharedForceAndCorneringProbeRepeatsExactly(string profileId)
        {
            var track = Track();
            var model = Model();
            var conditions = Conditions(track, profileId);
            var first = Probe(track, model, conditions);
            var second = Probe(track, model, RaceSnowConditions.FromJson(conditions.ToJson()));
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.DistanceNanometers, Is.GreaterThan(0));
            Assert.That(first.DistanceMeters, Is.LessThan(track.GetLoop("main").Length.Meters),
                "This short component probe must not be presented as a finished lap or race.");
            TestContext.WriteLine(FormattableString.Invariant(
                $"woodland_snow_probe={{\"profile_id\":\"{profileId}\",\"ticks\":{ProbeTicks},\"frequency_hz\":{ProbeFrequency},\"distance_nm\":{first.DistanceNanometers},\"speed_mps\":{first.SpeedMetersPerSecond:R},\"completed_race\":false}}"));
        }

        private static SkiState Probe(TrackDefinition track, SkiModelDefinitions model, RaceSnowConditions conditions)
        {
            var sampler = new TrackSampler(track.GetLoop("main"));
            var snow = new SnowConditionSystem(model.Snow, conditions, track);
            var force = new SkiForceSystem(model.Physics, ProbeFrequency);
            var corner = new CorneringSystem(model.Cornering, model.Physics);
            var state = default(SkiState);
            for (int tick = 0; tick < ProbeTicks; tick++)
            {
                var sample = sampler.Sample(state.Distance, TrackBoundaryMode.Wrap);
                var profile = snow.Sample(sample);
                var turn = corner.Evaluate(state, sample, 100, 100, 1, 0, profile.GripMultiplier);
                state = force.Step(state, ProbePowerWatts, sample, brakingNewtons: turn.BrakingNewtons,
                    snowResistanceMultiplier: profile.ResistanceMultiplier).State;
            }
            return state;
        }
    }
}

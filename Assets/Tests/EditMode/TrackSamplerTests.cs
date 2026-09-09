using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TrackSamplerTests
    {
        [Test]
        public void SegmentBoundariesUseNextSegmentAndTerminalUsesLastEnd()
        {
            var loop = TrackContractTests.Track().GetLoop("main_short");
            var sampler = new TrackSampler(loop);
            for (int index = 1; index < loop.SegmentCount; index++)
            {
                long mm = loop.GetSegment(index).StartDistance.Millimeters;
                Assert.That(sampler.Sample(new TrackDistance(mm - 1)).SegmentIndex, Is.EqualTo(index - 1));
                Assert.That(sampler.Sample(new TrackDistance(mm)).SegmentIndex, Is.EqualTo(index));
                Assert.That(sampler.Sample(new TrackDistance(mm)).SegmentFraction, Is.Zero);
                Assert.That(sampler.Sample(new TrackDistance(mm + 1)).SegmentIndex, Is.EqualTo(index));
            }
            var terminal = sampler.Sample(loop.Length);
            Assert.That(terminal.SegmentIndex, Is.EqualTo(loop.SegmentCount - 1));
            Assert.That(terminal.SegmentFraction, Is.EqualTo(1));
            Assert.That(sampler.Sample(new TrackDistance(0)).SegmentIndex, Is.Zero);
        }
        [Test]
        public void GradeInterpolatesAndMetadataComesFromSelectedSegment()
        {
            var sampler = new TrackSampler(TrackContractTests.Track().GetLoop("main_short"));
            Assert.That(sampler.Sample(TrackDistance.FromMeters(400)).Grade, Is.EqualTo(0.05));
            var middle = sampler.Sample(TrackDistance.FromMeters(500));
            Assert.That(middle.Grade, Is.EqualTo(0.1).Within(1e-12));
            Assert.That(middle.SegmentFraction, Is.EqualTo(0.5));
            Assert.That(middle.WidthMeters, Is.EqualTo(6));
            Assert.That(middle.WindExposure, Is.EqualTo(0.5));
            Assert.That(middle.TerrainMuscleFactor, Is.EqualTo(1.5));
            Assert.That(middle.SemanticZone, Is.EqualTo(TrackSemanticZone.ClimbLong));
            Assert.That(middle.SnowProfileId, Is.EqualTo("snow_test_packed"));
            var range = sampler.Sample(TrackDistance.FromMeters(850));
            Assert.That(range.DraftAllowed, Is.False);
            Assert.That(range.OvertakeAllowed, Is.False);
            var penalty = new TrackSampler(TrackContractTests.Track().GetLoop("penalty_150")).Sample(TrackDistance.FromMeters(75));
            Assert.That(penalty.CurvaturePerMeter, Is.EqualTo(0.04));
        }
        [Test]
        public void ExplicitBoundaryModesHandleManyLapsWithoutOverflow()
        {
            var loop = TrackContractTests.Track().GetLoop("main_short");
            var sampler = new TrackSampler(loop);
            Assert.That(sampler.Sample(loop.Length, TrackBoundaryMode.Wrap).Distance.Millimeters, Is.Zero);
            Assert.That(sampler.Sample(new TrackDistance(long.MaxValue), TrackBoundaryMode.Clamp).Distance, Is.EqualTo(loop.Length));
            Assert.That(sampler.Sample(new TrackDistance(long.MaxValue), TrackBoundaryMode.Wrap).Distance.Millimeters,
                Is.EqualTo(long.MaxValue % loop.Length.Millimeters));
            Assert.Catch(() => sampler.Sample(new TrackDistance(loop.Length.Millimeters + 1)));
            Assert.Catch(() => sampler.Sample(default, (TrackBoundaryMode)999));
            string openJson = new Regex("\"closed\"\\s*:\\s*true").Replace(TrackContractTests.Json(), "\"closed\": false", 1);
            var open = new TrackSampler(TrackDefinition.FromJson(openJson).GetLoop("main_short"));
            Assert.Catch(() => open.Sample(default, TrackBoundaryMode.Wrap));
        }
        [Test]
        public void FullLoopSamplingMatchesFixedNumericBaselineWithoutAllocations()
        {
            var sampler = new TrackSampler(TrackContractTests.Track().GetLoop("main_short"));
            sampler.Sample(default);
            long gradeSum = 0;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                var sample = sampler.Sample(new TrackDistance(i * 1000L), TrackBoundaryMode.Wrap);
                gradeSum += (long)Math.Round(sample.Grade * 1e9, MidpointRounding.ToEven);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(gradeSum, Is.EqualTo(-500000000L));
        }
    }
}

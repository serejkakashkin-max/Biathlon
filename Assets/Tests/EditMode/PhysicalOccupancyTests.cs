using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalOccupancyTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private static PhysicalTrackMap Map(bool closed = true)
        {
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            if (!closed) track = TrackDefinition.FromJson(new Regex("\"closed\":true").Replace(track.ToJson(), "\"closed\":false", 2));
            var requirements = CompetitionProfileTests.TestProfile().ToRouteRequirements();
            return new PhysicalTrackMap(track, new RaceRoute(track,
                RaceRoutePlan.TimeTrial("physical", track.Id, "main_long", requirements), requirements));
        }
        private static PhysicalOccupancySweep Sweep(PhysicalTrackMap map, string id, long start, long travel = 0,
            double lateralBefore = 0, double lateralAfter = 0) => new PhysicalOccupancySweep(new AthleteId(id),
                map.MapRoute(start), travel, lateralBefore, lateralAfter);

        [Test] public void LappedBodiesAndSeamUseTheSamePhysicalSpace()
        {
            var map = Map(); var query = new PhysicalOccupancySystem(map, OccupancyTests.Definition());
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 0), Sweep(map, "b", 1000 * M)).Blocked, Is.True);
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 999 * M), Sweep(map, "b", 0)).Blocked, Is.True);
            var touch = OccupancyTests.Definition().LongitudinalSeparation;
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 1000 * M - (long)(touch * M)), Sweep(map, "b", 0)).Blocked, Is.False);
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 1000 * M - (long)(touch * M) + 1), Sweep(map, "b", 0)).Blocked, Is.True);
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 999 * M, 0, 2, 2), Sweep(map, "b", 0)).Blocked, Is.False);
        }

        [Test] public void SweepsCatchPassThroughAcrossSeamAndAnEntireCircuit()
        {
            var map = Map(); var query = new PhysicalOccupancySystem(map, OccupancyTests.Definition());
            var a = Sweep(map, "a", 990 * M, 30 * M); var b = Sweep(map, "b", 1005 * M, 5 * M);
            var result = query.QueryRelativeMove(a, b);
            Assert.That(result.Blocked, Is.True); Assert.That(result.BlockerId.Value, Is.EqualTo("b"));
            Assert.That(result.FirstContactFraction, Is.EqualTo((15 - OccupancyTests.Definition().LongitudinalSeparation) / 25).Within(1e-12));
            Assert.That(query.QueryRelativeMove(b, a).FirstContactFraction, Is.EqualTo(result.FirstContactFraction));
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 0, 1000 * M), Sweep(map, "b", 500 * M)).Blocked, Is.True);
            // Requires the +2 image: both original endpoints are widely separated in lateral space.
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 999 * M, 1000 * M, 10, 0), Sweep(map, "b", 0)).Blocked, Is.True);
        }

        [Test] public void RelativeLateralMotionIsContinuousAndOpenLoopsDoNotWrap()
        {
            var map = Map(); var query = new PhysicalOccupancySystem(map, OccupancyTests.Definition());
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 0, 0, -2, 2), Sweep(map, "b", 1000 * M, 0, 2, -2)).Blocked, Is.True);
            var open = Map(false); var openQuery = new PhysicalOccupancySystem(open, OccupancyTests.Definition());
            Assert.That(openQuery.QueryRelativeMove(Sweep(open, "a", 999 * M), Sweep(open, "b", 0)).Blocked, Is.False);
            var penalty = new PhysicalOccupancySweep(new AthleteId("b"), map.MapPenalty(0), 0, 0, 0);
            Assert.That(query.QueryRelativeMove(Sweep(map, "a", 0), penalty).Blocked, Is.False);
        }

        [Test] public void InvalidOrAmbiguousMovesAreRejectedBeforeQueries()
        {
            var map = Map(); var query = new PhysicalOccupancySystem(map, OccupancyTests.Definition()); var a = Sweep(map, "a", 0);
            Assert.Catch(() => query.QueryRelativeMove(default, a));
            Assert.Catch(() => query.QueryRelativeMove(a, Sweep(Map(), "b", 0)));
            Assert.Catch(() => query.QueryRelativeMove(a, a));
            Assert.Catch(() => Sweep(map, "b", 0, -1));
            Assert.Catch(() => Sweep(map, "b", 0, 1000 * M + 1));
            Assert.Catch(() => Sweep(Map(false), "b", 999 * M, 2 * M));
            Assert.Catch(() => Sweep(map, "b", 0, 0, double.NaN));
        }

        [Test] public void PeriodicQueriesMatchSampledGeometryAndAllocateNothing()
        {
            var map = Map(); var query = new PhysicalOccupancySystem(map, OccupancyTests.Definition());
            var random = new Random(7115); var definition = OccupancyTests.Definition();
            for (int i = 0; i < 200; i++)
            {
                var a = Sweep(map, "a", random.Next(1000) * M, random.Next(1001) * M, random.Next(-2, 3), random.Next(-2, 3));
                var b = Sweep(map, "b", random.Next(1000) * M, random.Next(1001) * M, random.Next(-2, 3), random.Next(-2, 3));
                var result = query.QueryRelativeMove(a, b);
                Assert.That(query.QueryRelativeMove(b, a).Blocked, Is.EqualTo(result.Blocked));
                for (int sample = 0; sample <= 1000; sample++)
                {
                    double t = sample / 1000.0;
                    double gap = (a.Start.LocalNanometers - b.Start.LocalNanometers + (a.TravelNanometers - b.TravelNanometers) * t) / M;
                    gap -= Math.Round(gap / 1000) * 1000;
                    double lateral = a.LateralBeforeMeters - b.LateralBeforeMeters + t *
                        (a.LateralAfterMeters - a.LateralBeforeMeters - b.LateralAfterMeters + b.LateralBeforeMeters);
                    if (Math.Abs(gap) < definition.LongitudinalSeparation && Math.Abs(lateral) < definition.LateralSeparation)
                    { Assert.That(result.Blocked, Is.True); Assert.That(result.FirstContactFraction, Is.LessThanOrEqualTo(t + 1e-12)); }
                }
            }
            var x = Sweep(map, "a", 999 * M, 1000 * M); var y = Sweep(map, "b", 0);
            query.QueryRelativeMove(x, y);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) query.QueryRelativeMove(x, y);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }

        [Test] public void BatchRechecksStationarySuffixAndReportsStableEarliestPair()
        {
            var map = Map(); var query = new PhysicalOccupancySystem(map, OccupancyTests.Definition());
            // Before shortening, equal-speed trajectories maintain a safe 5 m separation.
            var batch = new[] { Sweep(map, "a", 995 * M, 10 * M), Sweep(map, "b", 1000 * M, 10 * M) };
            Assert.That(query.QueryBatch(batch).Blocked, Is.False);
            // The leading athlete is stopped by a different constraint. The follower must now be reconsidered.
            batch[1] = Sweep(map, "b", 1000 * M);
            var conflict = query.QueryBatch(batch);
            Assert.That(conflict.Blocked, Is.True);
            Assert.That(conflict.FirstContactFraction, Is.EqualTo((5 - OccupancyTests.Definition().LongitudinalSeparation) / 10).Within(1e-12));
            Array.Reverse(batch); Assert.That(query.QueryBatch(batch), Is.EqualTo(conflict));
            var tied = new[] { Sweep(map, "z", 0), Sweep(map, "b", 1000 * M), Sweep(map, "a", 2000 * M) };
            var tie = query.QueryBatch(tied);
            Assert.That(tie.FirstAthlete.Value, Is.EqualTo("a")); Assert.That(tie.SecondAthlete.Value, Is.EqualTo("b"));
            Array.Reverse(tied); Assert.That(query.QueryBatch(tied), Is.EqualTo(tie));
            tied[2] = default; Assert.Catch(() => query.QueryBatch(tied));
            tied[2] = tied[0]; Assert.Catch(() => query.QueryBatch(tied));
            query.QueryBatch(batch);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) query.QueryBatch(batch);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

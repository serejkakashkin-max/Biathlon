using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangeArrivalTests
    {
        private static readonly AthleteId Actor = new AthleteId("arrival_athlete");
        private static readonly SimulationTick Tick = new SimulationTick(17);
        private const int Frequency = 20;
        private const double Duration = 1.0 / Frequency;

        private static RaceMotionSegment Segment(long before, long after, long travelBefore, long travelAfter,
            double start, double end, bool penalty = false, bool approach = false)
            => (RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment),
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                new object[] { before, after, travelBefore, travelAfter, start, end, penalty, approach }, null);

        private static bool Find(long line, RaceMotionPoint initial, RaceMotionSegment[] segments,
            out PhysicalRangeArrival arrival)
            => PhysicalRangeArrival.TryFromAcceptedMotion(Actor, 2, Tick, Frequency, line, initial, segments, out arrival);

        private static void AssertMinimumCrossing(RaceMotionSegment[] segments, RaceMotionPoint initial,
            long line, long expectedFraction)
        {
            Assert.That(Find(line, initial, segments, out var arrival), Is.True);
            Assert.That(arrival.IsValid, Is.True);
            Assert.That(arrival.AthleteId, Is.EqualTo(Actor));
            Assert.That(arrival.BoutIndex, Is.EqualTo(2));
            Assert.That(arrival.Tick, Is.EqualTo(Tick));
            Assert.That(arrival.TickFrequency, Is.EqualTo(Frequency));
            Assert.That(arrival.Fraction, Is.EqualTo(expectedFraction));
            Assert.That(FractionalMotionTimeline.Sample(segments, initial, arrival.Fraction, Duration).RouteNanometers,
                Is.GreaterThanOrEqualTo(line));
            if (arrival.Fraction > 0 && initial.RouteNanometers < line)
                Assert.That(FractionalMotionTimeline.Sample(segments, initial, arrival.Fraction - 1, Duration).RouteNanometers,
                    Is.LessThan(line));
        }

        [TestCase(3, 1, 333333334)]
        [TestCase(3, 2, 666666667)]
        [TestCase(3, 3, 1000000000)]
        [TestCase(7, 1, 142857143)]
        [TestCase(7, 6, 857142858)]
        public void ArrivalIsTheFirstRetimedFractionWhoseFloorSampleReachesTheLine(long distance, long line, long expected)
        {
            var segments = new[] { Segment(0, distance, 0, distance, 0, Duration) };
            AssertMinimumCrossing(segments, default, line, expected);
        }

        [Test] public void InteriorReceiptUsesItsOwnRetimedStartAndEnd()
        {
            var initial = new RaceMotionPoint(100, 900);
            var segments = new[] { Segment(100, 107, 900, 907, 0.01, 0.04) };
            AssertMinimumCrossing(segments, initial, 102, 371428572);
            AssertMinimumCrossing(segments, initial, 107, 800000000);
        }

        [Test] public void SharedReceiptEndpointIsCountedAtTheEndpointWithoutWaitingForTheNextSegment()
        {
            var segments = new[]
            {
                Segment(0, 10, 0, 10, 0, 0.02),
                Segment(10, 20, 10, 20, 0.02, Duration)
            };
            AssertMinimumCrossing(segments, default, 10, 400000000);
        }

        [Test] public void TickEndArrivalRetainsFractionParts()
        {
            var segments = new[] { Segment(40, 50, 900, 910, 0.01, Duration) };
            AssertMinimumCrossing(segments, new RaceMotionPoint(40, 900), 50, JunctionAdmissionSession.FractionParts);
        }

        [Test] public void IntermediateMultiplicationCannotOverflowLongOrLoseTheMinimumFraction()
        {
            const long distance = 9000000000000000000L;
            const long line = 8000000000000000000L;
            var segments = new[] { Segment(0, distance, 0, distance, 0, Duration) };
            AssertMinimumCrossing(segments, default, line, 888888889);
        }

        [Test] public void LargeAbsoluteRouteAndTravelOriginsDoNotChangeRelativeArrival()
        {
            const long origin = 7000000000000000000L;
            var initial = new RaceMotionPoint(origin, origin);
            var segments = new[] { Segment(origin, origin + 7, origin, origin + 7, 0.01, 0.04) };
            AssertMinimumCrossing(segments, initial, origin + 2, 371428572);
        }

        [Test] public void ExactlyOnLineRequiresPositiveNormalMovementAndUsesItsActualStart()
        {
            var initial = new RaceMotionPoint(5, 50);
            var delayed = new[] { Segment(5, 15, 50, 60, 0.01, Duration) };
            AssertMinimumCrossing(delayed, initial, 5, 200000000);
            var immediate = new[] { Segment(5, 15, 50, 60, 0, Duration) };
            AssertMinimumCrossing(immediate, initial, 5, 0);
            Assert.That(Find(5, initial, Array.Empty<RaceMotionSegment>(), out var missing), Is.False);
            Assert.That(missing.IsValid, Is.False);
            var stationary = new[] { Segment(5, 5, 50, 50, 0, Duration) };
            Assert.That(Find(5, initial, stationary, out missing), Is.False);
            Assert.That(missing.IsValid, Is.False);
        }

        [Test] public void AcceptedPrefixBeforeTheLineDoesNotTurnItsStationarySuffixIntoArrival()
        {
            var segments = new[] { Segment(0, 9, 0, 9, 0, 0.03) };
            Assert.That(Find(10, default, segments, out var missing), Is.False);
            Assert.That(missing.IsValid, Is.False);
            Assert.That(Find(100, default, Array.Empty<RaceMotionSegment>(), out missing), Is.False);
        }

        [Test] public void PenaltyDistanceIsNotRouteArrivalButTheFollowingNormalSegmentCanArrive()
        {
            var penaltyOnly = new[] { Segment(0, 0, 0, 40, 0, Duration, penalty: true) };
            Assert.That(Find(0, default, penaltyOnly, out var missing), Is.False);
            Assert.That(missing.IsValid, Is.False);
            Assert.That(Find(5, default, penaltyOnly, out missing), Is.False);
            var mixed = new[]
            {
                Segment(0, 0, 0, 40, 0, 0.01, penalty: true),
                Segment(0, 9, 40, 49, 0.01, Duration)
            };
            AssertMinimumCrossing(mixed, default, 5, 644444445);
        }

        [Test] public void StartApproachDistanceIsNotArrivalAtRouteOrigin()
        {
            var approachOnly = new[] { Segment(0, 0, 0, 40, 0, Duration, approach: true) };
            Assert.That(Find(0, default, approachOnly, out var missing), Is.False);
            Assert.That(missing.IsValid, Is.False);
            var mixed = new[]
            {
                Segment(0, 0, 0, 40, 0, 0.01, approach: true),
                Segment(0, 9, 40, 49, 0.01, Duration)
            };
            AssertMinimumCrossing(mixed, default, 0, 200000000);
        }

        [TestCase("disconnected_route")]
        [TestCase("disconnected_travel")]
        [TestCase("time_overlap")]
        [TestCase("reverse_route")]
        [TestCase("invalid_late_approach")]
        [TestCase("invalid_late_time")]
        public void MalformedSuffixIsRejectedEvenAfterAnEarlierValidCrossing(string defect)
        {
            var suffix = Segment(10, 20, 10, 20, 0.02, Duration);
            switch (defect)
            {
                case "disconnected_route": suffix = Segment(11, 21, 10, 20, 0.02, Duration); break;
                case "disconnected_travel": suffix = Segment(10, 20, 11, 21, 0.02, Duration); break;
                case "time_overlap": suffix = Segment(10, 20, 10, 20, 0.01, Duration); break;
                case "reverse_route": suffix = Segment(10, 9, 10, 9, 0.02, Duration); break;
                case "invalid_late_approach": suffix = Segment(10, 10, 10, 20, 0.02, Duration, approach: true); break;
                case "invalid_late_time": suffix = Segment(10, 20, 10, 20, 0.02, double.NaN); break;
            }
            var segments = new[] { Segment(0, 10, 0, 10, 0, 0.02), suffix };
            Assert.Catch(() => Find(5, default, segments, out _));
        }

        [TestCase("movement")]
        [TestCase("initial_gap")]
        [TestCase("late_gap")]
        [TestCase("late_movement")]
        [TestCase("suffix")]
        public void CollapsedRetimedPrecisionIsRejectedBeforeReturningAnyArrival(string defect)
        {
            RaceMotionSegment[] segments;
            switch (defect)
            {
                case "movement": segments = new[] { Segment(0, 10, 0, 10, 0, 1e-12) }; break;
                case "initial_gap": segments = new[] { Segment(0, 10, 0, 10, 1e-12, Duration) }; break;
                case "late_gap": segments = new[]
                {
                    Segment(0, 10, 0, 10, 0, 0.01),
                    Segment(10, 20, 10, 20, 0.01 + 1e-12, Duration)
                }; break;
                case "late_movement": segments = new[]
                {
                    Segment(0, 10, 0, 10, 0, 0.01),
                    Segment(10, 20, 10, 20, 0.02, 0.02 + 1e-12)
                }; break;
                default: segments = new[] { Segment(0, 10, 0, 10, 0, Duration - 1e-12) }; break;
            }
            Assert.Throws<InvalidOperationException>(() => Find(5, default, segments, out _));
        }

        [Test] public void ReceiptMustContinueTheSuppliedInitialPositionAndCannotStartPastTheLine()
        {
            var segments = new[] { Segment(0, 10, 0, 10, 0, Duration) };
            Assert.Catch(() => Find(5, new RaceMotionPoint(1, 0), segments, out _));
            Assert.Catch(() => Find(5, new RaceMotionPoint(0, 1), segments, out _));
            var alreadyPast = new[] { Segment(6, 10, 6, 10, 0, Duration) };
            Assert.Catch(() => Find(5, new RaceMotionPoint(6, 6), alreadyPast, out _));
        }

        [Test] public void CapturedArrivalRemainsIndependentOfTheCallerReceiptBuffer()
        {
            var segments = new[] { Segment(0, 3, 0, 3, 0, Duration) };
            Assert.That(Find(1, default, segments, out var arrival), Is.True);
            segments[0] = default;
            Assert.That(arrival.IsValid, Is.True);
            Assert.That(arrival.AthleteId, Is.EqualTo(Actor));
            Assert.That(arrival.Fraction, Is.EqualTo(333333334));
        }

        [Test] public void WarmReceiptExtractionAllocatesNoManagedMemory()
        {
            var segments = new[]
            {
                Segment(0, 0, 0, 40, 0, 0.01, penalty: true),
                Segment(0, 9, 40, 49, 0.01, Duration)
            };
            PhysicalRangeArrival arrival = default;
            bool found = false;
            for (int i = 0; i < 1000; i++) found = Find(5, default, segments, out arrival);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) found = Find(5, default, segments, out arrival);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(found, Is.True);
            Assert.That(arrival.Fraction, Is.EqualTo(644444445));
            Assert.That(allocated, Is.Zero);
        }
    }
}

using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SubstepCheckpointTests
    {
        [Test] public void SubstepCrossingIncludesEarlierStationaryPenaltyTime()
        {
            var time = FinishTime.AtSubstepCrossing(new SimulationTick(100), 20, 900, 1100, 1000, 0.03, 0.05);
            Assert.That(time.WholeTicks, Is.EqualTo(99)); Assert.That(time.FractionParts, Is.EqualTo(800000000));
            Assert.That(FinishTime.InsideTick(new SimulationTick(100), 20, 0.05).WholeTicks, Is.EqualTo(100));
            Assert.Catch(() => FinishTime.AtSubstepCrossing(new SimulationTick(100), 20, 900, 1100, 1000, 0.04, 0.03));
        }
        [Test] public void TimedTrajectoryMatchesPathStopAndRejectsWrongRouteWithoutMutation()
        {
            var p = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", p.ToRouteRequirements()), p.ToRouteRequirements());
            var layout = new CheckpointLayout(track, route); var athlete = TimeTrialTests.Athlete(); var log = new RaceEventLog(30);
            var tracker = new CheckpointTracker(layout, new AthleteId(athlete.Id), 20, default, log);
            tracker.Observe(new SimulationTick(1), 0, 799900000000);
            var motion = new RacePathMotion(new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), athlete, track, route,
                SnowConditionTests.Conditions("packed_neutral"), "ski_neutral", 20);
            var buffer = new RaceMotionSegment[10];
            var input = new TimeTrialInput(new SimulationInput(new SimulationTick(2), new AthleteId(athlete.Id), 800), 1, 1, 0);
            var frame = motion.Preview(new SkiState(799900000000, 10), 799900000000, 800200000000, null, 350, 0, input, buffer);
            var wrong = new CheckpointTracker(layout, new AthleteId(athlete.Id), 20, default, new RaceEventLog(10));
            Assert.Catch(() => wrong.ObserveMotion(new SimulationTick(1), buffer.AsSpan(0, frame.SegmentCount)));
            Assert.That(wrong.Tick.Value, Is.Zero); Assert.That(wrong.Count, Is.Zero);
            Assert.That(tracker.ObserveMotion(new SimulationTick(2), buffer.AsSpan(0, frame.SegmentCount)), Is.EqualTo(1));
            Assert.That(tracker.Get(1).GlobalTime, Is.EqualTo(FinishTime.InsideTick(new SimulationTick(2), 20, buffer[0].EndSeconds)));
            Assert.That(tracker.RouteNanometers, Is.EqualTo(frame.RouteNanometers));
            int count = log.Count; long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 3; tick <= 10002; tick++) tracker.ObserveMotion(new SimulationTick(tick), ReadOnlySpan<RaceMotionSegment>.Empty);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(log.Count, Is.EqualTo(count));
        }
    }
}

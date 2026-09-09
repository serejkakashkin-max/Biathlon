using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class RangeMotionPreviewTests
    {
        [Test] public void PreviewMatchesAuthoritativeTraversalAcrossEveryAnchorAndPenaltyBoundary()
        {
            for (int misses = 0; misses <= 5; misses++)
            {
                var log = new RaceEventLog(100); var s = RangePenaltyTraversalTests.Create(log);
                Assert.That(s.PreviewMotion().DistanceToBoundaryNanometers, Is.EqualTo(30000000000L));
                Assert.That(s.PreviewMotion(long.MaxValue).Phase, Is.EqualTo(RangeTraversalPhase.Shooting));
                Assert.That(s.Tick.Value, Is.Zero); Assert.That(log.Count, Is.Zero);
                s.Step(new SimulationTick(1), 30000000000L);
                var completed = new ShotResolutionFrame(new ShootingFrame(new SimulationTick(2), ShootingPhase.Complete, 5, false, 0),
                    default, default, ShotOutcome.NoShot, (1 << (5 - misses)) - 1);
                s.Step(new SimulationTick(2), 0, completed);
                for (int tick = 3; s.Phase != RangeTraversalPhase.Complete; tick++)
                {
                    var predicted = s.PreviewMotion(37000000000L); int count = log.Count;
                    Assert.That(log.Count, Is.EqualTo(count));
                    var actual = s.Step(new SimulationTick(tick), 37000000000L);
                    Assert.That(predicted.Phase, Is.EqualTo(actual.Phase));
                    Assert.That(predicted.RouteNanometers, Is.EqualTo(actual.RouteNanometers));
                    Assert.That(predicted.PenaltyNanometers, Is.EqualTo(actual.PenaltyNanometers));
                }
                Assert.That(s.PreviewMotion().DistanceToBoundaryNanometers, Is.Zero);
            }
        }
    }
}

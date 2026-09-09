using System;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TacticalCadenceTests
    {
        public static TacticalCadenceDefinition Definition(int hz = 3) => TacticalCadenceDefinition.FromJson(
            PhysiologyContractTests.ReplaceNumber(PhysiologyContractTests.DefinitionJson("tactical_cadence_provisional_v1.json"), "decision_hz", hz.ToString()));
        [Test] public void ExactCountsAndNondivisibleIntervalsDoNotDriftAcrossSimulationRates()
        {
            foreach (int frequency in new[] { 20, 30, 60 })
            for (int hz = 2; hz <= 5; hz++)
            {
                var clock = new TacticalDecisionClock(Definition(hz), frequency); long last = 0; int count = 0;
                for (int tick = 1; tick <= frequency * 100; tick++)
                {
                    var window = clock.Step(new SimulationTick(tick)); if (!window.DecisionDue) continue;
                    long expected = 1 + (long)Math.Ceiling(count * (decimal)frequency / hz);
                    Assert.That(tick, Is.EqualTo(expected)); Assert.That(tick, Is.GreaterThan(last)); last = tick; count++;
                    Assert.That(window.PlanningEndTick.Value - tick, Is.EqualTo(4 * frequency));
                    var intent = new TacticalIntentDecision("cadence", new AthleteId("self"), TacticalIntentKind.Hold, window.Tick, window.NextDecisionTick);
                    Assert.That(intent.IsCurrentAt(new SimulationTick(window.NextDecisionTick.Value - 1)), Is.True);
                    Assert.That(intent.IsCurrentAt(window.NextDecisionTick), Is.False);
                }
                Assert.That(count, Is.EqualTo(hz * 100)); Assert.That(clock.DecisionCount, Is.EqualTo(count));
            }
        }
        [Test] public void ShiftedOriginAndInvalidStepsPreserveExactClockState()
        {
            var a = new TacticalDecisionClock(Definition(), 20); var b = new TacticalDecisionClock(Definition(), 20, new SimulationTick(1234));
            for (int i = 1; i <= 100; i++)
            {
                var x = a.Step(new SimulationTick(i)); var y = b.Step(new SimulationTick(i + 1234));
                Assert.That(y.DecisionDue, Is.EqualTo(x.DecisionDue)); Assert.That(y.NextDecisionTick.Value, Is.EqualTo(x.NextDecisionTick.Value + 1234));
                Assert.That(y.PlanningEndTick.Value, Is.EqualTo(x.PlanningEndTick.Value + 1234));
            }
            long count = a.DecisionCount; var next = a.NextDecisionTick;
            Assert.Catch(() => a.Step(new SimulationTick(102))); Assert.That(a.Tick.Value, Is.EqualTo(100));
            Assert.That(a.DecisionCount, Is.EqualTo(count)); Assert.That(a.NextDecisionTick, Is.EqualTo(next));
            Assert.Catch(() => new TacticalDecisionClock(Definition(), 2));
            Assert.Catch(() => new TacticalDecisionClock(Definition(), 20, new SimulationTick(long.MaxValue - 2)));
            Assert.Catch(() => Definition(6));
            var fractional = TacticalCadenceDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Definition().ToJson(), "planning_horizon_seconds", "0.333"));
            Assert.That(new TacticalDecisionClock(fractional, 20).PlanningHorizonTicks, Is.EqualTo(7));
        }
        [Test] public void SchedulingHotLoopHasNoAllocations()
        {
            var clock = new TacticalDecisionClock(Definition(), 20); clock.Step(new SimulationTick(1));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= 10000; i++) clock.Step(new SimulationTick(i));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(clock.DecisionCount, Is.EqualTo(1500));
        }
    }
}

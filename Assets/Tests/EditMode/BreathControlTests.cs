using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class BreathControlTests
    {
        public static BreathControlDefinition Definition() => BreathControlDefinition.FromJson(PhysiologyContractTests.DefinitionJson("breath_control_v1.json"));
        private static BreathControlSystem Create(RaceEventLog log, ShootingStanceProfile stance = null) => new BreathControlSystem(Definition(), new AthleteId("breath"), 20, log, stance);
        [Test] public void HoldBenefitHasDebtReboundAndForcedReleaseWithRearm()
        {
            var log = new RaceEventLog(100); var s = Create(log);
            Assert.That(s.Step(new SimulationTick(1), true, 0).BreathingMultiplier, Is.LessThan(1));
            for (int i = 2; i <= 600; i++) s.Step(new SimulationTick(i), true, 0);
            Assert.That(s.LastFrame.Holding, Is.False); Assert.That(s.LastFrame.Blocked, Is.True);
            Assert.That(s.LastFrame.Debt, Is.Zero); Assert.That(s.LastFrame.BreathingMultiplier, Is.EqualTo(1));
            Assert.That(log.Count, Is.EqualTo(2)); Assert.That(log[1].EventType, Is.EqualTo(RaceEventType.BreathHoldEnded)); Assert.That(log[1].Payload, Is.EqualTo(1));
            s.Step(new SimulationTick(601), false, 0);
            Assert.That(s.Step(new SimulationTick(602), true, 0).Holding, Is.True);
            for (int i = 603; i <= 620; i++) s.Step(new SimulationTick(i), true, 0);
            Assert.That(s.Step(new SimulationTick(621), false, 0).BreathingMultiplier, Is.GreaterThan(1));
        }
        [Test] public void LongHoldIsWorseThanBriefHoldAndExpertStillPaysCost()
        {
            var novice = Create(new RaceEventLog(100)); var expert = Create(new RaceEventLog(100));
            double early = novice.Step(new SimulationTick(1), true, 0).BreathingMultiplier;
            expert.Step(new SimulationTick(1), true, 100);
            for (int i = 2; i <= 120; i++) { novice.Step(new SimulationTick(i), true, 0); expert.Step(new SimulationTick(i), true, 100); }
            Assert.That(novice.LastFrame.BreathingMultiplier, Is.GreaterThan(1));
            Assert.That(novice.LastFrame.BreathingMultiplier, Is.GreaterThan(early));
            Assert.That(expert.LastFrame.Debt, Is.InRange(0.01, novice.LastFrame.Debt));
        }
        [Test] public void ProneRecoversFasterAndCompositionChangesOnlyBreathing()
        {
            var stances = ShootingStanceTests.Definition(); var prone = Create(new RaceEventLog(100), stances.Prone); var standing = Create(new RaceEventLog(100), stances.Standing);
            for (int i = 1; i <= 40; i++) { prone.Step(new SimulationTick(i), true, 0); standing.Step(new SimulationTick(i), true, 0); }
            prone.Step(new SimulationTick(41), false, 0); standing.Step(new SimulationTick(41), false, 0);
            Assert.That(prone.LastFrame.Debt, Is.LessThan(standing.LastFrame.Debt));
            var breath = new BreathingFrame(0.25, 0.2, 0.001, 0.001); var heart = new HeartbeatFrame(0, 0, false, 1, 0.001, 0.001);
            var neutral = ShootingSwaySystem.Compose(stances.Prone, breath, heart, default);
            var held = ShootingSwaySystem.Compose(stances.Prone, breath, heart, default, 0.2);
            Assert.That(held.BreathingRadians, Is.LessThan(neutral.BreathingRadians)); Assert.That(held.HeartbeatRadians, Is.EqualTo(neutral.HeartbeatRadians));
        }
        [Test] public void ReplayEventsAtomicityAndActiveAllocationAreStable()
        {
            var la = new RaceEventLog(100); var lb = new RaceEventLog(100); var a = Create(la); var b = Create(lb);
            Assert.Catch(() => a.Step(new SimulationTick(1), true, 101));
            for (int i = 1; i <= 500; i++) Assert.That(a.Step(new SimulationTick(i), i % 200 < 100, 50), Is.EqualTo(b.Step(new SimulationTick(i), i % 200 < 100, 50)));
            Assert.That(la.Count, Is.EqualTo(lb.Count)); for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 501; i <= 1500; i++) a.Step(new SimulationTick(i), i % 200 < 100, 50);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

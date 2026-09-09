using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingHandlingTests
    {
        public static ShootingHandlingDefinition Definition() => ShootingHandlingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_handling_v1.json"));
        public static ShootingEquipmentDefinition Equipment() => ShootingEquipmentDefinition.FromJson(PhysiologyContractTests.DefinitionJson("rifle_neutral_v1.json"));
        [Test] public void SkillChangesActualDischargeTimingButNotValidGeometry()
        {
            long[] secondShots = new long[2]; int slot = 0;
            foreach (int skill in new[] { 0, 100 })
            {
                var profile = new ShootingHandlingProfile(Definition(), Equipment(), skill);
                var s = new ShotResolutionSystem(ShootingStateTests.Definition(), TargetGeometryTests.Definition(), ShootingWindTests.Definition(),
                    ShootingStanceTests.Definition().Standing, new AthleteId("handling"), "range", 20, new RaceEventLog(100), handling: profile);
                s.Step(new SimulationTick(1), ShootingAction.EnterRange);
                s.Step(new SimulationTick(2), ShootingAction.AssignLane, new ShootingLaneAssignment("range", "lane", 1));
                for (int i = 3; i <= 500; i++)
                {
                    var f = s.Step(new SimulationTick(i), ShootingAction.Trigger, ray: new ShotRay(0, 0, 0, 0, 0, 1));
                    if (f.Lifecycle.ShotAccepted && s.ShotsFired == 1) Assert.That(f.Outcome, Is.EqualTo(ShotOutcome.TargetClosed));
                    if (f.Lifecycle.ShotAccepted && s.ShotsFired == 2) secondShots[slot] = i;
                }
                slot++;
            }
            Assert.That(secondShots[1], Is.LessThan(secondShots[0]));
        }
        [Test] public void RecoilIsPostShotBoundedAndExpertsSettleFaster()
        {
            var d = Definition(); var a = new ShootingHandlingSystem(d, Equipment(), 0, 20); var b = new ShootingHandlingSystem(d, Equipment(), 100, 20);
            Assert.That(a.LastFrame.VerticalRadians, Is.Zero);
            Assert.That(a.Step(new SimulationTick(1), true), Is.EqualTo(b.Step(new SimulationTick(1), true)));
            for (int i = 2; i <= 21; i++) { a.Step(new SimulationTick(i), false); b.Step(new SimulationTick(i), false); }
            Assert.That(b.LastFrame.VerticalRadians, Is.LessThan(a.LastFrame.VerticalRadians));
            Assert.That(a.LastFrame.VerticalRadians, Is.EqualTo(d.RecoilY * Math.Exp(-1 / d.NoviceSettleTau)).Within(1e-8));
            for (int i = 22; i <= 1000; i++) a.Step(new SimulationTick(i), true);
            Assert.That(a.LastFrame.VerticalRadians, Is.LessThanOrEqualTo(d.MaximumRecoil));
        }
        [Test] public void EquipmentEnvelopeReplayAtomicityAndAllocationsAreControlled()
        {
            var d = Definition(); var gear = ShootingEquipmentDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Equipment().ToJson(), "handling_multiplier", "0.5"));
            Assert.Catch(() => new ShootingHandlingProfile(d, gear, 50));
            var a = new ShootingHandlingSystem(d, Equipment(), 50, 20); var b = new ShootingHandlingSystem(d, Equipment(), 50, 20);
            Assert.Catch(() => a.Step(new SimulationTick(2), true));
            for (int i = 1; i <= 100; i++) Assert.That(a.Step(new SimulationTick(i), i % 20 == 0), Is.EqualTo(b.Step(new SimulationTick(i), i % 20 == 0)));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 101; i <= 10100; i++) a.Step(new SimulationTick(i), i % 20 == 0);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingStanceTests
    {
        public static ShootingStanceDefinition Definition() => ShootingStanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_stances_v1.json"));
        [Test] public void ProneReducesEverySwayComponentAndImprovesRecoveryContract()
        {
            var d = Definition(); var p = PhysiologyContractTests.Definition();
            var b = new BreathingSystem(BreathingTests.Definition(), p, 20).Step(new SimulationTick(1), p.MaxHrBpm, 1, 0);
            var h = new HeartbeatSystem(HeartbeatTests.Definition(), p, 20).Step(new SimulationTick(1), p.MaxHrBpm);
            var m = new MuscularSwayFrame(0.001, 0.001, 0.001);
            var prone = ShootingSwaySystem.Compose(d.Prone, b, h, m); var standing = ShootingSwaySystem.Compose(d.Standing, b, h, m);
            Assert.That(prone.HorizontalRadians, Is.LessThan(standing.HorizontalRadians));
            Assert.That(prone.VerticalRadians, Is.LessThan(standing.VerticalRadians));
            Assert.That(prone.BreathingRadians, Is.LessThan(standing.BreathingRadians));
            Assert.That(prone.HeartbeatRadians, Is.LessThan(standing.HeartbeatRadians));
            Assert.That(d.Prone.RecoveryMultiplier, Is.GreaterThan(d.Standing.RecoveryMultiplier));
        }
        [Test] public void StancesDriveActualSetupAndAimRate()
        {
            var d = Definition(); var athlete = new AthleteId("stance");
            foreach (var stance in new[] { d.Prone, d.Standing })
            {
                var s = new ShootingStateMachine(ShootingStateTests.Definition(), athlete, "range", 20, new RaceEventLog(100), stance: stance);
                s.Step(new SimulationTick(1), ShootingAction.EnterRange);
                var f = s.Step(new SimulationTick(2), ShootingAction.AssignLane, new ShootingLaneAssignment("range", "lane", 1));
                Assert.That(f.ReadyTick, Is.EqualTo(2 + System.Math.Ceiling(ShootingStateTests.Definition().SetupSeconds * 20 * stance.SetupMultiplier)));
                for (long i = 3; i < f.ReadyTick; i++) Assert.That(s.Step(new SimulationTick(i), ShootingAction.Trigger).ShotAccepted, Is.False);
                Assert.That(s.Step(new SimulationTick(f.ReadyTick), ShootingAction.Trigger).ShotAccepted, Is.True);
                var aim = new WeaponAimSystem(WeaponAimTests.Definition(), athlete, 20, stance: stance);
                var state = aim.Step(new WeaponAimCommand(new SimulationTick(1), athlete, 1000, 0));
                Assert.That(state.YawRadians, Is.EqualTo(WeaponAimTests.Definition().RateRadiansPerSecond / 20 * stance.AimRateMultiplier).Within(1e-9));
            }
        }
        [Test] public void InvalidIdentityAndInvertedAdvantageFail()
        {
            var d = Definition(); Assert.That(ShootingStanceDefinition.FromJson(d.ToJson()).Prone.Id, Is.EqualTo(d.Prone.Id));
            Assert.Catch(() => d.Get((ShootingStance)5));
            string json = d.ToJson().Replace("\"breathing_multiplier\":0.65", "\"breathing_multiplier\":4");
            Assert.Catch(() => ShootingStanceDefinition.FromJson(json));
        }
    }
}

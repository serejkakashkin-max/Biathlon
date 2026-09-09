using System;
using System.Diagnostics;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingMilestoneTests
    {
        public static string Benchmark()
        {
            var s = ShootingSessionTests.Create(new RaceEventLog(1000));
            ShootingSessionInput Input(ShootingAction action) => new ShootingSessionInput(
                new WeaponAimCommand(s.Tick.Next(), s.AthleteId, 0, 0), action,
                action == ShootingAction.AssignLane ? new ShootingLaneAssignment("training_range", "lane", 1) : default, false, 180, 0.7, 0.5);
            s.Step(Input(ShootingAction.EnterRange)); s.Step(Input(ShootingAction.AssignLane));
            for (int i = 0; i < 100; i++) s.Step(Input(ShootingAction.None));
            var watch = new Stopwatch(); long before = GC.GetAllocatedBytesForCurrentThread(); watch.Start();
            for (int i = 0; i < 100000; i++) s.Step(Input(ShootingAction.None));
            watch.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            return "{\"schema_version\":1,\"id\":\"m4_active_session_benchmark\",\"ticks\":100000,\"simulated_seconds\":5000,\"elapsed_ms\":" +
                watch.Elapsed.TotalMilliseconds.ToString("0.####", CultureInfo.InvariantCulture) + ",\"allocated_bytes\":" + allocated +
                ",\"final_yaw_rad\":" + s.LastFrame.ShotAim.YawRadians.ToString("R", CultureInfo.InvariantCulture) +
                ",\"final_pitch_rad\":" + s.LastFrame.ShotAim.PitchRadians.ToString("R", CultureInfo.InvariantCulture) + "}";
        }
        [Test] public void LongActiveSessionHasNoManagedAllocation()
        {
            string result = Benchmark(); TestContext.WriteLine(result);
            Assert.That(result, Does.Contain("\"allocated_bytes\":0"));
        }
        [Test] public void GeometricPlayerHitsNeverDependOnHandlingStatsOrSeed()
        {
            var targets = TargetGeometryTests.Definition(); var stance = ShootingStanceTests.Definition().Prone;
            foreach (int skill in new[] { 0, 50, 100 })
            {
                var profile = new ShootingHandlingProfile(ShootingHandlingTests.Definition(), ShootingHandlingTests.Equipment(), skill);
                var s = new ShotResolutionSystem(ShootingStateTests.Definition(), targets, ShootingWindTests.Definition(), stance,
                    new AthleteId("perfect_player"), "range", 20, new RaceEventLog(100), handling: profile);
                s.Step(new SimulationTick(1), ShootingAction.EnterRange);
                s.Step(new SimulationTick(2), ShootingAction.AssignLane, new ShootingLaneAssignment("range", "lane", 1));
                for (int i = 3; i <= 500 && s.Phase != ShootingPhase.Complete; i++)
                {
                    int target = Math.Min(4, s.ShotsFired);
                    var ray = new ShotRay(0, 0, 0, targets.CenterX(target), 0, targets.DistanceMeters);
                    s.Step(new SimulationTick(i), ShootingAction.Trigger, ray: ray);
                }
                Assert.That(s.ClosedMask, Is.EqualTo(31)); Assert.That(s.ShotsFired, Is.EqualTo(5));
            }
        }
    }
}

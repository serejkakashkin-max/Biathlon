using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShortBurstTests
    {
        private static PhysiologyDefinition Production() => PhysiologyDefinition.FromJson(
            PhysiologyContractTests.DefinitionJson("physiology_gameplay_v2.json"));
        private static PhysiologySession Session(PhysiologyDefinition d) => new PhysiologySession(
            new SimulationConfig(1, "short_burst_review_v1", 20, 4181), d,
            new AthleteId("burst_athlete"), PhysiologyState.Rested(d), new RaceEventLog());
        private static PhysiologyFrame Step(PhysiologySession session, int control) => session.Step(
            new PhysiologyInput(new SimulationInput(session.Clock.Tick.Next(), session.AthleteId, control), 1, 1));

        [Test]
        public void FullShiftGivesDistinctPowerAndSpendsUsefulReserveWithinTwentySeconds()
        {
            var d = Production();
            var s = Session(d);
            var normal = new EffortSystem(d).Evaluate(800, s.State, 1);
            var burst = Step(s, 1000);
            for (int i = 1; i < 400; i++) Step(s, 1000);
            TestContext.WriteLine(FormattableString.Invariant(
                $"normal_watts={normal.DeliveredPowerWatts:F2};burst_watts={burst.Effort.DeliveredPowerWatts:F2};reserve_after20s={s.State.AnaerobicReserve:F4};effort_after20s={s.LastFrame.Effort.DeliveredRelativeEffort:F3}"));
            Assert.That(burst.Effort.DeliveredPowerWatts, Is.GreaterThan(normal.DeliveredPowerWatts * 1.45),
                "Shift must produce a distinct physical power surge, not another almost identical pace setting.");
            Assert.That(s.State.AnaerobicReserve, Is.LessThan(0.2),
                "A full burst must spend most of the real short-term reserve in seconds, not last for several minutes.");
        }

        [Test]
        public void OrdinaryRaceAndFreshClimbControlsRetainTheirMeaning()
        {
            var d = Production();
            var effort = new EffortSystem(d);
            foreach (int control in new[] { 0, 350, 600, 780, 800, 835, 900, 919, 930, 950 })
                Assert.That(effort.Evaluate(control, PhysiologyState.Rested(d), 1).DeliveredRelativeEffort,
                    Is.EqualTo(control * 1.3 / 1000).Within(1e-12), "Normal/terrain-adjusted control " + control);
            double previous = -1;
            for (int control = 0; control <= 1000; control++)
            {
                double next = effort.Evaluate(control, PhysiologyState.Rested(d), 1).RelativeEffort;
                Assert.That(next, Is.GreaterThan(previous));
                previous = next;
            }
        }

        [TestCase(30)]
        [TestCase(50)]
        [TestCase(80)]
        public void BurstDurationUsesIndividualCapacityAndPredictsFullPowerBoundary(int capacity)
        {
            var d = PhysiologyStatTests.Curves().Apply(Production(), new AthletePhysiologyStats(50, capacity, 50, 50));
            var s = Session(d);
            double predicted = ShortBurstTelemetry.Evaluate(d, s.State, 1000).FullPowerSecondsRemaining;
            int ticks = 0;
            while (s.State.AnaerobicReserve > d.BurstProfile.FullPowerReserveFraction && ticks < 1000)
            { Step(s, 1000); ticks++; }
            Assert.That(ticks / 20.0, Is.EqualTo(predicted).Within(0.051));
            Assert.That(predicted, Is.InRange(10, 22));
            Assert.That(ShortBurstTelemetry.Evaluate(d, s.State, 1000).FullPowerSecondsRemaining, Is.Zero);
            double fade = Step(s, 1000).Effort.DeliveredRelativeEffort;
            Assert.That(fade, Is.LessThan(d.MaximumRelativeEffort));
            Assert.That(fade, Is.GreaterThan(d.AerobicThreshold));
        }

        [Test]
        public void AttackHasPersistentFatigueAndSlowRecoveryRatherThanAFreeRepeatedBoost()
        {
            var s = Session(Production());
            for (int i = 0; i < 1200; i++) Step(s, 800);
            var normal = s.State;
            for (int i = 0; i < 400; i++) Step(s, 1000);
            var after = s.State;
            Assert.That(after.HeartRateBpm, Is.GreaterThan(normal.HeartRateBpm));
            Assert.That(after.MuscularFatigue, Is.GreaterThan(normal.MuscularFatigue));
            for (int i = 0; i < 200; i++) Step(s, 350);
            Assert.That(s.State.AnaerobicReserve, Is.LessThan(after.AnaerobicReserve + 0.04),
                "Ten seconds of easing off cannot reset the burst resource.");
            for (int i = 0; i < 1000; i++) Step(s, 350);
            Assert.That(s.State.AnaerobicReserve, Is.LessThan(0.5));
            Assert.That(s.State.AnaerobicReserve, Is.GreaterThan(after.AnaerobicReserve));
            Assert.That(s.State.MuscularFatigue, Is.GreaterThan(normal.MuscularFatigue));
        }

        [Test]
        public void ShiftReleaseRestoresTheChosenDialWithoutResettingEnergy()
        {
            var d = Production();
            var s = Session(d);
            var builder = new DeviceCommandBuilder(DeviceControlDefinition.FromJson(
                PhysiologyContractTests.DefinitionJson("device_control_gameplay_v3.json")), s.AthleteId, 20);
            var control = new EffortControlSystem(EffortControlTests.Definition(), s.AthleteId, 20);
            builder.Sample(new DeviceControlSample(false, 1, 0));
            for (int i = 0; i < 32; i++)
            {
                var command = builder.Build(s.Clock.Tick.Next());
                Step(s, control.Step(command, true).AppliedControl);
            }
            int dial = builder.Build(s.Clock.Tick.Next()).AnalogEffort;
            // Consume this neutral held sample at the same simulation boundary.
            var held = new PlayerCommand(s.Clock.Tick.Next(), s.AthleteId, dial, 0);
            Step(s, control.Step(held, true).AppliedControl);
            builder.Sample(new DeviceControlSample(false, 0, 0, EffortPreset.Attack));
            for (int i = 0; i < 200; i++)
            {
                var command = builder.Build(s.Clock.Tick.Next());
                Step(s, control.Step(command, true).AppliedControl);
            }
            double spent = s.State.AnaerobicReserve;
            builder.Sample(new DeviceControlSample(false, 0, 0));
            PlayerCommand released = default;
            for (int i = 0; i < 10; i++)
            {
                released = builder.Build(s.Clock.Tick.Next());
                Step(s, control.Step(released, true).AppliedControl);
            }
            Assert.That(released.AnalogEffort, Is.EqualTo(dial));
            Assert.That(control.ContinuousControl, Is.EqualTo(control.AnalogTarget(dial)));
            Assert.That(s.State.AnaerobicReserve, Is.LessThanOrEqualTo(spent));
            Assert.That(s.LastFrame.Effort.RelativeEffort, Is.LessThan(1.1));
        }

        [Test]
        public void SharedPowerCausesActualAccelerationAndDissipatesWhenReleased()
        {
            var d = Production();
            var normal = Session(d);
            var burst = Session(d);
            var physics = new SkiForceSystem(SkiForceTests.Definition(), 20);
            var flat = new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long"))
                .Sample(TrackDistance.FromMeters(100));
            var normalMotion = new SkiState(0, 8);
            var burstMotion = normalMotion;
            for (int i = 0; i < 160; i++)
            {
                normalMotion = physics.Step(normalMotion, Step(normal, 800).Effort.DeliveredPowerWatts, flat).State;
                burstMotion = physics.Step(burstMotion, Step(burst, 1000).Effort.DeliveredPowerWatts, flat).State;
            }
            TestContext.WriteLine(FormattableString.Invariant($"normal_speed8s={normalMotion.SpeedMetersPerSecond:F3};burst_speed8s={burstMotion.SpeedMetersPerSecond:F3};reserve8s={burst.State.AnaerobicReserve:F4}"));
            Assert.That(burstMotion.SpeedMetersPerSecond - normalMotion.SpeedMetersPerSecond, Is.GreaterThan(1));
            double peak = burstMotion.SpeedMetersPerSecond;
            for (int i = 0; i < 400; i++)
                burstMotion = physics.Step(burstMotion, Step(burst, 800).Effort.DeliveredPowerWatts, flat).State;
            Assert.That(burstMotion.SpeedMetersPerSecond, Is.LessThan(peak), "Momentum must dissipate through shared ski forces; no permanent speed bonus.");
        }

        [Test]
        public void TelemetryIsFiniteAllocationFreeAndDoesNotInventLegacyCapability()
        {
            var d = Production();
            var s = Session(d);
            var power = Step(s, 1000).Effort;
            var initial = ShortBurstTelemetry.Evaluate(d, s.State, power, true);
            Assert.That(initial.Enabled && initial.Active, Is.True);
            Assert.That(initial.PowerFraction, Is.EqualTo(1));
            long before = GC.GetAllocatedBytesForCurrentThread();
            double sum = 0;
            for (int i = 0; i < 10000; i++)
                sum += ShortBurstTelemetry.Evaluate(d, s.State, power, true).FullPowerSecondsRemaining;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero);
            Assert.That(double.IsInfinity(sum) || double.IsNaN(sum), Is.False);
            Assert.That(ShortBurstTelemetry.Evaluate(d, s.State, power, false).Active, Is.False);
            var empty = new PhysiologyState(d.RestingHrBpm, 0, 0, 0);
            var depleted = ShortBurstTelemetry.Evaluate(d, empty, 1000);
            Assert.That(depleted.PowerFraction, Is.Zero);
            Assert.That(depleted.FullPowerSecondsRemaining, Is.Zero);
            Assert.That(ShortBurstTelemetry.Evaluate(d, s.State, 1000, 0).Active, Is.False);
            var legacy = PhysiologyContractTests.Definition();
            Assert.That(ShortBurstTelemetry.Evaluate(legacy, PhysiologyState.Rested(legacy), 1000).Enabled, Is.False);
        }

        [Test]
        public void ProductionBurstReplayRepeatsAndOptionalLegacyDefinitionRoundTrips()
        {
            string Trace()
            {
                var s = Session(Production());
                using (var hash = new PhysiologyTraceFingerprint(s))
                {
                    for (int i = 0; i < 2000; i++)
                    {
                        var input = new PhysiologyInput(new SimulationInput(s.Clock.Tick.Next(), s.AthleteId,
                            i % 800 < 300 ? 1000 : 600), 0.96, 1);
                        hash.Append(input, s.Step(input));
                    }
                    return hash.Complete();
                }
            }
            Assert.That(Trace(), Is.EqualTo(Trace()));
            var legacy = PhysiologyContractTests.Definition();
            Assert.That(legacy.ToJson(), Does.Not.Contain("burst_profile"));
            var d = Production();
            Assert.That(PhysiologyDefinition.FromJson(d.ToJson()).ToJson(), Is.EqualTo(d.ToJson()));
            Assert.Catch(() => PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(), "activation_control", "1000")));
            Assert.Catch(() => PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(), "full_power_reserve_fraction", "0")));
            Assert.Catch(() => PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(), "normal_maximum_relative_effort", "1.7")));
        }
    }
}

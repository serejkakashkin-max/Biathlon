using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiSteeringTests
    {
        private const string DefinitionJson = "{\"schema_version\":1,\"id\":\"manual_ski_test_v1\","
            + "\"maximum_yaw_rate_radps\":1.2,\"yaw_acceleration_radps2\":2.4,"
            + "\"neutral_yaw_deceleration_radps2\":3,\"high_speed_reference_mps\":12,"
            + "\"high_speed_response_multiplier\":0.55,\"minimum_speed_for_grip_limit_mps\":0.5,"
            + "\"path_integration_substeps\":8}";
        private static ManualSkiSteeringDefinition Definition() => ManualSkiSteeringDefinition.FromJson(DefinitionJson);
        private static ManualSkiSteeringSystem System(int frequency = 20) => new ManualSkiSteeringSystem(Definition(), frequency);
        private static ManualSkiCommand Command(long tick = 1, int steering = 0) => new ManualSkiCommand(
            new SimulationTick(tick), new AthleteId("manual_player"), 723, steering, EffortPreset.Attack,
            true, true, false, "skate_climb");

        [Test]
        public void ExplicitV2RoundTripPreservesLargeTickAndRejectsLegacyStream()
        {
            var command = Command(9007199254740993, -431);
            string json = command.ToJson();
            var restored = ManualSkiCommand.FromJson(json);
            Assert.That(restored, Is.EqualTo(command));
            Assert.That(restored.Tick.Value, Is.EqualTo(9007199254740993));
            Assert.That(restored.ToJson(), Is.EqualTo(json));
            Assert.That(json, Does.Contain("\"steering_input\":-431"));
            Assert.That(json, Does.Not.Contain("line_intent"));
            Assert.That(restored.ToEffortCommand().LineIntent, Is.Zero);
            Assert.That(restored.ToEffortCommand().AnalogEffort, Is.EqualTo(723));
            Assert.That(restored.ToEffortCommand().TuckHeld, Is.True);
            Assert.Catch(() => PlayerCommand.FromJson(json));
            var legacy = new PlayerCommand(new SimulationTick(1), command.AthleteId, 723, -431);
            string legacyJson = legacy.ToJson();
            Assert.Catch(() => ManualSkiCommand.FromJson(legacyJson));
            Assert.That(PlayerCommand.FromJson(legacyJson).ToJson(), Is.EqualTo(legacyJson));
            Assert.That(legacyJson, Does.Contain("\"schema_version\":1"));
            Assert.That(legacy.LineIntent, Is.EqualTo(-431));
        }

        [Test]
        public void RequiredV2IntentAndTuningFieldsRejectMissingInvalidOrAmbiguousData()
        {
            string json = Command().ToJson();
            foreach (string field in new[] { "schema_version", "id", "target_tick", "athlete_id", "analog_effort",
                "steering_input", "effort_preset", "tuck_held", "overtake_requested", "cancel_overtake", "technique_id" })
                Assert.Catch(() => ManualSkiCommand.FromJson(json.Replace("\"" + field + "\"", "\"missing_" + field + "\"")), field);
            foreach (var pair in new[] { ("schema_version", "1"), ("target_tick", "0"), ("analog_effort", "1001"),
                ("steering_input", "1001"), ("effort_preset", "5") })
                Assert.Catch(() => ManualSkiCommand.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair.Item1, pair.Item2)));
            Assert.Catch(() => new ManualSkiCommand(new SimulationTick(1), new AthleteId("a"), 1, 0,
                overtakeRequested: true, cancelOvertake: true));
            Assert.Catch(() => default(ManualSkiCommand).ToJson());
            Assert.Catch(() => default(ManualSkiCommand).ToEffortCommand());
            Assert.That(ManualSkiSteeringDefinition.FromJson(Definition().ToJson()).ToJson(), Is.EqualTo(Definition().ToJson()));
            foreach (var pair in new[] { ("schema_version", "2"), ("maximum_yaw_rate_radps", "0"),
                ("yaw_acceleration_radps2", "0"), ("neutral_yaw_deceleration_radps2", "0"),
                ("high_speed_reference_mps", "0"), ("high_speed_response_multiplier", "1.1"),
                ("minimum_speed_for_grip_limit_mps", "0"), ("path_integration_substeps", "0"),
                ("path_integration_substeps", "1025") })
                Assert.Catch(() => ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(DefinitionJson, pair.Item1, pair.Item2)));
            Assert.Catch(() => ManualSkiSteeringDefinition.FromJson(DefinitionJson.Replace("\"yaw_acceleration_radps2\"", "\"missing\"")));
        }

        [Test]
        public void NeutralInputKeepsAbsoluteWorldDirectionAndDoesNotSeekCentre()
        {
            var system = System();
            var state = new ManualSkiHeadingState(2.3, -8, 0.37);
            var initial = state;
            for (int i = 0; i < 100; i++) state = system.Preview(state, 0, 0.3, 6, 4).State;
            Assert.That(state.HeadingNanoradians, Is.EqualTo(initial.HeadingNanoradians));
            Assert.That(state.YawRateRadiansPerSecond, Is.Zero);
            Assert.That(state.X, Is.EqualTo(initial.X + 30 * Math.Sin(initial.HeadingRadians)).Within(6e-8));
            Assert.That(state.Z, Is.EqualTo(initial.Z + 30 * Math.Cos(initial.HeadingRadians)).Within(6e-8));
            Assert.That(state.X, Is.GreaterThan(initial.X), "Neutral input cannot pull an off-centre athlete toward an ideal line.");
        }

        [Test]
        public void LeftRightRequestsChangeTravelDirectionSymmetricallyWithAngularAcceleration()
        {
            var system = System();
            var initial = new ManualSkiHeadingState(0, 0, 0);
            var right = system.Preview(initial, 1000, 0.3, 6, 4);
            var left = system.Preview(initial, -1000, 0.3, 6, 4);
            Assert.That(right.State.X, Is.GreaterThan(0));
            Assert.That(left.State.X, Is.EqualTo(-right.State.X).Within(1e-9));
            Assert.That(left.State.Z, Is.EqualTo(right.State.Z).Within(1e-9));
            Assert.That(right.HeadingDeltaRadians, Is.EqualTo(-left.HeadingDeltaRadians));
            Assert.That(right.State.YawRateRadiansPerSecond, Is.LessThan(right.TargetYawRate));
            Assert.That(right.State.YawRateRadiansPerSecond, Is.EqualTo(2.4 * 0.775 / 20).Within(1e-9));
            Assert.That(right.HeadingDeltaRadians, Is.EqualTo(0.5 * 2.4 * 0.775 / (20 * 20)).Within(1e-12));
            var still = system.Preview(initial, 1000, 0, 0, 4);
            Assert.That(still.State.X, Is.Zero); Assert.That(still.State.Z, Is.Zero);
            Assert.That(still.HeadingDeltaRadians, Is.GreaterThan(0), "Standing rotation must not fabricate travel.");
        }

        [Test]
        public void ReleaseDampsYawWithoutSnappingHeadingAndStopsAtExactSubtick()
        {
            var system = System();
            var initial = new ManualSkiHeadingState(5, 8, 0.4, 0.09);
            var output = system.Preview(initial, 0, 0.4, 8, 4);
            Assert.That(output.State.YawRateRadiansPerSecond, Is.Zero);
            Assert.That(output.HeadingDeltaRadians, Is.EqualTo(0.09 * 0.03 / 2).Within(1e-12));
            Assert.That(output.Sample(0.3).YawRateRadiansPerSecond, Is.EqualTo(0.045).Within(1e-9));
            Assert.That(output.Sample(0.8).HeadingNanoradians, Is.EqualTo(output.State.HeadingNanoradians));
            Assert.That(output.Sample(0.8).YawRateRadiansPerSecond, Is.Zero);
            var next = system.Preview(output.State, 0, 0.4, 8, 4, postureResponse: 0);
            Assert.That(next.State.HeadingNanoradians, Is.EqualTo(output.State.HeadingNanoradians));
            Assert.That(next.State.X, Is.GreaterThan(output.State.X));
        }

        [Test]
        public void HigherSpeedTuckAndGripBoundRequestsUsingTheSameActorIndependentKernel()
        {
            var system = System(); var initial = new ManualSkiHeadingState(0, 0, 0);
            var slow = system.Preview(initial, 1000, 0.1, 2, 4);
            var fast = system.Preview(initial, 1000, 0.6, 12, 4);
            var tuck = system.Preview(initial, 1000, 0.1, 2, 4, 0.75);
            Assert.That(fast.State.YawRateRadiansPerSecond, Is.LessThan(slow.State.YawRateRadiansPerSecond));
            Assert.That(tuck.State.YawRateRadiansPerSecond, Is.LessThan(slow.State.YawRateRadiansPerSecond));
            var state = initial; ManualSkiHeadingFrame limited = default;
            for (int i = 0; i < 100; i++) { limited = system.Preview(state, 1000, 0.5, 10, 0.7); state = limited.State; }
            Assert.That(limited.RequestLimited, Is.True);
            Assert.That(limited.ExceedsGrip, Is.False);
            Assert.That(state.YawRateRadiansPerSecond, Is.EqualTo(0.07).Within(1e-9));
            Assert.That(limited.LateralAccelerationDemand, Is.EqualTo(0.7).Within(1e-9));
            var player = system.Preview(initial, Command(steering: -500), 0.1, 2, 4);
            var ai = system.Preview(initial, -500, 0.1, 2, 4);
            Assert.That(player.State, Is.EqualTo(ai.State));
        }

        [Test]
        public void GripDropReportsExistingMomentumInsteadOfSnappingDirectionOrHidingCost()
        {
            var system = System(); var state = new ManualSkiHeadingState(0, 0, 0.2, 0.6);
            var result = system.Preview(state, 1000, 0.5, 10, 0);
            Assert.That(result.TargetYawRate, Is.Zero);
            Assert.That(result.State.YawRateRadiansPerSecond, Is.GreaterThan(0));
            Assert.That(result.State.YawRateRadiansPerSecond, Is.LessThan(state.YawRateRadiansPerSecond));
            Assert.That(result.ExceedsGrip, Is.True);
            Assert.That(result.LateralAccelerationDemand, Is.EqualTo(6));
            Assert.That(result.HeadingDeltaRadians, Is.GreaterThan(0));
            Assert.That(system.Preview(state, 0, 0.5, 10, 0, 0).State.YawRateRadiansPerSecond,
                Is.EqualTo(0.45), "Passive neutral damping is retained when posture disables active steering.");
        }

        [Test]
        public void ConstantRateTrajectoryMatchesCircularArcAndSamplesPaidPrefix()
        {
            var system = System(); var initial = new ManualSkiHeadingState(0, 0, 0, 0.66);
            var result = system.Preview(initial, 1000, 0.6, 12, 100);
            Assert.That(result.TargetYawRate, Is.EqualTo(0.66).Within(1e-9));
            double angle = 0.66 / 20, radius = 12 / 0.66;
            Assert.That(result.State.X, Is.EqualTo(radius * (1 - Math.Cos(angle))).Within(1e-9));
            Assert.That(result.State.Z, Is.EqualTo(radius * Math.Sin(angle)).Within(1e-9));
            var half = result.Sample(0.5);
            Assert.That(half.X, Is.EqualTo(radius * (1 - Math.Cos(angle / 2))).Within(1e-9));
            Assert.That(half.Z, Is.EqualTo(radius * Math.Sin(angle / 2)).Within(1e-9));
            Assert.That(result.Sample(0), Is.EqualTo(initial));
            Assert.That(result.Sample(1), Is.EqualTo(result.State));
            Assert.That(Math.Sqrt(result.State.X * result.State.X + result.State.Z * result.State.Z), Is.LessThan(result.PlanarTravelDistanceMeters));
        }

        [Test]
        public void HeadingWrapIsCanonicalAndOppositeWorldHeadingDoesNotBecomeForwardRouteTravel()
        {
            Assert.That(new ManualSkiHeadingState(0, 0, 2 * Math.PI).HeadingNanoradians, Is.Zero);
            Assert.That(new ManualSkiHeadingState(0, 0, -0.1).HeadingNanoradians,
                Is.EqualTo(ManualSkiHeadingState.FullTurnNanoradians - 100000000));
            var result = System().Preview(new ManualSkiHeadingState(0, 0, Math.PI), 0, 0.5, 10, 4);
            Assert.That(result.State.Z, Is.EqualTo(-0.5).Within(1e-9));
            Assert.That(result.State.X, Is.EqualTo(0).Within(1e-9));
            // This kernel emits world movement only. The route owner must account for backward
            // projection explicitly; no positive route progress is invented here.
        }

        [Test]
        public void InvalidInputsFailBeforeChangingTheImmutablePose()
        {
            var system = System(); var state = new ManualSkiHeadingState(1, 2, 0.3);
            Assert.That(default(ManualSkiHeadingState).IsValid, Is.False);
            Assert.Catch(() => system.Preview(default(ManualSkiHeadingState), 0, 0.1, 2, 4));
            Assert.Catch(() => default(ManualSkiHeadingFrame).Sample(0));
            Assert.Catch(() => system.Preview(state, default(ManualSkiCommand), 0.1, 2, 4));
            Assert.Catch(() => system.Preview(state, 1001, 0.1, 2, 4));
            Assert.Catch(() => system.Preview(state, 0, double.NaN, 2, 4));
            Assert.Catch(() => system.Preview(state, 0, -0.1, 2, 4));
            Assert.Catch(() => system.Preview(state, 0, 0.1, double.PositiveInfinity, 4));
            Assert.Catch(() => system.Preview(state, 0, 0.1, 2, -1));
            Assert.Catch(() => system.Preview(state, 0, 0.1, 2, 4, 1.1));
            Assert.Catch(() => system.Preview(new ManualSkiHeadingState(1, 2, 0.3, 1.3), 0, 0.1, 2, 4));
            Assert.Catch(() => new ManualSkiHeadingState(double.NaN, 0, 0));
            Assert.Catch(() => new ManualSkiHeadingState(0, 0, double.PositiveInfinity));
            Assert.Catch(() => new ManualSkiSteeringSystem(Definition(), 0));
            var frame = system.Preview(state, 0, 0.1, 2, 4);
            Assert.Catch(() => frame.Sample(-0.01)); Assert.Catch(() => frame.Sample(double.NaN));
            Assert.That(state.X, Is.EqualTo(1)); Assert.That(state.Z, Is.EqualTo(2));
        }

        [Test]
        public void LongFixedCommandStreamRepeatsExactlyWithoutHotLoopAllocation()
        {
            var system = System(); var a = new ManualSkiHeadingState(0, 0, 0.37); var b = a;
            system.Preview(a, 500, 0.3, 6, 4).Sample(0.3);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++)
            {
                int intent = i % 90 < 25 ? 700 : i % 90 < 50 ? -700 : 0;
                var first = system.Preview(a, intent, 0.3, 6, 4);
                var second = system.Preview(b, intent, 0.3, 6, 4);
                a = first.State; b = second.State;
                // The sweep sampler is part of the hot path as well.
                first.Sample(0.33); second.Sample(0.33);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(a, Is.EqualTo(b));
            TestContext.WriteLine(FormattableString.Invariant($"manual_heading_probe={a.X:R}:{a.Z:R}:{a.HeadingNanoradians}:{a.YawRateRadiansPerSecond:R}"));
        }
    }
}

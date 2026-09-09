using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class GlobalShootingClockTests
    {
        private static ShootingSession Session(RaceEventLog log, long origin, PhysiologyDefinition physical = null) => new ShootingSession(
            new SimulationConfig(1, "shooting_training_v1", 20, 123), ShootingSessionTests.Model(), ShootingSessionTests.Athlete(),
            ShootingStance.Prone, "training_range", log, new SimulationTick(origin), physical);
        private static ShootingSessionInput Shift(ShootingSessionInput input, long delta, double? hr = null) => new ShootingSessionInput(
            new WeaponAimCommand(new SimulationTick(input.Aim.Tick.Value + delta), input.Aim.AthleteId, input.Aim.Horizontal, input.Aim.Vertical, input.Aim.FineControl),
            input.Action, input.Lane, input.HoldBreath, hr ?? input.HeartRateBpm, input.Fatigue, input.ArmFatigue,
            input.Wind, input.HeadingRadians, input.Exposure, input.Pressure);
        [Test] public void CompleteShiftedBoutHasIdenticalGeometryAndDurationsWithGlobalEventsAndReplay()
        {
            const long origin = 12345;
            var zeroLog = new RaceEventLog(1000); var shiftedLog = new RaceEventLog(1000); var replayLog = new RaceEventLog(1000);
            var zero = Session(zeroLog, 0); var shifted = Session(shiftedLog, origin); var replay = Session(replayLog, origin);
            var tape = new ShootingCommandTape("global", shifted.ContextJson, shifted.AthleteId, 1000, shifted.InitialTick);
            var frames = new ShootingSessionFrame[1000];
            while (zero.Phase != ShootingPhase.Complete)
            {
                var input = ShootingSessionTests.Script(zero); var expected = zero.Step(input);
                var actual = tape.StepAndRecord(shifted, Shift(input, origin)); frames[tape.Count - 1] = actual;
                Assert.That(actual.Aim, Is.EqualTo(expected.Aim)); Assert.That(actual.Breathing, Is.EqualTo(expected.Breathing));
                Assert.That(actual.Heartbeat, Is.EqualTo(expected.Heartbeat)); Assert.That(actual.Muscular, Is.EqualTo(expected.Muscular));
                Assert.That(actual.BreathControl, Is.EqualTo(expected.BreathControl)); Assert.That(actual.Pressure, Is.EqualTo(expected.Pressure));
                Assert.That(actual.Sway, Is.EqualTo(expected.Sway)); Assert.That(actual.Handling, Is.EqualTo(expected.Handling));
                Assert.That(actual.ShotAim, Is.EqualTo(expected.ShotAim)); Assert.That(actual.Shot.Impact, Is.EqualTo(expected.Shot.Impact));
                Assert.That(actual.Shot.ClosedMask, Is.EqualTo(expected.Shot.ClosedMask)); Assert.That(actual.Shot.Lifecycle.Phase, Is.EqualTo(expected.Shot.Lifecycle.Phase));
                if (expected.Shot.Lifecycle.ReadyTick > 0) Assert.That(actual.Shot.Lifecycle.ReadyTick, Is.EqualTo(expected.Shot.Lifecycle.ReadyTick + origin));
            }
            Assert.That(shifted.Tick.Value, Is.EqualTo(zero.Tick.Value + origin)); Assert.That(shiftedLog.Count, Is.EqualTo(zeroLog.Count));
            for (int i = 0; i < zeroLog.Count; i++)
            { Assert.That(shiftedLog[i].Tick.Value, Is.EqualTo(zeroLog[i].Tick.Value + origin));
              Assert.That(shiftedLog[i].EventType, Is.EqualTo(zeroLog[i].EventType)); Assert.That(shiftedLog[i].Payload, Is.EqualTo(zeroLog[i].Payload)); }
            var restored = ShootingCommandTape.FromJson(tape.ToJson()); Assert.That(restored.InitialTick.Value, Is.EqualTo(origin));
            for (int i = 0; i < restored.Count; i++) Assert.That(restored.ReplayNext(replay), Is.EqualTo(frames[i]));
            Assert.That(replayLog.Count, Is.EqualTo(shiftedLog.Count)); for (int i = 0; i < replayLog.Count; i++) Assert.That(replayLog[i], Is.EqualTo(shiftedLog[i]));
        }
        [Test] public void OriginAndPhysiologyMismatchCannotPartlyAdvanceOrRecord()
        {
            var log = new RaceEventLog(100); var s = Session(log, 1000);
            var tape = new ShootingCommandTape("wrong", s.ContextJson, s.AthleteId, 10);
            var zero = Session(new RaceEventLog(100), 0); var input = Shift(ShootingSessionTests.Script(zero), 1000);
            Assert.Catch(() => tape.StepAndRecord(s, input)); Assert.That(s.Tick.Value, Is.EqualTo(1000)); Assert.That(log.Count, Is.Zero);
            var valid = new ShootingCommandTape("right", s.ContextJson, s.AthleteId, 10, s.InitialTick); valid.StepAndRecord(s, input);
            Assert.Catch(() => ShootingCommandTape.FromJson(PhysiologyContractTests.ReplaceNumber(valid.ToJson(), "initial_tick", "999")));
            var model = ShootingSessionTests.Model();
            var physical = PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(model.Physiology.ToJson(), "resting_hr_bpm", "60"));
            var custom = Session(new RaceEventLog(100), 1000, physical);
            Assert.That(custom.ContextJson, Is.Not.EqualTo(Session(new RaceEventLog(100), 1000).ContextJson));
            Assert.Catch(() => valid.ReplayNext(custom));
            Assert.Catch(() => custom.Step(Shift(input, 0, 57))); Assert.That(custom.Tick.Value, Is.EqualTo(1000));
            custom.Step(Shift(input, 0, 60)); Assert.That(custom.Tick.Value, Is.EqualTo(1001));
        }
        [Test] public void AiDecisionsRemainIdenticalUnderClockTranslation()
        {
            var model = ShootingSessionTests.Model(); var athlete = new AthleteId("ai");
            var a = new ShootingAiController(ShootingAiTests.Definition(), model.Targets, model.Aim, model.Stances.Prone, athlete, 20, new SeededRandomSource(5));
            var b = new ShootingAiController(ShootingAiTests.Definition(), model.Targets, model.Aim, model.Stances.Prone, athlete, 20, new SeededRandomSource(5), new SimulationTick(7000));
            for (int tick = 1; tick <= 2000; tick++)
            {
                var observation = new ShootingAiObservation(ShootingPhase.Aiming, 0, 0, 0, 0);
                var x = a.Step(new SimulationTick(tick), observation); var y = b.Step(new SimulationTick(tick + 7000), observation);
                Assert.That(y.Aim.Horizontal, Is.EqualTo(x.Aim.Horizontal)); Assert.That(y.Aim.Vertical, Is.EqualTo(x.Aim.Vertical));
                Assert.That(y.Trigger, Is.EqualTo(x.Trigger)); Assert.That(y.DecisionMade, Is.EqualTo(x.DecisionMade));
            }
        }
    }
}

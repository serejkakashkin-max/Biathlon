using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ShootingSessionTests
    {
        public static ShootingModelDefinitions Model() => new ShootingModelDefinitions(PhysiologyContractTests.DefinitionJson);
        public static ShootingAthleteDefinition Athlete() => ShootingAthleteDefinition.FromJson(PhysiologyContractTests.DefinitionJson("shooting_athlete_v1.json"));
        public static ShootingSession Create(RaceEventLog log, ulong seed = 123, ShootingStance stance = ShootingStance.Prone) =>
            new ShootingSession(new SimulationConfig(1, "shooting_training_v1", 20, seed), Model(), Athlete(), stance, "training_range", log);
        public static ShootingSessionInput Script(ShootingSession s)
        {
            long tick = s.Tick.Value + 1;
            var action = tick == 1 ? ShootingAction.EnterRange : tick == 2 ? ShootingAction.AssignLane : ShootingAction.Trigger;
            return new ShootingSessionInput(new WeaponAimCommand(new SimulationTick(tick), s.AthleteId, tick % 200 < 100 ? -1 : 1, 0, true), action,
                tick == 2 ? new ShootingLaneAssignment("training_range", "training_lane", 1) : default, tick % 100 < 50,
                150, 0.3, 0.2, new WindVector(1, 0), 0, 0.8, new ShootingPressureContext(0.5, 0.8, 0.7, 0.6, true));
        }
        [Test] public void EntireComposedBoutSerializesAndReplaysAllFramesEventsAndCsv()
        {
            var la = new RaceEventLog(1000); var lb = new RaceEventLog(1000); var a = Create(la); var b = Create(lb);
            var tape = new ShootingCommandTape("training", a.ContextJson, a.AthleteId, 10000);
            var frames = new ShootingSessionFrame[10000]; var csvA = new StringWriter(); var csvB = new StringWriter();
            while (a.Phase != ShootingPhase.Complete && tape.Count < 10000)
            {
                var f = tape.StepAndRecord(a, Script(a)); frames[tape.Count - 1] = f; ShootingCsv.WriteFrame(csvA, f);
            }
            Assert.That(a.Phase, Is.EqualTo(ShootingPhase.Complete)); Assert.That(a.ShotsFired, Is.EqualTo(5));
            var restored = ShootingCommandTape.FromJson(tape.ToJson());
            for (int i = 0; i < restored.Count; i++)
            { var frame = restored.ReplayNext(b); Assert.That(frame, Is.EqualTo(frames[i])); ShootingCsv.WriteFrame(csvB, frame); }
            Assert.That(csvB.ToString(), Is.EqualTo(csvA.ToString())); Assert.That(lb.Count, Is.EqualTo(la.Count));
            for (int i = 0; i < la.Count; i++) Assert.That(lb[i], Is.EqualTo(la[i]));
        }
        [Test] public void InvalidLateInputCannotAdvanceAnyPrivateSubsystemOrTape()
        {
            var log = new RaceEventLog(1000); var a = Create(log); var b = Create(new RaceEventLog(1000));
            var tape = new ShootingCommandTape("atomic", a.ContextJson, a.AthleteId, 1);
            var input = Script(a);
            var bad = new ShootingSessionInput(input.Aim, input.Action, input.Lane, true, input.HeartRateBpm, input.Fatigue, double.NaN);
            Assert.Catch(() => tape.StepAndRecord(a, bad)); Assert.That(a.Tick.Value, Is.Zero); Assert.That(tape.Count, Is.Zero); Assert.That(log.Count, Is.Zero);
            Assert.That(tape.StepAndRecord(a, input), Is.EqualTo(b.Step(input)));
            Assert.Catch(() => tape.StepAndRecord(a, Script(a))); Assert.That(a.Tick.Value, Is.EqualTo(1));
            Assert.Catch(() => tape.ReplayNext(Create(new RaceEventLog(1000), 124)));
            Assert.Catch(() => ShootingCommandTape.FromJson(PhysiologyContractTests.ReplaceNumber(tape.ToJson(), "schema_version", "2")));
        }
        [Test] public void AiUsesCompleteSwayAndHandlingSessionWithoutAuthorityBypass()
        {
            var model = Model(); var actor = Athlete(); var stance = model.Stances.Standing;
            var s = Create(new RaceEventLog(1000), stance: ShootingStance.Standing);
            var ai = new ShootingAiController(ShootingAiTests.Definition(), model.Targets, model.Aim, stance, s.AthleteId, s.Frequency, new SeededRandomSource(444));
            for (int i = 1; i <= 10000 && s.Phase != ShootingPhase.Complete; i++)
            {
                var sight = s.LastFrame.ShotAim;
                var c = ai.Step(new SimulationTick(i), new ShootingAiObservation(s.Phase, s.ClosedMask, s.ShotsFired, sight.YawRadians, sight.PitchRadians));
                var action = i == 1 ? ShootingAction.EnterRange : i == 2 ? ShootingAction.AssignLane : c.Trigger ? ShootingAction.Trigger : ShootingAction.None;
                s.Step(new ShootingSessionInput(c.Aim, action, i == 2 ? new ShootingLaneAssignment("training_range", "lane", 1) : default,
                    c.HoldBreath, 130, 0.2, 0.2));
            }
            Assert.That(s.Phase, Is.EqualTo(ShootingPhase.Complete)); Assert.That(s.ShotsFired, Is.EqualTo(5));
            Assert.That(s.ClosedMask, Is.Not.Zero);
        }
        [Test] public void ActiveRecordedSessionDoesNotAllocate()
        {
            var s = Create(new RaceEventLog(1000)); var tape = new ShootingCommandTape("gc", s.ContextJson, s.AthleteId, 10000);
            tape.StepAndRecord(s, Script(s));
            long before = GC.GetAllocatedBytesForCurrentThread();
            while (s.Phase != ShootingPhase.Complete && tape.Count < 10000) tape.StepAndRecord(s, Script(s));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(s.ShotsFired, Is.EqualTo(5));
        }
        [Test] public void TrainingCsvAndEventsMatchPinnedCliTraceInEveryRuntime()
        {
            var scenario = ShootingTrainingScenario.FromJson(PhysiologyContractTests.DefinitionJson("shooting_training_v1.json"));
            var events = new RaceEventLog(1000); var runner = new ShootingScenarioRunner(scenario, Model(), Athlete(), events);
            var csv = new StringWriter(); csv.Write(ShootingCsv.Header + "\n");
            while (!runner.IsComplete) ShootingCsv.WriteFrame(csv, runner.Session.Step(runner.NextInput()));
            var eventText = new StringBuilder("tick,type,payload\n");
            for (int i = 0; i < events.Count; i++) eventText.Append(events[i].Tick.Value).Append(',').Append((int)events[i].EventType).Append(',').Append(events[i].Payload).Append('\n');
            string Hash(string text)
            { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(); }
            string[] expected = PhysiologyContractTests.ProjectFile("tests/fixtures/shooting-traces-v1.sha256").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.That(Hash(csv.ToString()), Is.EqualTo(expected[0].Split(' ')[0]));
            Assert.That(Hash(eventText.ToString()), Is.EqualTo(expected[1].Split(' ')[0]));
            Assert.That(runner.Session.Tick.Value, Is.EqualTo(214)); Assert.That(runner.Session.ShotsFired, Is.EqualTo(5));
        }
    }
}

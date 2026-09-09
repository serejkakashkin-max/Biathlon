using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class PaceControllerTests
    {
        public static PaceControllerDefinition Definition() => PaceControllerDefinition.FromJson(PhysiologyContractTests.DefinitionJson("pace_controller_provisional_v1.json"));
        [Test] public void EffortResponseIsBoundedSeededAndInvalidInputDoesNotConsumeRandom()
        {
            var self = new AthleteId("self"); var random = new SeededRandomSource(123); var bRandom = new SeededRandomSource(123);
            var a = new PaceController(Definition(), self, 20, random); var b = new PaceController(Definition(), self, 20, bRandom);
            var decision = new TacticalIntentDecision("pace", self, TacticalIntentKind.Attack, new SimulationTick(1), new SimulationTick(50));
            ulong seed = random.State;
            Assert.Catch(() => a.Step(new SimulationTick(2), decision)); Assert.That(random.State, Is.EqualTo(seed));
            int last = 0, error = 0;
            for (int i = 1; i < 50; i++)
            {
                TacticalIntentDecision? command = i == 1 ? decision : (TacticalIntentDecision?)null;
                var x = a.Step(new SimulationTick(i), command); var y = b.Step(new SimulationTick(i), command);
                Assert.That(x, Is.EqualTo(y)); Assert.That(Math.Abs(x.Input.RequestedEffort - last), Is.LessThanOrEqualTo(25));
                Assert.That(x.ControlError, Is.InRange(-20, 20)); Assert.That(x.TargetEffort, Is.InRange(0, 1000));
                if (i == 1) error = x.ControlError; Assert.That(x.ControlError, Is.EqualTo(error)); last = x.Input.RequestedEffort;
            }
            seed = random.State; Assert.Catch(() => a.Step(new SimulationTick(50))); Assert.That(a.Tick.Value, Is.EqualTo(49));
            Assert.That(random.State, Is.EqualTo(seed)); Assert.That(last, Is.EqualTo(a.LastFrame.TargetEffort));
        }
        [Test] public void CompleteSharedSprintWithCadencedPaceReplaysIdentically()
        {
            var log = new RaceEventLog(4000); var replayLog = new RaceEventLog(4000); var field = SprintFieldTests.Create(log, 1); var replay = SprintFieldTests.Create(replayLog, 1);
            var self = field.GetAthlete(0).AthleteId; var control = new PaceController(Definition(), self, 20, new SeededRandomSource(42));
            var clock = new TacticalDecisionClock(TacticalCadenceTests.Definition(), 20); var inputs = new SprintAthleteInput[1];
            while (!field.IsComplete && field.Tick.Value < 30000)
            {
                var tick = field.Tick.Next(); var window = clock.Step(tick);
                TacticalIntentDecision? decision = window.DecisionDue ? new TacticalIntentDecision("pace", self, TacticalIntentKind.Hold, tick, window.NextDecisionTick) : (TacticalIntentDecision?)null;
                var command = control.Step(tick, decision);
                inputs[0] = new SprintAthleteInput(new TimeTrialInput(command.Input, 1, 1, 0), new WeaponAimCommand(tick, self, 0, 0), true);
                field.Step(inputs); replay.Step(inputs);
                Assert.That(field.GetAthlete(0).LastFrame, Is.EqualTo(replay.GetAthlete(0).LastFrame));
            }
            Assert.That(field.IsComplete, Is.True); Assert.That(field.GetAthlete(0).TotalPenaltyLoops, Is.EqualTo(8));
            Assert.That(field.Result.ToJson(), Is.EqualTo(replay.Result.ToJson())); Assert.That(log.Count, Is.EqualTo(replayLog.Count));
            for (int i = 0; i < log.Count; i++) Assert.That(log[i], Is.EqualTo(replayLog[i]));
        }
        [Test] public void DefinitionValidationAndWarmedControlAllocation()
        {
            string json = Definition().ToJson(); Assert.That(PaceControllerDefinition.FromJson(json).ToJson(), Is.EqualTo(json));
            Assert.Catch(() => PaceControllerDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, "control_error_effort", "1001")));
            Assert.Catch(() => PaceControllerDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, "response_effort_per_second", "0")));
            var self = new AthleteId("self"); var control = new PaceController(Definition(), self, 20, new SeededRandomSource(1));
            control.Step(new SimulationTick(1), new TacticalIntentDecision("pace", self, TacticalIntentKind.Hold, new SimulationTick(1), new SimulationTick(10001)));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 2; i <= 10000; i++) control.Step(new SimulationTick(i));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

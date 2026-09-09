using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PreRangeRecoveryTests
    {
        public static PreRangeRecoveryDefinition Definition() => PreRangeRecoveryDefinition.FromJson(PhysiologyContractTests.DefinitionJson("pre_range_recovery_provisional_v1.json"));
        [Test] public void DistanceAndHeartRateScaleReductionAndOtherPhasesCannotRecover()
        {
            var p = new PreRangeRecoveryPlanner(Definition());
            Assert.That(p.Evaluate(true, 0, 175).EffortReduction, Is.EqualTo(350));
            Assert.That(p.Evaluate(true, 90000000000L, 175).EffortReduction, Is.EqualTo(175));
            Assert.That(p.Evaluate(true, 0, 155).EffortReduction, Is.EqualTo(175));
            Assert.That(p.Evaluate(true, 0, 135).Requested, Is.False);
            Assert.That(p.Evaluate(true, 180000000000L, 190).Requested, Is.False);
            Assert.That(p.Evaluate(false, 0, 190).Requested, Is.False);
            Assert.That(p.Evaluate(true, null, 190).Requested, Is.False);
            Assert.Catch(() => p.Evaluate(true, -1, 150)); Assert.Catch(() => p.Evaluate(true, 0, double.NaN));
        }
        [Test] public void SchemaAndWarmAllocationAreValidated()
        {
            string json = Definition().ToJson(); Assert.That(PreRangeRecoveryDefinition.FromJson(json).ToJson(), Is.EqualTo(json));
            Assert.Catch(() => PreRangeRecoveryDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, "full_recovery_hr_bpm", "135")));
            Assert.Catch(() => PreRangeRecoveryDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, "maximum_effort_reduction", "1001")));
            Assert.Catch(() => PreRangeRecoveryDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json, "approach_distance_m", "0")));
            var p = new PreRangeRecoveryPlanner(Definition()); p.Evaluate(true, 0, 170);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) p.Evaluate(true, 0, 170);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
        [Test] public void CompleteRacePaysApproachTimeForLowerArrivalHrAndReplays()
        {
            var logs = new[] { new RaceEventLog(10000), new RaceEventLog(10000) };
            var replayLog = new RaceEventLog(10000);
            var fields = new[] { PackSprintFieldTests.Create(logs[0], 1), PackSprintFieldTests.Create(logs[1], 1) };
            var replay = PackSprintFieldTests.Create(replayLog, 1);
            var arrivalTick = new long[2]; var arrivalHr = new double[2]; int requests = 0;
            for (int run = 0; run < 2; run++)
            {
                var field = fields[run]; var source = new SprintPerceptionSource(field); var definition = Definition(); var planner = new PreRangeRecoveryPlanner(definition);
                var clock = new TacticalDecisionClock(TacticalCadenceTests.Definition(), 20);
                var paceDefinition = PaceControllerTests.Definition(); var id = field.GetAthlete(0).AthleteId;
                var pace = new PaceController(paceDefinition, id, 20, new SeededRandomSource(42));
                var commands = new SprintAthleteInput[1]; var lines = new double[1];
                while (!field.IsComplete && field.Tick.Value < 40000)
                {
                    var observation = source.Capture(0, default, Array.Empty<TrackDistance>(), Array.Empty<AiRouteLookahead>(), Array.Empty<AiRouteNeighbor>());
                    var tick = field.Tick.Next(); var window = clock.Step(tick); TacticalIntentDecision? decision = null; int adjustment = 0;
                    if (window.DecisionDue)
                    {
                        var recovery = planner.Evaluate(observation); bool selected = run == 1 && recovery.Requested;
                        if (selected) requests++;
                        var kind = selected ? TacticalIntentKind.PreRangeRecovery : TacticalIntentKind.Hold;
                        decision = new TacticalIntentDecision(definition.Id, id, kind, tick, window.NextDecisionTick);
                        // Replace the baseline intent effort rather than stacking two reductions.
                        if (selected) adjustment = paceDefinition.Effort(TacticalIntentKind.Hold) - recovery.EffortReduction - paceDefinition.Effort(kind);
                    }
                    var frame = pace.Step(tick, decision, adjustment);
                    commands[0] = new SprintAthleteInput(new TimeTrialInput(frame.Input, 1, 1, 0), new WeaponAimCommand(tick, id, 0, 0), true);
                    field.Step(commands, lines);
                    if (run == 1) { replay.Step(commands, lines); Assert.That(field.GetAthlete(0).LastFrame, Is.EqualTo(replay.GetAthlete(0).LastFrame)); }
                    if (arrivalTick[run] == 0 && field.GetAthlete(0).CurrentRangeMotion?.Phase == RangeTraversalPhase.Shooting)
                    { arrivalTick[run] = field.Tick.Value; arrivalHr[run] = field.GetAthlete(0).PhysiologyState.HeartRateBpm; }
                }
                Assert.That(field.IsComplete, Is.True);
            }
            Assert.That(requests, Is.GreaterThan(0)); Assert.That(arrivalTick[0], Is.GreaterThan(0));
            Assert.That(arrivalTick[1], Is.GreaterThan(arrivalTick[0])); Assert.That(arrivalHr[1], Is.LessThan(arrivalHr[0]));
            Assert.That(fields[1].Result.ToJson(), Is.EqualTo(replay.Result.ToJson())); Assert.That(logs[1].Count, Is.EqualTo(replayLog.Count));
            for (int i = 0; i < logs[1].Count; i++) Assert.That(logs[1][i], Is.EqualTo(replayLog[i]));
            TestContext.Out.WriteLine("Arrival ticks: " + arrivalTick[0] + " -> " + arrivalTick[1] + "; HR: " + arrivalHr[0] + " -> " + arrivalHr[1]);
        }
    }
}

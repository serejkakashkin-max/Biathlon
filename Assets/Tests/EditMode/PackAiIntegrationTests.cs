using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PackAiIntegrationTests
    {
        [Test] public void CadencedPaceDraftAndOvertakeCommandsCompleteThePhysicalFieldAndReplay()
        {
            const int count = 3;
            var log = new RaceEventLog(30000); var replayLog = new RaceEventLog(30000);
            var field = PackSprintFieldTests.Create(log, count, 2); var replay = PackSprintFieldTests.Create(replayLog, count, 2);
            var raceSource = new SprintPerceptionSource(field); var physicalSource = new PackPerceptionSource(field);
            var clocks = new TacticalDecisionClock[count]; var pace = new PaceController[count];
            var drafts = new DraftTargetSelector[count]; var passes = new OvertakeController[count];
            var occupancy = new OccupancySystem(OccupancyTests.Definition()); var movement = OvertakeTests.Definition();
            var overtake = new OvertakeDecisionEvaluator(OvertakeDecisionTests.Definition(), movement, occupancy);
            var terrain = new TerrainPacePlanner(TerrainPlanningTests.Definition());
            var decisionDefinition = DraftSelectionDefinition.FromJson(PhysiologyContractTests.DefinitionJson("draft_selection_provisional_v1.json"));
            var policyLog = new RaceEventLog(20000);
            for (int i = 0; i < count; i++)
            {
                var id = field.GetAthlete(i).AthleteId;
                clocks[i] = new TacticalDecisionClock(TacticalCadenceTests.Definition(), 20);
                // Heterogeneous requested effort creates an actual catch/pass opportunity;
                // all athletes retain identical capability and use the same force equations.
                var paceDefinition = PaceControllerDefinition.FromJson(PaceControllerTests.Definition().ToJson()
                    .Replace("450,750,750,650", i == 0 ? "450,500,500,500" : "450,850,850,800"));
                pace[i] = new PaceController(paceDefinition, id, 20, new SeededRandomSource((ulong)(42 + i)));
                drafts[i] = new DraftTargetSelector(decisionDefinition, DraftingTests.Definition(), id, 20);
                passes[i] = new OvertakeController(movement, occupancy, id, 20, policyLog);
            }
            var commands = new SprintAthleteInput[count]; var lines = new double[count];
            var placements = new SkiPlacement[count]; var neighbors = new AiRouteNeighbor[count - 1];
            var offsets = new[] { default(TrackDistance), TrackDistance.FromMeters(10), TrackDistance.FromMeters(40) };
            var ahead = new AiRouteLookahead[offsets.Length]; var radius = TrackDistance.FromMeters(50);
            int physicalObservations = 0, draftDecisions = 0, passDecisions = 0;
            while (!field.IsComplete && field.Tick.Value < 100000)
            {
                var tick = field.Tick.Next();
                // Every observation/command is produced before the single field step.
                for (int i = 0; i < count; i++)
                {
                    var observation = raceSource.Capture(i, radius, offsets, ahead, neighbors);
                    Assert.That(observation.Tick, Is.EqualTo(field.Tick));
                    int size = physicalSource.CapturePlacements(i, radius, placements);
                    var actor = field.GetAthlete(i); var point = field.GetPhysicalPoint(i);
                    var self = size > 0 ? placements[0] : new SkiPlacement(actor.AthleteId, point.LoopId,
                        new SkiState(point.LoopLengthNanometers, actor.Motion.SpeedMetersPerSecond), field.GetLateral(i));
                    var roster = placements.AsSpan(0, size); var window = clocks[i].Step(tick);
                    TacticalIntentDecision? decision = null; int adjustment = 0; bool request = false;
                    bool skiing = size > 0 && observation.Phase == CompetitionPhase.Skiing;
                    if (size > 0) physicalObservations++;
                    if (window.DecisionDue)
                    {
                        var selected = size > 0 ? drafts[i].Evaluate(tick, 0, roster, observation.CurrentTrack).Selected : default;
                        if (selected.Strength > 0) draftDecisions++;
                        var segment = field.Track.GetLoop(observation.CurrentTrack.LoopId).GetSegment(observation.CurrentTrack.SegmentIndex);
                        long clearMm = segment.StartDistance.Millimeters + segment.Length.Millimeters - observation.CurrentTrack.Distance.Millimeters;
                        if (observation.NextRangeEntryRouteDistanceNanometers.HasValue)
                            clearMm = Math.Min(clearMm, observation.NextRangeEntryRouteDistanceNanometers.Value / SkiState.NanometersPerMillimeter);
                        var pass = skiing ? overtake.Evaluate(self, roster, observation.Physiology, 1,
                            observation.CurrentTrack.WidthMeters, observation.CurrentTrack.OvertakeAllowed,
                            Math.Abs(observation.CurrentTrack.Grade), Math.Abs(observation.CurrentTrack.CurvaturePerMeter), new TrackDistance(clearMm)) : default;
                        request = skiing && pass.Request && pass.Opportunity.Available;
                        if (request) passDecisions++;
                        var kind = request ? TacticalIntentKind.Overtake : selected.Strength > 0 ? TacticalIntentKind.Draft : TacticalIntentKind.Hold;
                        var target = request ? pass.Opportunity.Target.AthleteId : selected.Strength > 0 ? selected.LeaderId : default;
                        decision = new TacticalIntentDecision("pack_integration", actor.AthleteId, kind, tick, window.NextDecisionTick, target);
                        if (skiing) adjustment = terrain.Evaluate(observation.SpeedMetersPerSecond, clocks[i].PlanningHorizonTicks / 20.0, observation.Physiology, ahead).EffortAdjustment;
                    }
                    var effort = pace[i].Step(tick, decision, adjustment);
                    var lane = passes[i].Step(effort.Input, self, observation.CurrentTrack, roster, request, !skiing);
                    // Pack pays steering work exactly once. Do not also apply the controller's
                    // informational effort/propulsion surcharge to the base input.
                    lines[i] = lane.DesiredLateralMeters;
                    commands[i] = new SprintAthleteInput(new TimeTrialInput(effort.Input, 1, 1, 0), new WeaponAimCommand(tick, actor.AthleteId, 0, 0), true);
                }
                field.Step(commands, lines); replay.Step(commands, lines);
                for (int i = 0; i < count; i++)
                {
                    Assert.That(field.GetAthlete(i).LastFrame, Is.EqualTo(replay.GetAthlete(i).LastFrame));
                    Assert.That(field.GetLateral(i), Is.EqualTo(replay.GetLateral(i)));
                }
            }
            Assert.That(field.IsComplete, Is.True); Assert.That(field.Result.Count, Is.EqualTo(count));
            Assert.That(field.Result.ToJson(), Is.EqualTo(replay.Result.ToJson())); Assert.That(log.Count, Is.EqualTo(replayLog.Count));
            for (int i = 0; i < log.Count; i++) Assert.That(log[i], Is.EqualTo(replayLog[i]));
            Assert.That(physicalObservations, Is.GreaterThan(0)); Assert.That(draftDecisions, Is.GreaterThan(0));
            Assert.That(passDecisions, Is.GreaterThan(0));
            TestContext.Out.WriteLine("Pack AI: ticks=" + field.Tick.Value + ", draft_decisions=" + draftDecisions + ", pass_requests=" + passDecisions);
        }

        [Test] public void PhysicalSnapshotExcludesStagingAndRejectsAmbiguousLoopRadiusBeforeWriting()
        {
            var field = PackSprintFieldTests.Create(new RaceEventLog(), 3, 2); var source = new PackPerceptionSource(field);
            var rows = new SkiPlacement[3]; var radius = TrackDistance.FromMeters(50);
            Assert.That(source.CapturePlacements(0, radius, rows), Is.Zero);
            Assert.Catch(() => source.CapturePlacements(0, TrackDistance.FromMeters(501), rows));
            Assert.That(rows[0].IsValid, Is.False);
            var inputs = new SprintAthleteInput[3]; var lines = new double[3];
            for (int i = 0; i < 3; i++) { PackSprintFieldTests.Fill(field, inputs); field.Step(inputs, lines); }
            int size = source.CapturePlacements(0, radius, rows);
            Assert.That(size, Is.EqualTo(1)); Assert.That(rows[0].AthleteId, Is.EqualTo(field.GetAthlete(0).AthleteId));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) source.CapturePlacements(0, radius, rows);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

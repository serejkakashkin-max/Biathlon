using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePerceptionTests
    {
        private const long M = SkiState.NanometersPerMeter;
        private static readonly TrackDistance Radius = TrackDistance.FromMeters(10000);

        // Observe a single owned athlete transaction without advancing the unrelated field roster.
        // This fixture exercises accepted versus prepared association, not shared-body admission.
        private sealed class SingleAthleteView : ISprintFieldView
        {
            private readonly SprintFieldSession field;
            public SingleAthleteView(SprintFieldSession field) { this.field = field; }
            public SimulationTick Tick => GetAthlete(0).Tick;
            public int Count => 1;
            public TrackDefinition Track => field.Track;
            public RaceRoute Route => field.Route;
            public CompetitionProfile Profile => field.Profile;
            public CheckpointTimingBoard Timing => field.Timing;
            public SprintAthleteSession GetAthlete(int index)
            {
                if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                return field.GetAthlete(0);
            }
        }

        private static SprintFieldSession Create(RaceEventLog log)
        {
            var profile = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(
                CompetitionProfileTests.TestProfile().ToJson(), "distance_m", "900"));
            var definition = PhysicalRangePathDefinitionTests.Create(out var track, out _, out _);
            var requirements = profile.ToRouteRequirements();
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("physical_perception_route", track.Id, "main", requirements), requirements);
            var layout = new PhysicalRangeFieldLayout(new PhysicalTrackMap(track, route), track, route, definition);
            var ski = TimeTrialTests.Athlete(); var shot = ShootingSessionTests.Athlete();
            var entrants = new SprintEntrant[2];
            for (int i = 0; i < entrants.Length; i++)
            {
                string id = "physical_perception_" + i;
                entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id, id)),
                    ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id, id)), "ski_neutral");
            }
            return new SprintFieldSession(new SimulationConfig(1, "physical_perception_sim", 20, 123), profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), track, route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                SprintResultsTests.Timing(), entrants, new SimulationTick(1), "physical_perception_result", log, rangeLayout: layout);
        }

        private static void AdvanceToPrivatePath(SprintFieldSession field, int actor)
        {
            var inputs = new SprintAthleteInput[field.Count];
            long stop = actor == 0 ? 14 * M : 34 * M;
            while (field.GetAthlete(actor).RouteNanometers < stop && field.Tick.Value < 4000)
            { SprintFieldTests.Fill(field, inputs); field.Step(inputs); }
            Assert.That(field.GetAthlete(actor).RouteNanometers, Is.GreaterThanOrEqualTo(stop));
            Assert.That(field.GetAthlete(actor).RangePath.SampleRoute(field.GetAthlete(actor).RouteNanometers).LoopId,
                Is.EqualTo(actor == 0 ? "mat_1" : "mat_2"));
        }

        [TestCase(0)][TestCase(1)]
        public void LookaheadKeepsCompetitionCoordinatesButObservesAuthoredForksTerrainAndFinalBypass(int actor)
        {
            var log = new RaceEventLog(4000); var field = Create(log); var source = new SprintPerceptionSource(field);
            double[] distances = { 0, 10, 20, 30, 50, 70, 80, 100, 320, 620, 10000 };
            var offsets = new TrackDistance[distances.Length];
            for (int i = 0; i < offsets.Length; i++) offsets[i] = TrackDistance.FromMeters(distances[i]);
            var ahead = new AiRouteLookahead[offsets.Length]; var neighbors = new AiRouteNeighbor[field.Count - 1];
            var state = source.Capture(actor, Radius, offsets, ahead, neighbors);
            Assert.That(state.Tick.Value, Is.Zero); Assert.That(state.CurrentTrack.LoopId, Is.EqualTo("main"));
            Assert.That(state.NextRangeEntryRouteDistanceNanometers, Is.EqualTo(5 * M));
            Assert.That(ahead[2].Sample.Track.LoopId, Is.EqualTo(actor == 0 ? "mat_1" : "main"));
            Assert.That(ahead[2].Sample.Track.WidthMeters, Is.EqualTo(actor == 0 ? 2 : 6));
            Assert.That(ahead[2].Sample.Track.CurvaturePerMeter, Is.EqualTo(actor == 0 ? .08 : 0));
            Assert.That(ahead[3].Sample.Track.LoopId, Is.EqualTo(actor == 0 ? "mat_1" : "mat_2"));
            Assert.That(ahead[3].Sample.Track.CurvaturePerMeter, Is.EqualTo(actor == 0 ? -.08 : .06));
            Assert.That(ahead[6].Sample.Track.LoopId, Is.EqualTo("exit_aisle"));
            Assert.That(ahead[6].Sample.Track.Distance.Meters, Is.EqualTo(40));
            Assert.That(ahead[8].Sample.LapIndex, Is.EqualTo(1));
            Assert.That(ahead[8].Sample.Track.LoopId, Is.EqualTo(actor == 0 ? "mat_1" : "main"));
            Assert.That(ahead[9].Sample.LapIndex, Is.EqualTo(2));
            Assert.That(ahead[9].Sample.Track.LoopId, Is.EqualTo("main"));
            Assert.That(ahead[9].Sample.Track.WidthMeters, Is.EqualTo(6));
            Assert.That(ahead[10].Offset.Meters, Is.EqualTo(900));
            for (int i = 0; i < ahead.Length; i++)
            {
                var canonical = field.Route.Sample(ahead[i].Offset);
                Assert.That(ahead[i].Sample.RouteDistance, Is.EqualTo(canonical.RouteDistance));
                Assert.That(ahead[i].Sample.SectionIndex, Is.EqualTo(canonical.SectionIndex));
                Assert.That(ahead[i].Sample.LapIndex, Is.EqualTo(canonical.LapIndex));
            }
            Assert.That(log.Count, Is.Zero); Assert.That(field.Tick.Value, Is.Zero);
        }

        [TestCase(0)][TestCase(1)]
        public void CurrentPrivateTrackUsesActualWidthAndCurvatureWithoutChangingAcceptedState(int actor)
        {
            var log = new RaceEventLog(4000); var field = Create(log); AdvanceToPrivatePath(field, actor);
            var athlete = field.GetAthlete(actor); var source = new SprintPerceptionSource(field);
            var offsets = new[] { default(TrackDistance), TrackDistance.FromMeters(5), TrackDistance.FromMeters(20) };
            var ahead = new AiRouteLookahead[offsets.Length]; var neighbors = new AiRouteNeighbor[field.Count - 1];
            var frame = athlete.LastFrame; var tick = field.Tick; int eventCount = log.Count;
            var state = source.Capture(actor, Radius, offsets, ahead, neighbors);
            Assert.That(state.CurrentTrack.LoopId, Is.EqualTo(actor == 0 ? "mat_1" : "mat_2"));
            Assert.That(state.CurrentTrack.WidthMeters, Is.EqualTo(actor == 0 ? 2 : 2.2));
            Assert.That(state.CurrentTrack.CurvaturePerMeter, Is.EqualTo(actor == 0 ? .08 : .06));
            Assert.That(state.CurrentTrack.Distance.Millimeters,
                Is.EqualTo(athlete.RouteNanometers / SkiState.NanometersPerMillimeter - (actor == 0 ? 10000 : 30000)));
            Assert.That(ahead[0].Sample.Track, Is.EqualTo(state.CurrentTrack));
            Assert.That(field.Route.Sample(new TrackDistance(athlete.RouteNanometers / SkiState.NanometersPerMillimeter)).Track.LoopId,
                Is.EqualTo("main"), "Competition bookkeeping must remain distinct from physical terrain.");
            Assert.That(athlete.LastFrame, Is.EqualTo(frame)); Assert.That(field.Tick, Is.EqualTo(tick)); Assert.That(log.Count, Is.EqualTo(eventCount));
        }

        [Test] public void TerrainPlannerRespondsToThePrivateTurnInsteadOfTheFlatCompetitionInterval()
        {
            var field = Create(new RaceEventLog(4000)); var source = new SprintPerceptionSource(field);
            var offsets = new[] { default(TrackDistance), TrackDistance.FromMeters(20) };
            var actual = new AiRouteLookahead[2]; var canonical = new AiRouteLookahead[2];
            source.Capture(0, Radius, offsets, actual, new AiRouteNeighbor[1]);
            for (int i = 0; i < offsets.Length; i++) canonical[i] = new AiRouteLookahead(offsets[i], field.Route.Sample(offsets[i]));
            var planner = new TerrainPacePlanner(TerrainPlanningTests.Definition()); var rested = new PhysiologyState(100, 1, 0, 0);
            var pathPlan = planner.Evaluate(10, 4, rested, actual); var basePlan = planner.Evaluate(10, 4, rested, canonical);
            Assert.That(basePlan.TurnDemand, Is.Zero); Assert.That(basePlan.EffortAdjustment, Is.Zero);
            Assert.That(pathPlan.TurnDemand, Is.GreaterThan(0)); Assert.That(pathPlan.EffortAdjustment, Is.LessThan(0));
        }

        [Test] public void PreparedOrCancelledChoiceIsInvisibleUntilTheAcceptedPathPublishes()
        {
            var log = new RaceEventLog(4000); var field = Create(log); var athlete = field.GetAthlete(0);
            var source = new SprintPerceptionSource(new SingleAthleteView(field));
            var offsets = new[] { default(TrackDistance), TrackDistance.FromMeters(20) };
            var ahead = new AiRouteLookahead[2]; var initialPath = athlete.RangePath;
            SprintStepPreview preview;
            do
            {
                var choice = athlete.CreateRangePathChoice(2);
                preview = athlete.PrepareRangePathStep(SprintAthleteSessionTests.Script(athlete), choice);
                if (preview.MovementActive && preview.Movement.RouteNanometers >= 5 * M) break;
                athlete.CommitStep(preview);
            } while (athlete.Tick.Value < 3000);
            Assert.That(preview.MovementActive && preview.Movement.RouteNanometers >= 5 * M, Is.True);
            Assert.That(athlete.RouteNanometers, Is.LessThan(5 * M));
            Assert.That(athlete.GetPreparedRangePath(preview).LaneOrdinal, Is.EqualTo(2));
            int events = log.Count;
            source.Capture(0, Radius, offsets, ahead, Span<AiRouteNeighbor>.Empty);
            Assert.That(ahead[1].Sample.Track.LoopId, Is.EqualTo("mat_1"));
            Assert.That(athlete.RangePath, Is.SameAs(initialPath)); Assert.That(log.Count, Is.EqualTo(events));
            athlete.CommitStep(athlete.ConstrainPreparedTravel(preview, 0));
            source.Capture(0, Radius, offsets, ahead, Span<AiRouteNeighbor>.Empty);
            Assert.That(ahead[1].Sample.Track.LoopId, Is.EqualTo("mat_1")); Assert.That(athlete.RangePath, Is.SameAs(initialPath));
            do
            {
                var choice = athlete.CreateRangePathChoice(2);
                athlete.CommitStep(athlete.PrepareRangePathStep(SprintAthleteSessionTests.Script(athlete), choice));
            } while (ReferenceEquals(athlete.RangePath, initialPath) && athlete.Tick.Value < 3000);
            Assert.That(athlete.RangePath.LaneOrdinal, Is.EqualTo(2));
            source.Capture(0, Radius, offsets, ahead, Span<AiRouteNeighbor>.Empty);
            Assert.That(ahead[1].Sample.Track.LoopId, Is.EqualTo("main"));
        }

        [Test] public void CompleteCapturedRaceKeepsPenaltyPriorityFinalBypassFramesEventsAndResults()
        {
            var la = new RaceEventLog(4000); var lb = new RaceEventLog(4000); var a = Create(la); var b = Create(lb);
            var source = new SprintPerceptionSource(a); var offsets = new[] { default(TrackDistance), TrackDistance.FromMeters(20), TrackDistance.FromMeters(10000) };
            var ahead = new AiRouteLookahead[3]; var neighbors = new AiRouteNeighbor[1]; var inputs = new SprintAthleteInput[a.Count];
            bool privatePath = false, sharedExit = false, penalty = false, finalBypass = false;
            while (!a.IsComplete && a.Tick.Value < 30000)
            {
                SprintFieldTests.Fill(a, inputs); a.Step(inputs); b.Step(inputs);
                for (int i = 0; i < a.Count; i++)
                {
                    var athlete = a.GetAthlete(i); var state = source.Capture(i, Radius, offsets, ahead, neighbors);
                    privatePath |= state.CurrentTrack.LoopId == "mat_1" || state.CurrentTrack.LoopId == "mat_2";
                    sharedExit |= state.CurrentTrack.LoopId == "exit_aisle";
                    if (athlete.CurrentRangeMotion?.Phase == RangeTraversalPhase.PenaltyLoop)
                    { penalty = true; Assert.That(state.CurrentTrack.LoopId, Is.EqualTo(a.Track.Penalty.LoopId)); }
                    if (athlete.RouteNanometers >= 610 * M && athlete.RouteNanometers < 630 * M)
                    { finalBypass = true; Assert.That(state.CurrentTrack.LoopId, Is.EqualTo("main")); Assert.That(state.CurrentTrack.WidthMeters, Is.EqualTo(6)); }
                    Assert.That(ahead[2].Sample.RouteDistance, Is.EqualTo(a.Route.Length));
                    Assert.That(athlete.LastFrame, Is.EqualTo(b.GetAthlete(i).LastFrame));
                }
            }
            Assert.That(a.IsComplete && b.IsComplete, Is.True);
            Assert.That(privatePath && sharedExit && penalty && finalBypass, Is.True);
            Assert.That(a.Result.ToJson(), Is.EqualTo(b.Result.ToJson())); Assert.That(la.Count, Is.EqualTo(lb.Count));
            for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
        }

        [Test] public void WarmPhysicalPerceptionAndLookaheadAllocateNothing()
        {
            var field = Create(new RaceEventLog(4000)); AdvanceToPrivatePath(field, 0); var source = new SprintPerceptionSource(field);
            var offsets = new[] { default(TrackDistance), TrackDistance.FromMeters(20), TrackDistance.FromMeters(80) };
            var ahead = new AiRouteLookahead[3]; var neighbors = new AiRouteNeighbor[1];
            for (int i = 0; i < 1000; i++) source.Capture(0, Radius, offsets, ahead, neighbors);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) source.Capture(0, Radius, offsets, ahead, neighbors);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}

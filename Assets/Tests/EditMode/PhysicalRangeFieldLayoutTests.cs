using System;
using System.Reflection;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    // Independent field wiring only. No shared-body admission or venue acceptance.
    public class PhysicalRangeFieldLayoutTests
    {
        private sealed class Fixture
        {
            internal CompetitionProfile Profile;
            internal TrackDefinition Track;
            internal RaceRoute Route;
            internal PhysicalTrackMap Map;
            internal PhysicalRangePathDefinition Definition;
            internal PhysicalRangeFieldLayout Layout;
            internal MassStartGrid Grid;
            internal StartApproachMap Approach;
            internal SprintEntrant[] Entrants;
        }

        private static Fixture Create(int count = 2, string definitionId = "range_path_contract_v1")
        {
            var f = new Fixture();
            f.Profile = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(
                StartApproachTests.Profile().ToJson(), "distance_m", "1500"));
            const string approachLoop = "{\"id\":\"layout_start\",\"kind\":\"race\",\"closed\":false," +
                "\"segments\":[{\"id\":\"layout_start_segment\",\"length_m\":30,\"start_grade\":0,\"end_grade\":0," +
                "\"curvature_per_m\":0,\"width_m\":6,\"snow_profile_id\":\"packed\",\"wind_exposure\":1," +
                "\"terrain_muscle_factor\":1,\"draft_allowed\":false,\"overtake_allowed\":false," +
                "\"risk_level\":0,\"semantic_zone\":\"stadium\"}]}";
            string trackJson = Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("track"),
                "\"loops\"\\s*:\\s*\\[", match => match.Value + approachLoop + ",");
            f.Track = TrackDefinition.FromJson(trackJson);
            var requirements = f.Profile.ToRouteRequirements();
            f.Route = new RaceRoute(f.Track, RaceRoutePlan.TimeTrial("layout_field_route", f.Track.Id, "main", requirements), requirements);
            f.Map = new PhysicalTrackMap(f.Track, f.Route);
            var junctions = PhysicalJunctionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("junctions"), f.Track);
            var connections = PhysicalConnectionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("connections"), junctions);
            f.Definition = PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths")
                .Replace("range_path_contract_v1", definitionId), f.Track, junctions, connections);
            f.Layout = new PhysicalRangeFieldLayout(f.Map, f.Track, f.Route, f.Definition);
            var skiTemplate = TimeTrialTests.Athlete(); var shootingTemplate = ShootingSessionTests.Athlete();
            var roster = new AthleteId[count]; f.Entrants = new SprintEntrant[count];
            for (int i = 0; i < count; i++)
            {
                roster[i] = new AthleteId("layout_field_" + i);
                f.Entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(skiTemplate.ToJson().Replace(skiTemplate.Id, roster[i].Value)),
                    ShootingAthleteDefinition.FromJson(shootingTemplate.ToJson().Replace(shootingTemplate.Id, roster[i].Value)), "ski_neutral");
            }
            f.Grid = new MassStartGrid(f.Profile, MassStartGridTests.Definition(), OccupancyTests.Definition(), roster, 6, 30);
            f.Approach = new StartApproachMap(f.Track, f.Route, StartApproachDefinition.FromJson(
                "{\"schema_version\":1,\"id\":\"layout_start_v1\",\"track_id\":\"" + f.Track.Id +
                "\",\"loop_id\":\"layout_start\",\"route_entry_loop_id\":\"main\"}"));
            return f;
        }

        private static SprintFieldSession Field(Fixture f, RaceEventLog log, bool withLayout = true)
            => new SprintFieldSession(new SimulationConfig(1, "layout_field_sim", 20, 123), f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), f.Track, f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                SprintResultsTests.Timing(), f.Entrants, new SimulationTick(1), "layout_field_result", log,
                massGrid: f.Grid, startApproach: f.Approach, rangeLayout: withLayout ? f.Layout : null);

        private static void AssertEvents(RaceEventLog expected, RaceEventLog actual)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Count));
            for (int i = 0; i < expected.Count; i++) Assert.That(actual[i], Is.EqualTo(expected[i]));
        }

        [Test] public void EveryAthleteUsesOneColdLayoutFamilyAndItsOwnInitialPhysicalLane()
        {
            var f = Create(); var field = Field(f, new RaceEventLog(2000));
            Assert.That(f.Layout.Count, Is.EqualTo(2));
            for (int actor = 0; actor < field.Count; actor++)
            {
                var athlete = field.GetAthlete(actor);
                Assert.That(athlete.RangePath, Is.SameAs(f.Layout.GetPath(actor)));
                Assert.That(athlete.RangePathChoiceCount, Is.EqualTo(2));
                Assert.That(athlete.AssignedShootingLane.Ordinal, Is.EqualTo(actor + 1));
                Assert.That(athlete.AssignedShootingLane.LaneId, Is.EqualTo(f.Layout.GetPath(actor).LaneId));
                for (int lane = 0; lane < f.Layout.Count; lane++)
                    Assert.That(athlete.GetRangePathChoice(lane), Is.SameAs(f.Layout.GetPath(lane)));
            }
            Assert.That(f.Layout.GetPath(0), Is.Not.SameAs(f.Layout.GetPath(1)));
            var copy = (PhysicalRangePathMap[])typeof(PhysicalRangeFieldLayout)
                .GetMethod("CreateChoices", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(f.Layout, null);
            copy[0] = copy[1];
            Assert.That(f.Layout.GetPath(0).LaneOrdinal, Is.EqualTo(1));
            for (int actor = 0; actor < field.Count; actor++)
                Assert.That(field.GetAthlete(actor).GetRangePathChoice(0), Is.SameAs(f.Layout.GetPath(0)));
        }

        [Test] public void CompleteIndependentMassStartFieldStopsAtPersonalMatsAndRepeatsFramesEventsAndResults()
        {
            var f = Create(); var la = new RaceEventLog(5000); var lb = new RaceEventLog(5000);
            var a = Field(f, la); var b = Field(f, lb); var inputs = new SprintAthleteInput[a.Count];
            var stops = new int[a.Count]; var wasShooting = new bool[a.Count];
            while (!a.IsComplete && a.Tick.Value < 60000)
            {
                SprintFieldTests.Fill(a, inputs); a.Step(inputs); b.Step(inputs);
                for (int i = 0; i < a.Count; i++)
                {
                    var athlete = a.GetAthlete(i);
                    Assert.That(athlete.LastFrame, Is.EqualTo(b.GetAthlete(i).LastFrame));
                    bool shooting = athlete.CurrentRangeMotion?.Phase == RangeTraversalPhase.Shooting;
                    if (shooting && !wasShooting[i])
                    {
                        Assert.That(stops[i], Is.LessThan(f.Profile.BoutCount));
                        int lap = f.Profile.GetBout(stops[i]).AfterLap - 1;
                        var path = f.Layout.GetPath(i);
                        Assert.That(athlete.RouteNanometers, Is.EqualTo(path.FiringRouteNanometers(lap)));
                        Assert.That(path.MapRoute(athlete.RouteNanometers).LoopId, Is.EqualTo(path.MatAnchor.LoopId));
                        Assert.That(path.MapRoute(athlete.RouteNanometers).LocalNanometers,
                            Is.EqualTo(path.MatAnchor.Distance.Millimeters * SkiState.NanometersPerMillimeter));
                        Assert.That(athlete.Motion.SpeedMetersPerSecond, Is.Zero); stops[i]++;
                    }
                    wasShooting[i] = shooting;
                }
            }
            Assert.That(a.IsComplete && b.IsComplete, Is.True);
            Assert.That(a.MassStartResult.ToJson(), Is.EqualTo(b.MassStartResult.ToJson()));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(stops[i], Is.EqualTo(4));
                var athlete = a.GetAthlete(i);
                Assert.That(athlete.Lifecycle.CompletedLaps, Is.EqualTo(5));
                Assert.That(athlete.Lifecycle.CompletedBouts, Is.EqualTo(4));
                Assert.That(athlete.Motion.DistanceNanometers,
                    Is.EqualTo((1500L + 150L * athlete.TotalPenaltyLoops) * SkiState.NanometersPerMeter));
            }
            AssertEvents(la, lb);
        }

        [TestCase("track")][TestCase("route")]
        public void LayoutRejectsEqualJsonObjectsOutsideTheExactPhysicalMapBinding(string foreign)
        {
            var f = Create(); var track = f.Track; var route = f.Route;
            if (foreign == "track") track = TrackDefinition.FromJson(f.Track.ToJson());
            else route = new RaceRoute(f.Track, RaceRoutePlan.FromJson(f.Route.PlanSnapshotJson),
                CompetitionRouteRequirements.FromJson(f.Route.RequirementsSnapshotJson));
            Assert.That(track.ToJson(), Is.EqualTo(f.Track.ToJson()));
            Assert.That(route.PlanSnapshotJson, Is.EqualTo(f.Route.PlanSnapshotJson));
            Assert.Throws<ArgumentException>(() => new PhysicalRangeFieldLayout(f.Map, track, route, f.Definition));
        }

        [TestCase("track")][TestCase("route")]
        public void FieldRejectsForeignReferencesEvenWhenTheirSourceJsonMatches(string foreign)
        {
            var f = Create(); var log = new RaceEventLog(2000);
            if (foreign == "track") f.Track = TrackDefinition.FromJson(f.Track.ToJson());
            else f.Route = new RaceRoute(f.Track, RaceRoutePlan.FromJson(f.Route.PlanSnapshotJson),
                CompetitionRouteRequirements.FromJson(f.Route.RequirementsSnapshotJson));
            Assert.Throws<ArgumentException>(() => Field(f, log));
            Assert.That(log.Count, Is.Zero);
        }

        [TestCase("junction")][TestCase("connection")]
        public void CapturedTopologySnapshotsChangeContextEvenWithIdenticalSerializedPathDefinition(string changedSource)
        {
            var f = Create(); var original = Field(f, new RaceEventLog(2000));
            var tape = new SprintRaceCommandTape("topology_context", original, 2);
            string junctionJson = PhysicalRangePathDefinitionTests.Fixture("junctions");
            string connectionJson = PhysicalRangePathDefinitionTests.Fixture("connections");
            if (changedSource == "junction")
            {
                junctionJson = junctionJson.Replace("range_path_contract_junctions_v1", "other_topology_junctions_v1");
                connectionJson = connectionJson.Replace("range_path_contract_junctions_v1", "other_topology_junctions_v1");
            }
            else connectionJson = connectionJson.Replace("range_path_contract_connections_v1", "other_topology_connections_v1");
            var junctions = PhysicalJunctionDefinition.FromJson(junctionJson, f.Track);
            var connections = PhysicalConnectionDefinition.FromJson(connectionJson, junctions);
            var changed = PhysicalRangePathDefinition.FromJson(f.Definition.ToJson(), f.Track, junctions, connections);
            Assert.That(changed.ToJson(), Is.EqualTo(f.Definition.ToJson()));
            var previousContext = f.Layout.ContextJson;
            f.Layout = new PhysicalRangeFieldLayout(f.Map, f.Track, f.Route, changed);
            Assert.That(f.Layout.ContextJson, Is.Not.EqualTo(previousContext));
            var log = new RaceEventLog(2000); var field = Field(f, log);
            Assert.Throws<ArgumentException>(() => tape.CreateReplay(field));
            Assert.That(field.Tick.Value, Is.Zero); Assert.That(log.Count, Is.Zero);
        }

        [Test] public void RosterLargerThanTheAuthoredLayoutFailsBeforeEvents()
        {
            var f = Create(3); var log = new RaceEventLog(2000);
            Assert.Throws<ArgumentException>(() => Field(f, log));
            Assert.That(log.Count, Is.Zero);
        }

        [TestCase("null")][TestCase("equipment")]
        public void InvalidLastEntrantCannotPublishEventsFromEarlierConstructedAthletes(string defect)
        {
            var f = Create(); var log = new RaceEventLog(2000); var last = f.Entrants[1];
            f.Entrants[1] = defect == "null" ? null : new SprintEntrant(last.SkiAthlete, last.ShootingAthlete, "missing_ski");
            Assert.Catch(() => Field(f, log));
            Assert.That(log.Count, Is.Zero);
        }

        [Test] public void ExplicitNullLayoutKeepsTheExistingDefaultContext()
        {
            var f = Create(); var explicitNull = Field(f, new RaceEventLog(2000), withLayout: false);
            var implicitDefault = new SprintFieldSession(new SimulationConfig(1, "layout_field_sim", 20, 123), f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), f.Track, f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                SprintResultsTests.Timing(), f.Entrants, new SimulationTick(1), "layout_field_result", new RaceEventLog(2000),
                massGrid: f.Grid, startApproach: f.Approach);
            Assert.That(explicitNull.ContextJson, Is.EqualTo(implicitDefault.ContextJson));
            Assert.That(explicitNull.GetAthlete(0).RangePath, Is.Null);
            Assert.That(Field(f, new RaceEventLog(2000)).ContextJson, Is.Not.EqualTo(explicitNull.ContextJson));
        }

        [Test] public void FixedPathFieldTapeRoundTripsCompleteRaceAndRejectsDifferentLayoutContext()
        {
            var f = Create(); var la = new RaceEventLog(5000); var lb = new RaceEventLog(5000);
            var a = Field(f, la); var tape = new SprintRaceCommandTape("layout_fixed_path_tape", a, 8);
            var inputs = new SprintAthleteInput[a.Count];
            while (!a.IsComplete && a.Tick.Value < 60000)
            { SprintFieldTests.Fill(a, inputs); tape.StepAndRecord(a, inputs); }
            Assert.That(a.IsComplete, Is.True);
            var restored = SprintRaceCommandTape.FromJson(tape.ToJson());
            Assert.That(restored.ToJson(), Is.EqualTo(tape.ToJson()));
            var rebuilt = Create(); var b = Field(rebuilt, lb);
            Assert.That(b.ContextJson, Is.EqualTo(a.ContextJson));
            var direct = Field(f, new RaceEventLog(5000)); var replay = restored.CreateReplay(b);
            while (replay.HasNext)
            {
                replay.Step(); SprintFieldTests.Fill(direct, inputs); direct.Step(inputs);
                for (int i = 0; i < b.Count; i++)
                    Assert.That(b.GetAthlete(i).LastFrame, Is.EqualTo(direct.GetAthlete(i).LastFrame));
            }
            Assert.That(b.MassStartResult.ToJson(), Is.EqualTo(a.MassStartResult.ToJson())); AssertEvents(la, lb);
            Assert.Throws<ArgumentException>(() => restored.CreateReplay(Field(f, new RaceEventLog(2000), withLayout: false)));
            var changed = Create(definitionId: "range_path_contract_other_v1");
            Assert.Throws<ArgumentException>(() => restored.CreateReplay(Field(changed, new RaceEventLog(2000))));
        }

        [Test] public void SharedPackRejectsUnintegratedLayoutBeforeAttemptingOtherConstruction()
        {
            var f = Create(); var log = new RaceEventLog(10);
            // Deliberately incomplete dependencies prove the dedicated rangeLayout rejection
            // happens before the independent field or physical pack constructor is entered.
            var error = Assert.Throws<ArgumentException>(() => new PackSprintFieldSession(null, null, null, null,
                null, null, null, null, Array.Empty<SprintEntrant>(), default, null,
                null, null, null, null, null, log, rangeLayout: f.Layout));
            Assert.That(error.ParamName, Is.EqualTo("rangeLayout"));
            Assert.That(log.Count, Is.Zero);
        }
    }
}

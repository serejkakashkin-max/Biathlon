using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    // Shared two-athlete runtime fixture. It does not provide world geometry or certify a venue.
    public class PhysicalRangePackTests
    {
        public sealed class Fixture
        {
            public CompetitionProfile Profile;
            public TrackDefinition Track;
            public RaceRoute Route;
            public PhysicalTrackMap Map;
            public PhysicalJunctionDefinition Junctions;
            public PhysicalConnectionDefinition Connections;
            public PhysicalRangeFieldLayout Layout;
            public MassStartGrid Grid;
            public StartApproachMap Approach;
            public SprintEntrant[] Entrants;
        }

        public static Fixture CreateFixture(int count = 2, bool exitDraftAllowed = false, bool flatPrivate = false, int gridColumns = 1, double exitGrade = 0)
        {
            var f = new Fixture();
            f.Profile = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(
                StartApproachTests.Profile().ToJson(), "distance_m", "1500"));
            const string approachLoop = "{\"id\":\"pack_start\",\"kind\":\"race\",\"closed\":false," +
                "\"segments\":[{\"id\":\"pack_start_segment\",\"length_m\":30,\"start_grade\":0,\"end_grade\":0," +
                "\"curvature_per_m\":0,\"width_m\":6,\"snow_profile_id\":\"packed\",\"wind_exposure\":1," +
                "\"terrain_muscle_factor\":1,\"draft_allowed\":false,\"overtake_allowed\":false," +
                "\"risk_level\":0,\"semantic_zone\":\"stadium\"}]}";
            string trackJson = Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("track"),
                "\"loops\"\\s*:\\s*\\[", match => match.Value + approachLoop + ",");
            if (flatPrivate)
                trackJson = Regex.Replace(trackJson, "(\"curvature_per_m\"\\s*:\\s*)-?0\\.0[68](?=\\s*[,}])", "${1}0");
            if (exitDraftAllowed)
                trackJson = Regex.Replace(trackJson,
                    "(\"id\"\\s*:\\s*\"exit_aisle\"[\\s\\S]*?\"draft_allowed\"\\s*:\\s*)false", "${1}true");
            if (exitGrade != 0)
            {
                string grade = exitGrade.ToString(System.Globalization.CultureInfo.InvariantCulture);
                trackJson = Regex.Replace(trackJson,
                    "(\"id\"\\s*:\\s*\"exit_aisle\"[\\s\\S]*?\"start_grade\"\\s*:\\s*)0", "${1}" + grade);
                trackJson = Regex.Replace(trackJson,
                    "(\"id\"\\s*:\\s*\"exit_aisle\"[\\s\\S]*?\"end_grade\"\\s*:\\s*)0", "${1}" + grade);
            }
            f.Track = TrackDefinition.FromJson(trackJson);
            var requirements = f.Profile.ToRouteRequirements();
            f.Route = new RaceRoute(f.Track, RaceRoutePlan.TimeTrial("pack_path_route", f.Track.Id, "main", requirements), requirements);
            f.Map = new PhysicalTrackMap(f.Track, f.Route);
            f.Junctions = PhysicalJunctionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("junctions"), f.Track);
            f.Connections = PhysicalConnectionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("connections"), f.Junctions);
            var paths = PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"), f.Track, f.Junctions, f.Connections);
            f.Layout = new PhysicalRangeFieldLayout(f.Map, f.Track, f.Route, paths);
            var skiTemplate = TimeTrialTests.Athlete(); var shootingTemplate = ShootingSessionTests.Athlete();
            var roster = new AthleteId[count]; f.Entrants = new SprintEntrant[count];
            for (int i = 0; i < count; i++)
            {
                roster[i] = new AthleteId("pack_path_" + i);
                f.Entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(skiTemplate.ToJson().Replace(skiTemplate.Id, roster[i].Value)),
                    ShootingAthleteDefinition.FromJson(shootingTemplate.ToJson().Replace(shootingTemplate.Id, roster[i].Value)), "ski_neutral");
            }
            // Centered rows have real approach travel and already fit the two narrow private paths.
            var gridDefinition = MassStartGridDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(
                MassStartGridTests.Definition().ToJson(), "columns", gridColumns.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            f.Grid = new MassStartGrid(f.Profile, gridDefinition, OccupancyTests.Definition(), roster, 6, 30);
            f.Approach = new StartApproachMap(f.Track, f.Route, StartApproachDefinition.FromJson(
                "{\"schema_version\":1,\"id\":\"pack_start_v1\",\"track_id\":\"" + f.Track.Id +
                "\",\"loop_id\":\"pack_start\",\"route_entry_loop_id\":\"main\"}"));
            return f;
        }

        public static PackSprintFieldSession CreatePack(Fixture f, RaceEventLog log, MassStartRangePolicyDefinition policy = null)
            => new PackSprintFieldSession(new SimulationConfig(1, "pack_path_sim", 20, 123), f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), f.Track, f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                SprintResultsTests.Timing(), f.Entrants, new SimulationTick(1), "pack_path_result", OccupancyTests.Definition(),
                DraftingTests.Definition(), OvertakeTests.Definition(), f.Junctions, f.Connections, log,
                massGrid: f.Grid, startApproach: f.Approach, rangePolicy: policy, rangeLayout: f.Layout);

        private static SprintFieldSession Independent(Fixture f, RaceEventLog log)
            => new SprintFieldSession(new SimulationConfig(1, "pack_path_sim", 20, 123), f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), f.Track, f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                SprintResultsTests.Timing(), f.Entrants, new SimulationTick(1), "pack_path_result", log,
                massGrid: f.Grid, startApproach: f.Approach, rangeLayout: f.Layout);

        public static void AssertBodiesAndWidth(PackSprintFieldSession field, Fixture f)
        {
            var occupancy = new OccupancySystem(OccupancyTests.Definition());
            var body = occupancy.Definition;
            for (int i = 0; i < field.Count; i++) if (field.IsOnCourse(i))
            {
                var point = field.GetPhysicalPoint(i);
                var sample = new TrackSampler(f.Track.GetLoop(point.LoopId))
                    .Sample(new TrackDistance(point.LocalNanometers / SkiState.NanometersPerMillimeter));
                Assert.That(occupancy.FitsWidth(field.GetLateral(i), sample.WidthMeters), Is.True,
                    "Body outside actual corridor at tick " + field.Tick.Value + ", actor " + i + ", loop " + point.LoopId);
                for (int j = 0; j < i; j++) if (field.IsOnCourse(j))
                {
                    var other = field.GetPhysicalPoint(j); if (point.LoopId != other.LoopId) continue;
                    long gap = Math.Abs(point.LocalNanometers - other.LocalNanometers);
                    if (point.Closed) gap = Math.Min(gap, point.LoopLengthNanometers - gap);
                    Assert.That(gap / 1e9 >= body.LongitudinalSeparation ||
                        Math.Abs(field.GetLateral(i) - field.GetLateral(j)) >= body.LateralSeparation, Is.True,
                        "Accepted bodies overlap at tick " + field.Tick.Value);
                }
            }
        }

        [Test] public void TwoFixedPathsCompleteRepeatedMassStartWithPersonalStopsActualLoopsAndNoBodyOverlap()
        {
            var fa = CreateFixture(); var fb = CreateFixture();
            var la = new RaceEventLog(20000); var lb = new RaceEventLog(20000);
            var a = CreatePack(fa, la); var b = CreatePack(fb, lb);
            var commands = new SprintAthleteInput[2]; var lines = new double[2];
            var stops = new int[2]; var wasShooting = new bool[2];
            bool mat1 = false, mat2 = false, exit = false, penalty = false;
            while (!a.IsComplete && a.Tick.Value < 80000)
            {
                PackSprintFieldTests.Fill(a, commands); a.Step(commands, lines); b.Step(commands, lines);
                // Step performs the solver's continuous receipt/tail validation. These assertions
                // additionally verify published physical bodies, without treating endpoints as a new proof.
                AssertBodiesAndWidth(a, fa);
                for (int i = 0; i < a.Count; i++)
                {
                    var athlete = a.GetAthlete(i);
                    Assert.That(athlete.LastFrame, Is.EqualTo(b.GetAthlete(i).LastFrame));
                    Assert.That(a.GetLateral(i), Is.EqualTo(b.GetLateral(i)));
                    var point = a.GetPhysicalPoint(i);
                    mat1 |= point.LoopId == "mat_1"; mat2 |= point.LoopId == "mat_2";
                    exit |= point.LoopId == "exit_aisle"; penalty |= point.LoopId == "penalty";
                    bool shooting = athlete.CurrentRangeMotion?.Phase == RangeTraversalPhase.Shooting;
                    if (shooting && !wasShooting[i])
                    {
                        Assert.That(stops[i], Is.LessThan(4));
                        var path = fa.Layout.GetPath(i); int lap = fa.Profile.GetBout(stops[i]).AfterLap - 1;
                        Assert.That(athlete.RouteNanometers, Is.EqualTo(path.FiringRouteNanometers(lap)));
                        Assert.That(point.LoopId, Is.EqualTo(path.MatAnchor.LoopId));
                        Assert.That(point.LocalNanometers, Is.EqualTo(path.MatAnchor.Distance.Millimeters * SkiState.NanometersPerMillimeter));
                        Assert.That(athlete.Motion.SpeedMetersPerSecond, Is.Zero); stops[i]++;
                    }
                    wasShooting[i] = shooting;
                }
            }
            Assert.That(a.IsComplete && b.IsComplete, Is.True, "Fixed-path pack must complete without permanent queue.");
            Assert.That(mat1 && mat2 && exit && penalty, Is.True);
            Assert.That(a.ContextJson, Is.EqualTo(b.ContextJson));
            Assert.That(a.MassStartResult.ToJson(), Is.EqualTo(b.MassStartResult.ToJson()));
            for (int i = 0; i < 2; i++)
            {
                var athlete = a.GetAthlete(i);
                Assert.That(stops[i], Is.EqualTo(4));
                Assert.That(athlete.Lifecycle.CompletedLaps, Is.EqualTo(5));
                Assert.That(athlete.Lifecycle.CompletedBouts, Is.EqualTo(4));
                Assert.That(athlete.Motion.DistanceNanometers, Is.EqualTo(
                    (1500L + 150L * athlete.TotalPenaltyLoops) * SkiState.NanometersPerMeter + athlete.InitialStartApproachNanometers));
            }
            Assert.That(a.GetAthlete(1).InitialStartApproachNanometers, Is.EqualTo(3 * SkiState.NanometersPerMeter));
            Assert.That(la.Count, Is.EqualTo(lb.Count));
            for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
        }

        [Test] public void SoloFixedPathPackUsesTheSameActualPrivatePhysicsAsIndependentField()
        {
            var f = CreateFixture(1); var a = CreatePack(f, new RaceEventLog(10000));
            var independent = Independent(f, new RaceEventLog(10000));
            var commands = new SprintAthleteInput[1]; var lines = new double[1]; bool privateLoop = false;
            while (!a.IsComplete && a.Tick.Value < 60000)
            {
                PackSprintFieldTests.Fill(a, commands); a.Step(commands, lines); independent.Step(commands);
                Assert.That(a.GetAthlete(0).LastFrame, Is.EqualTo(independent.GetAthlete(0).LastFrame));
                privateLoop |= a.GetPhysicalPoint(0).LoopId == "mat_1";
            }
            Assert.That(a.IsComplete && independent.IsComplete, Is.True); Assert.That(privateLoop, Is.True);
            Assert.That(a.MassStartResult.ToJson(), Is.EqualTo(independent.MassStartResult.ToJson()));
        }

        [Test] public void PrivateCurvatureChangesAcceptedMotionWithoutChangingTheBaseRouteTerrain()
        {
            var curvedFixture = CreateFixture(1); var flatFixture = CreateFixture(1, flatPrivate: true);
            var curved = CreatePack(curvedFixture, new RaceEventLog(10000)); var flat = CreatePack(flatFixture, new RaceEventLog(10000));
            var commands = new SprintAthleteInput[1]; var lines = new double[1]; bool differentMotion = false;
            while (!curved.IsComplete && !flat.IsComplete && curved.Tick.Value < 60000)
            {
                PackSprintFieldTests.Fill(curved, commands); curved.Step(commands, lines); flat.Step(commands, lines);
                differentMotion |= curved.GetAthlete(0).Motion.DistanceNanometers != flat.GetAthlete(0).Motion.DistanceNanometers;
            }
            Assert.That(curvedFixture.Route.Sample(new TrackDistance(35 * 1000)).Track.CurvaturePerMeter,
                Is.EqualTo(flatFixture.Route.Sample(new TrackDistance(35 * 1000)).Track.CurvaturePerMeter));
            Assert.That(curvedFixture.Layout.GetPath(0).SampleRoute(35 * SkiState.NanometersPerMeter).CurvaturePerMeter,
                Is.Not.EqualTo(flatFixture.Layout.GetPath(0).SampleRoute(35 * SkiState.NanometersPerMeter).CurvaturePerMeter));
            Assert.That(differentMotion, Is.True, "Changing only private curvature must affect accepted path physics.");
        }

        [Test] public void WideApproachAllowsLateralMovementThenRecentersGraduallyBeforeTheNarrowPrivatePath()
        {
            var f = CreateFixture(1); var field = CreatePack(f, new RaceEventLog(5000));
            var commands = new SprintAthleteInput[1]; var lines = new double[1]; bool wideMovement = false, enteredPrivate = false;
            double previousLine = 0; double step = OvertakeTests.Definition().LateralSpeed / field.TickFrequency;
            while (!enteredPrivate && field.Tick.Value < 3000)
            {
                lines[0] = field.GetAthlete(0).RouteNanometers < 2 * SkiState.NanometersPerMeter ? 2 : 0;
                PackSprintFieldTests.Fill(field, commands); field.Step(commands, lines);
                double lateral = field.GetLateral(0);
                Assert.That(Math.Abs(lateral - previousLine), Is.LessThanOrEqualTo(step + 1e-9));
                previousLine = lateral; AssertBodiesAndWidth(field, f);
                wideMovement |= lateral > 0.6 + 1e-9;
                enteredPrivate = field.GetPhysicalPoint(0).LoopId == "mat_1";
            }
            Assert.That(wideMovement, Is.True, "Private width must not globally limit the six-metre main approach.");
            Assert.That(enteredPrivate, Is.True);
            Assert.That(Math.Abs(field.GetLateral(0)), Is.LessThanOrEqualTo(0.6));
        }

        [Test] public void DraftingReadsTheSharedExitSamplePermissionRatherThanTheCanonicalBypass()
        {
            // A test-only uphill exit lets zero-effort athletes stop through actual forces,
            // collect a close pair, and then release the leader without repositioning bodies.
            var f = CreateFixture(exitDraftAllowed: true, exitGrade: 0.05); var field = CreatePack(f, new RaceEventLog(20000));
            var commands = new SprintAthleteInput[2]; var lines = new double[2]; var wasExit = new bool[2];
            var entered = new bool[2]; var old = new SkiPlacement[2]; var oldRoute = new long[2]; var expected = new DraftOutput[2];
            var system = new DraftingSystem(DraftingTests.Definition()); bool actualExitDraft = false, release = false;
            while (!field.IsComplete && field.Tick.Value < 80000 && !actualExitDraft)
            {
                int leader = -1;
                for (int i = 0; i < 2; i++)
                {
                    var actor = field.GetAthlete(i); var point = field.GetPhysicalPoint(i);
                    wasExit[i] = point.LoopId == "exit_aisle"; entered[i] |= wasExit[i]; oldRoute[i] = actor.RouteNanometers;
                    old[i] = new SkiPlacement(actor.AthleteId, point.LoopId,
                        new SkiState(point.LoopLengthNanometers + point.LocalNanometers, actor.Motion.SpeedMetersPerSecond), field.GetLateral(i));
                    if (wasExit[i] && (leader < 0 || point.LocalNanometers > field.GetPhysicalPoint(leader).LocalNanometers)) leader = i;
                }
                if (wasExit[0] && wasExit[1] && leader >= 0)
                {
                    long gap = Math.Abs(field.GetPhysicalPoint(0).LocalNanometers - field.GetPhysicalPoint(1).LocalNanometers);
                    double releaseGap = (DraftingTests.Definition().MinimumGap + DraftingTests.Definition().MaximumGap) / 2;
                    release |= gap / (double)SkiState.NanometersPerMeter < releaseGap;
                }
                for (int i = 0; i < 2; i++)
                {
                    int control = wasExit[i] && !release && (!entered[1 - i] || i == leader) ? 0 : 800;
                    var id = field.GetAthlete(i).AthleteId; var tick = field.Tick.Next();
                    commands[i] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, id, control), 1, 1, 0),
                        new WeaponAimCommand(tick, id, 0, 0), true);
                    expected[i] = wasExit[i] ? system.Evaluate(old[i], old, f.Layout.GetPath(i).SampleRoute(oldRoute[i])) : default;
                }
                field.Step(commands, lines);
                for (int i = 0; i < 2; i++) if (wasExit[i]) Assert.That(field.GetDraft(i), Is.EqualTo(expected[i]));
                for (int i = 0; i < 2; i++) if (wasExit[i] && field.GetDraft(i).Strength > 0)
                {
                    actualExitDraft = true;
                    Assert.That(f.Route.Sample(new TrackDistance(oldRoute[i] / SkiState.NanometersPerMillimeter)).Track.DraftAllowed, Is.False);
                }
                AssertBodiesAndWidth(field, f);
            }
            if (!actualExitDraft)
            {
                TestContext.Out.WriteLine("Exit draft terminal tick=" + field.Tick.Value + "; released=" + release);
                for (int i = 0; i < 2; i++)
                    TestContext.Out.WriteLine("actor=" + i + "; entered=" + entered[i] + "; point=" + field.GetPhysicalPoint(i).LoopId +
                        ":" + field.GetPhysicalPoint(i).LocalNanometers + "; route=" + field.GetAthlete(i).RouteNanometers +
                        "; speed=" + field.GetAthlete(i).Motion.SpeedMetersPerSecond + "; phase=" + field.GetAthlete(i).CurrentRangeMotion?.Phase);
            }
            Assert.That(actualExitDraft, Is.True, "Same-loop exit followers should receive authored drafting permission.");
        }

        [Test] public void WarmMovementOnTheSharedPhysicalMainAllocatesNoManagedMemory()
        {
            var f = CreateFixture(1); var field = CreatePack(f, new RaceEventLog(10000));
            var commands = new SprintAthleteInput[1]; var lines = new double[1];
            while (field.GetAthlete(0).RouteNanometers < 130 * SkiState.NanometersPerMeter && field.Tick.Value < 20000)
            { PackSprintFieldTests.Fill(field, commands); field.Step(commands, lines); }
            Assert.That(field.GetPhysicalPoint(0).LoopId, Is.EqualTo("main"));
            long room = 280 * SkiState.NanometersPerMeter - field.GetAthlete(0).RouteNanometers;
            long maximumTickTravel = (long)Math.Ceiling(SkiForceTests.Definition().MaximumSpeedMetersPerSecond /
                field.TickFrequency * SkiState.NanometersPerMeter);
            int count = (int)Math.Min(100, room / maximumTickTravel);
            Assert.That(count, Is.GreaterThan(0));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < count; i++) { PackSprintFieldTests.Fill(field, commands); field.Step(commands, lines); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(field.GetAthlete(0).RouteNanometers, Is.LessThan(300 * SkiState.NanometersPerMeter));
            Assert.That(allocated, Is.Zero);
        }

        [Test] public void OrderedPathsAcceptTwoSameTickFirstArrivalsAndKeepSequentialRepeatedBoutAssignments()
        {
            var policy = MassStartRangePolicyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json"));
            var f = CreateFixture(gridColumns: 2); var repeatedFixture = CreateFixture(gridColumns: 2);
            var log = new RaceEventLog(30000); var repeated = new RaceEventLog(30000);
            var a = CreatePack(f, log, policy); var b = CreatePack(repeatedFixture, repeated, policy);
            var commands = new SprintAthleteInput[2]; var lines = new double[2];
            while (!a.IsComplete && a.Tick.Value < 80000)
            {
                for (int i = 0; i < 2; i++)
                {
                    var actor = a.GetAthlete(i); var point = a.GetPhysicalPoint(i);
                    var tick = a.Tick.Next();
                    commands[i] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, actor.AthleteId, 800), 1, 1, 0),
                        new WeaponAimCommand(tick, actor.AthleteId, 0, 0), true);
                    long offset = actor.RouteNanometers % (300 * SkiState.NanometersPerMeter);
                    // Separate lanes in the six-metre common approach; each selected path
                    // centers before its own private fork. Mat bodies remain physically present.
                    long centerAt = (actor.RangePath.LaneOrdinal == 1 ? 5 : 25) * SkiState.NanometersPerMeter;
                    lines[i] = point.LoopId == "main" && (offset < centerAt || offset >= 100 * SkiState.NanometersPerMeter)
                        ? (i == 0 ? -1 : 1) : 0;
                }
                a.Step(commands, lines); b.Step(commands, lines); AssertBodiesAndWidth(a, f);
                for (int i = 0; i < 2; i++)
                {
                    var actor = a.GetAthlete(i);
                    Assert.That(actor.LastFrame, Is.EqualTo(b.GetAthlete(i).LastFrame));
                    Assert.That(actor.AssignedShootingLane.Ordinal, Is.EqualTo(actor.RangePath.LaneOrdinal));
                    if (actor.CurrentRangeMotion?.Phase == RangeTraversalPhase.Shooting)
                        Assert.That(a.GetPhysicalPoint(i).LoopId, Is.EqualTo(actor.RangePath.MatAnchor.LoopId));
                }
            }
            Assert.That(a.IsComplete && b.IsComplete, Is.True);
            var visits = new int[2]; var assigned = new int[2, 4]; long firstTick = -1; int firstAssignments = 0;
            for (int e = 0; e < log.Count; e++) if (log[e].EventType == RaceEventType.PhysicalRangePathAssigned)
            {
                int actor = log[e].AthleteId.Equals(a.GetAthlete(0).AthleteId) ? 0 : 1;
                int bout = visits[actor]++;
                Assert.That(bout, Is.LessThan(4));
                assigned[actor, bout] = log[e].Payload;
                if (bout == 0)
                {
                    Assert.That(log[e].Payload, Is.EqualTo(actor + 1));
                    if (firstTick < 0) firstTick = log[e].Tick.Value;
                    Assert.That(log[e].Tick.Value, Is.EqualTo(firstTick)); firstAssignments++;
                }
            }
            Assert.That(firstAssignments, Is.EqualTo(2)); Assert.That(visits, Is.All.EqualTo(4));
            for (int bout = 0; bout < 4; bout++)
            {
                Assert.That(a.GetRangeArrivalCount(bout), Is.EqualTo(2));
                var first = a.GetAcceptedRangeArrival(0, bout); var second = a.GetAcceptedRangeArrival(1, bout);
                Assert.That(first.BoutIndex, Is.EqualTo(bout)); Assert.That(second.BoutIndex, Is.EqualTo(bout));
                Assert.That(first.AthleteId, Is.EqualTo(a.GetAthlete(0).AthleteId));
                Assert.That(second.AthleteId, Is.EqualTo(a.GetAthlete(1).AthleteId));
                Assert.That(first.AdmissionRouteNanometers,
                    Is.EqualTo((bout * 300L + 5) * SkiState.NanometersPerMeter));
                Assert.That(first.AdmissionRouteNanometers, Is.EqualTo(second.AdmissionRouteNanometers));
                if (bout > 0)
                {
                    bool actorZeroEarlier = first.Tick.Value < second.Tick.Value ||
                        first.Tick.Value == second.Tick.Value && first.Fraction <= second.Fraction;
                    Assert.That(assigned[actorZeroEarlier ? 0 : 1, bout], Is.EqualTo(1));
                    Assert.That(assigned[actorZeroEarlier ? 1 : 0, bout], Is.EqualTo(2));
                }
            }
            Assert.That(log.Count, Is.EqualTo(repeated.Count));
            for (int i = 0; i < log.Count; i++) Assert.That(log[i], Is.EqualTo(repeated[i]));
            Assert.That(a.MassStartResult.ToJson(), Is.EqualTo(b.MassStartResult.ToJson()));
        }
    }
}

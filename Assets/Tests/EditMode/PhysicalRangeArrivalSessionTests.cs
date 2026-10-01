using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangeArrivalSessionTests
    {
        private const int Frequency = 20;
        private const long AdmissionLine = 5 * SkiState.NanometersPerMeter;

        private static SprintAthleteSession[] Create(RaceEventLog log, out PhysicalRangeArrivalOrder order,
            out PhysicalRangePathMap[] choices)
        {
            var profile = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(
                StartApproachTests.Profile().ToJson(), "distance_m", "1500"));
            // A dedicated test approach satisfies the Mass Start adapter contract. Both test
            // athletes occupy the front row, so neither receives approach distance credit.
            const string approachLoop = "{\"id\":\"arrival_start\",\"kind\":\"race\",\"closed\":false," +
                "\"segments\":[{\"id\":\"arrival_start_segment\",\"length_m\":30,\"start_grade\":0,\"end_grade\":0," +
                "\"curvature_per_m\":0,\"width_m\":6,\"snow_profile_id\":\"packed\",\"wind_exposure\":1," +
                "\"terrain_muscle_factor\":1,\"draft_allowed\":false,\"overtake_allowed\":false," +
                "\"risk_level\":0,\"semantic_zone\":\"stadium\"}]}";
            string trackJson = Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("track"),
                "\"loops\"\\s*:\\s*\\[", match => match.Value + approachLoop + ",");
            var track = TrackDefinition.FromJson(trackJson);
            var requirements = profile.ToRouteRequirements();
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("arrival_session_route", track.Id, "main", requirements), requirements);
            var junctions = PhysicalJunctionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("junctions"), track);
            var connections = PhysicalConnectionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("connections"), junctions);
            var definition = PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"), track, junctions, connections);
            var map = new PhysicalTrackMap(track, route);
            choices = new[] { new PhysicalRangePathMap(map, track, route, definition, 0), new PhysicalRangePathMap(map, track, route, definition, 1) };
            var roster = new[] { new AthleteId("arrival_session_0"), new AthleteId("arrival_session_1") };
            var grid = new MassStartGrid(profile, MassStartGridTests.Definition(), OccupancyTests.Definition(), roster, 6, 30);
            order = new PhysicalRangeArrivalOrder(profile, grid,
                MassStartRangePolicyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json")), Frequency);
            var approach = new StartApproachMap(track, route, StartApproachDefinition.FromJson(
                "{\"schema_version\":1,\"id\":\"arrival_start_v1\",\"track_id\":\"" + track.Id +
                "\",\"loop_id\":\"arrival_start\",\"route_entry_loop_id\":\"main\"}"));
            var skiTemplate = TimeTrialTests.Athlete();
            var shootingTemplate = ShootingSessionTests.Athlete();
            var athletes = new SprintAthleteSession[roster.Length];
            for (int i = 0; i < athletes.Length; i++)
            {
                var ski = TimeTrialAthlete.FromJson(skiTemplate.ToJson().Replace(skiTemplate.Id, roster[i].Value));
                var shooting = ShootingAthleteDefinition.FromJson(shootingTemplate.ToJson().Replace(shootingTemplate.Id, roster[i].Value));
                // Opposite initial lanes make successful path publication observable.
                var original = choices[1 - i];
                athletes[i] = new SprintAthleteSession(new SimulationConfig(1, "arrival_session_sim", Frequency, 123), profile,
                    new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), ski, shooting,
                    track, route, new CheckpointLayout(track, route),
                    RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                    "ski_neutral", new SimulationTick(1), new ShootingLaneAssignment(track.Range.Id, original.LaneId, original.LaneOrdinal),
                    log, approach, -grid.Get(i).LongitudinalNanometers, original, choices);
            }
            Assert.That(route.Length.Meters, Is.EqualTo(1500));
            Assert.That(route.LapCount, Is.EqualTo(5));
            Assert.That(profile.BoutCount, Is.EqualTo(4));
            return athletes;
        }

        private static int AssignmentCount(RaceEventLog log)
        {
            int count = 0;
            for (int i = 0; i < log.Count; i++) if (log[i].EventType == RaceEventType.PhysicalRangePathAssigned) count++;
            return count;
        }

        private static bool Arrival(SprintAthleteSession athlete, SprintStepPreview preview, out PhysicalRangeArrival arrival)
        {
            var segments = new RaceMotionSegment[preview.Movement.SegmentCount];
            for (int i = 0; i < segments.Length; i++) segments[i] = athlete.GetPreparedSegment(preview, i);
            return PhysicalRangeArrival.TryFromAcceptedMotion(athlete.AthleteId, 0, preview.Tick, Frequency, AdmissionLine,
                new RaceMotionPoint(athlete.RouteNanometers, athlete.Motion.DistanceNanometers), segments, out arrival);
        }

        private static void CommitValidated(SprintAthleteSession[] athletes, SprintStepPreview[] previews,
            PhysicalRangeArrivalOrder order, PhysicalRangeArrivalPreview arrivalPreview)
        {
            order.ValidatePreview(arrivalPreview);
            // These owned accessors validate every athlete handle before the first publication.
            // The fixture verifies receipt/order/path cooperation, not collision-safe field admission.
            for (int i = 0; i < athletes.Length; i++) athletes[i].GetPreparedRangePath(previews[i]);
            for (int i = 0; i < athletes.Length; i++) athletes[i].CommitStep(previews[i]);
            order.Commit(arrivalPreview);
        }

        [Test] public void RejectedCommonLinePrefixesPublishNothingAndFinalReceiptsCommitMatchingBibPaths()
        {
            var log = new RaceEventLog(2000);
            var athletes = Create(log, out var order, out var choices);
            var previews = new SprintStepPreview[athletes.Length];
            var arrivals = new PhysicalRangeArrival[athletes.Length];
            bool rejected = false;
            while (order.GetArrivalCount(0) < athletes.Length && order.Tick.Value < 3000)
            {
                int arrivalCount = 0;
                var nextTick = order.Tick.Next();
                for (int i = 0; i < athletes.Length; i++)
                {
                    var athlete = athletes[i];
                    Assert.That(athlete.Tick, Is.EqualTo(order.Tick));
                    previews[i] = order.GetAssignedOrdinal(i, 0) == 0
                        ? athlete.PrepareRangePathStep(SprintAthleteSessionTests.Script(athlete), athlete.CreateRangePathChoice(i + 1))
                        : athlete.PrepareStep(SprintAthleteSessionTests.Script(athlete));
                    if (order.GetAssignedOrdinal(i, 0) == 0 && Arrival(athlete, previews[i], out var arrival))
                        arrivals[arrivalCount++] = arrival;
                }
                var tentative = order.Prepare(nextTick, arrivals.AsSpan(0, arrivalCount));
                if (!rejected && arrivalCount > 0)
                {
                    Assert.That(arrivalCount, Is.EqualTo(2));
                    Assert.That(order.GetPreparedArrivalCount(tentative, 0), Is.EqualTo(2));
                    Assert.That(order.GetArrivalCount(0), Is.Zero);
                    var replaced = (SprintStepPreview[])previews.Clone();
                    for (int i = 0; i < athletes.Length; i++)
                    {
                        Assert.That(order.GetPreparedOrdinal(tentative, i, 0), Is.EqualTo(i + 1));
                        Assert.That(athletes[i].RangePath, Is.SameAs(choices[1 - i]));
                        previews[i] = athletes[i].ConstrainPreparedTravel(previews[i], 0);
                        Assert.That(Arrival(athletes[i], previews[i], out _), Is.False);
                    }
                    var cancelled = order.Prepare(nextTick, ReadOnlySpan<PhysicalRangeArrival>.Empty);
                    Assert.Throws<ArgumentException>(() => order.Commit(tentative));
                    for (int i = 0; i < athletes.Length; i++)
                    {
                        int index = i;
                        Assert.Throws<ArgumentException>(() => athletes[index].CommitStep(replaced[index]));
                    }
                    CommitValidated(athletes, previews, order, cancelled);
                    Assert.That(order.GetArrivalCount(0), Is.Zero);
                    Assert.That(AssignmentCount(log), Is.Zero);
                    for (int i = 0; i < athletes.Length; i++)
                    {
                        Assert.That(order.GetAssignedOrdinal(i, 0), Is.Zero);
                        Assert.That(order.GetLastAssignedBout(i), Is.EqualTo(-1));
                        Assert.That(athletes[i].RangePath, Is.SameAs(choices[1 - i]));
                        Assert.That(athletes[i].AssignedShootingLane.Ordinal, Is.EqualTo(2 - i));
                        Assert.That(athletes[i].RouteNanometers, Is.LessThan(AdmissionLine));
                    }
                    rejected = true;
                    continue;
                }
                for (int i = 0; i < athletes.Length; i++)
                    if (order.GetPreparedOrdinal(tentative, i, 0) != 0)
                        Assert.That(athletes[i].GetPreparedRangePath(previews[i]).LaneOrdinal,
                            Is.EqualTo(order.GetPreparedOrdinal(tentative, i, 0)));
                CommitValidated(athletes, previews, order, tentative);
            }
            Assert.That(rejected, Is.True);
            Assert.That(order.GetArrivalCount(0), Is.EqualTo(2));
            Assert.That(AssignmentCount(log), Is.EqualTo(2));
            for (int i = 0; i < athletes.Length; i++)
            {
                Assert.That(order.GetAssignedOrdinal(i, 0), Is.EqualTo(i + 1));
                Assert.That(athletes[i].AssignedShootingLane.Ordinal, Is.EqualTo(i + 1));
                Assert.That(athletes[i].RangePath, Is.SameAs(choices[i]));
                Assert.That(order.GetAcceptedArrival(i, 0).AthleteId, Is.EqualTo(athletes[i].AthleteId));
                Assert.That(order.GetAcceptedArrival(i, 0).AdmissionRouteNanometers, Is.EqualTo(AdmissionLine));
                Assert.That(order.GetAcceptedArrival(i, 0).Tick, Is.EqualTo(athletes[i].Tick));
                Assert.That(athletes[i].RouteNanometers, Is.GreaterThanOrEqualTo(AdmissionLine));
            }
        }
    }
}

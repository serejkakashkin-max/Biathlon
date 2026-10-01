using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePackTransactionTests
    {
        private static MassStartRangePolicyDefinition Policy() => MassStartRangePolicyDefinition.FromJson(
            PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json"));
        private static PackSprintFieldSession Pack(PhysicalRangePackTests.Fixture f, RaceEventLog log, int frequency = 20)
            => new PackSprintFieldSession(new SimulationConfig(1, "pack_guard_sim", frequency, 123), f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), f.Track, f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                SprintResultsTests.Timing(), f.Entrants, new SimulationTick(1), "pack_guard_result", OccupancyTests.Definition(),
                DraftingTests.Definition(), OvertakeTests.Definition(), f.Junctions, f.Connections, log,
                massGrid:f.Grid, startApproach:f.Approach, rangePolicy:Policy(), rangeLayout:f.Layout);

        [Test] public void InsufficientSharedPrefixRejectsBeforePublishingAnArrivalOrEvent()
        {
            var f = PhysicalRangePackTests.CreateFixture(); var log = new RaceEventLog(100);
            var error = Assert.Throws<ArgumentException>(() => Pack(f, log, frequency:10));
            Assert.That(error.Message, Does.Contain("identical shared path")); Assert.That(log.Count, Is.Zero);
        }

        [Test] public void ChangedCapturedConnectionTopologyRejectsEvenWhenAllCoordinatesMatch()
        {
            var f = PhysicalRangePackTests.CreateFixture(); var log = new RaceEventLog(100);
            f.Connections = PhysicalConnectionDefinition.FromJson(f.Connections.ToJson().Replace(
                "range_path_contract_connections_v1", "range_path_changed_connections_v1"), f.Junctions);
            var error = Assert.Throws<ArgumentException>(() => Pack(f, log));
            Assert.That(error.Message, Does.Contain("captured range-path topology")); Assert.That(log.Count, Is.Zero);
        }

        [Test] public void InvalidLastCommandRowDoesNotPublishBodyPathClockOrArrival()
        {
            var f = PhysicalRangePackTests.CreateFixture(); var log = new RaceEventLog(1000); var pack = Pack(f, log);
            var inputs = new SprintAthleteInput[2]; var lines = new double[2];
            PackSprintFieldTests.Fill(pack, inputs); pack.Step(inputs, lines);
            var tick = pack.Tick; int events = log.Count;
            var points = new[] { pack.GetPhysicalPoint(0), pack.GetPhysicalPoint(1) };
            var frames = new[] { pack.GetAthlete(0).LastFrame, pack.GetAthlete(1).LastFrame };
            PackSprintFieldTests.Fill(pack, inputs); lines[1] = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => pack.Step(inputs, lines));
            Assert.That(pack.Tick, Is.EqualTo(tick)); Assert.That(log.Count, Is.EqualTo(events));
            Assert.That(pack.GetRangeArrivalCount(0), Is.Zero);
            for (int i = 0; i < 2; i++)
            {
                Assert.That(pack.GetAthlete(i).LastFrame, Is.EqualTo(frames[i]));
                Assert.That(pack.GetPhysicalPoint(i), Is.EqualTo(points[i]));
                Assert.That(pack.GetAthlete(i).RangePath, Is.SameAs(f.Layout.GetPath(i)));
            }
        }

        [Test] public void FrontBodyStopsBeforeNarrowForkAndCanRecenterWithoutLateralTeleport()
        {
            var f = PhysicalRangePackTests.CreateFixture(1); var field = PhysicalRangePackTests.CreatePack(f, new RaceEventLog(5000));
            var inputs = new SprintAthleteInput[1]; var lines = new[] { 0.8 };
            double half = OccupancyTests.Definition().LongitudinalSeparation * .5;
            bool reachedStop = false;
            for (int n = 0; n < 1000; n++)
            {
                PackSprintFieldTests.Fill(field, inputs); field.Step(inputs, lines);
                long route = field.GetAthlete(0).RouteNanometers;
                if (field.GetLateral(0) > 0.6)
                    Assert.That(route / (double)SkiState.NanometersPerMeter + half, Is.LessThan(10),
                        "The front body must fit the private corridor before its center crosses the seam.");
                if (field.GetAthlete(0).Motion.SpeedMetersPerSecond == 0 && route > 8 * SkiState.NanometersPerMeter)
                { reachedStop = true; break; }
            }
            Assert.That(reachedStop, Is.True); Assert.That(field.GetPhysicalPoint(0).LoopId, Is.EqualTo("main"));
            lines[0] = 0; bool entered = false;
            for (int n = 0; n < 500 && !entered; n++)
            {
                double old = field.GetLateral(0); PackSprintFieldTests.Fill(field, inputs); field.Step(inputs, lines);
                Assert.That(Math.Abs(field.GetLateral(0) - old), Is.LessThanOrEqualTo(OvertakeTests.Definition().LateralSpeed / field.TickFrequency + 1e-9));
                entered = field.GetPhysicalPoint(0).LoopId == "mat_1";
            }
            Assert.That(entered, Is.True);
        }
    }
}

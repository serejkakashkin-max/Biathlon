using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ReusableRangePackTests
    {
        private static PursuitQualificationSnapshot Qualification(PhysicalRangePackTests.Fixture f)
        {
            var ids = PhysicalRangeReservationsTests.Roster(f);
            var source = new SprintResultBook(CompetitionProfileTests.TestProfile(), SprintResultsTests.Timing(), ids, 20, new RaceEventLog(100));
            for (int i = 0; i < ids.Length; i++) source.Record(SprintResultsTests.Finisher(ids[i].Value, 1, 200 + 2 * i));
            return PursuitQualificationSnapshot.Create("reservation_qualification", source.Complete("reservation_source"), f.Profile);
        }
        private static PackSprintFieldSession Create(PhysicalRangePackTests.Fixture f, RaceEventLog log, bool reusable = true,
            OccupancyDefinition body = null, bool routeBoundJunctions = false)
            => new PackSprintFieldSession(new SimulationConfig(1, "reservation_pack_sim", 20, 123), f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), f.Track, f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed", "packed")),
                SprintResultsTests.Timing(), f.Entrants, new SimulationTick(1), "reservation_pack_result", body ?? OccupancyTests.Definition(),
                DraftingTests.Definition(), OvertakeTests.Definition(), f.Junctions, f.Connections, log,
                qualification: f.Profile.Format == CompetitionFormat.Pursuit ? Qualification(f) : null,
                rangeLayout: f.Layout, reusableRangeMats: reusable, routeBoundJunctions: routeBoundJunctions);
        private static void Fill(PackSprintFieldSession field, SprintAthleteInput[] commands, bool trigger)
        {
            var tick = field.Tick.Next();
            for (int i = 0; i < commands.Length; i++)
            {
                var id = field.GetAthlete(i).AthleteId;
                commands[i] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, id, 800), 1, 1, 0),
                    new WeaponAimCommand(tick, id, 0, 0), trigger);
            }
        }
        private static void AssertPublishedEqual(PackSprintFieldSession a, PackSprintFieldSession b)
        {
            Assert.That(a.Tick, Is.EqualTo(b.Tick)); Assert.That(a.RangeReservations.Tick, Is.EqualTo(a.Tick));
            Assert.That(b.RangeReservations.Tick, Is.EqualTo(b.Tick));
            for (int i = 0; i < a.Count; i++)
            {
                Assert.That(a.GetAthlete(i).LastFrame, Is.EqualTo(b.GetAthlete(i).LastFrame));
                var point = a.GetPhysicalPoint(i); var other = b.GetPhysicalPoint(i);
                Assert.That(point.LoopId, Is.EqualTo(other.LoopId));
                Assert.That(point.LocalNanometers, Is.EqualTo(other.LocalNanometers));
                Assert.That(point.LoopLengthNanometers, Is.EqualTo(other.LoopLengthNanometers));
                Assert.That(point.Closed, Is.EqualTo(other.Closed));
                Assert.That(a.GetLateral(i), Is.EqualTo(b.GetLateral(i)));
                Assert.That(a.GetAssignedRangeLane(i), Is.EqualTo(b.GetAssignedRangeLane(i)));
                Assert.That(a.IsWaitingForRangeCapacity(i), Is.EqualTo(b.IsWaitingForRangeCapacity(i)));
                for (int bout = 0; bout < a.Profile.BoutCount; bout++)
                    Assert.That(a.RangeReservations.GetRecord(i, bout), Is.EqualTo(b.RangeReservations.GetRecord(i, bout)));
            }
        }

        [Test] public void CapacityDenialReplansWhileOtherAthletesMoveThenReusesAReleasedMat()
        {
            var f = PhysicalRangeReservationsTests.Fixture(); var log = new RaceEventLog(30000); var field = Create(f, log);
            var commands = new SprintAthleteInput[field.Count]; var lines = new double[field.Count];
            bool waitedWithMovement = false, sustainedHold = false, reused = false; int holdTicks = 0;
            for (int n = 0; n < 3000 && !sustainedHold; n++)
            {
                var before = new long[field.Count]; for (int i = 0; i < field.Count; i++) before[i] = field.GetAthlete(i).RouteNanometers;
                Fill(field, commands, false); field.Step(commands, lines); PhysicalRangePackTests.AssertBodiesAndWidth(field, f);
                bool waiting = false, moving = false;
                for (int i = 0; i < field.Count; i++)
                {
                    moving |= field.GetAthlete(i).RouteNanometers > before[i];
                    if (!field.IsWaitingForRangeCapacity(i)) continue;
                    waiting = true;
                    Assert.That(field.GetAssignedRangeLane(i), Is.Zero);
                    Assert.That(field.GetAthlete(i).RouteNanometers, Is.LessThan(field.RangeReservations.AdmissionRouteNanometers(0)));
                    Assert.That(field.RangeReservations.GetRecord(i, 0).IsValid, Is.False);
                }
                waitedWithMovement |= waiting && moving;
                if (waiting) holdTicks++;
                sustainedHold = holdTicks >= 5 && field.GetAthlete(0).CurrentRangeMotion?.Phase == RangeTraversalPhase.Shooting &&
                    field.GetAthlete(1).CurrentRangeMotion?.Phase == RangeTraversalPhase.Shooting;
            }
            Assert.That(waitedWithMovement, Is.True, "A real capacity replan must include an uncapped moving actor.");
            Assert.That(sustainedHold, Is.True); Assert.That(field.GetRangeArrivalCount(0), Is.EqualTo(2));
            // Warm a sustained capacity hold before measuring the new replan branch.
            Fill(field, commands, false); field.Step(commands, lines);
            long beforeAllocation = GC.GetAllocatedBytesForCurrentThread();
            for (int n = 0; n < 100; n++) { Fill(field, commands, false); field.Step(commands, lines); }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - beforeAllocation;
            Assert.That(allocated, Is.Zero, "Warm capacity hold steps must retain their preallocated scratch.");
            var tick = field.Tick; int eventCount = log.Count;
            var frames = new SprintAthleteFrame[field.Count]; var points = new PhysicalTrackPoint[field.Count]; var waitingFlags = new bool[field.Count];
            for (int i = 0; i < field.Count; i++) { frames[i] = field.GetAthlete(i).LastFrame; points[i] = field.GetPhysicalPoint(i); waitingFlags[i] = field.IsWaitingForRangeCapacity(i); }
            Fill(field, commands, true); lines[lines.Length - 1] = double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(() => field.Step(commands, lines));
            Assert.That(field.Tick, Is.EqualTo(tick)); Assert.That(log.Count, Is.EqualTo(eventCount));
            for (int i = 0; i < field.Count; i++)
            {
                Assert.That(field.GetAthlete(i).LastFrame, Is.EqualTo(frames[i])); Assert.That(field.GetPhysicalPoint(i), Is.EqualTo(points[i]));
                Assert.That(field.IsWaitingForRangeCapacity(i), Is.EqualTo(waitingFlags[i]));
            }
            lines[lines.Length - 1] = 0;
            while (!field.IsComplete && field.Tick.Value < 60000)
            {
                Fill(field, commands, true); field.Step(commands, lines); PhysicalRangePackTests.AssertBodiesAndWidth(field, f);
                reused |= field.RangeReservations.GetRecord(2, 0).IsValid;
            }
            Assert.That(field.IsComplete && reused, Is.True);
            Assert.That(field.RangeReservations.GetRecord(2, 0).ArrivalRank, Is.EqualTo(3));
            Assert.That(field.RangeReservations.GetRecord(2, 0).LaneOrdinal, Is.InRange(1, 2));
            for (int i = 0; i < field.Count; i++) Assert.That(field.RangeReservations.GetOwnedLane(i), Is.Zero);
        }

        [TestCase(false)] [TestCase(true)]
        public void CompleteThreeOnTwoSprintAndPursuitTapeRederivesExactReservationsFramesEventsAndResults(bool pursuit)
        {
            var f = PhysicalRangeReservationsTests.Fixture(pursuit: pursuit); var log = new RaceEventLog(50000); var source = Create(f, log);
            var tape = new PackSprintRaceCommandTape("reservation_tape", source, 100);
            var commands = new SprintAthleteInput[source.Count]; var lines = new double[source.Count];
            bool waited = false, retainedPath = false, retainedPrivateTail = false;
            while (!source.IsComplete && source.Tick.Value < 80000)
            {
                Fill(source, commands, source.Tick.Value >= 600);
                long before = GC.GetAllocatedBytesForCurrentThread();
                tape.StepAndRecord(source, commands, lines);
                long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
                if(source.RangeReservations.LastChangeCount > 0 && source.GetRangeArrivalCount(0) >= source.Count)
                    Assert.That(bytes, Is.Zero, "Warm accepted reservation acquisition/release must allocate no managed memory.");
                PhysicalRangePackTests.AssertBodiesAndWidth(source, f);
                for (int i = 0; i < source.Count; i++)
                {
                    var actor = source.GetAthlete(i); int owned = source.RangeReservations.GetOwnedLane(i);
                    waited |= source.IsWaitingForRangeCapacity(i);
                    if (owned > 0)
                    {
                        int bout = source.RangeReservations.GetOwnedBout(i); long clearance = source.RangeReservations.ClearanceRouteNanometers(owned, bout);
                        if (source.GetPhysicalPoint(i).LoopId == "exit_aisle" && actor.RouteNanometers < clearance)
                        {
                            retainedPrivateTail = true;
                            Assert.That(source.RangeReservations.GetRecord(i, bout).IsReleased, Is.False);
                        }
                    }
                    else if (source.GetPhysicalPoint(i).LoopId == "exit_aisle")
                    {
                        retainedPath = true;
                        Assert.That(actor.RangePath.LaneId, Is.EqualTo(actor.AssignedShootingLane.LaneId));
                    }
                }
            }
            Assert.That(source.IsComplete, Is.True); Assert.That(waited && retainedPath && retainedPrivateTail, Is.True);
            var restored = PackSprintRaceCommandTape.FromJson(tape.ToJson()); Assert.That(restored.ToJson(), Is.EqualTo(tape.ToJson()));
            var replayLog = new RaceEventLog(50000); var target = Create(PhysicalRangeReservationsTests.Fixture(pursuit: pursuit), replayLog);
            var direct = Create(PhysicalRangeReservationsTests.Fixture(pursuit: pursuit), new RaceEventLog(50000)); var replay = restored.CreateReplay(target);
            while (replay.HasNext)
            {
                Fill(direct, commands, direct.Tick.Value >= 600); direct.Step(commands, lines); replay.Step(); AssertPublishedEqual(direct, target);
            }
            AssertPublishedEqual(source, target);
            Assert.That(replayLog.Count, Is.EqualTo(log.Count));
            int acquisitions = 0, releases = 0, paths = 0;
            for (int i = 0; i < log.Count; i++)
            {
                Assert.That(replayLog[i], Is.EqualTo(log[i]));
                if (log[i].EventType == RaceEventType.PhysicalRangeReservationAcquired) acquisitions++;
                if (log[i].EventType == RaceEventType.PhysicalRangeReservationReleased) releases++;
                if (log[i].EventType == RaceEventType.PhysicalRangePathAssigned) paths++;
            }
            Assert.That(acquisitions, Is.EqualTo(source.Count * source.Profile.BoutCount));
            Assert.That(releases, Is.EqualTo(acquisitions)); Assert.That(paths, Is.EqualTo(acquisitions));
            for (int i = 0; i < source.Count; i++)
            {
                var actor = source.GetAthlete(i); Assert.That(actor.Lifecycle.CompletedBouts, Is.EqualTo(source.Profile.BoutCount));
                Assert.That(actor.Lifecycle.CompletedLaps, Is.EqualTo(source.Profile.LapCount));
                Assert.That(actor.Motion.DistanceNanometers, Is.EqualTo((source.Profile.Distance.Meters + 150L * actor.TotalPenaltyLoops) * SkiState.NanometersPerMeter));
                for (int bout = 0; bout < source.Profile.BoutCount; bout++) Assert.That(source.RangeReservations.GetRecord(i, bout).IsReleased, Is.True);
            }
            Assert.That(pursuit ? target.PursuitResult.ToJson() : target.Result.ToJson(),
                Is.EqualTo(pursuit ? source.PursuitResult.ToJson() : source.Result.ToJson()));
            var changedBody = OccupancyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(OccupancyTests.Definition().ToJson(), "body_length_m", "1.6"));
            var changed = Create(PhysicalRangeReservationsTests.Fixture(pursuit: pursuit), new RaceEventLog(100), body: changedBody);
            Assert.Throws<ArgumentException>(() => restored.CreateReplay(changed)); Assert.That(changed.Tick.Value, Is.Zero);
        }

        [Test] public void ReusableAndRouteBindingOptionsAreExactReplayContextWithTheSameRosterAndLayout()
        {
            var f = PhysicalRangeReservationsTests.Fixture(count: 2); var source = Create(f, new RaceEventLog(100));
            var tape = new PackSprintRaceCommandTape("optional_context", source, 2);
            var legacy = Create(f, new RaceEventLog(100), reusable: false);
            var routeBound = Create(f, new RaceEventLog(100), routeBoundJunctions: true);
            Assert.That(legacy.ContextJson, Is.Not.EqualTo(source.ContextJson));
            Assert.That(routeBound.ContextJson, Is.Not.EqualTo(source.ContextJson));
            Assert.Throws<ArgumentException>(() => tape.CreateReplay(legacy));
            Assert.Throws<ArgumentException>(() => tape.CreateReplay(routeBound));
            Assert.That(legacy.Tick.Value, Is.Zero); Assert.That(routeBound.Tick.Value, Is.Zero);
            Assert.That(routeBound.ContextJson, Does.Contain("reachable_physical_ports_v1"));
        }
    }
}

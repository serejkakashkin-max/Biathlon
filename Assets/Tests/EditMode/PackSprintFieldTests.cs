using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PackSprintFieldTests
    {
        public static PackSprintFieldSession Create(RaceEventLog log, int count = 3, double interval = 30, string trackId = "synthetic_flat_v1", bool closeForPack = true, ulong seed = 123)
        {
            var profile = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(CompetitionProfileTests.TestProfile().ToJson(), "start_interval_s", interval.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var track = SyntheticTrackTests.Generated(trackId);
            // Explicit synthetic closed variant: old solo descent fixtures are open, so replaying
            // their end as the next lap's start is not valid physical pack topology.
            if (closeForPack) track = TrackDefinition.FromJson(track.ToJson().Replace("\"closed\":false", "\"closed\":true"));
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", profile.ToRouteRequirements()), profile.ToRouteRequirements());
            var entrants = new SprintEntrant[count]; var ski = TimeTrialTests.Athlete(); var shot = ShootingSessionTests.Athlete();
            for (int i = 0; i < count; i++)
            {
                string id = "athlete_" + i;
                entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id, id)), ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id, id)), "ski_neutral");
            }
            var junction = PhysicalJunctionDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_flat_junctions_v1.json").Replace("synthetic_flat_v1", trackId), track);
            var connection = PhysicalConnectionDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_flat_connections_v1.json"), junction);
            return new PackSprintFieldSession(new SimulationConfig(1, "sprint_field_v1", 20, seed), profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), track, route,
                SnowConditionTests.Conditions(), SprintResultsTests.Timing(), entrants, new SimulationTick(1), "sprint_result_v1",
                OccupancyTests.Definition(), DraftingTests.Definition(), OvertakeTests.Definition(), junction, connection, log);
        }
        public static void Fill(PackSprintFieldSession field, SprintAthleteInput[] commands, int baseEffort = 700)
        {
            var tick = field.Tick.Next();
            for (int i = 0; i < field.Count; i++)
            {
                var id = field.GetAthlete(i).AthleteId;
                commands[i] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, id, Math.Min(1000, baseEffort + i % 3 * 100)), 1, 1, 0), new WeaponAimCommand(tick, id, 0, 0), true);
            }
        }
        [Test] public void SharedSprintCompletesRealBoutsPenaltiesAndReplaysExactResultsWithoutOverlap()
        {
            var log = new RaceEventLog(20000); var replayLog = new RaceEventLog(20000);
            var a = Create(log, 3, 2); var b = Create(replayLog, 3, 2);
            var commands = new SprintAthleteInput[a.Count]; var lines = new double[a.Count];
            var previousTravel = new long[a.Count]; bool draft = false, contact = false;
            var body = OccupancyTests.Definition();
            while (!a.IsComplete && a.Tick.Value < 100000)
            {
                Fill(a, commands);
                for (int i = 0; i < a.Count; i++) previousTravel[i] = a.GetAthlete(i).Motion.DistanceNanometers;
                a.Step(commands, lines); b.Step(commands, lines);
                for (int i = 0; i < a.Count; i++)
                {
                    Assert.That(a.GetAthlete(i).LastFrame, Is.EqualTo(b.GetAthlete(i).LastFrame));
                    Assert.That(a.GetAthlete(i).Motion.DistanceNanometers - previousTravel[i], Is.InRange(0, 5000000000L));
                    draft |= a.GetDraft(i).Strength > 0;
                    if (!a.IsOnCourse(i)) continue;
                    for (int j = 0; j < i; j++) if (a.IsOnCourse(j))
                    {
                        var pa = a.GetPhysicalPoint(i); var pb = a.GetPhysicalPoint(j);
                        if (pa.LoopId != pb.LoopId) continue;
                        long gap = Math.Abs(pa.LocalNanometers - pb.LocalNanometers);
                        if (pa.Closed) gap = Math.Min(gap, pa.LoopLengthNanometers - gap);
                        Assert.That(gap / 1e9 >= body.LongitudinalSeparation || Math.Abs(a.GetLateral(i) - a.GetLateral(j)) >= body.LateralSeparation, Is.True);
                    }
                }
            }
            Assert.That(a.IsComplete, Is.True, "Pack must not deadlock before finishing.");
            Assert.That(a.Result.ToJson(), Is.EqualTo(b.Result.ToJson())); Assert.That(a.ContextJson, Is.EqualTo(b.ContextJson));
            Assert.That(a.ContextJson, Is.Not.EqualTo(SprintFieldTests.Create(new RaceEventLog(), 3).ContextJson));
            Assert.That(log.Count, Is.EqualTo(replayLog.Count));
            int seams = 0;
            for (int i = 0; i < log.Count; i++) { Assert.That(log[i], Is.EqualTo(replayLog[i])); contact |= log[i].EventType == RaceEventType.ContactStarted; if (log[i].EventType == RaceEventType.PhysicalSeamCrossed) seams++; }
            Assert.That(draft && contact, Is.True); Assert.That(seams, Is.EqualTo(12));
            for (int i = 0; i < a.Count; i++) { Assert.That(a.GetAthlete(i).TotalPenaltyLoops, Is.GreaterThan(0)); Assert.That(a.GetAthlete(i).Lifecycle.CompletedBouts, Is.EqualTo(2)); }
        }
        [Test] public void SoloPackUsesTheSameRacePhysicsAndInvalidLastRowsAreAtomic()
        {
            var log = new RaceEventLog(10000); var pack = Create(log, 1); var solo = SprintFieldTests.Create(new RaceEventLog(10000), 1);
            var commands = new SprintAthleteInput[1]; var lines = new double[1];
            while (!pack.IsComplete && pack.Tick.Value < 40000)
            {
                Fill(pack, commands); pack.Step(commands, lines); solo.Step(commands);
                Assert.That(pack.GetAthlete(0).LastFrame, Is.EqualTo(solo.GetAthlete(0).LastFrame));
            }
            Assert.That(pack.IsComplete, Is.True);
            var field = Create(log, 3); commands = new SprintAthleteInput[3]; lines = new double[3]; Fill(field, commands);
            int count = log.Count; lines[2] = double.NaN;
            Assert.Catch(() => field.Step(commands, lines)); Assert.That(field.Tick.Value, Is.Zero); Assert.That(log.Count, Is.EqualTo(count));
            for (int i = 0; i < field.Count; i++) Assert.That(field.GetAthlete(i).Tick.Value, Is.Zero);
        }

        [TestCase("synthetic_flat_v1", 12, 0.5, "164f496e078824383039bcf9ca6ac747f407c95b358f21bc2b26ea82a81a9bb7")]
        [TestCase("synthetic_flat_v1", 30, 2, "e55374aadb1053b48b012c74413b319db0cd2bfb4db3dd05b5826b2f88871594")]
        [TestCase("synthetic_rolling_v1", 6, 1, "1f03aa6fdc5c237f53b666b2e01524cd0ae49bcb601836b9036a74820fd90895")]
        [TestCase("synthetic_technical_descent_v1", 6, 1, "c1f89ea3d39d903aa5a7c0d522e374781ed7226ef1a315178436f033e09b1d3e")]
        public void DenseChangingLanesCompleteWithoutOverlapOrPermanentQueue(string track, int count, double interval, string resultHash)
        {
            var log = new RaceEventLog(100000); var field = Create(log, count, interval, track);
            var commands = new SprintAthleteInput[count]; var lines = new double[count]; var oldLines = new double[count];
            var body = OccupancyTests.Definition(); double lateralStep = OvertakeTests.Definition().LateralSpeed / 20;
            long steps = 0; int draftTicks = 0, stagingWaits = 0;
            while (!field.IsComplete && steps < 120000)
            {
                Fill(field, commands, 750);
                for (int i = 0; i < count; i++)
                {
                    oldLines[i] = field.GetLateral(i);
                    lines[i] = (i % 3 - 1) * 0.9 * ((steps / 1000) % 2 == 0 ? 1 : -1);
                    if (!field.IsOnCourse(i) && field.Tick.Value > field.GetAthlete(i).ScheduledStart.Value && !field.GetAthlete(i).HasFinished) stagingWaits++;
                }
                field.Step(commands, lines); steps++;
                for (int i = 0; i < count; i++)
                {
                    Assert.That(Math.Abs(field.GetLateral(i) - oldLines[i]), Is.LessThanOrEqualTo(lateralStep + 1e-9));
                    if (field.GetDraft(i).Strength > 0) draftTicks++;
                    if (!field.IsOnCourse(i)) continue;
                    for (int j = 0; j < i; j++) if (field.IsOnCourse(j))
                    {
                        var a = field.GetPhysicalPoint(i); var b = field.GetPhysicalPoint(j); if (a.LoopId != b.LoopId) continue;
                        long gap = Math.Abs(a.LocalNanometers - b.LocalNanometers); if (a.Closed) gap = Math.Min(gap, a.LoopLengthNanometers - gap);
                        Assert.That(gap / 1e9 >= body.LongitudinalSeparation || Math.Abs(field.GetLateral(i) - field.GetLateral(j)) >= body.LateralSeparation, Is.True, "Shared bodies overlap.");
                    }
                }
            }
            Assert.That(field.IsComplete, Is.True, "Dense field failed to complete."); Assert.That(field.Result.Count, Is.EqualTo(count));
            Assert.That(field.PrecisionFallbackTicks, Is.Zero, "Authored-resolution braking must avoid microscopic stop/restart fragments in these fixtures.");
            for (int i = 0; i < count; i++)
            {
                var athlete = field.GetAthlete(i);
                Assert.That(athlete.Lifecycle.CompletedBouts, Is.EqualTo(2));
                Assert.That(athlete.Motion.DistanceNanometers, Is.EqualTo((3000 + 150L * athlete.TotalPenaltyLoops) * SkiState.NanometersPerMeter));
            }
            if (track == "synthetic_technical_descent_v1") Assert.That(draftTicks, Is.Zero, "The raced technical loop and penalty loop prohibit drafting.");
            else Assert.That(draftTicks, Is.GreaterThan(0));
            if (interval < 1) Assert.That(stagingWaits, Is.GreaterThan(0));
            TestContext.Out.WriteLine("Pack scenario " + track + ": athletes=" + count + ", ticks=" + steps + ", events=" + log.Count + ", draft_frames=" + draftTicks + ", staging_frames=" + stagingWaits + ", precision_fallback_ticks=" + field.PrecisionFallbackTicks);
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                string actual = BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(field.Result.ToJson()))).Replace("-", "").ToLowerInvariant();
                Assert.That(actual, Is.EqualTo(resultHash), "Shared pack results must match in .NET and Unity.");
                TestContext.Out.WriteLine("Result SHA256: " + actual);
            }
        }

        [Test] public void LateralWorkHasPhysicalCostAndActivePreparationAllocatesNothing()
        {
            var log = new RaceEventLog(10000); var moving = Create(log, 1); var straight = Create(new RaceEventLog(10000), 1);
            var commands = new SprintAthleteInput[1]; var line = new double[1]; var centre = new double[1];
            for (int i = 0; i < 100; i++)
            { Fill(moving, commands, 700); line[0] = i % 20 < 10 ? 0.9 : -0.9; moving.Step(commands, line); straight.Step(commands, centre); }
            Assert.That(moving.GetAthlete(0).PhysiologyState.MuscularFatigue, Is.GreaterThan(straight.GetAthlete(0).PhysiologyState.MuscularFatigue));
            Fill(moving, commands); moving.Step(commands, line);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) { Fill(moving, commands); moving.Step(commands, line); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
        [Test] public void OpenRepeatedLapCannotTeleportBackToStartInPhysicalPack()
        { Assert.Catch(() => Create(new RaceEventLog(), 2, 1, "synthetic_technical_descent_v1", false)); }

        [Test] public void SoloZeroEffortIsNotReportedAsAthleteContact()
        {
            var log = new RaceEventLog(100); var field = Create(log, 1);
            var commands = new SprintAthleteInput[1]; var lines = new double[1];
            for (int i = 0; i < 20; i++) { Fill(field, commands, 0); field.Step(commands, lines); }
            Assert.That(field.GetAthlete(0).Motion.SpeedMetersPerSecond, Is.Zero);
            for (int i = 0; i < log.Count; i++) Assert.That(log[i].EventType, Is.Not.EqualTo(RaceEventType.ContactStarted));
        }
    }
}


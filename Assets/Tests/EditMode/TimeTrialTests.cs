using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TimeTrialTests
    {
        public static TimeTrialAthlete Athlete() => TimeTrialAthlete.FromJson(PhysiologyContractTests.DefinitionJson("time_trial_athlete_v1.json"));
        public static TimeTrialSession Create(RaceEventLog log, string trackId = "synthetic_flat_v1", int laps = 1,
            string snowProfile = "packed_neutral", string skiId = "ski_neutral", TimeTrialAthlete athlete = null)
        {
            var track = SyntheticTrackTests.Generated(trackId);
            var requirements = CompetitionRouteRequirements.TimeTrial("trial_profile", track.GetLoop("main_long").Length, laps);
            var plan = RaceRoutePlan.TimeTrial("trial_route", track.Id, "main_long", requirements);
            return new TimeTrialSession(new SimulationConfig(1, "trial_sim", 20, 12345),
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), athlete ?? Athlete(), track,
                new RaceRoute(track, plan, requirements), SnowConditionTests.Conditions(snowProfile), skiId, log);
        }
        public static TimeTrialInput Input(TimeTrialSession s, int control = 800, bool tuck = false, bool sprint = false,
            string technique = null, double headwind = 0) => new TimeTrialInput(new SimulationInput(s.Clock.Tick.Next(), s.AthleteId, control), 1, 1, headwind, tuck, sprint, technique);
        [TestCase("synthetic_flat_v1")]
        [TestCase("synthetic_climb_v1")]
        [TestCase("synthetic_rolling_v1")]
        [TestCase("synthetic_technical_descent_v1")]
        [TestCase("synthetic_range_approach_v1")]
        public void EverySyntheticCourseFinishesWithResourceTraceAndSplit(string track)
        {
            var events = new RaceEventLog(1000); var session = Create(events, track);
            while (!session.HasFinished && session.Clock.Tick.Value < 100000)
            {
                var frame = session.Step(Input(session, tuck: true, sprint: true));
                Assert.That(frame.Forces.State.SpeedMetersPerSecond, Is.InRange(0, 60));
                Assert.That(frame.Physiology.State.AnaerobicReserve, Is.InRange(0, 1));
                Assert.That(frame.Technique.PropulsivePowerWatts, Is.LessThanOrEqualTo(frame.Physiology.Effort.DeliveredPowerWatts + 1e-9));
            }
            Assert.That(session.HasFinished, Is.True); Assert.That(session.CompletedLaps, Is.EqualTo(1));
            Assert.That(session.FinishTime.CompareTo(session.GetLapSplit(0)), Is.Zero);
            Assert.That(events[events.Count - 1].EventType, Is.EqualTo(RaceEventType.RaceFinished));
            TestContext.WriteLine(FormattableString.Invariant($"{track}: ticks={session.Clock.Tick.Value}, finish={session.FinishTime.WholeTicks}:{session.FinishTime.FractionParts}, nm={session.Motion.DistanceNanometers}"));
        }
        [Test]
        public void CompleteInputReplayPreservesMotionResourcesAndOrderedEvents()
        {
            var logA = new RaceEventLog(1000); var logB = new RaceEventLog(1000);
            var a = Create(logA, "synthetic_rolling_v1", 2); var b = Create(logB, "synthetic_rolling_v1", 2);
            while (!a.HasFinished && a.Clock.Tick.Value < 100000)
            {
                var input = Input(a, a.Clock.Tick.Value % 400 < 200 ? 850 : 500, tuck: true, sprint: true);
                var x = a.Step(input); var y = b.Step(input);
                Assert.That(x.Forces.State, Is.EqualTo(y.Forces.State)); Assert.That(x.Physiology.State, Is.EqualTo(y.Physiology.State));
                Assert.That(x.Technique.TechniqueId, Is.EqualTo(y.Technique.TechniqueId));
            }
            Assert.That(a.HasFinished, Is.True); Assert.That(b.FinishTime, Is.EqualTo(a.FinishTime));
            Assert.That(a.CompletedLaps, Is.EqualTo(2)); Assert.That(a.GetLapSplit(0).CompareTo(a.GetLapSplit(1)), Is.LessThan(0));
            Assert.That(logA.Count, Is.EqualTo(logB.Count));
            for (int i = 0; i < logA.Count; i++) Assert.That(logA[i], Is.EqualTo(logB[i]));
        }
        [Test]
        public void CanonicalCsvNormalizesSignedZero()
        {
            var session = Create(new RaceEventLog(1000));
            double negativeZero = BitConverter.Int64BitsToDouble(long.MinValue);
            var frame = session.Step(Input(session, headwind: negativeZero));
            using (var writer = new StringWriter(System.Globalization.CultureInfo.InvariantCulture))
            {
                TimeTrialCsv.WriteFrame(writer, frame);
                Assert.That(writer.ToString().TrimEnd('\n').Split(',')[5], Is.EqualTo("0"));
            }
        }
        [TestCase(1)]
        [TestCase(2)]
        public void CanonicalCsvTraceMatchesCliForEverySyntheticCourse(int version)
        {
            string fixture = PhysiologyContractTests.ProjectFile("tests/fixtures/ski-traces-v" + version + ".sha256");
            foreach (string line in fixture.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                var parts = line.Split(' '); string track = parts[0];
                var session = Create(new RaceEventLog(1000), track);
                bool tuck = track == "synthetic_rolling_v1" || track == "synthetic_technical_descent_v1";
                using (var bytes = new MemoryStream())
                {
                    using (var writer = new StreamWriter(bytes, new UTF8Encoding(false), 4096, true))
                    {
                        writer.Write(TimeTrialCsv.Header + "\n");
                        while (!session.HasFinished && session.Clock.Tick.Value < 100000) TimeTrialCsv.WriteFrame(writer, session.Step(Input(session, tuck: tuck, sprint: true)));
                    }
                    Assert.That(session.HasFinished, Is.True); bytes.Position = 0;
                    if (version == 1)
                    {
                        string expanded = Encoding.UTF8.GetString(bytes.ToArray());
                        var legacy = new StringBuilder();
                        foreach (string row in expanded.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            legacy.Append(row.Substring(0, row.LastIndexOf(','))).Append('\n');
                        byte[] originalColumns = Encoding.UTF8.GetBytes(legacy.ToString());
                        bytes.SetLength(0); bytes.Write(originalColumns, 0, originalColumns.Length); bytes.Position = 0;
                    }
                    using (var sha = SHA256.Create())
                    {
                        string hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                        Assert.That(hash, Is.EqualTo(parts[1]), track);
                    }
                }
            }
        }
        [Test]
        public void InvalidLateStageInputDoesNotPartlyCommitPhysiologyOrEvents()
        {
            var events = new RaceEventLog(1000); var s = Create(events); var initial = s.PhysiologyState;
            Assert.Catch(() => s.Step(Input(s, technique: "unknown")));
            Assert.Catch(() => s.Step(Input(s, headwind: 41)));
            Assert.Catch(() => s.Step(default));
            Assert.That(s.Clock.Tick.Value, Is.Zero); Assert.That(s.Motion.DistanceNanometers, Is.Zero);
            Assert.That(s.PhysiologyState, Is.EqualTo(initial)); Assert.That(events.Count, Is.Zero);
            s.Step(Input(s)); Assert.That(s.Clock.Tick.Value, Is.EqualTo(1));
        }
        [Test]
        public void PreviewIsReadOnlyAndIntegratedTicksAllocateNothing()
        {
            var events = new RaceEventLog(1000); var physiology = PhysiologyIntegrationTests.Create(events);
            var input = PhysiologyIntegrationTests.Input(physiology, 1000);
            var preview = physiology.Preview(input);
            Assert.That(physiology.Clock.Tick.Value, Is.Zero); Assert.That(events.Count, Is.Zero);
            Assert.That(physiology.Step(input), Is.EqualTo(preview));
            var session = Create(new RaceEventLog(1000), laps: 10);
            session.Step(Input(session));
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) session.Step(Input(session));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(session.Motion.DistanceMeters, Is.GreaterThan(3000));
        }
    }
}

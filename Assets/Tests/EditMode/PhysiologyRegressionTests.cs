using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysiologyRegressionTests
    {
        public static string TraceHashes()
        {
            var suite = PhysiologyScenarioTests.Suite();
            var text = new StringBuilder("schema_version=1\nid=physiology_trace_regression_v1\n");
            for (int i = 0; i < suite.Count; i++)
            {
                var runner = PhysiologyScenarioTests.Runner(i, new RaceEventLog());
                using (var hash = new PhysiologyTraceFingerprint(runner.Session))
                {
                    while (!runner.IsComplete) { var frame = runner.Step(); hash.Append(runner.LastInput, frame); }
                    text.Append(runner.Scenario.Id).Append('=').Append(hash.Complete()).Append('\n');
                }
            }
            return text.ToString();
        }
        public static string BenchmarkSummary()
        {
            const int ticks = 100000;
            var session = PhysiologyIntegrationTests.Create(new RaceEventLog(2000));
            for (int i = 0; i < 1000; i++) session.Step(PhysiologyIntegrationTests.Input(session, 700));
            var stopwatch = new Stopwatch();
            long before = GC.GetAllocatedBytesForCurrentThread();
            stopwatch.Start();
            for (int i = 0; i < ticks; i++) session.Step(PhysiologyIntegrationTests.Input(session, i % 2400 < 1200 ? 1000 : 385));
            stopwatch.Stop();
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            return FormattableString.Invariant($"{{\"ticks\":{ticks},\"simulated_seconds\":{ticks / 20},\"elapsed_ms\":{stopwatch.Elapsed.TotalMilliseconds},\"allocated_bytes\":{allocated}}}");
        }
        [Test]
        public void CanonicalStateAndFullTraceSnapshotsMatchQualifiedBaseline()
        {
            string traces = PhysiologyContractTests.ProjectFile("tests/fixtures/physiology-trace-v1.sha256").Replace("\r\n", "\n").Trim();
            string states = PhysiologyContractTests.ProjectFile("tests/fixtures/physiology-state-v1.sha256").Replace("\r\n", "\n").Trim();
            Assert.That(TraceHashes().Trim(), Is.EqualTo(traces), "Physiology trace changed; review before versioning the snapshot.");
            Assert.That(("schema_version=1\nid=physiology_state_regression_v1\n" + PhysiologyScenarioTests.ScenarioHashes()).Trim(), Is.EqualTo(states));
        }
        [Test]
        public void FullTraceCanBeRepeatedAndSamplesCannotBeReordered()
        {
            Assert.That(TraceHashes(), Is.EqualTo(TraceHashes()));
            var runner = PhysiologyScenarioTests.Runner(0, new RaceEventLog());
            using (var hash = new PhysiologyTraceFingerprint(runner.Session))
            {
                var frame = runner.Step(); hash.Append(runner.LastInput, frame);
                Assert.Catch(() => hash.Append(runner.LastInput, frame));
                Assert.That(hash.Complete().Length, Is.EqualTo(64));
                Assert.Catch(() => hash.Complete());
            }
        }
        [Test]
        public void UnrepresentableParameterCombinationsFailBeforeTicks()
        {
            string source = PhysiologyContractTests.DefinitionJson();
            var power = PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(source, "sustainable_power_watts", "1000000"));
            Assert.Catch(() => new EffortSystem(power));
            var fatigue = PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(source, "fatigue_exponent", "1000000"));
            Assert.Catch(() => new FatigueSystem(fatigue, 20));
            var reserve = PhysiologyDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(
                PhysiologyContractTests.ReplaceNumber(source, "maximum_relative_effort", "1000000"), "anaerobic_drain_exponent", "1000000"));
            Assert.Catch(() => new AnaerobicSystem(reserve, 20));
        }
        [Test]
        public void LongExtremeInitialStatesRemainValidAndRecoveryWorks()
        {
            var template = PhysiologyContractTests.Definition();
            var curves = PhysiologyStatTests.Curves();
            foreach (int stat in new[] { 0, 50, 100 })
            foreach (double form in new[] { 0.0, 0.5, 1.10 })
            {
                var d = curves.Apply(template, new AthletePhysiologyStats(stat, stat, stat, stat));
                var s = new PhysiologySession(new SimulationConfig(1, "long_extreme_v1", 20, 12345), d,
                    PhysiologyIntegrationTests.Athlete, new PhysiologyState(d.HrSafetyCeilingBpm, 0, 1, d.DriftSaturationSeconds), new RaceEventLog(200));
                for (int i = 0; i < 100000; i++) s.Step(PhysiologyIntegrationTests.Input(s, i < 50000 ? 1000 : 0, form, d.MaximumTerrainMuscleFactor));
                s.State.Validate(d);
                Assert.That(s.State.AnaerobicReserve, Is.GreaterThan(0.9));
                Assert.That(s.State.MuscularFatigue, Is.LessThan(1));
                Assert.That(s.State.HeartRateBpm, Is.LessThan(d.HrSafetyCeilingBpm));
            }
            string benchmark = BenchmarkSummary();
            Assert.That(benchmark, Does.Contain("\"allocated_bytes\":0}"));
            TestContext.WriteLine(benchmark);
        }
    }
}

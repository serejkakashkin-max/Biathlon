using System;
using System.Globalization;
using System.IO;
using System.Text;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysiologyScenarioTests
    {
        public static PhysiologyScenarioSuite Suite() => PhysiologyScenarioSuite.FromJson(PhysiologyContractTests.DefinitionJson("physiology_scenarios_v1.json"));
        public static PhysiologyScenarioRunner Runner(int index, RaceEventLog events)
        {
            var suite = Suite();
            return new PhysiologyScenarioRunner(suite, suite.GetScenario(index), PhysiologyContractTests.Definition(), PhysiologyStatTests.Curves(), events);
        }
        public static string ScenarioHashes()
        {
            var output = new StringBuilder();
            var suite = Suite();
            for (int i = 0; i < suite.Count; i++)
            {
                var events = new RaceEventLog();
                var runner = Runner(i, events);
                while (!runner.IsComplete) runner.Step();
                output.Append(runner.Scenario.Id).Append('=').Append(PhysiologyFingerprint.Compute(runner.Session, events)).Append('\n');
            }
            return output.ToString();
        }
        [Test]
        public void WorkloadBoundariesAndDurationsAreExact()
        {
            var suite = Suite();
            var s = suite.GetScenario(1);
            Assert.That(s.TotalTicks, Is.EqualTo(4800));
            Assert.That(s.AtTick(3600).Control, Is.EqualTo(731));
            Assert.That(s.AtTick(3601).Control, Is.EqualTo(385));
            Assert.Catch(() => s.AtTick(0));
            Assert.Catch(() => s.AtTick(4801));
            var runner = Runner(1, new RaceEventLog());
            while (!runner.IsComplete) runner.Step();
            Assert.That(runner.Session.Clock.ElapsedSeconds, Is.EqualTo(240));
            Assert.Catch(() => runner.Step());
        }
        [Test]
        public void CsvHasCompleteInputsAndTelemetryInInvariantCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
                var runner = Runner(0, new RaceEventLog());
                var frame = runner.Step();
                using (var writer = new StringWriter())
                {
                    PhysiologyCsv.WriteFrame(writer, runner.LastInput, frame, 20);
                    string[] cells = writer.ToString().Trim().Split(',');
                    Assert.That(cells.Length, Is.EqualTo(PhysiologyCsv.Header.Split(',').Length));
                    Assert.That(cells[0], Is.EqualTo("1"));
                    Assert.That(cells[1], Is.EqualTo("0.05"));
                    Assert.That(cells[2], Is.EqualTo("462"));
                    Assert.That(double.Parse(cells[10], CultureInfo.InvariantCulture), Is.EqualTo(frame.State.HeartRateBpm));
                }
            }
            finally { CultureInfo.CurrentCulture = previous; }
        }
        [Test]
        public void AllCannedScenariosReplayIdenticalFingerprints()
        {
            string first = ScenarioHashes();
            Assert.That(ScenarioHashes(), Is.EqualTo(first));
            TestContext.WriteLine(first);
        }
        [Test]
        public void FingerprintDetectsPhysiologyAndConfigurationChanges()
        {
            var log = new RaceEventLog();
            var a = Runner(0, log);
            string before = PhysiologyFingerprint.Compute(a.Session, log);
            a.Step();
            Assert.That(PhysiologyFingerprint.Compute(a.Session, log), Is.Not.EqualTo(before));
            var other = Runner(3, new RaceEventLog());
            Assert.That(PhysiologyFingerprint.Compute(other.Session, new RaceEventLog()), Is.Not.EqualTo(before));
        }
        [Test]
        public void InvalidSuiteAndWorkloadsAreRejected()
        {
            string json = PhysiologyContractTests.DefinitionJson("physiology_scenarios_v1.json");
            foreach (string invalid in new[] { "{}", "null", json.Replace("low_5min", "../unsafe"),
                PhysiologyContractTests.ReplaceNumber(json, "tick_frequency", "0"),
                PhysiologyContractTests.ReplaceNumber(json, "ticks", "0"),
                PhysiologyContractTests.ReplaceNumber(json, "control", "1001"),
                PhysiologyContractTests.ReplaceNumber(json, "initial_reserve", "-1") })
                Assert.Catch(() => PhysiologyScenarioSuite.FromJson(invalid));
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintRegressionTests
    {
        [Test] public void CompleteScenarioTracesMatchPinnedCliHashesAndPhysicalTradeoffs()
        {
            var suite = SprintRegressionSuite.FromJson(PhysiologyContractTests.DefinitionJson("sprint_regression_v1.json"));
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            string[] hashes = PhysiologyContractTests.ProjectFile("tests/fixtures/sprint-traces-v1.sha256").Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var finishes = new double[suite.Count]; var reserves = new double[suite.Count]; var fatigue = new double[suite.Count];
            Assert.That(hashes.Length, Is.EqualTo(suite.Count));
            for (int i = 0; i < suite.Count; i++)
            {
                var scenario = suite.Get(i); var events = new RaceEventLog(4000); var runner = new SprintRegressionRunner(suite, scenario, PhysiologyContractTests.DefinitionJson, track, events);
                var inputs = new SprintAthleteInput[1]; var csv = new StringWriter(); csv.Write(SprintCsv.Header + "\n");
                while (!runner.Field.IsComplete)
                { inputs[0] = runner.NextInput(); runner.Field.Step(inputs); SprintCsv.WriteFrame(csv, runner.Field.GetAthlete(0).LastFrame); }
                var eventText = new StringBuilder("tick,athlete,type,payload\n");
                for (int j = 0; j < events.Count; j++)
                { var e = events[j]; eventText.Append(e.Tick.Value).Append(',').Append(e.AthleteId.Value).Append(',').Append((int)e.EventType).Append(',').Append(e.Payload).Append('\n'); }
                string[] expected = hashes[i].Split(' '); Assert.That(expected[0], Is.EqualTo(scenario.Id));
                Assert.That(Hash(csv.ToString()), Is.EqualTo(expected[1]), scenario.Id + " full tick CSV");
                Assert.That(Hash(eventText.ToString()), Is.EqualTo(expected[2]), scenario.Id + " events");
                var s = runner.Field.GetAthlete(0); int penalties = scenario.IntentionalFirstMiss ? 1 : 0;
                Assert.That(s.TotalPenaltyLoops, Is.EqualTo(penalties)); Assert.That(s.Motion.DistanceMeters, Is.EqualTo(3000 + penalties * 150));
                finishes[i] = s.Lifecycle.ElapsedFinishTime.Seconds; reserves[i] = s.PhysiologyState.AnaerobicReserve; fatigue[i] = s.PhysiologyState.MuscularFatigue;
            }
            Assert.That(finishes[1], Is.GreaterThan(finishes[0]));
            Assert.That(finishes[2], Is.LessThan(finishes[3])); Assert.That(reserves[2], Is.LessThan(reserves[3])); Assert.That(fatigue[2], Is.GreaterThan(fatigue[3]));
            Assert.That(finishes[4], Is.GreaterThan(finishes[0])); Assert.That(finishes[5], Is.LessThan(finishes[0]));
        }
        private static string Hash(string text)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant(); }
        [Test] public void ScenarioSchemaAndControllerMissBoundsAreValidatedBeforeRun()
        {
            string json = PhysiologyContractTests.DefinitionJson("sprint_regression_v1.json");
            Assert.That(SprintRegressionSuite.FromJson(SprintRegressionSuite.FromJson(json).ToJson()).Count, Is.EqualTo(6));
            foreach (var pair in new[] { new[] { "schema_version", "2" }, new[] { "effort_control", "1001" }, new[] { "maximum_ticks", "0" }, new[] { "tick_frequency", "0" } })
                Assert.Catch(() => SprintRegressionSuite.FromJson(PhysiologyContractTests.ReplaceNumber(json, pair[0], pair[1])));
            var tooClose = SprintRegressionSuite.FromJson(PhysiologyContractTests.ReplaceNumber(json, "intentional_miss_yaw_rad", "0.001"));
            Assert.Catch(() => new SprintRegressionRunner(tooClose, tooClose.Get(0), PhysiologyContractTests.DefinitionJson,
                SyntheticTrackTests.Generated("synthetic_flat_v1"), new RaceEventLog(100)));
        }
    }
}

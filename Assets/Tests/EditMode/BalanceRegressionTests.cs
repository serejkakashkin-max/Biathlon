using System;
using System.Collections.Generic;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class BalanceRegressionTests
    {
        private static string Json()=>PhysiologyContractTests.ProjectFile("tests/fixtures/balance/canonical-v1.json");
        // This fixture predates the approved M13 world-tuck option. Keep its exact
        // recorded input context rather than silently replacing all pinned hashes.
        private static string HistoricalDefinition(string name)
        {
            string json=PhysiologyContractTests.DefinitionJson(name);
            return name=="tuck_provisional_v1.json"?json.Replace("\"world_hold_through_turns\": true","\"world_hold_through_turns\": false"):json;
        }
        [Test] public void CanonicalSeedSuiteMatchesContextMetricsAndWindowsAndReportsMutation()
        {
            var suite=BalanceBatchTests.Suite();var snapshot=BalanceRegressionSnapshot.FromJson(Json());var rows=new List<BalanceMetrics>();
            for(int i=0;i<suite.Iterations;i++)for(int c=0;c<suite.Count;c++)
            {
                var events=new BalanceEventAudit(5);var track=TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+suite.Get(c).TrackFile));
                var runner=new BalanceRaceRunner(suite,c,i,HistoricalDefinition,track,events);
                var row=BalanceMeasurement.Run(runner,suite,c,i,events);row.Values["context_sha256"]=BalanceRegression.Hash(runner.ContextJson);rows.Add(row);
            }
            var initialDiff=BalanceRegression.Compare(suite,snapshot,rows);
            if(initialDiff.Length>0)foreach(var row in rows)if(Array.Exists(initialDiff,x=>x.StartsWith((string)row.Values["id"]+".sha256",StringComparison.Ordinal)))TestContext.Out.WriteLine(row.ToJson());
            Assert.That(initialDiff,Is.Empty);
            rows[0].Values["finish_seconds"]=10000.0;
            var diff=BalanceRegression.Compare(suite,snapshot,rows);Assert.That(string.Join("\n",diff),Does.Contain("aggressive_0.sha256").And.Contain("aggressive.finish_seconds.mean"));
            rows.RemoveAt(0);Assert.That(string.Join("\n",BalanceRegression.Compare(suite,snapshot,rows)),Does.Contain("missing").And.Contain("records.count"));
            TestContext.Out.WriteLine("Canonical balance: 12 exact context/metric records and 9 target windows passed before deliberate mutations.");
        }
        [Test] public void CurrentWorldTuckContextIsDistinctButSoloPacingMetricsRemainExact()
        {
            Assert.That(TuckDefinition.FromJson(PhysiologyContractTests.DefinitionJson("tuck_provisional_v1.json")).WorldHoldThroughTurns,Is.True);
            Assert.That(TuckDefinition.FromJson(HistoricalDefinition("tuck_provisional_v1.json")).WorldHoldThroughTurns,Is.False);
            var suite=BalanceBatchTests.Suite();
            for(int iteration=0;iteration<suite.Iterations;iteration++)for(int c=0;c<suite.Count;c++)
            {
                var track=TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+suite.Get(c).TrackFile));
                var oldEvents=new BalanceEventAudit(5);var currentEvents=new BalanceEventAudit(5);
                var historical=new BalanceRaceRunner(suite,c,iteration,HistoricalDefinition,track,oldEvents);
                var current=new BalanceRaceRunner(suite,c,iteration,PhysiologyContractTests.DefinitionJson,track,currentEvents);
                var oldRow=BalanceMeasurement.Run(historical,suite,c,iteration,oldEvents);var currentRow=BalanceMeasurement.Run(current,suite,c,iteration,currentEvents);
                Assert.That(BalanceRegression.Hash(current.ContextJson),Is.Not.EqualTo(BalanceRegression.Hash(historical.ContextJson)),"The approved M13 option must remain observable.");
                Assert.That(currentRow.Values.Count,Is.EqualTo(oldRow.Values.Count));
                foreach(var pair in oldRow.Values)Assert.That(currentRow.Values[pair.Key],Is.EqualTo(pair.Value),pair.Key);
            }
        }
        [Test] public void InvalidSnapshotsRejectAndWrongSuiteCannotPass()
        {
            var s=BalanceRegressionSnapshot.FromJson(Json());Assert.That(BalanceRegressionSnapshot.FromJson(s.ToJson()).RecordCount,Is.EqualTo(12));
            Assert.Catch(()=>BalanceRegressionSnapshot.FromJson(PhysiologyContractTests.ReplaceNumber(Json(),"schema_version","2")));
            Assert.Catch(()=>BalanceRegressionSnapshot.FromJson(Json().Replace("provisional_observation","approved")));
            Assert.Catch(()=>BalanceRegressionSnapshot.FromJson(PhysiologyContractTests.ReplaceNumber(Json(),"mean_min","99999")));
            var suite=BalanceBatchDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(BalanceBatchTests.Suite().ToJson(),"seed","42"));
            Assert.That(string.Join("\n",BalanceRegression.Compare(suite,s,new BalanceMetrics[0])),Does.Contain("suite_sha256"));
        }
    }
}

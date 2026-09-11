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
        [Test] public void CanonicalSeedSuiteMatchesContextMetricsAndWindowsAndReportsMutation()
        {
            var suite=BalanceBatchTests.Suite();var snapshot=BalanceRegressionSnapshot.FromJson(Json());var rows=new List<BalanceMetrics>();
            for(int i=0;i<suite.Iterations;i++)for(int c=0;c<suite.Count;c++)
            {
                var events=new BalanceEventAudit(5);var track=TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+suite.Get(c).TrackFile));
                var runner=new BalanceRaceRunner(suite,c,i,PhysiologyContractTests.DefinitionJson,track,events);
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

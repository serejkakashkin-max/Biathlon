using System;
using System.Collections.Generic;
using System.Text;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PacingCalibrationTests
    {
        private static BalanceBatchDefinition Suite()=>BalanceBatchDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/balance/pacing-holdout-v1.json"));
        [Test] public void LongTrackContextsHaveCanonicalZeroAndMatchCli()
        {
            var suite=Suite();var expected=PhysiologyContractTests.ProjectFile("tests/fixtures/balance/pacing-context-v1.sha256").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries);
            Assert.That(expected.Length,Is.EqualTo(suite.Count));
            for(int i=0;i<suite.Count;i++)
            {
                var track=TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+suite.Get(i).TrackFile));
                var loop=track.GetLoop("main_long");for(int j=0;j<loop.SegmentCount;j++)if(loop.GetSegment(j).CurvaturePerMeter==0)Assert.That(BitConverter.DoubleToInt64Bits(loop.GetSegment(j).CurvaturePerMeter),Is.Zero);
                var runner=new BalanceRaceRunner(suite,i,0,PhysiologyContractTests.DefinitionJson,track,new BalanceEventAudit(5));Assert.That(BalanceRegression.Hash(runner.ContextJson),Is.EqualTo(expected[i]),suite.Get(i).Id);
            }
        }
#if !UNITY_EDITOR
        [Test,Timeout(600000)] public void HeldOutPacingTradeoffsAndExactCliMetrics()=>Run();
#else
        [UnityEngine.TestTools.UnityTest,Timeout(600000)]
        public System.Collections.IEnumerator HeldOutPacingTradeoffsAndExactCliMetrics()
        {
            using(var cancellation=new System.Threading.CancellationTokenSource())
            {
                var work=System.Threading.Tasks.Task.Run(()=>Run(cancellation.Token),cancellation.Token);
                try{while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}finally{cancellation.Cancel();}
            }
        }
#endif
        private static void Run(System.Threading.CancellationToken cancellation=default)
        {
            var suite=Suite();var rows=new List<BalanceMetrics>();var text=new StringBuilder();
            var tracks=new Dictionary<string,TrackDefinition>();for(int c=0;c<suite.Count;c++){string name=suite.Get(c).TrackFile;if(!tracks.ContainsKey(name))tracks.Add(name,TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+name)));}
            for(int i=0;i<suite.Iterations;i++)for(int c=0;c<suite.Count;c++)
            {
                var events=new BalanceEventAudit(5);var runner=new BalanceRaceRunner(suite,c,i,PhysiologyContractTests.DefinitionJson,tracks[suite.Get(c).TrackFile],events);
                var row=BalanceMeasurement.Run(runner,suite,c,i,events,cancellation);row.Values["context_sha256"]=BalanceRegression.Hash(runner.ContextJson);rows.Add(row);text.Append(row.ToJson()).Append("\r\n");
            }
            string digest=BalanceRegression.Hash(text.ToString());
            var root=new System.IO.DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while(root!=null&&!System.IO.File.Exists(System.IO.Path.Combine(root.FullName,"AGENTS.md")))root=root.Parent;
            if(root!=null)
            {
                string output=System.IO.Path.Combine(root.FullName,"artifacts/balance");System.IO.Directory.CreateDirectory(output);
#if UNITY_EDITOR
                System.IO.File.WriteAllText(System.IO.Path.Combine(output,"wp095-unity-rows.jsonl"),text.ToString());
#else
                System.IO.File.WriteAllText(System.IO.Path.Combine(output,"wp095-dotnet-test-rows.jsonl"),text.ToString());
#endif
            }
            Assert.That(digest,Is.EqualTo(PhysiologyContractTests.ProjectFile("tests/fixtures/balance/pacing-holdout-v1.sha256").Trim()),"Complete paired metrics/context trace");
            foreach(string terrain in new[]{"flat","rolling"})
            {
                var optimal=new BalancePairedComparison(suite,rows,terrain+"_aggressive",terrain+"_optimal_candidate");
                var conservative=new BalancePairedComparison(suite,rows,terrain+"_aggressive",terrain+"_conservative");
                Assert.That(optimal.SecondsSaved.Mean,Is.GreaterThan(0));Assert.That(optimal.RangeHrDelta.Mean,Is.LessThan(0));
                Assert.That(optimal.FatigueDelta.Mean,Is.LessThan(0));Assert.That(optimal.FinalSpeedDelta.Mean,Is.GreaterThan(0));
                Assert.That(conservative.SecondsSaved.Mean,Is.LessThan(0));Assert.That(conservative.FatigueDelta.Mean,Is.LessThan(0));
                TestContext.Out.WriteLine(terrain+": candidate savings="+optimal.SecondsSaved.Mean.ToString("F6",System.Globalization.CultureInfo.InvariantCulture)+" s; wins="+optimal.ChangedWins+"/"+suite.Iterations);
            }
            TestContext.Out.WriteLine("Pacing holdout1536 SHA256="+digest);
        }
        [Test] public void PairedReportsRejectUnmatchedSeedsMissingRowsAndDifferentTracks()
        {
            var suite=Suite();Assert.Catch(()=>new BalancePairedComparison(suite,new BalanceMetrics[0],"flat_aggressive","rolling_aggressive"));
            Assert.Catch(()=>new BalancePairedComparison(suite,new BalanceMetrics[0],"flat_aggressive","flat_optimal_candidate"));
            var row=new BalanceMetrics();row.Values["case_id"]="flat_aggressive";row.Values["iteration"]=0;row.Values["seed"]="999";
            Assert.Catch(()=>new BalancePairedComparison(suite,new[]{row},"flat_aggressive","flat_optimal_candidate"));
        }
    }
}

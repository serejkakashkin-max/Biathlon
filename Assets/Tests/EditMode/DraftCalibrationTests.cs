using System;
using System.IO;
using System.Text;
using Biathlon.AI;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class DraftCalibrationTests
    {
        public static void Artifact(string name, string text)
        {
            var root = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (root != null && !File.Exists(Path.Combine(root.FullName,"AGENTS.md"))) root = root.Parent;
            if (root == null) throw new InvalidOperationException("Project root not found.");
            string directory = Path.Combine(root.FullName,"artifacts/balance"); Directory.CreateDirectory(directory);
#if UNITY_EDITOR
            name = "unity-" + name;
#else
            name = "dotnet-" + name;
#endif
            File.WriteAllText(Path.Combine(directory,name),text);
            string fixtureName=name.Replace("unity-","").Replace("dotnet-","");
            Assert.That(BalanceRegression.Hash(text),Is.EqualTo(PhysiologyContractTests.ProjectFile("tests/fixtures/balance/"+fixtureName+".sha256").Trim()),"Portable calibration output");
        }
#if UNITY_EDITOR
        [UnityEngine.TestTools.UnityTest, Timeout(300000)]
        public System.Collections.IEnumerator MatchedSharedRacesMeasureNonDominantDraftBenefit()
        {
            var work=System.Threading.Tasks.Task.Run((Action)Run);
            while(!work.IsCompleted)yield return null;
            work.GetAwaiter().GetResult();
        }
#else
        [Test] public void MatchedSharedRacesMeasureNonDominantDraftBenefit()=>Run();
#endif
        private static void Run()
        {
            string Read(string name)
            {
                string json=PhysiologyContractTests.DefinitionJson(name);
                return name=="sprint_test_v1.json" ? PhysiologyContractTests.ReplaceNumber(json,"start_interval_s","2") : json;
            }
            string Fixture(string name)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+name);
            var text=new StringBuilder();double sum=0,exposure=0;
            for(ulong seed=41000;seed<41008;seed++)
            {
                var off=DraftCalibrationRunner.Run(Read,Fixture,seed,3,20,100000,false);
                var on=DraftCalibrationRunner.Run(Read,Fixture,seed,3,20,100000,true);
                double benefit=off.Number("mean_finish_seconds")-on.Number("mean_finish_seconds");
                var pair=new BalanceMetrics();pair.Values["seed"]=seed.ToString();pair.Values["seconds_saved"]=benefit;
                pair.Values["fraction_saved"]=benefit/off.Number("mean_finish_seconds");
                text.Append(off.ToJson()).Append('\n').Append(on.ToJson()).Append('\n').Append(pair.ToJson()).Append('\n');
                Assert.That(off.Number("draft_athlete_seconds"),Is.Zero);
                Assert.That(on.Number("maximum_drag_reduction"),Is.InRange(0,.35));
                Assert.That(Math.Abs(pair.Number("fraction_saved")),Is.LessThan(.05));
                sum+=benefit;exposure+=on.Number("draft_athlete_seconds");
            }
            Artifact("wp097.jsonl",text.ToString());
            TestContext.Out.WriteLine("Mean draft seconds saved="+sum/8+", exposure="+exposure);
            Assert.That(exposure,Is.GreaterThan(0));Assert.That(sum,Is.GreaterThan(0));
        }
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using Biathlon.Career;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ReliabilityDiagnosticsTests
    {
        private static ReliabilityPolicy Policy()=>ReliabilityPolicy.FromJson(PhysiologyContractTests.DefinitionJson("reliability_policy_v1.json"));
        [Test]public void RingRetainsOnlyBoundedOrderedIssueCodesAndRejectsInvalidEvents()
        {
            var policy=Policy();var rows=new ReliabilityEventBuffer(policy);for(int i=0;i<100;i++)rows.Record(DiagnosticIssue.SaveWriteFailed,1000+i,i);
            var copy=rows.Snapshot();Assert.That(copy.Length,Is.EqualTo(policy.EventCapacity));Assert.That(copy[0].RaceTick,Is.EqualTo(100-policy.EventCapacity));Assert.That(copy[^1].RaceTick,Is.EqualTo(99));
            Assert.Throws<ArgumentException>(()=>rows.Record((DiagnosticIssue)999,1));Assert.Throws<ArgumentException>(()=>rows.Record(DiagnosticIssue.RuntimeError,-1));
            copy[0]=default;Assert.That(rows.Snapshot()[0].Code,Is.EqualTo("SaveWriteFailed"));
        }
        [Test]public void ExportContainsUsefulStructureButNoIdentityPathsRawLogsOrRandomCheckpoint()
        {
            string directory=Path.Combine(Path.GetTempPath(),"biathlon-diagnostic-"+Guid.NewGuid().ToString("N"));
            try
            {
                var d=new CareerDefinitions(PhysiologyContractTests.DefinitionJson);var save=CareerFactory.Create(d.Roster,d.Calendar,"career_privacy","PRIVATE_NAME_TOKEN","NOR","private_appearance",new Biathlon.Simulation.SeededRandomSource(578));string before=save.ToJson();
                var config=new DiagnosticConfiguration("6000.6.0f1","0.16.0","ru",2,true,false,"high");
                var rows=new[]{new DiagnosticEvent(DiagnosticIssue.SaveBackupRecovered,1000,42)};string path=DiagnosticBundle.Export(directory,Policy(),config,save,rows,1001);
                using(var zip=ZipFile.OpenRead(path))
                {
                    Assert.That(zip.Entries.Select(e=>e.FullName),Is.EquivalentTo(new[]{"manifest.json","configuration.json","save-structure.json","events.json"}));
                    string all="";foreach(var item in zip.Entries)using(var reader=new StreamReader(item.Open()))all+=reader.ReadToEnd();
                    foreach(string secret in new[]{"PRIVATE_NAME_TOKEN","career_privacy","private_appearance",save.RandomState.ToString(),directory,"athlete_0","C:\\Users","access_token"})Assert.That(all,Does.Not.Contain(secret));
                    Assert.That(all,Does.Contain("SaveBackupRecovered"));Assert.That(all,Does.Contain("6000.6.0f1"));Assert.That(all,Does.Contain("\"saved_races\":0"));
                    using(var reader=new StreamReader(zip.GetEntry("save-structure.json").Open()))
                    {string structure=reader.ReadToEnd();Assert.That(structure,Does.Contain("\"schema_version\":1"));Assert.That(structure,Does.Contain("\"save_version\":3"));}
                }
                Assert.That(save.ToJson(),Is.EqualTo(before));Assert.That(Directory.GetFiles(directory,"*.tmp"),Is.Empty);
                string again=DiagnosticBundle.Export(directory,Policy(),config,save,rows,1001);Assert.That(again,Is.Not.EqualTo(path));
            }
            finally{if(Directory.Exists(directory)){foreach(string path in Directory.GetFiles(directory))File.Delete(path);Directory.Delete(directory);}}
        }
        [Test]public void PublicMetadataRejectsArbitraryPathOrLogInjection()
        {
            Assert.Throws<ArgumentException>(()=>new DiagnosticConfiguration("C:/Users/secret","0.16.0","ru",0,false,false,"high"));
            Assert.Throws<ArgumentException>(()=>new DiagnosticConfiguration("6000.6.0f1","token=user@mail","ru",0,false,false,"high"));
            Assert.Throws<ArgumentException>(()=>new DiagnosticConfiguration("6000.6.0f1","0.16.0","ru",0,false,false,"secret"));
            Assert.Throws<SerializationException>(()=>ReliabilityPolicy.FromJson("{\"schema_version\":1,\"id\":\"bad\",\"diagnostic_event_capacity\":999999,\"maximum_bundle_bytes\":262144}"));
        }
    }
}

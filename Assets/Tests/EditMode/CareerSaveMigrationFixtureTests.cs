using System;
using System.IO;
using System.Runtime.Serialization;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerSaveMigrationFixtureTests
    {
        private static string Fixture(string name)=>PhysiologyContractTests.ProjectFile("Assets/Tests/Fixtures/CareerMigration/"+name);
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void ReleasedFixtureMigrationsPreserveAuthoritativeStateAndRecordedPayload(int version)
        {
            string source=Fixture("v"+version+".json"),expected=Fixture("v3.json");
            Assert.That(BalanceRegression.Hash(source),Is.EqualTo(Fixture("v"+version+".sha256.txt").Trim()),"An archived fixture must not silently be regenerated.");
            var result=CareerSaveMigrations.Load(source);Assert.That(result.SourceVersion,Is.EqualTo(version));
            Assert.That(result.TargetVersion,Is.EqualTo(3));Assert.That(result.StepCount,Is.EqualTo(3-version));Assert.That(result.WasMigrated,Is.EqualTo(version!=3));
            Assert.That(result.Save.ToJson(),Is.EqualTo(expected),"Only the declared confidence/legacy appearance migration is permitted.");
            Assert.That(BalanceRegression.Hash(result.Save.ToJson()),Is.EqualTo(Fixture("target-v3.sha256.txt").Trim()));
            var canonical=CareerSave.FromJson(expected);Assert.That(result.Save.RandomState,Is.EqualTo(canonical.RandomState));
            Assert.That(result.Save.PlayerId,Is.EqualTo(canonical.PlayerId));Assert.That(result.Save.NextAthleteOrdinal,Is.EqualTo(canonical.NextAthleteOrdinal));
            Assert.That(result.Save.Race(0).AuthorityResultJson,Is.EqualTo(canonical.Race(0).AuthorityResultJson));
            Assert.That(result.Save.Race(0).AuthoritySha256,Is.EqualTo(canonical.Race(0).AuthoritySha256));
            for(int i=0;i<result.Save.AthleteCount;i++)for(int stat=0;stat<13;stat++)
                Assert.That(result.Save.Athlete(i).Stat((CareerStat)stat),Is.EqualTo(canonical.Athlete(i).Stat((CareerStat)stat)));
            Assert.That(CareerSaveMigrations.Load(result.Save.ToJson()).StepCount,Is.Zero);
            if(version==1){Assert.That(result.StepId(0),Is.EqualTo("career_v1_to_v2_confidence"));Assert.That(result.StepId(1),Is.EqualTo("career_v2_to_v3_appearance"));}
            if(version==2)Assert.That(result.StepId(0),Is.EqualTo("career_v2_to_v3_appearance"));
            Assert.Throws<ArgumentOutOfRangeException>(()=>result.StepId(-1));Assert.Throws<ArgumentOutOfRangeException>(()=>result.StepId(result.StepCount));
        }
        [TestCase(1)]
        [TestCase(2)]
        public void LegacyFileIsReadWithoutRewriteAndNextCasSaveRetainsOriginalBackup(int version)
        {
            string folder=Path.Combine(Path.GetTempPath(),"biathlon-migration-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
            try
            {
                string source=Fixture("v"+version+".json"),target=Path.Combine(folder,"career_migration.json");File.WriteAllText(target,source);
                var store=new FileCareerProfileStore(folder);Assert.That(store.ReadLatest(),Is.EqualTo(source));
                Assert.That(store.LastRead.WasMigrated,Is.True);Assert.That(store.LastRead.SourceSaveVersion,Is.EqualTo(version));Assert.That(File.ReadAllText(target),Is.EqualTo(source));
                string canonical=CareerSave.FromJson(source).ToJson();store.Replace("career_migration",canonical,canonical);
                Assert.That(File.ReadAllText(target+".bak"),Is.EqualTo(source));Assert.That(store.ReadLatest(),Is.EqualTo(canonical));Assert.That(store.LastRead.WasMigrated,Is.False);
            }
            finally{foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder,false);}
        }
        [TestCase("{\"schema_version\":1,\"save_version\":3}")]
        [TestCase("[]")]
        [TestCase("null")]
        [TestCase("{\"schema_version\":1,\"save_version\":3,}")]
        [TestCase("{\"schema_version\":1,\"save_version\":3,\"save_version\":1}")]
        [TestCase("{\"schema_version\":1,\"save_version\":03}")]
        [TestCase("{\"schema_version\":1,\"save_version\":3e}")]
        [TestCase("{\"schema_version\":1,\"save_version\":3,\"text\":\"bad\\q\"}")]
        public void AmbiguousMalformedOrIncompleteSchemaCannotMigrate(string json)
        {Assert.Catch<SerializationException>(()=>CareerSaveMigrations.Load(json));}
        [Test] public void TrailingWhitespaceAndSingleUtf8BomRemainCompatible()
        {Assert.That(CareerSave.FromJson("\uFEFF  "+Fixture("v3.json")+"\r\n\t").ToJson(),Is.EqualTo(Fixture("v3.json")));}
        [Test] public void EscapedDuplicateMemberNameIsAlsoRejected()
        {
            string source=Fixture("v3.json");string ambiguous=source.Insert(1,"\"\\u0073chema_version\":1,");
            Assert.Catch<SerializationException>(()=>CareerSave.FromJson(ambiguous));
        }
    }
}

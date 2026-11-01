using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Biathlon.Career;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerProfileReliabilityTests
    {
        private string folder;
        private string target;
        private string initial;
        private string next;
        [SetUp] public void Setup()
        {
            folder=Path.Combine(Path.GetTempPath(),"biathlon-reliability-"+Guid.NewGuid().ToString("N"));
            target=Path.Combine(folder,"career_test.json");Directory.CreateDirectory(folder);
            initial=CareerFoundationTests.Create().ToJson();next=PhysiologyContractTests.ReplaceNumber(initial,"week","2");
        }
        [TearDown] public void Teardown()
        {
            // The fixture owns this single, non-recursive temporary directory.
            if(Directory.Exists(folder)){foreach(string file in Directory.GetFiles(folder))File.Delete(file);Directory.Delete(folder,false);}
        }
        [Test] public void CorruptPrimaryRecoversSameCareerBackupWithoutMutatingAnyBytes()
        {
            var store=new FileCareerProfileStore(folder);store.WriteNew("career_test",initial);store.Replace("career_test",initial,next);
            File.WriteAllText(target,"{truncated");
            Assert.That(store.HasProfile,Is.True);Assert.That(store.ReadLatest(),Is.EqualTo(initial));
            Assert.That(store.LastRead.Source,Is.EqualTo(CareerProfileSource.Backup));Assert.That(store.LastRead.WasRecovered,Is.True);
            Assert.That(store.LastRead.ContentSha256,Is.EqualTo(Biathlon.AI.BalanceRegression.Hash(initial)));
            Assert.That(File.ReadAllText(target),Is.EqualTo("{truncated"));Assert.That(File.ReadAllText(target+".bak"),Is.EqualTo(initial));
        }
        [Test] public void SuccessfulPublicationsUpdateCurrentHealthWithoutErasingRecoveryHistory()
        {
            var store=new FileCareerProfileStore(folder);Assert.That(store.CurrentSource,Is.Null);
            store.WriteNew("career_test",initial);Assert.That(store.CurrentSource,Is.EqualTo(CareerProfileSource.Primary));
            store.Replace("career_test",initial,next);File.WriteAllText(target,"damaged");store.ReadLatest();
            Assert.That(store.CurrentSource,Is.EqualTo(CareerProfileSource.Backup));
            store.Replace("career_test",initial,next);
            Assert.That(store.CurrentSource,Is.EqualTo(CareerProfileSource.Primary));Assert.That(store.LastRead.Source,Is.EqualTo(CareerProfileSource.Backup));
            using(var locked=new FileStream(target+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))
                Assert.Throws<IOException>(()=>store.Replace("career_test",next,initial));
            Assert.That(store.CurrentSource,Is.EqualTo(CareerProfileSource.Primary));
        }
        [Test] public void RecoveredWriterRetainsGoodBackupAndQuarantinesCorruptPrimary()
        {
            var store=new FileCareerProfileStore(folder);store.WriteNew("career_test",initial);store.Replace("career_test",initial,next);
            File.WriteAllText(target,"damaged");Assert.That(store.ReadLatest(),Is.EqualTo(initial));store.Replace("career_test",initial,next);
            Assert.That(store.ReadLatest(),Is.EqualTo(next));Assert.That(store.LastRead.Source,Is.EqualTo(CareerProfileSource.Primary));
            Assert.That(File.ReadAllText(target+".bak"),Is.EqualTo(initial));
            var quarantine=Directory.GetFiles(folder,"*.corrupt-*");Assert.That(quarantine.Length,Is.EqualTo(1));Assert.That(File.ReadAllText(quarantine[0]),Is.EqualTo("damaged"));
            Assert.That(Directory.GetFiles(folder,"*.tmp"),Is.Empty);
        }
        [Test] public void MissingPrimaryBackupIsDiscoverableAndRepairDoesNotDestroyIt()
        {
            File.WriteAllText(target+".bak",initial);var store=new FileCareerProfileStore(folder);
            Assert.That(store.HasProfile,Is.True);Assert.That(store.ReadLatest(),Is.EqualTo(initial));store.Replace("career_test",initial,next);
            Assert.That(File.ReadAllText(target),Is.EqualTo(next));Assert.That(File.ReadAllText(target+".bak"),Is.EqualTo(initial));
            Assert.Throws<IOException>(()=>store.WriteNew("career_test",initial));
        }
        [Test] public void BeforeRaceCheckpointIsExactCommittedSourceAndCanRecoverWhenBothOtherCopiesFail()
        {
            var store=new FileCareerProfileStore(folder);var session=new FrontEndSession(PhysiologyContractTests.DefinitionJson,store,s=>new SeededRandomSource(s));
            session.OpenNewCareer();session.CreateCareer("career_test","Alex","NOR",12);
            for(int i=0;i<5;i++)session.RestWeek();
            session.OpenCareerPage(FrontEndScreen.Training);session.AddTraining(CareerTrainingKind.Endurance);session.Back();
            string source=session.Career.ToJson();session.OpenPreRace();
            Assert.That(session.PreparedWeek.RaceSnapshotJson,Is.Not.EqualTo(source));session.BeginRace();
            Assert.That(File.ReadAllText(target+".checkpoint"),Is.EqualTo(source));
            Assert.That(File.ReadAllText(target),Is.EqualTo(source));session.AbandonRace();
            Assert.That(session.Career.ToJson(),Is.EqualTo(source));
            File.WriteAllText(target,"bad");File.WriteAllText(target+".bak","bad backup");
            Assert.That(store.ReadLatest(),Is.EqualTo(source));Assert.That(store.LastRead.Source,Is.EqualTo(CareerProfileSource.BeforeRaceCheckpoint));
            // Repair itself is a persistence operation, not a fabricated career
            // transition: keep the exact development observation/calendar state.
            string after=source;store.Replace("career_test",source,after);
            Assert.That(File.ReadAllText(target+".bak"),Is.EqualTo(source));Assert.That(store.ReadLatest(),Is.EqualTo(after));
        }
        [Test] public void AbandonedTemporaryIsNeverPromotedAndNeverDeletedByReading()
        {
            string orphan=target+".interrupted.tmp";File.WriteAllText(orphan,next);
            var store=new FileCareerProfileStore(folder);Assert.That(store.HasProfile,Is.False);
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.ReadLatest()).Failure,Is.EqualTo(CareerProfileFailure.NoProfile));
            store.WriteNew("career_test",initial);Assert.That(store.ReadLatest(),Is.EqualTo(initial));Assert.That(File.ReadAllText(orphan),Is.EqualTo(next));
        }
        [Test] public void CorruptNewestCareerDoesNotSilentlyOpenAnUnrelatedOlderCareer()
        {
            string older=initial.Replace("career_test","career_older");File.WriteAllText(Path.Combine(folder,"career_older.json"),older);
            File.SetLastWriteTimeUtc(Path.Combine(folder,"career_older.json"),new DateTime(2020,1,1,0,0,0,DateTimeKind.Utc));
            File.WriteAllText(target,"damaged");var store=new FileCareerProfileStore(folder);
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.ReadLatest()).Failure,Is.EqualTo(CareerProfileFailure.Corrupt));
            Assert.That(File.ReadAllText(Path.Combine(folder,"career_older.json")),Is.EqualTo(older));
        }
        [TestCase("schema_version","2")]
        [TestCase("save_version","5")]
        public void FuturePrimaryIsNotDowngradedToSupportedBackup(string key,string version)
        {
            string future=PhysiologyContractTests.ReplaceNumber(initial,key,version);File.WriteAllText(target,future);File.WriteAllText(target+".bak",initial);
            var store=new FileCareerProfileStore(folder);
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.ReadLatest()).Failure,Is.EqualTo(CareerProfileFailure.UnsupportedVersion));
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.Replace("career_test",initial,next)).Failure,Is.EqualTo(CareerProfileFailure.UnsupportedVersion));
            Assert.That(File.ReadAllText(target),Is.EqualTo(future));Assert.That(File.ReadAllText(target+".bak"),Is.EqualTo(initial));
        }
        [Test] public void MismatchedBackupIdentityDoesNotPublishAnotherAthlete()
        {
            File.WriteAllText(target,"damaged");File.WriteAllText(target+".bak",initial.Replace("career_test","career_someone"));
            Assert.That(Assert.Throws<CareerProfileException>(()=>new FileCareerProfileStore(folder).ReadLatest()).Failure,Is.EqualTo(CareerProfileFailure.Corrupt));
        }
        [TestCase("trailing")]
        [TestCase("second-object")]
        [TestCase("truncated")]
        public void InvalidCompleteJsonNeverBecomesDiscoverable(string kind)
        {
            string malformed=kind=="trailing"?initial+"broken":kind=="second-object"?initial+"{}":initial.Substring(0,initial.Length-1);
            var store=new FileCareerProfileStore(folder);Assert.Catch(()=>store.WriteNew("career_test",malformed));
            Assert.That(store.HasProfile,Is.False);Assert.That(File.Exists(target),Is.False);
        }
        [Test] public void UnsupportedBackupIsPreservedRatherThanOverwrittenByOlderCheckpoint()
        {
            File.WriteAllText(target,"damaged");File.WriteAllText(target+".bak",PhysiologyContractTests.ReplaceNumber(initial,"save_version","99"));File.WriteAllText(target+".checkpoint",initial);
            Assert.That(Assert.Throws<CareerProfileException>(()=>new FileCareerProfileStore(folder).ReadLatest()).Failure,Is.EqualTo(CareerProfileFailure.UnsupportedVersion));
        }
        [Test] public void StaleWriterCannotUndoRecoveredOrCommittedCareer()
        {
            var store=new FileCareerProfileStore(folder);store.WriteNew("career_test",initial);store.Replace("career_test",initial,next);
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.Replace("career_test",initial,initial)).Failure,Is.EqualTo(CareerProfileFailure.Changed));
            Assert.That(File.ReadAllText(target),Is.EqualTo(next));Assert.That(File.ReadAllText(target+".bak"),Is.EqualTo(initial));
        }
        private sealed class FaultFiles : ICareerProfileFileSystem
        {
            private readonly CareerProfileFileSystem real=new CareerProfileFileSystem();
            public string Fault;
            public bool DirectoryExists(string path)=>real.DirectoryExists(path);
            public void CreateDirectory(string path)=>real.CreateDirectory(path);
            public string[] GetFiles(string path,string pattern)
            {if(Fault=="list-denied")throw new UnauthorizedAccessException("simulated");if(Fault=="list-io")throw new IOException("simulated");return real.GetFiles(path,pattern);}
            public bool FileExists(string path)=>real.FileExists(path);
            public DateTime LastWriteUtc(string path)=>real.LastWriteUtc(path);
            public string ReadUtf8(string path)
            {
                if(Fault=="read-denied"&&!path.EndsWith(".tmp",StringComparison.Ordinal))throw new UnauthorizedAccessException("simulated");
                if(Fault=="read-io"&&!path.EndsWith(".tmp",StringComparison.Ordinal))throw new IOException("simulated");
                string value=real.ReadUtf8(path);return Fault=="verify"&&path.EndsWith(".tmp",StringComparison.Ordinal)?value+" ":value;
            }
            public void WriteUtf8Flushed(string path,string value)
            {
                if(Fault=="write"||Fault=="write-cleanup") {real.WriteUtf8Flushed(path,"{interrupted");throw new IOException("simulated disk full");}
                real.WriteUtf8Flushed(path,value);
            }
            public IDisposable AcquireExclusive(string path)=>real.AcquireExclusive(path);
            public void MoveNew(string source,string destination) {if(Fault=="move")throw new IOException("simulated move");real.MoveNew(source,destination);}
            public void Replace(string source,string destination,string backup) {if(Fault=="replace")throw new IOException("simulated replace");real.Replace(source,destination,backup);}
            public void Delete(string path) {if(Fault=="write-cleanup")throw new UnauthorizedAccessException("simulated cleanup");real.Delete(path);}
        }
        [TestCase("write")]
        [TestCase("verify")]
        [TestCase("replace")]
        [TestCase("write-cleanup")]
        public void InterruptedWriteVerificationOrReplacementRetainsOnlyCommittedState(string fault)
        {
            var fs=new FaultFiles();var store=new FileCareerProfileStore(folder,fs);store.WriteNew("career_test",initial);fs.Fault=fault;
            Assert.Throws<IOException>(()=>store.Replace("career_test",initial,next));
            Assert.That(File.ReadAllText(target),Is.EqualTo(initial));Assert.That(File.Exists(target+".bak"),Is.False);
            fs.Fault=null;Assert.That(store.ReadLatest(),Is.EqualTo(initial));
            Assert.That(Directory.GetFiles(folder,"*.tmp").Length,Is.EqualTo(fault=="write-cleanup"?1:0));
            store.Replace("career_test",initial,next);Assert.That(store.ReadLatest(),Is.EqualTo(next));
        }
        [TestCase("write")]
        [TestCase("verify")]
        [TestCase("move")]
        public void FailedNewCareerIsNotDiscoverable(string fault)
        {
            var fs=new FaultFiles{Fault=fault};var store=new FileCareerProfileStore(folder,fs);
            Assert.Throws<IOException>(()=>store.WriteNew("career_test",initial));Assert.That(store.HasProfile,Is.False);
            Assert.That(File.Exists(target),Is.False);Assert.That(Directory.GetFiles(folder,"*.tmp"),Is.Empty);
        }
        [TestCase("read-denied",CareerProfileFailure.AccessDenied)]
        [TestCase("read-io",CareerProfileFailure.IoFailure)]
        public void AccessFailuresDoNotSilentlyRollBackToBackup(string fault,CareerProfileFailure reason)
        {
            File.WriteAllText(target,next);File.WriteAllText(target+".bak",initial);var fs=new FaultFiles{Fault=fault};var store=new FileCareerProfileStore(folder,fs);
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.ReadLatest()).Failure,Is.EqualTo(reason));Assert.That(File.ReadAllText(target),Is.EqualTo(next));
        }
        [TestCase("list-denied",CareerProfileFailure.AccessDenied)]
        [TestCase("list-io",CareerProfileFailure.IoFailure)]
        public void InaccessibleProfileDirectoryDoesNotBreakHomeOrPretendItIsEmpty(string fault,CareerProfileFailure reason)
        {
            var store=new FileCareerProfileStore(folder,new FaultFiles{Fault=fault});
            Assert.That(store.HasProfile,Is.True);
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.ReadLatest()).Failure,Is.EqualTo(reason));Assert.That(store.LastRead,Is.Null);
        }
        [Test] public void FailedReadClearsOldRecoveryReceiptInsteadOfReportingStaleSuccess()
        {
            var store=new FileCareerProfileStore(folder);store.WriteNew("career_test",initial);Assert.That(store.ReadLatest(),Is.EqualTo(initial));Assert.That(store.LastRead,Is.Not.Null);
            File.WriteAllText(target,"corrupt");Assert.Throws<CareerProfileException>(()=>store.ReadLatest());Assert.That(store.LastRead,Is.Null);
        }
        [Test] public void InvalidUtf8IsCorruptionAndFallsBackWithoutReplacingSourceBytes()
        {
            File.WriteAllText(target+".bak",initial);byte[] invalid={0x7b,0x22,0xc3,0x22,0x7d};File.WriteAllBytes(target,invalid);
            var store=new FileCareerProfileStore(folder);Assert.That(store.ReadLatest(),Is.EqualTo(initial));Assert.That(store.LastRead.Source,Is.EqualTo(CareerProfileSource.Backup));
            Assert.That(File.ReadAllBytes(target),Is.EqualTo(invalid));
        }
        [Test] public void FailedBeforeRaceCheckpointLeavesPreparedRaceAndDiskUnchanged()
        {
            var fs=new FaultFiles();var store=new FileCareerProfileStore(folder,fs);var ui=new FrontEndSession(PhysiologyContractTests.DefinitionJson,store,s=>new SeededRandomSource(s));
            ui.OpenNewCareer();ui.CreateCareer("career_test","Alex","NOR",12);for(int i=0;i<5;i++)ui.RestWeek();ui.OpenPreRace();
            string before=ui.Career.ToJson(),prepared=ui.PreparedWeek.RaceSnapshotJson;fs.Fault="write";
            Assert.Throws<IOException>(()=>ui.BeginRace());Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.PreRace));
            Assert.That(ui.Career.ToJson(),Is.EqualTo(before));Assert.That(ui.PreparedWeek.RaceSnapshotJson,Is.EqualTo(prepared));Assert.That(File.ReadAllText(target),Is.EqualTo(before));
            fs.Fault=null;ui.BeginRace();Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Race));Assert.That(File.ReadAllText(target+".checkpoint"),Is.EqualTo(before));
        }
        [Test] public void RetryingTheSamePreparedRaceReusesVerifiedCheckpointWithoutWritingAgain()
        {
            var fs=new FaultFiles();var store=new FileCareerProfileStore(folder,fs);var ui=new FrontEndSession(PhysiologyContractTests.DefinitionJson,store,s=>new SeededRandomSource(s));
            ui.OpenNewCareer();ui.CreateCareer("career_test","Alex","NOR",12);for(int i=0;i<5;i++)ui.RestWeek();ui.OpenPreRace();
            string before=ui.Career.ToJson(),prepared=ui.PreparedWeek.RaceSnapshotJson;ui.BeginRace();ui.AbandonRace();
            string checkpoint=File.ReadAllText(target+".checkpoint");DateTime written=File.GetLastWriteTimeUtc(target+".checkpoint");
            fs.Fault="write";
            Assert.DoesNotThrow(()=>ui.BeginRace(),"Retrying an already durable checkpoint must not perform redundant disk writes.");
            Assert.That(ui.Screen,Is.EqualTo(FrontEndScreen.Race));Assert.That(ui.Career.ToJson(),Is.EqualTo(before));Assert.That(ui.PreparedWeek.RaceSnapshotJson,Is.EqualTo(prepared));
            Assert.That(File.ReadAllText(target+".checkpoint"),Is.EqualTo(checkpoint));Assert.That(File.GetLastWriteTimeUtc(target+".checkpoint"),Is.EqualTo(written));
            Assert.That(File.ReadAllText(target),Is.EqualTo(before));Assert.That(Directory.GetFiles(folder,"*.tmp"),Is.Empty);
        }
        [Test] public void MatchingOldCheckpointNeverBypassesPrimaryCompareAndSwap()
        {
            var store=new FileCareerProfileStore(folder);store.WriteNew("career_test",initial);store.SaveBeforeRaceCheckpoint("career_test",initial);store.Replace("career_test",initial,next);
            Assert.That(Assert.Throws<CareerProfileException>(()=>store.SaveBeforeRaceCheckpoint("career_test",initial)).Failure,Is.EqualTo(CareerProfileFailure.Changed));
            Assert.That(File.ReadAllText(target+".checkpoint"),Is.EqualTo(initial));Assert.That(File.ReadAllText(target),Is.EqualTo(next));
        }
        [Test] public void ChangedSourceStillRequiresANewVerifiedCheckpoint()
        {
            var fs=new FaultFiles();var store=new FileCareerProfileStore(folder,fs);store.WriteNew("career_test",initial);store.SaveBeforeRaceCheckpoint("career_test",initial);store.Replace("career_test",initial,next);fs.Fault="write";
            Assert.Throws<IOException>(()=>store.SaveBeforeRaceCheckpoint("career_test",next));Assert.That(File.ReadAllText(target+".checkpoint"),Is.EqualTo(initial));
            fs.Fault=null;store.SaveBeforeRaceCheckpoint("career_test",next);Assert.That(File.ReadAllText(target+".checkpoint"),Is.EqualTo(next));
        }
        [Test] public void ConcurrentExclusiveLockPreventsAnyReplacement()
        {
            var store=new FileCareerProfileStore(folder);store.WriteNew("career_test",initial);
            using(new FileStream(target+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None))Assert.Throws<IOException>(()=>store.Replace("career_test",initial,next));
            Assert.That(store.ReadLatest(),Is.EqualTo(initial));
        }
    }
}

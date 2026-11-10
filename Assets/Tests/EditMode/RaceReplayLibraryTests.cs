using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RaceReplayLibraryTests
    {
        private string directory;
        [SetUp]public void Setup()=>directory=Path.Combine(Path.GetTempPath(),"biathlon-replay-"+Guid.NewGuid().ToString("N"));
        [TearDown]public void Cleanup(){if(Directory.Exists(directory)){foreach(var f in Directory.GetFiles(directory))File.Delete(f);Directory.Delete(directory,false);}}
        private RaceReplayInfo Info()=>new RaceReplayInfo{Id="race_test",RecordedUtc=DateTime.UtcNow.ToString("O"),CareerSnapshotJson="{}",EventId="test",VenueId="woodland_basin_v1",PlayerId="player",PlayerName="Alex",TickFrequency=20,TickCount=100,ResultSha256=new string('a',64),Events=Array.Empty<RecordedRaceEvent>(),Highlights=Array.Empty<RaceHighlight>()};
        private sealed class StalledDestination : MemoryStream
        {
            internal readonly ManualResetEventSlim Entered = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim Release = new ManualResetEventSlim();
            public override void Write(byte[] buffer, int offset, int count)
            {
                Entered.Set();
                if (!Release.Wait(TimeSpan.FromSeconds(10))) throw new IOException("Test destination timed out.");
                base.Write(buffer, offset, count);
            }
        }

        [Test]
        public void StalledReplayDestinationCannotBlockRaceProducer()
        {
            using var disk = new StalledDestination();
            using var producerReturned = new ManualResetEventSlim();
            var producer = Task.Run(() =>
            {
                using var output = new ReplayWriteQueue(disk);
                output.WriteByte(7);
                output.Flush();
                Assert.That(disk.Entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.Throws<IOException>(() => output.Write(new byte[2 * 1024 * 1024]));
                producerReturned.Set();
            });
            try
            {
                Assert.That(producerReturned.Wait(TimeSpan.FromSeconds(5)), Is.True,
                    "The producer must finish while the destination is still blocked.");
            }
            finally
            {
                disk.Release.Set();
                producer.GetAwaiter().GetResult();
                disk.Entered.Dispose();
                disk.Release.Dispose();
            }
        }

        [Test]
        public void QueuedReplayPublishesVerifiedBytesInOrder()
        {
            var library = new RaceReplayLibrary(directory);
            var info = Info();
            var bytes = new byte[195431];
            new Random(704).NextBytes(bytes);
            using (var output = new ReplayWriteQueue(library.CreatePending(info.Id)))
            {
                output.Write(bytes, 0, bytes.Length - 1);
                output.WriteByte(bytes[bytes.Length - 1]);
                output.Complete();
            }
            library.Publish(info);
            using var input = library.OpenVerified(library.List()[0]);
            using var actual = new MemoryStream();
            input.CopyTo(actual);
            Assert.That(actual.ToArray(), Is.EqualTo(bytes));
        }
        [Test]public void PublishesCompletePairsAndRejectsCorruptedCommandsAndDuplicatePublication()
        {
            var library=new RaceReplayLibrary(directory);var info=Info();using(var output=library.CreatePending(info.Id))output.Write(new byte[]{1,2,3},0,3);
            Assert.That(library.List(),Is.Empty);library.Publish(info);Assert.That(library.List().Length,Is.EqualTo(1));
            using(var opened=library.OpenVerified(library.List()[0]))Assert.That(opened.ReadByte(),Is.EqualTo(1));
            File.WriteAllBytes(library.PendingPath(info.Id),new byte[]{1,2,3});Assert.Throws<IOException>(()=>library.Publish(info));
            File.WriteAllBytes(library.CommandsPath(info.Id),new byte[]{4,5});Assert.Throws<InvalidDataException>(()=>library.OpenVerified(info));
        }
        [Test]public void InvalidMetadataAndUnpairedFilesAreIgnoredAndIdsCannotEscapeLibrary()
        {
            var library=new RaceReplayLibrary(directory);File.WriteAllText(Path.Combine(directory,"broken.json"),"{}");
            var info=Info();info.CommandSha256=new string('b',64);File.WriteAllText(Path.Combine(directory,"race_test.json"),info.ToJson());
            Assert.That(library.List(),Is.Empty);Assert.Throws<ArgumentException>(()=>library.PendingPath("../escape"));
            Assert.Throws<ArgumentException>(()=>library.CommandsPath("C:\\escape"));
        }
        [TestCase(-1)][TestCase(3)]public void UnsupportedStartOrderVersionCannotBePublished(int version)
        {
            var info=Info();info.CommandSha256=new string('b',64);info.StartOrderVersion=version;
            Assert.Throws<System.Runtime.Serialization.SerializationException>(()=>info.ToJson());
        }
        [TestCase(-1)][TestCase(4)]public void UnsupportedStartContinuationVersionCannotBePublished(int version)
        {var info=Info();info.CommandSha256=new string('b',64);info.StartContinuationVersion=version;Assert.Throws<System.Runtime.Serialization.SerializationException>(()=>info.ToJson());}
        [Test]public void PhysicalStartReleaseVersionIsRetainedInReplayMetadata()
        {var info=Info();info.CommandSha256=new string('b',64);info.StartContinuationVersion=3;Assert.That(RaceReplayInfo.FromJson(info.ToJson()).StartContinuationVersion,Is.EqualTo(3));}
        [TestCase(-1)][TestCase(2)]public void UnsupportedSnowSearchVersionCannotBePublished(int version)
        {var info=Info();info.CommandSha256=new string('b',64);info.AiSnowSearchVersion=version;Assert.Throws<System.Runtime.Serialization.SerializationException>(()=>info.ToJson());}
        [TestCase(-1)][TestCase(3)]public void UnsupportedMovingOvertakeVersionCannotBePublished(int version)
        {var info=Info();info.CommandSha256=new string('b',64);info.MovingOvertakeVersion=version;Assert.Throws<System.Runtime.Serialization.SerializationException>(()=>info.ToJson());}
        [Test]public void MovingOvertakeVersionIsExplicitAndMissingLegacyFieldRemainsAbsent()
        {
            var info=Info();info.CommandSha256=new string('b',64);string legacy=info.ToJson();
            Assert.That(legacy,Does.Not.Contain("moving_overtake_version"));Assert.That(RaceReplayInfo.FromJson(legacy).ToJson(),Is.EqualTo(legacy));
            info.MovingOvertakeVersion=1;Assert.That(RaceReplayInfo.FromJson(info.ToJson()).MovingOvertakeVersion,Is.EqualTo(1));
            info.MovingOvertakeVersion=2;Assert.That(RaceReplayInfo.FromJson(info.ToJson()).MovingOvertakeVersion,Is.EqualTo(2));
        }
    }
}

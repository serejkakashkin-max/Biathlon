using System;
using System.IO;
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
        [TestCase(-1)][TestCase(3)]public void UnsupportedStartContinuationVersionCannotBePublished(int version)
        {var info=Info();info.CommandSha256=new string('b',64);info.StartContinuationVersion=version;Assert.Throws<System.Runtime.Serialization.SerializationException>(()=>info.ToJson());}
        [TestCase(-1)][TestCase(2)]public void UnsupportedSnowSearchVersionCannotBePublished(int version)
        {var info=Info();info.CommandSha256=new string('b',64);info.AiSnowSearchVersion=version;Assert.Throws<System.Runtime.Serialization.SerializationException>(()=>info.ToJson());}
    }
}

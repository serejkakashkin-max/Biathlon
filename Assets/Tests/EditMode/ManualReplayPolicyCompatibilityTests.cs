using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using Biathlon.AI;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualReplayPolicyCompatibilityTests
    {
        private const string FixtureRoot="Assets/Tests/Fixtures/Replay/";
#if UNITY_EDITOR
        // Mono and CoreCLR historically serialize identical doubles differently.
        // Each runtime must open its own independently recorded original header;
        // the production reader never normalizes or ignores either header hash.
        private const string FixtureName="v25-original-native";
#else
        private const string FixtureName="v25-original";
#endif
        private static string Read(string n)=>PhysiologyContractTests.DefinitionJson(n);
        private static string Venue(string n)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+n+".json");
        private static (PackAiRaceRunner runner,ManualScopedFieldStepper stepper) Initial(ManualCourseReactionPolicy policy)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Venue(root+"manifest")),Venue);
            var runner=ProductionVenueRaceFactory.Create(venue,CompetitionProfile.FromJson(Venue(root+"mass_start_profile")),
                RaceRoutePlan.FromJson(Venue(root+"mass_start_route")),RaceSnowConditions.FromJson(Read("race_snow_neutral_v1.json")),Read,new RaceEventLog(100000),new AiDecisionCounts(),count:1,seed:123);
            var stepper=ManualProductionFieldComposition.Create(runner.Field,venue,Read,policy);
            runner.BindManualScopedCandidate(stepper,ManualAiSteeringDefinition.FromJson(Read("manual_ai_steering_v2.json")),ManualSkiSteeringDefinition.FromJson(Read("manual_ski_steering_v2.json")));
            return(runner,stepper);
        }
        private static byte[] FixtureBytes()
        {
            var directory=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while(directory!=null)
            {
                string path=Path.Combine(directory.FullName,FixtureRoot,FixtureName+".brp");
                if(File.Exists(path))using(var file=File.OpenRead(path))using(var zip=new GZipStream(file,CompressionMode.Decompress))using(var memory=new MemoryStream())
                {zip.CopyTo(memory);return memory.ToArray();}
                directory=directory.Parent;
            }
            throw new FileNotFoundException("Original v25 journal fixture missing.");
        }
        private static string Hash(byte[] data)
        {using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        [DataContract] private sealed class OriginalFixture
        {
            [DataMember(Name="baseline_commit",IsRequired=true)] public string Commit {get;set;}
            [DataMember(Name="ticks",IsRequired=true)] public int Ticks {get;set;}
            [DataMember(Name="stepper_sha256",IsRequired=true)] public string StepperHash {get;set;}
            [DataMember(Name="world_sha256",IsRequired=true)] public string WorldHash {get;set;}
            [DataMember(Name="commands_sha256",IsRequired=true)] public string CommandsHash {get;set;}
            [DataMember(Name="state",IsRequired=true)] public OriginalState State {get;set;}
            [DataMember(Name="reactions",IsRequired=true)] public OriginalReaction[] Reactions {get;set;}
            [DataMember(Name="metadata_json",IsRequired=true)] public string MetadataJson {get;set;}
        }
        [DataContract] private sealed class OriginalReaction
        {
            [DataMember(Name="input",IsRequired=true)] public double[] Input {get;set;}
            [DataMember(Name="result",IsRequired=true)] public double[] Result {get;set;}
        }
        [DataContract] private sealed class OriginalState
        {
            [DataMember(IsRequired=true)] public double X {get;set;}
            [DataMember(IsRequired=true)] public double Z {get;set;}
            [DataMember(IsRequired=true)] public double Heading {get;set;}
            [DataMember(IsRequired=true)] public double Spin {get;set;}
            [DataMember(IsRequired=true)] public double Vx {get;set;}
            [DataMember(IsRequired=true)] public double Vz {get;set;}
            [DataMember(IsRequired=true)] public int OccurrenceIndex {get;set;}
            [DataMember(IsRequired=true)] public long ObservedArcNanometers {get;set;}
            [DataMember(IsRequired=true)] public long RouteNanometers {get;set;}
            [DataMember(IsRequired=true)] public long DistanceNanometers {get;set;}
            [DataMember(IsRequired=true)] public double SpeedMetersPerSecond {get;set;}
        }
        private static OriginalFixture Fixture()
        {
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(PhysiologyContractTests.ProjectFile(FixtureRoot+FixtureName+".json"))))
                return (OriginalFixture)new DataContractJsonSerializer(typeof(OriginalFixture)).ReadObject(stream);
        }
        [Test] public void OriginalV25GitBlobJournalOpensAndReplaysWithoutChangingItsHeaderOrCommands()
        {
            var fixture=Fixture();Assert.That(fixture.Commit,Is.EqualTo("9867b24bfcaed10dc354267477eb9d665fe50d14"));
            var bytes=FixtureBytes();Assert.That(Hash(bytes),Is.EqualTo(fixture.CommandsHash));
            var target=Initial(ManualCourseReactionPolicy.LegacyV25).stepper;
            Assert.That(ManualContextIdentity.Sha256(target.ContextJson),Is.EqualTo(fixture.StepperHash));
            Assert.That(ManualContextIdentity.Sha256(target.World.ContextJson),Is.EqualTo(fixture.WorldHash));
            using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,target,fixture.Ticks))while(reader.HasNext)reader.Step();
            var actor=target.World.GetActor(0);var cursor=target.World.GetCursor(0);var a=target.Field.GetAthlete(0);var expected=fixture.State;
            Assert.That(target.World.Tick.Value,Is.EqualTo(fixture.Ticks));
            Assert.That(actor.Pose.X,Is.EqualTo(expected.X));Assert.That(actor.Pose.Z,Is.EqualTo(expected.Z));
            Assert.That(actor.Pose.HeadingRadians,Is.EqualTo(expected.Heading));Assert.That(actor.Pose.YawRateRadiansPerSecond,Is.EqualTo(expected.Spin));
            Assert.That(actor.Velocity.X,Is.EqualTo(expected.Vx));Assert.That(actor.Velocity.Z,Is.EqualTo(expected.Vz));
            Assert.That(cursor.OccurrenceIndex,Is.EqualTo(expected.OccurrenceIndex));Assert.That(cursor.ObservedArcNanometers,Is.EqualTo(expected.ObservedArcNanometers));
            Assert.That(a.RouteNanometers,Is.EqualTo(expected.RouteNanometers));Assert.That(a.Motion.DistanceNanometers,Is.EqualTo(expected.DistanceNanometers));
            Assert.That(a.Motion.SpeedMetersPerSecond,Is.EqualTo(expected.SpeedMetersPerSecond));Assert.That(Hash(bytes),Is.EqualTo(fixture.CommandsHash));
        }
        [Test] public void StrictJournalIdentityRejectsCrossPolicyAndTamperedHeadersBeforeAnyTick()
        {
            var bytes=FixtureBytes();var current=Initial(ManualCourseReactionPolicy.PassiveCurvedV26).stepper;
            Assert.Throws<InvalidDataException>(()=>{using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,current,2000)){} });
            Assert.That(current.Field.Tick.Value,Is.Zero);Assert.That(current.World.Tick.Value,Is.Zero);
            // Corrupt a header hash while retaining its encoding. A policy tag cannot bypass attestation.
            using(var stream=new MemoryStream(bytes,true))using(var reader=new BinaryReader(stream,Encoding.UTF8,true))
            {
                reader.ReadInt32();reader.ReadInt32();reader.ReadInt32();long position=stream.Position;string fieldHash=reader.ReadString();
                stream.Position=position;using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))writer.Write((fieldHash[0]=='a'?"b":"a")+fieldHash.Substring(1));
            }
            var legacy=Initial(ManualCourseReactionPolicy.LegacyV25).stepper;
            Assert.Throws<InvalidDataException>(()=>{using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,legacy,2000)){} });
            Assert.That(legacy.World.Tick.Value,Is.Zero);
        }
        [Test] public void HistoricalArrestedSpinReactionRetainsTheOriginalGitBlobForceResults()
        {
            var world=Initial(ManualCourseReactionPolicy.LegacyV25).stepper.World;var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var constructor=typeof(SkiForceCoupledTrajectory).GetConstructor(flags,null,new[]{typeof(ManualSkiHeadingState),typeof(SkiContactMotion),typeof(SkiContactAcceleration),typeof(double)},null);
            var samples=Fixture().Reactions;Assert.That(samples.Length,Is.EqualTo(3));
            foreach(var sample in samples)
            {
                var v=sample.Input;
                ((bool[])typeof(ManualScopedWorldRoster).GetField("courseSupports",flags).GetValue(world))[0]=true;
                ((double[])typeof(ManualScopedWorldRoster).GetField("courseNormalsX",flags).GetValue(world))[0]=v[0];
                ((double[])typeof(ManualScopedWorldRoster).GetField("courseNormalsZ",flags).GetValue(world))[0]=v[1];
                ((double[])typeof(ManualScopedWorldRoster).GetField("courseLevers",flags).GetValue(world))[0]=v[2];
                var free=(SkiForceCoupledTrajectory)constructor.Invoke(new object[]{world.GetActor(0).Pose,new SkiContactMotion(new SkiPlanarVelocity(4,3),.1),new SkiContactAcceleration(v[3],v[4],v[5]),.05});
                var actual=(SkiForceCoupledTrajectory)typeof(ManualScopedWorldRoster).GetMethod("ApplyCourseReaction",flags).Invoke(world,new object[]{0,free});
                Assert.That(new[]{actual.Acceleration.X,actual.Acceleration.Z,actual.Acceleration.Yaw},Is.EqualTo(sample.Result),"Original force samples must not inherit the new curved-contact fallback.");
                Assert.That(world.CurvedCourseContinuations,Is.Zero);
            }
        }
        [Test] public void NewV26JournalReplaysExactlyAndCannotBeReadWithTheHistoricalForceLaw()
        {
            var source=Initial(ManualCourseReactionPolicy.PassiveCurvedV26);byte[] bytes;
            using(var stream=new MemoryStream())using(var writer=new ManualRaceJournalWriter(stream,source.stepper,SkiTechniqueDefinition.FromJson(Read("ski_technique_provisional_v1.json"))))
            {source.stepper.CommandObserver=writer;for(int i=0;i<1200;i++)source.runner.Step();writer.Complete();Assert.That(writer.Failure,Is.Null);bytes=stream.ToArray();}
            for(int repeat=0;repeat<2;repeat++)
            {
                var target=Initial(ManualCourseReactionPolicy.PassiveCurvedV26).stepper;
                using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,target,1200))while(reader.HasNext)reader.Step();
                Assert.That(StateHash(target),Is.EqualTo(StateHash(source.stepper)),"Recorded commands must reproduce actual pose, progress, velocity and paid resources without AI.");
            }
            var legacy=Initial(ManualCourseReactionPolicy.LegacyV25).stepper;
            Assert.Throws<InvalidDataException>(()=>{using(var stream=new MemoryStream(bytes))using(var reader=new ManualRaceJournalReader(stream,legacy,1200)){} });
            Assert.That(legacy.World.Tick.Value,Is.Zero);
        }
        private static string StateHash(ManualScopedFieldStepper s)
        {
            using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream,Encoding.UTF8,true))
            {
                w.Write(s.World.Tick.Value);
                for(int i=0;i<s.Field.Count;i++)
                {
                    var a=s.World.GetActor(i);var c=s.World.GetCursor(i);var f=s.Field.GetAthlete(i);var p=f.PhysiologyState;
                    w.Write(a.Pose.X);w.Write(a.Pose.Z);w.Write(a.Pose.HeadingRadians);w.Write(a.Pose.YawRateRadiansPerSecond);w.Write(a.Velocity.X);w.Write(a.Velocity.Z);
                    w.Write(c.OccurrenceIndex);w.Write(c.ObservedArcNanometers);w.Write(f.RouteNanometers);w.Write(f.Motion.DistanceNanometers);w.Write(f.Motion.SpeedMetersPerSecond);
                    w.Write(p.HeartRateBpm);w.Write(p.AnaerobicReserve);w.Write(p.MuscularFatigue);w.Write(p.HighLoadSeconds);
                }
                w.Flush();return Hash(stream.ToArray());
            }
        }
        private static RaceReplayInfo Info()=>new RaceReplayInfo{Id="policy_test",RecordedUtc="2026-10-15T00:00:00Z",CareerSnapshotJson="{}",EventId="event",VenueId="woodland_basin_v1",PlayerId="player",PlayerName="Alex",TickFrequency=20,TickCount=1200,CommandSha256=new string('a',64),ResultSha256=new string('b',64),Events=Array.Empty<RecordedRaceEvent>(),Highlights=Array.Empty<RaceHighlight>()};
        [Test] public void LegacyMetadataRemainsByteIdenticalAndNewPolicyIsExplicitValidatedAndPrepared()
        {
            string original=Fixture().MetadataJson;var decoded=RaceReplayInfo.FromJson(original);
            Assert.That(decoded.ToJson(),Is.EqualTo(original),"The original Git-blob metadata must remain byte-identical, including its embedded raw v2 JSON.");
            Assert.That(decoded.CareerSnapshotJson,Is.EqualTo("{\"save_version\":2, \"note\":\"keep whitespace\" }"));
            var info=Info();string legacy=info.ToJson();Assert.That(legacy,Does.Not.Contain("course_reaction_version"));
            Assert.That(RaceReplayInfo.FromJson(legacy).ToJson(),Is.EqualTo(legacy));Assert.That(RaceReplayInfo.FromJson(legacy).CourseReactionPolicy,Is.EqualTo(ManualCourseReactionPolicy.LegacyV25));
            info.CourseReactionVersion=26;Assert.That(info.ToJson(),Does.Contain("\"course_reaction_version\":26"));Assert.That(RaceReplayInfo.FromJson(info.ToJson()).CourseReactionPolicy,Is.EqualTo(ManualCourseReactionPolicy.PassiveCurvedV26));
            info.CourseReactionVersion=24;Assert.Throws<SerializationException>(()=>info.ToJson());
            var d=new CareerDefinitions(Read,"woodland_development_calendar_v1.json");var save=CareerMassStartSeedingTests.Create(d);var provider=new CareerRaceProvider(Read,d,Venue);
            Assert.That(provider.Prepare(save,d.Calendar.Get(2),123).CourseReactionPolicy,Is.EqualTo(ManualCourseReactionPolicy.PassiveCurvedV26));
            Assert.That(provider.Prepare(save,d.Calendar.Get(2),123,courseReactionPolicy:ManualCourseReactionPolicy.LegacyV25).CourseReactionPolicy,Is.EqualTo(ManualCourseReactionPolicy.LegacyV25));
            Assert.Throws<ArgumentOutOfRangeException>(()=>provider.Prepare(save,d.Calendar.Get(2),123,courseReactionPolicy:(ManualCourseReactionPolicy)24));
        }
    }
}

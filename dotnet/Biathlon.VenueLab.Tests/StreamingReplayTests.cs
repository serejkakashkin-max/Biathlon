using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

public sealed class StreamingReplayTests
{
    private string output;
    private static string Project
    {
        get
        {
            var current=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while(current!=null&&!File.Exists(Path.Combine(current.FullName,"AGENTS.md")))current=current.Parent;
            return current?.FullName??throw new InvalidOperationException("Project root unavailable.");
        }
    }
    [SetUp] public void Setup()=>output=Path.Combine(Path.GetTempPath(),"biathlon-stream-replay-"+Guid.NewGuid().ToString("N"));
    [TearDown] public void Teardown(){if(Directory.Exists(output))Directory.Delete(output,true);}
    private string Tape(string name="one")=>Path.Combine(output,name+".gz");
    private static JsonDocument Report(string path)=>JsonDocument.Parse(File.ReadAllText(path));
    private int Record(string name="one",int ticks=12)=>ProductionReplayHarness.Run(new[]{"record",Path.Combine(Project,"Assets/Resources"),
        Path.Combine(Project,"Assets/Resources"),"Biathlon/Venues/woodland_basin_v1/manifest","Biathlon/Venues/woodland_basin_v1/sprint_profile",
        "Biathlon/Venues/woodland_basin_v1/sprint_route","Biathlon/race_snow_neutral_v1","2",ticks.ToString(),Tape(name),Path.Combine(output,name+".record.json")});

    [Test] public void AllAcceptedCommandFieldsRoundTripAtCallerOwnedTickAndIdentity()
    {
        var id=new AthleteId("persisted_command_actor");var tick=new SimulationTick(17);
        var command=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,833),.95,.81,-.3,true,true,"technique",.97),
            new WeaponAimCommand(tick,id,123,-234,true),true,true,new WindVector(1.25,-.5),new ShootingPressureContext(.8,.7,.6,.5,true,true));
        using(var stream=new MemoryStream())
        {
            using(var writer=new BinaryWriter(stream,Encoding.UTF8,true))ProductionReplayHarness.WriteCommand(writer,command,-2.25);
            byte[] expected=stream.ToArray();stream.Position=0;
            SprintAthleteInput rebuilt;double lateral;
            using(var reader=new BinaryReader(stream,Encoding.UTF8,true))rebuilt=ProductionReplayHarness.ReadCommand(reader,new SimulationTick(99),id,out lateral);
            Assert.That(rebuilt.Ski.Effort.Tick.Value,Is.EqualTo(99));Assert.That(rebuilt.Aim.Tick.Value,Is.EqualTo(99));
            Assert.That(rebuilt.Ski.Effort.AthleteId,Is.EqualTo(id));Assert.That(rebuilt.Aim.AthleteId,Is.EqualTo(id));
            Assert.That(rebuilt.Ski.Effort.RequestedEffort,Is.EqualTo(833));Assert.That(rebuilt.Aim.Horizontal,Is.EqualTo(123));Assert.That(lateral,Is.EqualTo(-2.25));
            using(var bytes=new MemoryStream())
            {using(var writer=new BinaryWriter(bytes,Encoding.UTF8,true))ProductionReplayHarness.WriteCommand(writer,rebuilt,lateral);Assert.That(bytes.ToArray(),Is.EqualTo(expected));}
        }
    }
    [Test] public void ActualFactoryPrefixIsPersistedDeterministicallyAndReplaysWithoutAi()
    {
        Assert.That(Record("one"),Is.Zero);Assert.That(Record("two"),Is.Zero);
        Assert.That(File.ReadAllBytes(Tape("one")),Is.EqualTo(File.ReadAllBytes(Tape("two"))));
        Assert.That(File.ReadAllBytes(Tape("one")+ProductionReplayHarness.ContextSuffix),Is.EqualTo(File.ReadAllBytes(Tape("two")+ProductionReplayHarness.ContextSuffix)));
        string report=Path.Combine(output,"replay.json");Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),report}),Is.Zero);
        using(var value=Report(report))
        {Assert.That(value.RootElement.GetProperty("status").GetString(),Is.EqualTo("PASS_PREFIX_EXACT_REPLAY"));
         Assert.That(value.RootElement.GetProperty("ticks").GetInt32(),Is.EqualTo(12));Assert.That(value.RootElement.GetProperty("complete_race").GetBoolean(),Is.False);
         Assert.That(value.RootElement.GetProperty("replay_ai_steps").GetInt32(),Is.Zero);}
    }
    [Test] public void CorruptedAcceptedCommandFailsAtItsFirstStateMismatch()
    {
        Assert.That(Record(),Is.Zero);byte[] raw;
        using(var file=File.OpenRead(Tape()))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var bytes=new MemoryStream())
        {gzip.CopyTo(bytes);raw=bytes.ToArray();}
        using(var bytes=new MemoryStream(raw))using(var reader=new BinaryReader(bytes,Encoding.UTF8,true))
        {
            reader.ReadString();reader.ReadInt32();reader.ReadString();reader.ReadInt32();
            int first=reader.ReadInt32();bytes.Position+=first+32;int second=reader.ReadInt32();long recordOffset=bytes.Position;
            reader.ReadInt64();reader.ReadInt32();
            // Effort remains valid command input but changes the accepted physiology.
            long effortOffset=bytes.Position;int effort=reader.ReadInt32();bytes.Position=effortOffset;
            using(var writer=new BinaryWriter(bytes,Encoding.UTF8,true))writer.Write(effort==0?1:0);
            // Recompute the integrity checksum to prove actual state comparison,
            // rather than only the earlier byte-integrity rejection.
            byte[] checksum=System.Security.Cryptography.SHA256.HashData(raw.AsSpan((int)recordOffset,second));
            bytes.Position=recordOffset+second;bytes.Write(checksum);
        }
        using(var file=File.Create(Tape()))using(var gzip=new GZipStream(file,CompressionLevel.Fastest))gzip.Write(raw);
        string report=Path.Combine(output,"corrupt.json");Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),report}),Is.EqualTo(1));
        using(var value=Report(report))Assert.That(value.RootElement.GetProperty("error").GetString(),Does.Contain("Public state differs at tick 2"));
    }
    [Test] public void AlteredPersistedContextOrOversizedRecordCannotClaimReplayPass()
    {
        Assert.That(Record(),Is.Zero);string context=Tape()+ProductionReplayHarness.ContextSuffix;
        File.AppendAllText(context," ");string report=Path.Combine(output,"context.json");
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),report}),Is.EqualTo(1));
        using(var value=Report(report))Assert.That(value.RootElement.GetProperty("error").GetString(),Does.Contain("context hash"));
        Assert.That(Record(),Is.Zero);byte[] raw;
        using(var file=File.OpenRead(Tape()))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var bytes=new MemoryStream())
        {gzip.CopyTo(bytes);raw=bytes.ToArray();}
        using(var bytes=new MemoryStream(raw))using(var reader=new BinaryReader(bytes,Encoding.UTF8,true))
        {
            reader.ReadString();reader.ReadInt32();reader.ReadString();reader.ReadInt32();
            using(var writer=new BinaryWriter(bytes,Encoding.UTF8,true))writer.Write(int.MaxValue);
        }
        using(var file=File.Create(Tape()))using(var gzip=new GZipStream(file,CompressionLevel.Fastest))gzip.Write(raw);
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),report}),Is.EqualTo(1));
        using(var value=Report(report))Assert.That(value.RootElement.GetProperty("error").GetString(),Does.Contain("Record length/tick capacity"));
    }
    [Test] public void ZeroTickAndInvalidCapacityRemainExplicitBoundedPrefixes()
    {
        Assert.That(Record(ticks:0),Is.Zero);string report=Path.Combine(output,"zero.json");
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),report}),Is.Zero);
        using(var value=Report(report)){Assert.That(value.RootElement.GetProperty("ticks").GetInt32(),Is.Zero);Assert.That(value.RootElement.GetProperty("complete_race").GetBoolean(),Is.False);}
        Assert.That(Record(ticks:120001),Is.EqualTo(1));
    }
    [Test] public void CompressedContextExpansionIsBoundedBeforeFactoryReconstruction()
    {
        Assert.That(Record(ticks:0),Is.Zero);string context=Tape()+ProductionReplayHarness.ContextSuffix;
        using(var file=File.Create(context))using(var gzip=new GZipStream(file,CompressionLevel.Fastest))
        {byte[] spaces=Enumerable.Repeat((byte)' ',4096).ToArray();for(int i=0;i<=32768;i++)gzip.Write(spaces);}
        string hash;
        using(var file=File.OpenRead(context))hash=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(file)).ToLowerInvariant();
        byte[] raw;
        using(var file=File.OpenRead(Tape()))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var bytes=new MemoryStream())
        {gzip.CopyTo(bytes);raw=bytes.ToArray();}
        using(var bytes=new MemoryStream(raw))using(var reader=new BinaryReader(bytes,Encoding.UTF8,true))
        {reader.ReadString();reader.ReadInt32();using(var writer=new BinaryWriter(bytes,Encoding.UTF8,true))writer.Write(hash);}
        using(var file=File.Create(Tape()))using(var gzip=new GZipStream(file,CompressionLevel.Fastest))gzip.Write(raw);
        string report=Path.Combine(output,"expansion.json");Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),report}),Is.EqualTo(1));
        using(var value=Report(report))Assert.That(value.RootElement.GetProperty("error").GetString(),Does.Contain("Uncompressed context exceeds 128 MiB"));
    }
    [Test] public void ReplayReportCannotOverwriteItsOwnTapeOrContext()
    {
        Assert.That(Record(ticks:0),Is.Zero);byte[] tape=File.ReadAllBytes(Tape()),context=File.ReadAllBytes(Tape()+ProductionReplayHarness.ContextSuffix);
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),Tape()}),Is.EqualTo(1));
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(),Tape()+ProductionReplayHarness.ContextSuffix}),Is.EqualTo(1));
        Assert.That(File.ReadAllBytes(Tape()),Is.EqualTo(tape));Assert.That(File.ReadAllBytes(Tape()+ProductionReplayHarness.ContextSuffix),Is.EqualTo(context));
    }
    [Test] public void TinyActualFactoryCompletesShootingAndReplaysTheExactFinalResult()
    {
        string root=TinyVenueFixture.Write(Project,output),record=Path.Combine(output,"complete.record.json"),replay=Path.Combine(output,"complete.replay.json");
        Assert.That(ProductionReplayHarness.Run(new[]{"record",root,Path.Combine(Project,"Assets/Resources"),"manifest","profile","route","conditions","1","12000",Tape("complete"),record}),Is.Zero);
        using(var value=Report(record))Assert.That(value.RootElement.GetProperty("status").GetString(),Is.EqualTo("RACE_COMPLETE_REPLAY_NOT_YET_PROVED"));
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape("complete"),replay}),Is.Zero);
        using(var value=Report(replay))using(var source=Report(record))
        {
            Assert.That(value.RootElement.GetProperty("status").GetString(),Is.EqualTo("PASS_COMPLETE_EXACT_REPLAY"));
            Assert.That(value.RootElement.GetProperty("result_json").GetString(),Is.EqualTo(source.RootElement.GetProperty("result_json").GetString()));
            Assert.That(value.RootElement.GetProperty("field_event_count").GetInt32(),Is.GreaterThan(20));
        }
        string chase=Path.Combine(output,"pursuit.record.json"),chaseReplay=Path.Combine(output,"pursuit.replay.json");
        Assert.That(ProductionReplayHarness.Run(new[]{"record",root,Path.Combine(Project,"Assets/Resources"),"manifest","pursuit_profile","pursuit_route","conditions","1","12000",Tape("pursuit"),chase,"--sprint-result",record}),Is.Zero);
        using(var value=Report(chase))Assert.That(value.RootElement.GetProperty("status").GetString(),Is.EqualTo("RACE_COMPLETE_REPLAY_NOT_YET_PROVED"));
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape("pursuit"),chaseReplay}),Is.Zero);
        using(var value=Report(chaseReplay))using(var source=Report(chase))
        {Assert.That(value.RootElement.GetProperty("status").GetString(),Is.EqualTo("PASS_COMPLETE_EXACT_REPLAY"));
         Assert.That(value.RootElement.GetProperty("result_json").GetString(),Is.EqualTo(source.RootElement.GetProperty("result_json").GetString()));}
    }
    [Test] public void ExplicitFileCapacityStopsAtAReplayablePrefixWithoutACompletionClaim()
    {
        var args=new[]{"record",Path.Combine(Project,"Assets/Resources"),Path.Combine(Project,"Assets/Resources"),
            "Biathlon/Venues/woodland_basin_v1/manifest","Biathlon/Venues/woodland_basin_v1/sprint_profile","Biathlon/Venues/woodland_basin_v1/sprint_route",
            "Biathlon/race_snow_neutral_v1","2","120000",Tape("capacity"),Path.Combine(output,"capacity.record.json"),"--max-tape-mib","16"};
        Assert.That(ProductionReplayHarness.Run(args),Is.Zero);
        using(var value=Report(Path.Combine(output,"capacity.record.json")))
        {Assert.That(value.RootElement.GetProperty("status").GetString(),Is.EqualTo("PREFIX_ONLY_REPLAY_NOT_YET_PROVED"));
         Assert.That(value.RootElement.GetProperty("stopped_reason").GetString(),Is.EqualTo("FILE_CAPACITY_PREFIX"));
         Assert.That(value.RootElement.GetProperty("compressed_bytes").GetInt64(),Is.LessThanOrEqualTo(16*1024*1024));}
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape("capacity"),Path.Combine(output,"capacity.replay.json")}),Is.Zero);
    }
}

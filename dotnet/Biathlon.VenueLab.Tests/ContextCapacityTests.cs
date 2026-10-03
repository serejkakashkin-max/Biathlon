using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

public sealed class ContextCapacityTests
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
    [SetUp] public void Setup()=>output=Path.Combine(Path.GetTempPath(),"biathlon-context-capacity-"+Guid.NewGuid().ToString("N"));
    [TearDown] public void Teardown(){if(Directory.Exists(output))Directory.Delete(output,true);}
    private string Tape(string name)=>Path.Combine(output,name+".gz");
    private string Report(string name)=>Path.Combine(output,name+".json");
    private string[] RecordArgs(string name,string root)=>new[]{"record",root,Path.Combine(Project,"Assets/Resources"),
        "manifest","profile","route","conditions","1","0",Tape(name),Report(name)};
    private int RecordTiny(string name,params string[] options)
    {string root=TinyVenueFixture.Write(Project,output);return ProductionReplayHarness.Run(RecordArgs(name,root).Concat(options).ToArray());}

    [Test] public void DefaultAndBothExplicitBoundariesPreserveContextAndTapeBytes()
    {
        Assert.That(RecordTiny("default"),Is.Zero);
        byte[] context=File.ReadAllBytes(Tape("default")+ProductionReplayHarness.ContextSuffix),tape=File.ReadAllBytes(Tape("default"));
        foreach(string value in new[]{"128","512"})
        {
            string name="cap"+value;
            Assert.That(RecordTiny(name,"--max-context-mib",value),Is.Zero);
            Assert.That(File.ReadAllBytes(Tape(name)+ProductionReplayHarness.ContextSuffix),Is.EqualTo(context));
            Assert.That(File.ReadAllBytes(Tape(name)),Is.EqualTo(tape));
            using(var report=JsonDocument.Parse(File.ReadAllText(Report(name))))
                Assert.That(report.RootElement.GetProperty("context_uncompressed_capacity_bytes").GetInt64(),Is.EqualTo(long.Parse(value)*1048576));
            Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape(name),Report(name+"-replay"),"--max-context-mib",value}),Is.Zero);
            using(var report=JsonDocument.Parse(File.ReadAllText(Report(name+"-replay"))))
            {
                Assert.That(report.RootElement.GetProperty("status").GetString(),Is.EqualTo("PASS_PREFIX_EXACT_REPLAY"));
                Assert.That(report.RootElement.GetProperty("context_uncompressed_capacity_bytes").GetInt64(),Is.EqualTo(long.Parse(value)*1048576));
            }
        }
        using(var report=JsonDocument.Parse(File.ReadAllText(Report("default"))))
            Assert.That(report.RootElement.GetProperty("context_uncompressed_capacity_bytes").GetInt64(),Is.EqualTo(128L*1048576));
    }

    [TestCase("127")][TestCase("513")][TestCase("0")][TestCase("-1")]
    [TestCase("2147483648")][TestCase("abc")][TestCase("1.5")][TestCase("")]
    public void InvalidContextCapacityFailsBeforeAnyContextOrTapeIsCreated(string value)
    {
        Assert.That(ProductionReplayHarness.Run(RecordArgs("bad","missing-root").Concat(new[]{"--max-context-mib",value}).ToArray()),Is.EqualTo(1));
        Assert.That(File.Exists(Tape("bad")),Is.False);Assert.That(File.Exists(Tape("bad")+ProductionReplayHarness.ContextSuffix),Is.False);
        using(var report=JsonDocument.Parse(File.ReadAllText(Report("bad"))))Assert.That(report.RootElement.GetProperty("status").GetString(),Is.EqualTo("FAILED"));
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape("missing"),Report("bad-reader"),"--max-context-mib",value}),Is.EqualTo(1));
        using(var report=JsonDocument.Parse(File.ReadAllText(Report("bad-reader"))))
            Assert.That(report.RootElement.GetProperty("error").GetString(),Does.Not.Contain("FileNotFoundException"));
    }

    [Test] public void MissingDuplicateOrUnknownReaderOptionsFailBeforeOpeningInput()
    {
        foreach(var extra in new[]{new[]{"--max-context-mib"},new[]{"--max-context-mib","256","--max-context-mib","512"},new[]{"--max-raw-mib","256"}})
        {
            Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape("missing"),Report("invalid-reader")}.Concat(extra).ToArray()),Is.EqualTo(1));
            using(var report=JsonDocument.Parse(File.ReadAllText(Report("invalid-reader"))))
                Assert.That(report.RootElement.GetProperty("error").GetString(),Does.Not.Contain("FileNotFoundException"));
        }
        Assert.That(ProductionReplayHarness.Run(RecordArgs("duplicate","missing-root").Concat(new[]{"--max-context-mib","256","--max-context-mib","512"}).ToArray()),Is.EqualTo(1));
        Assert.That(File.Exists(Tape("duplicate")+ProductionReplayHarness.ContextSuffix),Is.False);
    }

    [Test] public void ReaderExplicitlyOptsIntoLargerExpansionWithoutChangingAnyContextValue()
    {
        Assert.That(RecordTiny("large"),Is.Zero);
        string contextPath=Tape("large")+ProductionReplayHarness.ContextSuffix;
        byte[] original;
        using(var file=File.OpenRead(contextPath))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var buffer=new MemoryStream())
        {gzip.CopyTo(buffer);original=buffer.ToArray();}
        const long limit=128L*1048576;
        using(var file=File.Create(contextPath))using(var gzip=new GZipStream(file,CompressionLevel.Fastest))
        {
            gzip.Write(original);byte[] whitespace=Enumerable.Repeat((byte)' ',65536).ToArray();
            long left=limit+1-original.Length;
            while(left>0){int count=(int)Math.Min(left,whitespace.Length);gzip.Write(whitespace,0,count);left-=count;}
        }
        string hash;
        using(var file=File.OpenRead(contextPath))hash=Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
        byte[] raw;
        using(var file=File.OpenRead(Tape("large")))using(var gzip=new GZipStream(file,CompressionMode.Decompress))using(var bytes=new MemoryStream())
        {gzip.CopyTo(bytes);raw=bytes.ToArray();}
        using(var bytes=new MemoryStream(raw))using(var reader=new BinaryReader(bytes,Encoding.UTF8,true))
        {reader.ReadString();reader.ReadInt32();using(var writer=new BinaryWriter(bytes,Encoding.UTF8,true))writer.Write(hash);}
        using(var file=File.Create(Tape("large")))using(var gzip=new GZipStream(file,CompressionLevel.Fastest))gzip.Write(raw);
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape("large"),Report("default-reader")}),Is.EqualTo(1));
        using(var report=JsonDocument.Parse(File.ReadAllText(Report("default-reader"))))
            Assert.That(report.RootElement.GetProperty("error").GetString(),Does.Contain("Uncompressed context exceeds 128 MiB"));
        Assert.That(ProductionReplayHarness.Run(new[]{"replay",Tape("large"),Report("larger-reader"),"--max-context-mib","256"}),Is.Zero);
        using(var report=JsonDocument.Parse(File.ReadAllText(Report("larger-reader"))))
        {
            Assert.That(report.RootElement.GetProperty("status").GetString(),Is.EqualTo("PASS_PREFIX_EXACT_REPLAY"));
            Assert.That(report.RootElement.GetProperty("context_uncompressed_bytes").GetInt64(),Is.EqualTo(limit+1));
            Assert.That(report.RootElement.GetProperty("context_uncompressed_capacity_bytes").GetInt64(),Is.EqualTo(256L*1048576));
            Assert.That(report.RootElement.GetProperty("replay_ai_steps").GetInt64(),Is.Zero);
        }
    }
}

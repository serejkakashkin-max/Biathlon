using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using NUnit.Framework;

public sealed class FieldContextEncodingTests
{
    public sealed class Envelope
    {
        [JsonConverter(typeof(FieldContextJsonConverter))]
        public string FieldContext { get; set; }
    }
    [Test] public void LegacyContextRetainsByteExactEncodingAndReadsOldString()
    {
        string value="{\"geometry\":\"snow \\ slope —\"}";
        Assert.That(JsonSerializer.Serialize(new Envelope{FieldContext=value}),Is.EqualTo("{\"FieldContext\":"+JsonSerializer.Serialize(value)+"}"));
        Assert.That(JsonSerializer.Deserialize<Envelope>("{\"FieldContext\":"+JsonSerializer.Serialize(value)+"}").FieldContext,Is.EqualTo(value));
    }
    [Test] public void ContextAboveActualDotnetSingleStringLimitRoundTripsAllBytesThroughStream()
    {
        // The captured Mass30 failure was 176,996,464 characters, above .NET's
        // 166,666,666-character token limit. Test that real failure boundary.
        string value=new string('g',176996464);
        using(var buffer=new MemoryStream())
        {
            using(var gzip=new GZipStream(buffer,CompressionLevel.Fastest,true))
            using(var writer=new Utf8JsonWriter(gzip))JsonSerializer.Serialize(writer,new Envelope{FieldContext=value});
            buffer.Position=0;
            using(var gzip=new GZipStream(buffer,CompressionMode.Decompress,true))
                Assert.That(JsonSerializer.Deserialize<Envelope>(gzip).FieldContext,Is.EqualTo(value));
        }
    }
    [Test] public void ChunkBoundaryDoesNotSplitUnicodeSurrogatePair()
    {
        string value=new string('a',FieldContextJsonConverter.ChunkCharacters-1)+"\U0001F3BF"+new string('z',FieldContextJsonConverter.LegacyMaximumCharacters);
        using(var buffer=new MemoryStream())
        {
            using(var gzip=new GZipStream(buffer,CompressionLevel.Fastest,true))
            using(var writer=new Utf8JsonWriter(gzip))JsonSerializer.Serialize(writer,new Envelope{FieldContext=value});
            buffer.Position=0;
            using(var gzip=new GZipStream(buffer,CompressionMode.Decompress,true))
                Assert.That(JsonSerializer.Deserialize<Envelope>(gzip).FieldContext,Is.EqualTo(value));
        }
    }
    [TestCase("[]")][TestCase("[null]")][TestCase("[1]")][TestCase("[\"\"]")][TestCase("{}")]
    public void InvalidChunkedContextRejectsInsteadOfDroppingData(string json)
    {Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<Envelope>("{\"FieldContext\":"+json+"}"));}
}

using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>CLI evidence encoding only. Preserve small legacy strings; split very
/// large exact context strings into bounded JSON tokens. The outer context stream
/// still enforces the caller's unchanged 128..512 MiB expansion limit.</summary>
public sealed class FieldContextJsonConverter : JsonConverter<string>
{
    public const int LegacyMaximumCharacters = 120 * 1024 * 1024;
    public const int ChunkCharacters = 4 * 1024 * 1024;
    private const int MaximumCharacters = 512 * 1024 * 1024;
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value.Length <= LegacyMaximumCharacters) { writer.WriteStringValue(value); return; }
        if (value.Length > MaximumCharacters) throw new JsonException("Field context exceeds the bounded context capacity.");
        writer.WriteStartArray();
        for (int start = 0; start < value.Length;)
        {
            int count = Math.Min(ChunkCharacters, value.Length - start);
            if (start + count < value.Length && char.IsHighSurrogate(value[start + count - 1])) count--;
            writer.WriteStringValue(value.AsSpan(start, count));
            writer.Flush();
            start += count;
        }
        writer.WriteEndArray();
    }
    public override string Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String) return reader.GetString();
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Exact field context string or chunk array required.");
        var text = new StringBuilder(); int chunks = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType != JsonTokenType.String) throw new JsonException("Field context chunks must be strings.");
            string chunk = reader.GetString();
            if (chunk.Length == 0 || chunk.Length > ChunkCharacters || (long)text.Length + chunk.Length > MaximumCharacters)
                throw new JsonException("Field context chunk capacity exceeded.");
            text.Append(chunk); chunks++;
        }
        if (reader.TokenType != JsonTokenType.EndArray || chunks == 0) throw new JsonException("Complete nonempty context chunk array required.");
        return text.ToString();
    }
}

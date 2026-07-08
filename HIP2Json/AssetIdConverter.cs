using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

public static class AssetIDUtil
{
    public static uint Parse(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            value = value[2..];

        return Convert.ToUInt32(value, 16);
    }

    public static string Format(uint value)
        => $"0x{value:X8}";
}

public sealed class AssetIDConverter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => AssetIDUtil.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.GetUInt32(),
            _ => throw new JsonException()
        };
    }

    public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(AssetIDUtil.Format(value));
    }
}

public sealed class AssetIDArrayConverter : JsonConverter<uint[]>
{
    public override uint[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        var list = new List<uint>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return list.ToArray();

            list.Add(reader.TokenType switch
            {
                JsonTokenType.String => AssetIDUtil.Parse(reader.GetString()!),
                JsonTokenType.Number => reader.GetUInt32(),
                _ => throw new JsonException()
            });
        }

        throw new JsonException("Unexpected end of JSON array");
    }

    public override void Write(Utf8JsonWriter writer, uint[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        foreach (var v in value)
            writer.WriteStringValue(AssetIDUtil.Format(v));

        writer.WriteEndArray();
    }
}
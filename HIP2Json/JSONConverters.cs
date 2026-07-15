using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;

namespace HIP2Json;

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

public sealed class ButtonHitmaskConverter : JsonConverter<ButtonHitmask>
{
    public override ButtonHitmask Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return (ButtonHitmask)reader.GetUInt32();

        if (reader.TokenType == JsonTokenType.String)
        {
            string value = reader.GetString()!;

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return (ButtonHitmask)Convert.ToUInt32(value[2..], 16);

            return Enum.Parse<ButtonHitmask>(value, true);
        }

        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        ButtonHitmask flags = ButtonHitmask.None;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
                return flags;

            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException();

            string value = reader.GetString()!;

            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                flags |= (ButtonHitmask)Convert.ToUInt32(value[2..], 16);
            else
                flags |= Enum.Parse<ButtonHitmask>(value, true);
        }

        throw new JsonException("Unexpected end of JSON array");
    }

    public override void Write(Utf8JsonWriter writer, ButtonHitmask value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        uint remaining = (uint)value;

        foreach (ButtonHitmask flag in Enum.GetValues<ButtonHitmask>())
        {
            if (flag == ButtonHitmask.None)
                continue;

            uint flagValue = (uint)flag;

            if ((remaining & flagValue) != 0)
            {
                writer.WriteStringValue(flag.ToString());
                remaining &= ~flagValue;
            }
        }

        if (remaining != 0)
            writer.WriteStringValue($"0x{remaining:X8}");

        writer.WriteEndArray();
    }
}
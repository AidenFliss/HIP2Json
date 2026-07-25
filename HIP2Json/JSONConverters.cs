using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HIP2Json;

public static class AssetIDUtil
{
    public static uint Parse(string value)
    {
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            value = value[2..];

        return Convert.ToUInt32(value, 16);
    }

    public static string Format(uint value) => $"0x{value:X8}";
}

public sealed class AssetIDConverter : JsonConverter<uint>
{
    public override uint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => AssetIDUtil.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.GetUInt32(),
            _ => throw new JsonException(),
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

            list.Add(
                reader.TokenType switch
                {
                    JsonTokenType.String => AssetIDUtil.Parse(reader.GetString()!),
                    JsonTokenType.Number => reader.GetUInt32(),
                    _ => throw new JsonException(),
                }
            );
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

public class xMotionConverter : JsonConverter<xMotion>
{
    public override xMotion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var motion = new xMotion();

        if (root.TryGetProperty("type", out var typeProp))
            motion.type = JsonSerializer.Deserialize<MotionType>(typeProp.GetRawText(), options);

        if (root.TryGetProperty("useBanking", out var bankingProp))
            motion.useBanking = bankingProp.GetByte();

        if (root.TryGetProperty("flags", out var flagsProp))
            motion.flags = flagsProp.GetUInt16();

        if (root.TryGetProperty("specific", out var specificProp) && specificProp.ValueKind == JsonValueKind.Object)
        {
            Type targetType = motion.type switch
            {
                MotionType.ExtendRetract => typeof(ExtendRetractMotion),
                MotionType.Orbit => typeof(OrbitMotion),
                MotionType.Spline => typeof(SplineMotion),
                MotionType.MovePoint => typeof(MovePointMotion),
                MotionType.Mechanism => typeof(MechanismMotion),
                MotionType.Pendulum => typeof(PendulumMotion),
                _ => null,
            };

            if (targetType != null)
            {
                motion.specific = (MotionSpecificData)JsonSerializer.Deserialize(specificProp.GetRawText(), targetType, options);
            }
        }

        return motion;
    }

    public override void Write(Utf8JsonWriter writer, xMotion value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("type");
        JsonSerializer.Serialize(writer, value.type, options);

        writer.WriteNumber("useBanking", value.useBanking);
        writer.WriteNumber("flags", value.flags);

        writer.WritePropertyName("specific");
        JsonSerializer.Serialize(writer, (object)value.specific, options);

        writer.WriteEndObject();
    }
}

public class GameEnumConverter<TBFBB, TTSSM> : JsonConverter<object>
    where TBFBB : struct, Enum
    where TTSSM : struct, Enum
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string enumString = reader.GetString();

        if (Program.CurrentGame == GameType.BFBB)
        {
            if (Enum.TryParse<TBFBB>(enumString, out var bfbbVal))
                return bfbbVal;
        }
        else if (Program.CurrentGame == GameType.TSSM)
        {
            if (Enum.TryParse<TTSSM>(enumString, out var tssmVal))
                return tssmVal;
        }

        return 0;
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToString());
    }
}

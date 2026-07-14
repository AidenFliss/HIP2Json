using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

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
                _ => null
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
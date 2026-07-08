using System.IO;

namespace PortHeavyIronGameRewrite;

public sealed class game_object_Camera_TweakParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new game_object_Camera_Tweak
        {
            priority = ReadInt32BE(br),
            time = ReadFloatBE(br),
            pitch_adjust = ReadFloatBE(br),
            dist_adjust = ReadFloatBE(br)
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        game_object_Camera_Tweak cameraTweak = (game_object_Camera_Tweak)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, cameraTweak.priority);
        WriteFloatBE(bw, cameraTweak.time);
        WriteFloatBE(bw, cameraTweak.pitch_adjust);
        WriteFloatBE(bw, cameraTweak.dist_adjust);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "CameraTweak"; }
}

public class game_object_Camera_Tweak
{
    public int priority { get; set; }
    public float time { get; set; }
    public float pitch_adjust { get; set; }
    public float dist_adjust { get; set; }
}
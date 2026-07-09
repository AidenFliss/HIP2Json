using System.IO;

namespace PortHeavyIronGameRewrite;

public sealed class effect_ScreenFadeParser : AbstractDYNAParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart, short version)
    {
        return new effect_ScreenFade
        {
            dest = ReadColor(br),
            fadeDownTime = ReadFloatBE(br),
            waitTime = ReadFloatBE(br),
            fadeUpTime = ReadFloatBE(br),
        };
    }

    public override byte[] Serialize(object obj, short version)
    {
        effect_ScreenFade screenFade = (effect_ScreenFade)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteColorBE(bw, screenFade.dest);
        WriteFloatBE(bw, screenFade.fadeDownTime);
        WriteFloatBE(bw, screenFade.waitTime);
        WriteFloatBE(bw, screenFade.fadeUpTime);

        return ms.ToArray();
    }

    public override string GetFolderName() { return "ScreenFade"; }
}

public class effect_ScreenFade
{
    public xColor dest { get; set; }
    public float fadeDownTime { get; set; }
    public float waitTime { get; set; }
    public float fadeUpTime { get; set; }
}
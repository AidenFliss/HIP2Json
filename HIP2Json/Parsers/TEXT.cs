using System.IO;
using System.Linq;
using System.Text;

namespace HIP2Json;

public sealed class TEXTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        int charCount = ReadInt32BE(br); //not gonna worry abt padding to 0x04 uness i implement writing to files...
        return new TEXT
        {
            charCount = charCount,
            text = Encoding.UTF8.GetString(
                Enumerable.Range(0, charCount)
                    .Select(_ => ReadByte(br))
                    .ToArray())
        };
    }

    public override object Serialize(object obj)
    {
        TEXT text = (TEXT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteInt32BE(bw, text.charCount);
        WriteString(bw, text.text);
        while (ms.Length % 4 != 0) //padded to 0x04, bc i AM worrying abt writing to files now....
        {
            WriteByte(bw, 0);
        }
        
        return ms.ToArray();
    }
}

public class TEXT
{
    public int charCount { get; set; }
    public string text { get; set; }
}
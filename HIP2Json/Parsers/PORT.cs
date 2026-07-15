using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PORTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new PORT
        {
            assetCameraID = ReadUInt32BE(br),
            assetMarkerID = ReadUInt32BE(br),
            ang = ReadFloatBE(br),
            sceneID = UInt32ToASCII(ReadUInt32BE(br)),
        };
    }

    public override object Serialize(object obj)
    {
        PORT port = (PORT)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, port.assetCameraID);
        WriteUInt32BE(bw, port.assetMarkerID);
        WriteFloatBE(bw, port.ang);
        WriteUInt32BE(bw, Convert.ToUInt32(port.sceneID));

        return ms.ToArray();
    }
}

public class PORT
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint assetCameraID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint assetMarkerID { get; set; }
    public float ang { get; set; }
    public string sceneID { get; set; }
}
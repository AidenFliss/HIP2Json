using System;
using System.IO;

namespace HIP2Json;

public sealed class SNDParser : SoundAssetParser
{
}

public sealed class SNDSParser : SoundAssetParser
{
}

public abstract class SoundAssetParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte[] data = br.ReadBytes((int)(br.BaseStream.Length - br.BaseStream.Position));

        bool isVag = Program.CurrentPlatform == GamePlatform.PS2;

        string codec = isVag ? "VAG" : "DSPADPCM";
        int sampleRate = 22050;
        string wavBase64 = null;

        if (data.Length > 0)
        {
            short[] samples = isVag ? SoundCodec.DecodeVAG(data) : SoundCodec.Decode(data);
            if (samples.Length > 0)
                wavBase64 = Convert.ToBase64String(WavCodec.Encode(samples, sampleRate));
        }

        return new SND
        {
            codec = codec,
            sampleRate = sampleRate,
            channels = 1,
            wavBase64 = wavBase64,
            dataBase64 = Convert.ToBase64String(data),
        };
    }

    public override object Serialize(object obj)
    {
        SoundAssetBase snd = (SoundAssetBase)obj;

        bool isVag = Program.CurrentPlatform == GamePlatform.PS2;

        byte[] original = string.IsNullOrEmpty(snd.dataBase64) ? Array.Empty<byte>() : Convert.FromBase64String(snd.dataBase64);

        if (string.IsNullOrEmpty(snd.wavBase64))
        {
            if (string.IsNullOrEmpty(snd.dataBase64))
                return Array.Empty<byte>();
            return original;
        }

        short[] wavSamples = WavCodec.Decode(Convert.FromBase64String(snd.wavBase64), out int wavRate);

        if (original.Length > 0)
        {
            short[] origSamples = isVag ? SoundCodec.DecodeVAG(original) : SoundCodec.Decode(original);
            if (SoundCodec.EqualSamples(origSamples, wavSamples))
                return original;
        }

        return isVag ? SoundCodec.EncodeVAG(wavSamples) : SoundCodec.Encode(wavSamples);
    }
}

public class SND : SoundAssetBase
{
}

public class SNDS : SoundAssetBase
{
}

public abstract class SoundAssetBase
{
    public string codec { get; set; }
    public int sampleRate { get; set; }
    public int channels { get; set; }
    public string wavBase64 { get; set; }
    public string dataBase64 { get; set; }
}
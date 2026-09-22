using System;
using System.IO;

namespace HIP2Json;

public static class SoundCodec
{
    private static readonly short[,] Coeffs = BuildCoeffs();

    private static readonly short[,] VagCoeffs =
    {
        { 0, 0 },
        { 60, 0 },
        { 115, -52 },
        { 98, -55 },
        { 122, -60 },
    };

    private static short[,] BuildCoeffs()
    {
        int[] gc =
        {
            0, 0,
            -32767, 0,
            -28160, -8192,
            -31232, -8192,
            -28153, -10380,
            -26508, -14336,
            -25008, -16896,
            -24062, -19648,
            -23051, -22272,
            -22132, -24576,
            -21127, -26624,
            -20465, -28672,
            -19618, -30720,
            -18704, -32768,
            -17690, -32768,
            -16695, -32768,
        };

        short[,] t = new short[16, 2];
        for (int i = 0; i < 16; i++)
        {
            t[i, 0] = (short)(gc[i * 2] >> 4);
            t[i, 1] = (short)(gc[i * 2 + 1] >> 4);
        }

        return t;
    }

    public static short[] Decode(byte[] data)
    {
        if (data.Length == 0)
            return Array.Empty<short>();

        int frames = data.Length / 8;
        short[] samples = new short[frames * 14];

        int h1 = 0;
        int h2 = 0;
        int outIndex = 0;

        for (int f = 0; f < frames; f++)
        {
            byte hdr = data[f * 8];
            int scale = 1 << (hdr & 0x0F);
            int idx = (hdr >> 4) & 0x0F;
            short c1 = Coeffs[idx, 0];
            short c2 = Coeffs[idx, 1];

            for (int b = 1; b < 8; b++)
            {
                byte val = data[f * 8 + b];
                int n1 = val >> 4;
                int n2 = val & 0x0F;
                samples[outIndex++] = NextSample(n1, scale, c1, c2, ref h1, ref h2);
                samples[outIndex++] = NextSample(n2, scale, c1, c2, ref h1, ref h2);
            }
        }

        return samples;
    }

    public static short[] DecodeVAG(byte[] data)
    {
        if (data.Length <= 16)
            return Array.Empty<short>();

        int body = data.Length - 16;
        int frames = body / 16;
        short[] samples = new short[frames * 28];

        int h1 = 0;
        int h2 = 0;
        int outIndex = 0;

        for (int f = 0; f < frames; f++)
        {
            int frameOff = 16 + f * 16;
            byte hdr = data[frameOff];
            int shift = hdr & 0x0F;
            int idx = (hdr >> 4) & 0x0F;
            if (idx >= 5)
                return Array.Empty<short>();

            short c1 = VagCoeffs[idx, 0];
            short c2 = VagCoeffs[idx, 1];

            byte flag = data[frameOff + 1];

            for (int n = 0; n < 28; n++)
            {
                byte b = (n % 2 == 0)
                    ? data[frameOff + 2 + n / 2]
                    : data[frameOff + 2 + (n - 1) / 2];

                int nib = (n % 2 == 0) ? b & 0x0F : (b >> 4) & 0x0F;
                int sn = nib >= 8 ? nib - 16 : nib;

                int s;
                if ((flag & 0x07) < 0x07)
                {
                    s = (sn << 12 >> shift) + (h1 * c1 + h2 * c2) / 64;
                    if (s > 32767)
                        s = 32767;
                    if (s < -32768)
                        s = -32768;
                }
                else
                {
                    s = 0;
                }

                samples[outIndex++] = (short)s;
                h2 = h1;
                h1 = s;
            }
        }

        return samples;
    }

    public static byte[] EncodeVAG(short[] samples)
    {
        int count = samples.Length;
        int padded = (count + 27) / 28 * 28;
        int frames = padded / 28;
        byte[] data = new byte[16 + frames * 16];

        int h1 = 0;
        int h2 = 0;

        for (int f = 0; f < frames; f++)
        {
            int start = f * 28;
            short[] seg = new short[28];
            for (int i = 0; i < 28; i++)
                seg[i] = start + i < count ? samples[start + i] : (short)0;

            int bestP = 0;
            int bestShift = 0;
            long bestErr = long.MaxValue;
            byte[] bestNibs = new byte[28];

            for (int p = 0; p < 5; p++)
            {
                short c1 = VagCoeffs[p, 0];
                short c2 = VagCoeffs[p, 1];
                int carry1 = h1;
                int carry2 = h2;

                for (int sh = 0; sh <= 12; sh++)
                {
                    int ch1 = carry1;
                    int ch2 = carry2;
                    byte[] nibs = new byte[28];

                    for (int i = 0; i < 28; i++)
                    {
                        int predict = (c1 * ch1 + c2 * ch2) / 64;
                        int target = seg[i] - predict;
                        int step;
                        int shiftAmt = 12 - sh;
                        if (target >= 0)
                            step = (target + (1 << (shiftAmt - 1))) >> shiftAmt;
                        else
                            step = (target - (1 << (shiftAmt - 1))) >> shiftAmt;
                        if (step > 7)
                            step = 7;
                        if (step < -8)
                            step = -8;
                        nibs[i] = (byte)(step & 0x0F);
                        int sn = step >= 8 ? step - 16 : step;
                        int s = (sn << 12 >> sh) + predict;
                        if (s > 32767)
                            s = 32767;
                        if (s < -32768)
                            s = -32768;
                        ch2 = ch1;
                        ch1 = s;
                    }

                    long err = 0;
                    ch1 = carry1;
                    ch2 = carry2;
                    for (int i = 0; i < 28; i++)
                    {
                        int sn = nibs[i] >= 8 ? nibs[i] - 16 : nibs[i];
                        int predict = (c1 * ch1 + c2 * ch2) / 64;
                        int s = (sn << 12 >> sh) + predict;
                        if (s > 32767)
                            s = 32767;
                        if (s < -32768)
                            s = -32768;
                        long d = s - seg[i];
                        err += d * d;
                        ch2 = ch1;
                        ch1 = s;
                    }

                    if (err < bestErr)
                    {
                        bestErr = err;
                        bestP = p;
                        bestShift = sh;
                        nibs.CopyTo(bestNibs, 0);
                    }
                }
            }

            int frameOff = 16 + f * 16;
            data[frameOff] = (byte)((bestP << 4) | bestShift);
            data[frameOff + 1] = 0x00;

            for (int i = 0; i < 14; i++)
                data[frameOff + 2 + i] = (byte)(bestNibs[i * 2] | (bestNibs[i * 2 + 1] << 4));

            int hh1 = h1;
            int hh2 = h2;
            short bc1 = VagCoeffs[bestP, 0];
            short bc2 = VagCoeffs[bestP, 1];
            int useShift = bestShift;
            for (int i = 0; i < 28; i++)
            {
                int sn = bestNibs[i] >= 8 ? bestNibs[i] - 16 : bestNibs[i];
                int predict = (bc1 * hh1 + bc2 * hh2) / 64;
                int s = (sn << 12 >> useShift) + predict;
                if (s > 32767)
                    s = 32767;
                if (s < -32768)
                    s = -32768;
                hh2 = hh1;
                hh1 = s;
            }

            h2 = hh2;
            h1 = hh1;
        }

        return data;
    }

    private static short NextSample(int nibble, int scale, int c1, int c2, ref int h1, ref int h2)
    {
        int sn = nibble >= 8 ? nibble - 16 : nibble;
        int s = ((sn * scale) << 11) + 1024 + c1 * h1 + c2 * h2;
        s >>= 11;
        if (s > 32767)
            s = 32767;
        if (s < -32768)
            s = -32768;
        h2 = h1;
        h1 = s;
        return (short)s;
    }

    public static byte[] Encode(short[] samples)
    {
        int count = samples.Length;
        int padded = (count + 13) / 14 * 14;
        int frames = padded / 14;
        byte[] data = new byte[frames * 8];

        int h1 = 0;
        int h2 = 0;

        for (int f = 0; f < frames; f++)
        {
            int start = f * 14;
            short[] seg = new short[14];
            for (int i = 0; i < 14; i++)
                seg[i] = start + i < count ? samples[start + i] : (short)0;

            int bestP = 0;
            int bestScale = 0;
            long bestErr = long.MaxValue;

            for (int p = 0; p < 16; p++)
            {
                short c1 = Coeffs[p, 0];
                short c2 = Coeffs[p, 1];
                int carry1 = h1;
                int carry2 = h2;

                for (int sh = 0; sh <= 12; sh++)
                {
                    int scale = 1 << sh;

                    int ch1 = carry1;
                    int ch2 = carry2;
                    byte[] nibs = new byte[14];
                    bool ok = true;

                    int maxStep = 0;
                    for (int i = 0; i < 14; i++)
                    {
                        int predict = (1024 + c1 * ch1 + c2 * ch2) >> 11;
                        int target = seg[i] - predict;
                        int step = target / scale;
                        if (step > maxStep)
                            maxStep = step;
                        if (step > 8)
                            step = 8;
                        if (step < -9)
                            step = -8;
                        nibs[i] = (byte)(step & 0x0F);
                        int sn = step >= 8 ? step - 16 : step;
                        int s = ((sn * scale) << 11) + 1024 + c1 * ch1 + c2 * ch2;
                        s >>= 11;
                        if (s > 32767)
                            s = 32767;
                        if (s < -32768)
                            s = -32768;
                        ch2 = ch1;
                        ch1 = s;
                    }

                    if (maxStep > 8 && sh < 12)
                    {
                        ok = false;
                    }

                    if (!ok)
                        continue;

                    long err = 0;
                    ch1 = carry1;
                    ch2 = carry2;
                    for (int i = 0; i < 14; i++)
                    {
                        int sn = nibs[i] >= 8 ? nibs[i] - 16 : nibs[i];
                        int s = ((sn * scale) << 11) + 1024 + c1 * ch1 + c2 * ch2;
                        s >>= 11;
                        if (s > 32767)
                            s = 32767;
                        if (s < -32768)
                            s = -32768;
                        long d = s - seg[i];
                        err += d * d;
                        ch2 = ch1;
                        ch1 = s;
                    }

                    if (err < bestErr)
                    {
                        bestErr = err;
                        bestP = p;
                        bestScale = sh;
                    }
                }
            }

            int sh2 = bestScale;
            if (sh2 > 15)
                sh2 = 15;
            short bc1 = Coeffs[bestP, 0];
            short bc2 = Coeffs[bestP, 1];
            int scale2 = 1 << sh2;

            data[f * 8] = (byte)((bestP << 4) | sh2);
            int hh1 = h1;
            int hh2 = h2;
            byte[] raw = new byte[14];
            for (int i = 0; i < 14; i++)
            {
                int predict = (1024 + bc1 * hh1 + bc2 * hh2) >> 11;
                int target = seg[i] - predict;
                int step = target / scale2;
                if (step > 8)
                    step = 8;
                if (step < -8)
                    step = -8;
                raw[i] = (byte)(step & 0x0F);
                int sn = step >= 8 ? step - 16 : step;
                int s = ((sn * scale2) << 11) + 1024 + bc1 * hh1 + bc2 * hh2;
                s >>= 11;
                if (s > 32767)
                    s = 32767;
                if (s < -32768)
                    s = -32768;
                hh2 = hh1;
                hh1 = s;
            }

            for (int i = 0; i < 7; i++)
                data[f * 8 + 1 + i] = (byte)((raw[i * 2] << 4) | raw[i * 2 + 1]);

            h2 = hh2;
            h1 = hh1;
        }

        return data;
    }

    public static bool EqualSamples(short[] a, short[] b)
    {
        if (a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }
}

public static class WavCodec
{
    public static byte[] Encode(short[] samples, int sampleRate)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        byte[] pcm = new byte[samples.Length * 2];
        for (int i = 0; i < samples.Length; i++)
        {
            pcm[i * 2] = (byte)(samples[i] & 0xFF);
            pcm[i * 2 + 1] = (byte)((samples[i] >> 8) & 0xFF);
        }

        bw.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
        bw.Write(36 + pcm.Length);
        bw.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
        bw.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
        bw.Write(16);
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);
        bw.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
        bw.Write(pcm.Length);
        bw.Write(pcm);

        return ms.ToArray();
    }

    public static short[] Decode(byte[] wav, out int sampleRate)
    {
        sampleRate = 0;
        if (wav.Length < 44)
            throw new InvalidDataException("WAV too short");

        int formatPos = -1;
        int dataLen = 0;
        int dataPos = -1;
        int pos = 12;

        while (pos + 8 <= wav.Length)
        {
            string id = System.Text.Encoding.ASCII.GetString(wav, pos, 4);
            int size = wav[pos + 4] | (wav[pos + 5] << 8) | (wav[pos + 6] << 16) | (wav[pos + 7] << 24);
            int chunkData = pos + 8;

            if (id == "fmt ")
            {
                formatPos = chunkData;
            }
            else if (id == "data")
            {
                dataPos = chunkData;
                dataLen = size;
            }

            pos = chunkData + size + (size & 1);
        }

        if (formatPos < 0 || dataPos < 0)
            throw new InvalidDataException("WAV missing fmt/data chunk");

        int format = wav[formatPos] | (wav[formatPos + 1] << 8);
        int channels = wav[formatPos + 2] | (wav[formatPos + 3] << 8);
        sampleRate = wav[formatPos + 4] | (wav[formatPos + 5] << 8) | (wav[formatPos + 6] << 16) | (wav[formatPos + 7] << 24);
        int bits = wav[formatPos + 14] | (wav[formatPos + 15] << 8);

        if (format != 1)
            throw new InvalidDataException("WAV PCM only");
        if (channels != 1 || bits != 16)
            throw new InvalidDataException("WAV mono 16-bit only");

        int sampleCount = dataLen / 2;
        if (dataPos + sampleCount * 2 > wav.Length)
            sampleCount = (wav.Length - dataPos) / 2;

        short[] samples = new short[sampleCount];
        for (int i = 0; i < sampleCount; i++)
        {
            int lo = wav[dataPos + i * 2];
            int hi = wav[dataPos + i * 2 + 1];
            samples[i] = (short)(lo | (hi << 8));
        }

        return samples;
    }
}
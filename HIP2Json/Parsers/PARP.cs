using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class PARPParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        return new PARP
        {
            parSysID = ReadUInt32BE(br),

            rate = new xParInterp
            {
                val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                interp = ReadUInt32BE(br),
                freq = ReadFloatBE(br),
                oofreq = ReadFloatBE(br)
            },

            life = new xParInterp
            {
                val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                interp = ReadUInt32BE(br),
                freq = ReadFloatBE(br),
                oofreq = ReadFloatBE(br)
            },

            size_birth = new xParInterp
            {
                val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                interp = ReadUInt32BE(br),
                freq = ReadFloatBE(br),
                oofreq = ReadFloatBE(br)
            },

            size_death = new xParInterp
            {
                val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                interp = ReadUInt32BE(br),
                freq = ReadFloatBE(br),
                oofreq = ReadFloatBE(br)
            },

            color_birth = new[]
            {
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                },
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                },
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                },
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                }
            },

            color_death = new[]
            {
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                },
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                },
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                },
                new xParInterp
                {
                    val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                    interp = ReadUInt32BE(br),
                    freq = ReadFloatBE(br),
                    oofreq = ReadFloatBE(br)
                }
            },

            vel_scale = new xParInterp
            {
                val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                interp = ReadUInt32BE(br),
                freq = ReadFloatBE(br),
                oofreq = ReadFloatBE(br)
            },

            vel_angle = new xParInterp
            {
                val = new[] { ReadFloatBE(br), ReadFloatBE(br) },
                interp = ReadUInt32BE(br),
                freq = ReadFloatBE(br),
                oofreq = ReadFloatBE(br)
            },

            vel = ReadVector3BE(br),

            emit_limit = ReadUInt32BE(br),
            emit_limit_reset_time = ReadFloatBE(br)
        };
    }

    public override object Serialize(object obj)
    {
        PARP parp = (PARP)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, parp.parSysID); //god forgive my following sins...

        WriteFloatBE(bw, parp.rate.val[0]);
        WriteFloatBE(bw, parp.rate.val[1]);
        WriteUInt32BE(bw, parp.rate.interp);
        WriteFloatBE(bw, parp.rate.freq);
        WriteFloatBE(bw, parp.rate.oofreq);

        WriteFloatBE(bw, parp.life.val[0]);
        WriteFloatBE(bw, parp.life.val[1]);
        WriteUInt32BE(bw, parp.life.interp);
        WriteFloatBE(bw, parp.life.freq);
        WriteFloatBE(bw, parp.life.oofreq);

        WriteFloatBE(bw, parp.size_birth.val[0]);
        WriteFloatBE(bw, parp.size_birth.val[1]);
        WriteUInt32BE(bw, parp.size_birth.interp);
        WriteFloatBE(bw, parp.size_birth.freq);
        WriteFloatBE(bw, parp.size_birth.oofreq);

        WriteFloatBE(bw, parp.size_death.val[0]);
        WriteFloatBE(bw, parp.size_death.val[1]);
        WriteUInt32BE(bw, parp.size_death.interp);
        WriteFloatBE(bw, parp.size_death.freq);
        WriteFloatBE(bw, parp.size_death.oofreq);

        WriteFloatBE(bw, parp.color_birth[0].val[0]);
        WriteFloatBE(bw, parp.color_birth[0].val[1]);
        WriteUInt32BE(bw, parp.color_birth[0].interp);
        WriteFloatBE(bw, parp.color_birth[0].freq);
        WriteFloatBE(bw, parp.color_birth[0].oofreq);

        WriteFloatBE(bw, parp.color_birth[1].val[0]);
        WriteFloatBE(bw, parp.color_birth[1].val[1]);
        WriteUInt32BE(bw, parp.color_birth[1].interp);
        WriteFloatBE(bw, parp.color_birth[1].freq);
        WriteFloatBE(bw, parp.color_birth[1].oofreq);

        WriteFloatBE(bw, parp.color_birth[2].val[0]);
        WriteFloatBE(bw, parp.color_birth[2].val[1]);
        WriteUInt32BE(bw, parp.color_birth[2].interp);
        WriteFloatBE(bw, parp.color_birth[2].freq);
        WriteFloatBE(bw, parp.color_birth[2].oofreq);

        WriteFloatBE(bw, parp.color_birth[3].val[0]);
        WriteFloatBE(bw, parp.color_birth[3].val[1]);
        WriteUInt32BE(bw, parp.color_birth[3].interp);
        WriteFloatBE(bw, parp.color_birth[3].freq);
        WriteFloatBE(bw, parp.color_birth[3].oofreq);

        WriteFloatBE(bw, parp.color_death[0].val[0]);
        WriteFloatBE(bw, parp.color_death[0].val[1]);
        WriteUInt32BE(bw, parp.color_death[0].interp);
        WriteFloatBE(bw, parp.color_death[0].freq);
        WriteFloatBE(bw, parp.color_death[0].oofreq);

        WriteFloatBE(bw, parp.color_death[1].val[0]);
        WriteFloatBE(bw, parp.color_death[1].val[1]);
        WriteUInt32BE(bw, parp.color_death[1].interp);
        WriteFloatBE(bw, parp.color_death[1].freq);
        WriteFloatBE(bw, parp.color_death[1].oofreq);

        WriteFloatBE(bw, parp.color_death[2].val[0]);
        WriteFloatBE(bw, parp.color_death[2].val[1]);
        WriteUInt32BE(bw, parp.color_death[2].interp);
        WriteFloatBE(bw, parp.color_death[2].freq);
        WriteFloatBE(bw, parp.color_death[2].oofreq);

        WriteFloatBE(bw, parp.color_death[3].val[0]);
        WriteFloatBE(bw, parp.color_death[3].val[1]);
        WriteUInt32BE(bw, parp.color_death[3].interp);
        WriteFloatBE(bw, parp.color_death[3].freq);
        WriteFloatBE(bw, parp.color_death[3].oofreq);

        WriteFloatBE(bw, parp.vel_scale.val[0]);
        WriteFloatBE(bw, parp.vel_scale.val[1]);
        WriteUInt32BE(bw, parp.vel_scale.interp);
        WriteFloatBE(bw, parp.vel_scale.freq);
        WriteFloatBE(bw, parp.vel_scale.oofreq);

        WriteFloatBE(bw, parp.vel_angle.val[0]);
        WriteFloatBE(bw, parp.vel_angle.val[1]);
        WriteUInt32BE(bw, parp.vel_angle.interp);
        WriteFloatBE(bw, parp.vel_angle.freq);
        WriteFloatBE(bw, parp.vel_angle.oofreq);

        WriteVector3BE(bw, parp.vel);

        WriteUInt32BE(bw, parp.emit_limit);
        WriteFloatBE(bw, parp.emit_limit_reset_time);

        return ms.ToArray();
    }
}

public class PARP
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint parSysID { get; set; }
    public xParInterp rate;
    public xParInterp life;
    public xParInterp size_birth;
    public xParInterp size_death;
    public xParInterp[] color_birth;
    public xParInterp[] color_death;
    public xParInterp vel_scale;
    public xParInterp vel_angle;
    public xVec3 vel;
    public uint emit_limit;
    public float emit_limit_reset_time;
}

public class xParInterp
{
    public float[] val { get; set; }
    public uint interp { get; set; }
    public float freq { get; set; }
    public float oofreq { get; set; }
}
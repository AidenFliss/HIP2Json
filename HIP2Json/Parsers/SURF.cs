using System;
using System.IO;
using System.Text.Json.Serialization;

namespace PortHeavyIronGameRewrite;

public sealed class SURFParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte game_damage_type = ReadByte(br);
        byte game_sticky = ReadByte(br);
        byte game_damage_flags = ReadByte(br);
        byte surf_type = ReadByte(br);
        byte phys_pad = ReadByte(br);
        byte sld_start = ReadByte(br);
        byte sld_stop = ReadByte(br);
        byte phys_flags = ReadByte(br);
        float friction = ReadFloatBE(br);

        return new SURF
        {
            game_damage_type = game_damage_type,
            game_sticky = game_sticky,
            game_damage_flags = game_damage_flags,
            surf_type = surf_type,
            phys_pad = phys_pad,
            sld_start = sld_start,
            sld_stop = sld_stop,
            phys_flags = (PhysFlags)phys_flags,
            friction = friction,

            matfx = new zSurfMatFX
            {
                flags = ReadUInt32BE(br),
                bumpmapID = ReadUInt32BE(br),
                envmapID = ReadUInt32BE(br),
                shininess = ReadFloatBE(br),
                bumpiness = ReadFloatBE(br),
                dualmapID = ReadUInt32BE(br)
            },

            colorfx = new zSurfColorFX
            {
                flags = ReadUInt16BE(br),
                mode = ReadUInt16BE(br),
                speed = ReadFloatBE(br)
            },

            texture_anim_flags = ReadUInt32BE(br),

            texture_anm_0 = new zSurfTextureAnim
            {
                pad = ReadUInt16BE(br),
                mode = ReadUInt16BE(br),
                group = ReadUInt32BE(br),
                speed = ReadFloatBE(br)
            },

            texture_anm_1 = new zSurfTextureAnim
            {
                pad = ReadUInt16BE(br),
                mode = ReadUInt16BE(br),
                group = ReadUInt32BE(br),
                speed = ReadFloatBE(br)
            },

            uvfx_flags = ReadUInt32BE(br),

            uvfx_0 = new zSurfUVFX
            {
                mode = ReadInt32BE(br),
                rot = ReadFloatBE(br),
                rot_spd = ReadFloatBE(br),

                trans = ReadVector3BE(br),
                trans_spd = ReadVector3BE(br),
                scale = ReadVector3BE(br),
                scale_spd = ReadVector3BE(br),
                min = ReadVector3BE(br),
                max = ReadVector3BE(br),
                minmax_spd = ReadVector3BE(br)
            },

            uvfx_1 = new zSurfUVFX
            {
                mode = ReadInt32BE(br),
                rot = ReadFloatBE(br),
                rot_spd = ReadFloatBE(br),

                trans = ReadVector3BE(br),
                trans_spd = ReadVector3BE(br),
                scale = ReadVector3BE(br),
                scale_spd = ReadVector3BE(br),
                min = ReadVector3BE(br),
                max = ReadVector3BE(br),
                minmax_spd = ReadVector3BE(br)
            },

            on = ReadByte(br),
            pad_0 = ReadByte(br),
            pad_1 = ReadByte(br),
            pad_2 = ReadByte(br),

            oob_delay = ReadFloatBE(br),
            walljump_scale_xz = ReadFloatBE(br),
            walljump_scale_y = ReadFloatBE(br),
            damage_timer = ReadFloatLE(br),
            damage_bounce = ReadFloatLE(br)
        };
    }

    public override object Serialize(object obj)
    {
        SURF surf = (SURF)obj;
        
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteByte(bw, surf.game_damage_type);
        WriteByte(bw, surf.game_sticky);
        WriteByte(bw, surf.game_damage_flags);
        WriteByte(bw, surf.surf_type);
        WriteByte(bw, surf.phys_pad);
        WriteByte(bw, surf.sld_start);
        WriteByte(bw, surf.sld_stop);
        WriteByte(bw, (byte)surf.phys_flags);
        WriteFloatBE(bw, surf.friction);

        WriteUInt32BE(bw, surf.matfx.flags);
        WriteUInt32BE(bw, surf.matfx.bumpmapID);
        WriteUInt32BE(bw, surf.matfx.envmapID);
        WriteFloatBE(bw, surf.matfx.shininess);
        WriteFloatBE(bw, surf.matfx.bumpiness);
        WriteUInt32BE(bw, surf.matfx.dualmapID);

        WriteUInt16BE(bw, surf.colorfx.flags);
        WriteUInt16BE(bw, surf.colorfx.mode);
        WriteFloatBE(bw, surf.colorfx.speed);

        WriteUInt16BE(bw, surf.texture_anm_0.pad);
        WriteUInt16BE(bw, surf.texture_anm_0.mode);
        WriteUInt32BE(bw, surf.texture_anm_0.group);
        WriteFloatBE(bw, surf.texture_anm_0.speed);

        WriteUInt16BE(bw, surf.texture_anm_1.pad);
        WriteUInt16BE(bw, surf.texture_anm_1.mode);
        WriteUInt32BE(bw, surf.texture_anm_1.group);
        WriteFloatBE(bw, surf.texture_anm_1.speed);

        WriteUInt32BE(bw, surf.uvfx_flags);

        WriteInt32BE(bw, surf.uvfx_0.mode);
        WriteFloatBE(bw, surf.uvfx_0.rot);
        WriteFloatBE(bw, surf.uvfx_0.rot_spd);
        WriteVector3BE(bw, surf.uvfx_0.trans);
        WriteVector3BE(bw, surf.uvfx_0.trans_spd);
        WriteVector3BE(bw, surf.uvfx_0.scale);
        WriteVector3BE(bw, surf.uvfx_0.min);
        WriteVector3BE(bw, surf.uvfx_0.max);
        WriteVector3BE(bw, surf.uvfx_0.minmax_spd);

        WriteInt32BE(bw, surf.uvfx_1.mode);
        WriteFloatBE(bw, surf.uvfx_1.rot);
        WriteFloatBE(bw, surf.uvfx_1.rot_spd);
        WriteVector3BE(bw, surf.uvfx_1.trans);
        WriteVector3BE(bw, surf.uvfx_1.trans_spd);
        WriteVector3BE(bw, surf.uvfx_1.scale);
        WriteVector3BE(bw, surf.uvfx_1.min);
        WriteVector3BE(bw, surf.uvfx_1.max);
        WriteVector3BE(bw, surf.uvfx_1.minmax_spd);

        WriteByte(bw, surf.on);
        WriteByte(bw, surf.pad_0);
        WriteByte(bw, surf.pad_1);
        WriteByte(bw, surf.pad_2);

        WriteFloatBE(bw, surf.oob_delay);
        WriteFloatBE(bw, surf.walljump_scale_xz);
        WriteFloatBE(bw, surf.walljump_scale_y);
        WriteFloatLE(bw, surf.damage_timer);
        WriteFloatLE(bw, surf.damage_bounce);

        return ms.ToArray();
    }
}

public class SURF
{
    public byte game_damage_type { get; set; }
    public byte game_sticky { get; set; }
    public byte game_damage_flags { get; set; }
    public byte surf_type { get; set; }
    public byte phys_pad { get; set; }
    public byte sld_start { get; set; }
    public byte sld_stop { get; set; }
    public PhysFlags phys_flags { get; set; }
    public float friction { get; set; }
    public zSurfMatFX matfx { get; set; }
    public zSurfColorFX colorfx { get; set; }
    public uint texture_anim_flags { get; set; }
    public zSurfTextureAnim texture_anm_0 { get; set; }
    public zSurfTextureAnim texture_anm_1 { get; set; }
    public uint uvfx_flags { get; set; }
    public zSurfUVFX uvfx_0 { get; set; }
    public zSurfUVFX uvfx_1 { get; set; }
    public byte on { get; set; }
    public byte pad_0 { get; set; }
    public byte pad_1 { get; set; }
    public byte pad_2 { get; set; }
    public float oob_delay { get; set; }
    public float walljump_scale_xz { get; set; }
    public float walljump_scale_y { get; set; }
    public float damage_timer { get; set; }
    public float damage_bounce { get; set; }
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PhysFlags : byte
{
    SlideOffPlayer = 0,
    AnglePlayer = 0x02,
    NoStand = 0x04,
    OutOfBounds = 0x08,
    WallJump = 0x10,
    LedgeGrab = 0x20,
    Unknown40 = 0x40
}

public class zSurfMatFX
{
    public uint flags { get; set; }
    public uint bumpmapID { get; set; }
    public uint envmapID { get; set; }
    public float shininess { get; set; }
    public float bumpiness { get; set; }
    public uint dualmapID { get; set; }
}

public class zSurfColorFX
{
    public ushort flags { get; set; }
    public ushort mode { get; set; }
    public float speed { get; set; }
}

public class zSurfTextureAnim
{
    public ushort pad { get; set; }
    public ushort mode { get; set; }
    public uint group { get; set; }
    public float speed { get; set; }
}

public class zSurfUVFX
{
    public int mode { get; set; }
    public float rot { get; set; }
    public float rot_spd { get; set; }
    public xVec3 trans { get; set; }
    public xVec3 trans_spd { get; set; }
    public xVec3 scale { get; set; }
    public xVec3 scale_spd { get; set; }
    public xVec3 min { get; set; }
    public xVec3 max { get; set; }
    public xVec3 minmax_spd { get; set; }
}
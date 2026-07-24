using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class SURFParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        byte game_damage_type = ReadByte(br);
        byte game_sticky = ReadByte(br);
        byte game_damage_flags = ReadByte(br);
        byte surf_type = ReadByte(br);
        br.ReadBytes(1);
        byte sld_start = ReadByte(br);
        byte sld_stop = ReadByte(br);
        byte phys_flags = ReadByte(br);
        float friction = ReadFloatBE(br);

        uint matfx_flags = ReadUInt32BE(br);
        uint bumpmapID = ReadUInt32BE(br);
        uint envmapID = ReadUInt32BE(br);
        float shininess = ReadFloatBE(br);
        float bumpiness = ReadFloatBE(br);
        uint dualmapID = ReadUInt32BE(br);

        ushort colorfx_flags = ReadUInt16BE(br);
        ushort mode = ReadUInt16BE(br);
        float speed = ReadFloatBE(br);

        uint texture_anim_flags = ReadUInt32BE(br);

        br.ReadBytes(2);
        ushort texture_anm_0_mode = ReadUInt16BE(br);
        uint texture_anm_0_group = ReadUInt32BE(br);
        float texture_anm_0_speed = ReadFloatBE(br);

        zSurfTextureAnim texture_anm_0 = new zSurfTextureAnim
        {
            mode = texture_anm_0_mode,
            group = texture_anm_0_group,
            speed = texture_anm_0_speed
        };

        br.ReadBytes(2);
        ushort texture_anm_1_mode = ReadUInt16BE(br);
        uint texture_anm_1_group = ReadUInt32BE(br);
        float texture_anm_1_speed = ReadFloatBE(br);

        zSurfTextureAnim texture_anm_1 = new zSurfTextureAnim
        {
            mode = texture_anm_1_mode,
            group = texture_anm_1_group,
            speed = texture_anm_1_speed
        };

        uint uvfx_flags = ReadUInt32BE(br);

        int uvfx_0_mode = ReadInt32BE(br);
        float uvfx_0_rot = ReadFloatBE(br);
        float uvfx_0_rot_spd = ReadFloatBE(br);
        xVec3 uvfx_0_trans = ReadVector3BE(br);
        xVec3 uvfx_0_trans_spd = ReadVector3BE(br);
        xVec3 uvfx_0_scale = ReadVector3BE(br);
        xVec3 uvfx_0_scale_spd = ReadVector3BE(br);
        xVec3 uvfx_0_min = ReadVector3BE(br);
        xVec3 uvfx_0_max = ReadVector3BE(br);
        xVec3 uvfx_0_minmax_spd = ReadVector3BE(br);

        zSurfUVFX uvfx_0 = new zSurfUVFX
        {
            mode = uvfx_0_mode,
            rot = uvfx_0_rot,
            rot_spd = uvfx_0_rot_spd,
            trans = uvfx_0_trans,
            trans_spd = uvfx_0_trans_spd,
            scale = uvfx_0_scale,
            scale_spd = uvfx_0_scale_spd,
            min = uvfx_0_min,
            max = uvfx_0_max,
            minmax_spd = uvfx_0_minmax_spd
        };

        int uvfx_1_mode = ReadInt32BE(br);
        float uvfx_1_rot = ReadFloatBE(br);
        float uvfx_1_rot_spd = ReadFloatBE(br);
        xVec3 uvfx_1_trans = ReadVector3BE(br);
        xVec3 uvfx_1_trans_spd = ReadVector3BE(br);
        xVec3 uvfx_1_scale = ReadVector3BE(br);
        xVec3 uvfx_1_scale_spd = ReadVector3BE(br);
        xVec3 uvfx_1_min = ReadVector3BE(br);
        xVec3 uvfx_1_max = ReadVector3BE(br);
        xVec3 uvfx_1_minmax_spd = ReadVector3BE(br);

        zSurfUVFX uvfx_1 = new zSurfUVFX
        {
            mode = uvfx_1_mode,
            rot = uvfx_1_rot,
            rot_spd = uvfx_1_rot_spd,
            trans = uvfx_1_trans,
            trans_spd = uvfx_1_trans_spd,
            scale = uvfx_1_scale,
            scale_spd = uvfx_1_scale_spd,
            min = uvfx_1_min,
            max = uvfx_1_max,
            minmax_spd = uvfx_1_minmax_spd
        };

        byte on = ReadByte(br);

        br.ReadBytes(3);

        float oob_delay = ReadFloatBE(br);
        float walljump_scale_xz = ReadFloatBE(br);
        float walljump_scale_y = ReadFloatBE(br);
        float damage_timer = ReadFloatLE(br);
        float damage_bounce = ReadFloatLE(br);

        return new SURF
        {
            game_damage_type = game_damage_type,
            game_sticky = game_sticky,
            game_damage_flags = game_damage_flags,
            surf_type = surf_type,
            sld_start = sld_start,
            sld_stop = sld_stop,
            phys_flags = (PhysFlags)phys_flags,
            friction = friction,

            matfx = new zSurfMatFX
            {
                flags = matfx_flags,
                bumpmapID = bumpmapID,
                envmapID = envmapID,
                shininess = shininess,
                bumpiness = bumpiness,
                dualmapID = dualmapID
            },

            colorfx = new zSurfColorFX
            {
                flags = colorfx_flags,
                mode = mode,
                speed = speed
            },

            texture_anim_flags = texture_anim_flags,
            texture_anm_0 = texture_anm_0,
            texture_anm_1 = texture_anm_1,

            uvfx_flags = uvfx_flags,
            uvfx_0 = uvfx_0,
            uvfx_1 = uvfx_1,

            on = on,

            oob_delay = oob_delay,
            walljump_scale_xz = walljump_scale_xz,
            walljump_scale_y = walljump_scale_y,
            damage_timer = damage_timer,
            damage_bounce = damage_bounce
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
        bw.Write(new byte[1]);
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

        bw.Write(new byte[1]);
        WriteUInt16BE(bw, surf.texture_anm_0.mode);
        WriteUInt32BE(bw, surf.texture_anm_0.group);
        WriteFloatBE(bw, surf.texture_anm_0.speed);

        bw.Write(new byte[1]);
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
        bw.Write(new byte[3]);

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
using System;
using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class BOULParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        float gravity = ReadFloatBE(br);
        float mass = ReadFloatBE(br);
        float bounce = ReadFloatBE(br);
        float friction = ReadFloatBE(br);
        float statFric = 0f;

        if (Program.CurrentGame == GameType.BFBB)
            statFric = ReadFloatBE(br);

        float maxVel = ReadFloatBE(br);
        float maxAngVel = ReadFloatBE(br);
        float stickiness = ReadFloatBE(br);
        float bounceDamp = ReadFloatBE(br);
        uint flags = ReadUInt32BE(br);
        float killtimer = ReadFloatBE(br);
        uint hitpoints = ReadUInt32BE(br);

        uint soundID = 0;
        uint uSoundGroupHash = 0;
        float volume = 0;

        if (Program.CurrentGame == GameType.BFBB)
        {
            soundID = ReadUInt32BE(br);
            volume = ReadFloatBE(br);
        }
        else
        {
            uSoundGroupHash = ReadUInt32BE(br);
        }
        
        float minSoundVel = ReadFloatBE(br);
        float maxSoundVel = ReadFloatBE(br);

        float innerRadius = 0;
        float outerRadius = 0;

        float fSphereRadius = 0;
        byte uPad0 = 0;
        byte uPad1 = 0;
        byte uPad2 = 0;
        byte uBoneIndex = 0;
        if (Program.CurrentGame == GameType.BFBB)
        {
            innerRadius = ReadFloatBE(br);
            outerRadius = ReadFloatBE(br);
        }
        else
        {
            fSphereRadius = ReadFloatBE(br);
            uPad0 = ReadByte(br);
            uPad1 = ReadByte(br);
            uPad2 = ReadByte(br);
            uBoneIndex = ReadByte(br);
        }

        return new BOUL
        {
            gravity = gravity,
            mass = mass,
            bounce = bounce,
            friction = friction,
            statFric = statFric,
            maxVel = maxVel,
            maxAngVel = maxAngVel,
            stickiness = stickiness,
            bounceDamp = bounceDamp,
            flags = (BoulderFlags)flags,
            killtimer = killtimer,
            hitpoints = hitpoints,
            soundID = soundID,
            uSoundGroupHash = uSoundGroupHash,
            volume = volume,
            minSoundVel = minSoundVel,
            maxSoundVel = maxSoundVel,
            innerRadius = innerRadius,
            outerRadius = outerRadius,
            fSphereRadius = fSphereRadius,
            uPad0 = uPad0,
            uPad1 = uPad1,
            uPad2 = uPad2,
            uBoneIndex = uBoneIndex
        };
    }

    public override object Serialize(object obj)
    {
        BOUL boul = (BOUL)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteFloatBE(bw, boul.gravity);
        WriteFloatBE(bw, boul.mass);
        WriteFloatBE(bw, boul.bounce);
        WriteFloatBE(bw, boul.friction);

        if (Program.CurrentGame == GameType.BFBB)
            WriteFloatBE(bw, boul.statFric);
            
        WriteFloatBE(bw, boul.maxVel);
        WriteFloatBE(bw, boul.maxAngVel);
        WriteFloatBE(bw, boul.stickiness);
        WriteFloatBE(bw, boul.bounceDamp);
        WriteUInt32BE(bw, (uint)boul.flags);
        WriteFloatBE(bw, boul.killtimer);
        WriteUInt32BE(bw, boul.hitpoints);

        if (Program.CurrentGame == GameType.BFBB)
        {
            WriteUInt32BE(bw, boul.soundID);
            WriteFloatBE(bw, boul.volume);
        }
        else
        {
            WriteUInt32BE(bw, boul.uSoundGroupHash);
        }

        WriteFloatBE(bw, boul.minSoundVel);
        WriteFloatBE(bw, boul.maxSoundVel);

        if (Program.CurrentGame == GameType.BFBB)
        {
            WriteFloatBE(bw, boul.innerRadius);
            WriteFloatBE(bw, boul.outerRadius);
        }
        else
        {
            WriteFloatBE(bw, boul.fSphereRadius);
            WriteByte(bw, boul.uPad0);
            WriteByte(bw, boul.uPad1);
            WriteByte(bw, boul.uPad2);
            WriteByte(bw, boul.uBoneIndex);
        }

        return ms.ToArray();
    }
}

public class BOUL
{
    public float gravity { get; set; }
    public float mass { get; set; }
    public float bounce { get; set; }
    public float friction { get; set; }
    public float statFric { get; set; } //bfbb only
    public float maxVel { get; set; }
    public float maxAngVel { get; set; }
    public float stickiness { get; set; }
    public float bounceDamp { get; set; }
    public BoulderFlags flags { get; set; }
    public float killtimer { get; set; }
    public uint hitpoints { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint uSoundGroupHash { get; set; } //movie only
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundID { get; set; }
    public float volume { get; set; }
    public float minSoundVel { get; set; }
    public float maxSoundVel { get; set; }
    public float innerRadius { get; set; } //bfbb only
    public float outerRadius { get; set; } //bfbb only
    public float fSphereRadius { get; set; } //movie only
    public byte uPad0 { get; set; } //movie only
    public byte uPad1 { get; set; } //movie only
    public byte uPad2 { get; set; } //movie only
    public byte uBoneIndex { get; set; } //movie only
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BoulderFlags : uint
{
    None = 0,
    HitWalls = 0x001,
    DamagePlayer = 0x002,
    Unknown004 = 0x004,
    DamageNpcs = 0x008,
    Unknown010 = 0x010,
    DieOnOobSurfaces = 0x020,
    Unknown040 = 0x040,
    Unknown080 = 0x080,
    DieOnPlayerAttack = 0x100,
    DieAfterKillTimer = 0x200,
}
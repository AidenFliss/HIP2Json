using System.IO;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class DESTParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        DEST dest = new()
        {
            id = ReadUInt32BE(br),
            nstates = ReadUInt32BE(br),
            hit_points = ReadUInt32BE(br),
            hit_filter = ReadUInt32BE(br),
            launch_flag = ReadUInt32BE(br),
            behaviour = ReadUInt32BE(br),
            flags = ReadUInt32BE(br),
            soundgroupidleID = ReadUInt32BE(br),
            respawn = ReadFloatBE(br),
            target_priority = ReadByte(br),
        };

        br.ReadBytes(3);

        dest.states = new DESTState[dest.nstates];

        for (int i = 0; i < dest.nstates; i++)
        {
            dest.states[i] = new DESTState
            {
                percent = ReadUInt32BE(br),
                modelID = ReadUInt32BE(br),
                shrapnelID = ReadUInt32BE(br),
                shrapnelhitID = ReadUInt32BE(br),
                soundgroupidleID = ReadUInt32BE(br),
                soundgroupfxID = ReadUInt32BE(br),
                soundgrouphitID = ReadUInt32BE(br),
                soundgroupfxIDswitch = ReadUInt32BE(br),
                soundgrouphitIDswitch = ReadUInt32BE(br),
                rumbleIDhit = ReadUInt32BE(br),
                rumbleIDswitch = ReadUInt32BE(br),
                fx_flags = ReadUInt32BE(br),
            };

            uint nanimations = ReadUInt32BE(br);

            dest.states[i].animlist = new DESTAttachedAnimList
            {
                nanimations = nanimations,
                animationIDs = new uint[nanimations]
            };

            for (int j = 0; j < nanimations; j++)
            {
                dest.states[i].animlist.animationIDs[j] = ReadUInt32BE(br);
            }
        }

        return dest;
    }

    public override object Serialize(object obj)
    {
        DEST dest = (DEST)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, dest.id);
        WriteUInt32BE(bw, dest.nstates);
        WriteUInt32BE(bw, dest.hit_points);
        WriteUInt32BE(bw, dest.hit_filter);
        WriteUInt32BE(bw, dest.launch_flag);
        WriteUInt32BE(bw, dest.behaviour);
        WriteUInt32BE(bw, dest.flags);
        WriteUInt32BE(bw, dest.soundgroupidleID);
        WriteFloatBE(bw, dest.respawn);

        WriteByte(bw, dest.target_priority);
        bw.Write(new byte[3]);

        foreach (var state in dest.states)
        {
            WriteUInt32BE(bw, state.percent);
            WriteUInt32BE(bw, state.modelID);
            WriteUInt32BE(bw, state.shrapnelID);
            WriteUInt32BE(bw, state.shrapnelhitID);
            WriteUInt32BE(bw, state.soundgroupidleID);
            WriteUInt32BE(bw, state.soundgroupfxID);
            WriteUInt32BE(bw, state.soundgrouphitID);
            WriteUInt32BE(bw, state.soundgroupfxIDswitch);
            WriteUInt32BE(bw, state.soundgrouphitIDswitch);
            WriteUInt32BE(bw, state.rumbleIDhit);
            WriteUInt32BE(bw, state.rumbleIDswitch);
            WriteUInt32BE(bw, state.fx_flags);

            WriteUInt32BE(bw, state.animlist.nanimations);

            foreach (var anim in state.animlist.animationIDs)
                WriteUInt32BE(bw, anim);
        }

        return ms.ToArray();
    }
}


public class DEST
{
    [JsonConverter(typeof(AssetIDConverter))]
    public uint id { get; set; }
    public uint nstates { get; set; }
    public uint hit_points { get; set; }
    public uint hit_filter { get; set; }
    public uint launch_flag { get; set; }
    public uint behaviour { get; set; }
    public uint flags { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundgroupidleID { get; set; }
    public float respawn { get; set; }
    public byte target_priority { get; set; }
    public DESTState[] states { get; set; }
}


public class DESTState
{
    public uint percent { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint modelID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint shrapnelID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint shrapnelhitID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundgroupidleID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundgroupfxID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundgrouphitID { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundgroupfxIDswitch { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint soundgrouphitIDswitch { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint rumbleIDhit { get; set; }
    [JsonConverter(typeof(AssetIDConverter))]
    public uint rumbleIDswitch { get; set; }
    public uint fx_flags { get; set; }
    public DESTAttachedAnimList animlist { get; set; }
}


public class DESTAttachedAnimList
{
    public uint nanimations { get; set; }
    [JsonConverter(typeof(AssetIDArrayConverter))]
    public uint[] animationIDs { get; set; }
}
using System.IO;
using System.Text;

namespace HIP2Json;

public sealed class DSCOParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        z_disco_floor_asset discoFloorAsset = new z_disco_floor_asset
        {
            flags = (DiscoFloorFlags)ReadUInt32BE(br),
            interval_transition = ReadFloatBE(br),
            interval_state = ReadFloatBE(br),
            prefix_offset_off = ReadUInt32BE(br),
            prefix_offset_transition = ReadUInt32BE(br),
            prefix_offset_on = ReadUInt32BE(br),
            state_mask_size = ReadUInt32BE(br),
            states_offset = ReadUInt32BE(br),
            states_size = ReadUInt32BE(br),
        };

        long currentPos = br.BaseStream.Position;

        br.BaseStream.Position = dataStart + discoFloorAsset.prefix_offset_off;
        var offBytes = new System.Collections.Generic.List<byte>();
        while (true)
        {
            int value = br.ReadByte();
            if (value == -1 || value == 0)
                break;
            offBytes.Add((byte)value);
        }
        string prefix_off = Encoding.ASCII.GetString(offBytes.ToArray());

        br.BaseStream.Position = dataStart + discoFloorAsset.prefix_offset_transition;
        var transitionBytes = new System.Collections.Generic.List<byte>();
        while (true)
        {
            int value = br.ReadByte();
            if (value == -1 || value == 0)
                break;
            transitionBytes.Add((byte)value);
        }
        string prefix_transition = Encoding.ASCII.GetString(transitionBytes.ToArray());

        br.BaseStream.Position = dataStart + discoFloorAsset.prefix_offset_on;
        var onBytes = new System.Collections.Generic.List<byte>();
        while (true)
        {
            int value = br.ReadByte();
            if (value == -1 || value == 0)
                break;
            onBytes.Add((byte)value);
        }
        string prefix_on = Encoding.ASCII.GetString(onBytes.ToArray());

        int stateMaskBytes = (int)((discoFloorAsset.state_mask_size * 2 + 7) / 8);
        uint[] state_offsets = new uint[discoFloorAsset.states_size];
        byte[][] state_masks = new byte[discoFloorAsset.states_size][];

        br.BaseStream.Position = dataStart + discoFloorAsset.states_offset;
        for (int i = 0; i < state_offsets.Length; i++)
            state_offsets[i] = ReadUInt32BE(br);

        for (int i = 0; i < state_masks.Length; i++)
        {
            br.BaseStream.Position = dataStart + state_offsets[i];
            state_masks[i] = br.ReadBytes(stateMaskBytes);
        }

        br.BaseStream.Position = currentPos;

        return new DSCO
        {
            discoFloorAsset = discoFloorAsset,
            prefix_off = prefix_off,
            prefix_transition = prefix_transition,
            prefix_on = prefix_on,
            state_offsets = state_offsets,
            state_masks = state_masks,
        };
    }

    public override object Serialize(object obj)
    {
        DSCO dsco = (DSCO)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        z_disco_floor_asset asset = dsco.discoFloorAsset;

        int stateMaskBytes = (int)((asset.state_mask_size * 2 + 7) / 8);

        byte fill = Program.CurrentGame == GameType.BFBB ? (byte)0x00 : (byte)0xCD;

        uint prefixOff = 36;
        uint prefixTransition = Align4(prefixOff + (uint)dsco.prefix_off.Length + 1);
        uint prefixOn = Align4(prefixTransition + (uint)dsco.prefix_transition.Length + 1);
        uint statesOff = Align4(prefixOn + (uint)dsco.prefix_on.Length + 1);

        asset.prefix_offset_off = prefixOff;
        asset.prefix_offset_transition = prefixTransition;
        asset.prefix_offset_on = prefixOn;
        asset.states_offset = statesOff;
        asset.states_size = (uint)dsco.state_masks.Length;

        uint nextStateOffset = statesOff + asset.states_size * 4;
        dsco.state_offsets = new uint[asset.states_size];

        for (int i = 0; i < dsco.state_offsets.Length; i++)
        {
            dsco.state_offsets[i] = nextStateOffset;
            nextStateOffset += (uint)stateMaskBytes;
        }

        WriteUInt32BE(bw, (uint)asset.flags);
        WriteFloatBE(bw, asset.interval_transition);
        WriteFloatBE(bw, asset.interval_state);
        WriteUInt32BE(bw, asset.prefix_offset_off);
        WriteUInt32BE(bw, asset.prefix_offset_transition);
        WriteUInt32BE(bw, asset.prefix_offset_on);
        WriteUInt32BE(bw, asset.state_mask_size);
        WriteUInt32BE(bw, asset.states_offset);
        WriteUInt32BE(bw, asset.states_size);

        WriteRawString(bw, dsco.prefix_off, fill);
        WriteRawString(bw, dsco.prefix_transition, fill);
        WriteRawString(bw, dsco.prefix_on, fill);

        foreach (uint offset in dsco.state_offsets)
            WriteUInt32BE(bw, offset);

        foreach (byte[] mask in dsco.state_masks)
            bw.Write(mask);

        while (ms.Length % 4 != 0)
            bw.Write(fill);

        return ms.ToArray();
    }

    private static uint Align4(uint value)
    {
        return (value + 3) & ~3u;
    }

    private static void WriteRawString(BinaryWriter bw, string value, byte fill)
    {
        byte[] bytes = Encoding.ASCII.GetBytes(value);
        bw.Write(bytes);
        bw.Write((byte)0);
        while (bw.BaseStream.Position % 4 != 0)
            bw.Write(fill);
    }
}

public class DSCO
{
    public z_disco_floor_asset discoFloorAsset { get; set; }
    public string prefix_off { get; set; }
    public string prefix_transition { get; set; }
    public string prefix_on { get; set; }
    public uint[] state_offsets { get; set; }
    public byte[][] state_masks { get; set; }
}

public class z_disco_floor_asset
{
    public DiscoFloorFlags flags { get; set; }
    public float interval_transition { get; set; }
    public float interval_state { get; set; }
    public uint prefix_offset_off { get; set; }
    public uint prefix_offset_transition { get; set; }
    public uint prefix_offset_on { get; set; }
    public uint state_mask_size { get; set; }
    public uint states_offset { get; set; }
    public uint states_size { get; set; }
}

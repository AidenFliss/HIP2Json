using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace HIP2Json;

public sealed class ATBLParser : AssetParser
{
    public override object Parse(BinaryReader br, long assetStart, long dataStart)
    {
        uint magic = ReadUInt32BE(br);
        uint numRaw = ReadUInt32BE(br);
        uint numFiles = ReadUInt32BE(br);
        uint numStates = ReadUInt32BE(br);
        uint rawConstructFunc = ReadUInt32BE(br);

        object constructFunc = Program.CurrentGame switch
        {
            GameType.BFBB => (ConstructFuncBFBB)rawConstructFunc,
            GameType.TSSM => (ConstructFuncTSSM)rawConstructFunc,
            _ => rawConstructFunc,
        };

        uint[] raw = Enumerable.Range(0, (int)numRaw).Select(_ => ReadUInt32BE(br)).ToArray();

        AnimFile[] files = Enumerable.Range(0, (int)numFiles).Select(_ => ReadAnimFile(br)).ToArray();

        AnimState[] states = Enumerable.Range(0, (int)numStates).Select(_ => ReadAnimState(br)).ToArray();

        int effectCount = states.Sum(s => (int)s.effectCount);
        AnimEffect[] effects = Enumerable.Range(0, effectCount).Select(_ => ReadAnimEffect(br)).ToArray();

        uint[] listUnknown = Enumerable.Range(0, (int)numRaw).Select(_ => ReadUInt32BE(br)).ToArray();

        return new ATBL
        {
            magic = magic,
            numRaw = numRaw,
            numFiles = numFiles,
            numStates = numStates,
            constructFunc = constructFunc,
            raw = raw,
            files = files,
            states = states,
            effects = effects,
            listUnknown = listUnknown,
        };
    }

    public override object Serialize(object obj)
    {
        ATBL atbl = (ATBL)obj;

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        WriteUInt32BE(bw, atbl.magic);
        WriteUInt32BE(bw, (uint)(atbl.raw?.Length ?? 0));
        WriteUInt32BE(bw, (uint)(atbl.files?.Length ?? 0));
        WriteUInt32BE(bw, (uint)(atbl.states?.Length ?? 0));

        uint rawConstructFunc = atbl.constructFunc switch
        {
            ConstructFuncBFBB bfbb => (uint)bfbb,
            ConstructFuncTSSM tssm => (uint)tssm,
            uint val => val,
            _ => 0,
        };

        WriteUInt32BE(bw, rawConstructFunc);

        if (atbl.raw != null)
        {
            foreach (uint rawId in atbl.raw)
                WriteUInt32BE(bw, rawId);
        }

        if (atbl.files != null)
        {
            foreach (var file in atbl.files)
                WriteAnimFile(bw, file);
        }

        if (atbl.states != null)
        {
            foreach (var state in atbl.states)
                WriteAnimState(bw, state);
        }

        if (atbl.effects != null)
        {
            foreach (var effect in atbl.effects)
                WriteAnimEffect(bw, effect);
        }

        if (atbl.listUnknown != null)
        {
            foreach (uint unk in atbl.listUnknown)
                WriteUInt32BE(bw, unk);
        }

        return ms.ToArray();
    }

    private AnimFile ReadAnimFile(BinaryReader br)
    {
        return new AnimFile
        {
            fileFlags = ReadUInt32BE(br),
            duration = ReadFloatBE(br),
            timeOffset = ReadFloatBE(br),
            numAnims = new ushort[] { ReadUInt16BE(br), ReadUInt16BE(br) },
            rawData = ReadUInt32BE(br),
            physics = ReadInt32BE(br),
            startPose = ReadInt32BE(br),
            endPose = ReadInt32BE(br),
        };
    }

    private void WriteAnimFile(BinaryWriter bw, AnimFile file)
    {
        WriteUInt32BE(bw, file.fileFlags);
        WriteFloatBE(bw, file.duration);
        WriteFloatBE(bw, file.timeOffset);

        WriteUInt16BE(bw, file.numAnims != null && file.numAnims.Length > 0 ? file.numAnims[0] : (ushort)0);
        WriteUInt16BE(bw, file.numAnims != null && file.numAnims.Length > 1 ? file.numAnims[1] : (ushort)0);

        WriteUInt32BE(bw, file.rawData);
        WriteInt32BE(bw, file.physics);
        WriteInt32BE(bw, file.startPose);
        WriteInt32BE(bw, file.endPose);
    }

    private AnimState ReadAnimState(BinaryReader br)
    {
        return new AnimState
        {
            stateID = ReadUInt32BE(br),
            fileIndex = ReadUInt32BE(br),
            effectCount = ReadUInt32BE(br),
            effectOffset = ReadUInt32BE(br),
            speed = ReadFloatBE(br),
            subStateID = ReadUInt32BE(br),
            subStateCount = ReadUInt32BE(br),
        };
    }

    private void WriteAnimState(BinaryWriter bw, AnimState state)
    {
        WriteUInt32BE(bw, state.stateID);
        WriteUInt32BE(bw, state.fileIndex);
        WriteUInt32BE(bw, state.effectCount);
        WriteUInt32BE(bw, state.effectOffset);
        WriteFloatBE(bw, state.speed);
        WriteUInt32BE(bw, state.subStateID);
        WriteUInt32BE(bw, state.subStateCount);
    }

    private AnimEffect ReadAnimEffect(BinaryReader br)
    {
        return new AnimEffect
        {
            stateID = ReadUInt32BE(br),
            startTime = ReadFloatBE(br),
            endTime = ReadFloatBE(br),
            flags = ReadUInt32BE(br),
            effectType = ReadUInt32BE(br),
            userDataSize = ReadUInt32BE(br),
        };
    }

    private void WriteAnimEffect(BinaryWriter bw, AnimEffect effect)
    {
        WriteUInt32BE(bw, effect.stateID);
        WriteFloatBE(bw, effect.startTime);
        WriteFloatBE(bw, effect.endTime);
        WriteUInt32BE(bw, effect.flags);
        WriteUInt32BE(bw, effect.effectType);
        WriteUInt32BE(bw, effect.userDataSize);
    }
}

public class ATBL
{
    public uint magic { get; set; }
    public uint numRaw { get; set; }
    public uint numFiles { get; set; }
    public uint numStates { get; set; }

    [JsonConverter(typeof(GameEnumConverter<ConstructFuncBFBB, ConstructFuncTSSM>))]
    public object constructFunc { get; set; }
    public uint[] raw { get; set; }
    public AnimFile[] files { get; set; }
    public AnimState[] states { get; set; }
    public AnimEffect[] effects { get; set; }
    public uint[] listUnknown { get; set; }
}

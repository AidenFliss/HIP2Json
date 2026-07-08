using System.Collections.Generic;

namespace PortHeavyIronGameRewrite;

public static class ParserMaps
{
    public static Dictionary<string, AssetParser> AssetToParser = new()
    {
        { "BOUL", new BOULParser() },
        { "BUTN", new BUTNParser() },
        { "CAM", new CAMParser() },
        { "CNTR", new CNTRParser() },
        { "COLL", new COLLParser() },
        { "COND", new CONDParser() },
        { "CSNM", new CSNMParser() },
        { "DPAT", new DPATParser() },
        { "DSCO", new DSCOParser() },
        { "DSTR", new DSTRParser() },
        { "DYNA", new DYNAParser() },
        { "EGEN", new EGENParser() },
        { "ENV", new ENVParser() },
        { "FLY", new FLYParser() },
        { "FOG", new FOGParser() },
        { "GRUP", new GRUPParser() },
        { "JAW", new JAWParser() },
        { "LITE", new LITEParser() },
        { "LKIT", new LKITParser() },
        { "LODT", new LODTParser() },
        { "MAPR", new MAPRParser() },
        { "MINF", new MINFParser() },
        { "MRKR", new MRKRParser() },
        { "MVPT", new MVPTParser() },
        { "PARE", new PAREParser() },
        { "PARP", new PARPParser() },
        { "PARS", new PARSParser() },
        { "PICK", new PICKParser() },
        { "PIPT", new PIPTParser() },
        { "PKUP", new PKUPParser() },
        { "PLAT", new PLATParser() },
        { "PLYR", new PLYRParser() },
        { "PORT", new PORTParser() },
        { "SCRP", new SCRPParser() },
        { "SFX", new SFXParser() },
        { "SHDW", new SHDWParser() },
        { "SIMP", new SIMPParser() },
        { "SURF", new SURFParser() },
        { "TEXT", new TEXTParser() },
        { "TIMR", new TIMRParser() },
        { "TRIG", new TRIGParser() },
        { "UI", new UIParser() },
        { "UIFT", new UIFTParser() },
        { "VIL", new VILParser() }
    };

    public static Dictionary<string, AbstractDYNAParser> DYNAToParser = new()
    {
        { "game_object:BoulderGenerator", new game_object_BoulderGeneratorParser() },
        { "game_object:bungee_drop", new game_object_bungee_dropParser() },
        { "game_object:bungee_hook", new game_object_bungee_hookParser() },
        { "game_object:Camera_Tweak", new game_object_Camera_TweakParser() },
        { "game_object:Flythrough", new game_object_FlythroughParser() },
        { "game_object:NPCSettings", new game_object_NPCSettingsParser() },
        { "game_object:talk_box", new game_object_talk_boxParser() },
        { "game_object:task_box", new game_object_task_boxParser() },
        { "game_object:Taxi", new game_object_TaxiParser() },
        { "game_object:Teleport", new game_object_TeleportParser() },
        { "game_object:text_box", new game_object_text_boxParser() },
        { "game_object:BusStop", new game_object_BusStopParser() },
        { "hud_meter_font", new hud_meter_fontParser() },
        { "hud_meter_unit", new hud_meter_unitParser() },
        { "hud_model", new hud_modelParser() },
        { "hud_text", new hud_textParser() },
        { "pointer", new pointerParser() }
    };

    public static Dictionary<string, string> DYNAFolderToInternalName = new()
    {
        { "BoulderGenerator", "game_object:BoulderGenerator" },
        { "BungeeDrop", "game_object:bungee_drop" },
        { "BungeeHook", "game_object:bungee_hook" },
        { "BusStop", "game_object:BusStop" },
        { "CameraTweak", "game_object:Camera_Tweak" },
        { "FlythroughObject", "game_object:Flythrough" },
        { "NPCSettingsObject", "game_object:NPCSettings" },
        { "TalkBox", "game_object:talk_box" },
        { "TaskBox", "game_object:task_box" },
        { "Taxi", "game_object:Taxi" },
        { "Teleport", "game_object:Teleport" },
        { "TextBox", "game_object:text_box" },
        { "Pointer", "pointer" },
        { "HUDMeterFont", "hud_meter_font" },
        { "HUDMeterUnit", "hud_meter_unit" },
        { "HUDModel", "hud_model" },
        { "HUDText", "hud_text" },
    };
}
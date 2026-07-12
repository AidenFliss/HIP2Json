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
        { "CRDT", new CRDTParser() },
        { "CSNM", new CSNMParser() },
        { "CTOC", new CTOCParser() },
        { "DEST", new DESTParser() },
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
        { "RANM", new RANMParser() },
        { "SCRP", new SCRPParser() },
        { "SDFX", new SDFXParser() },
        { "SFX", new SFXParser() },
        { "SHDW", new SHDWParser() },
        { "SIMP", new SIMPParser() },
        { "SURF", new SURFParser() },
        { "TEXT", new TEXTParser() },
        { "TIMR", new TIMRParser() },
        { "TRIG", new TRIGParser() },
        { "UI", new UIParser() },
        { "UIFT", new UIFTParser() },
        { "UIM", new UIMParser() },
        { "VIL", new VILParser() }
    };

    public static Dictionary<string, AbstractDYNAParser> DYNAToParser = new()
    {
        { "effect:Lightning", new effect_LightningParser() },
        { "effect:Rumble Spherical Emitter", new effect_Rumble_Spherical_EmitterParser() },
        { "effect:Rumble", new effect_RumbleParser() },
        { "effect:ScreenFade", new effect_ScreenFadeParser() },
        { "effect:smoke_emitter", new effect_smoke_emitterParser() },
        { "effect:spotlight", new effect_spotlightParser() },
        { "effect:water_body", new effect_water_bodyParser() },
        { "Enemy:SB", new Enemy_SBParser() },
        { "game_object:BoulderGenerator", new game_object_BoulderGeneratorParser() },
        { "game_object:bungee_drop", new game_object_bungee_dropParser() },
        { "game_object:bungee_hook", new game_object_bungee_hookParser() },
        { "game_object:Camera_Tweak", new game_object_Camera_TweakParser() },
        { "game_object:Flythrough", new game_object_FlythroughParser() },
        { "game_object:NPCSettings", new game_object_NPCSettingsParser() },
        { "game_object:RaceTimer", new game_object_RaceTimerParser() },
        { "game_object:Ring", new game_object_RingParser() },
        { "game_object:RingControl", new game_object_RingControlParser() },
        { "game_object:talk_box", new game_object_talk_boxParser() },
        { "game_object:task_box", new game_object_task_boxParser() },
        { "game_object:Taxi", new game_object_TaxiParser() },
        { "game_object:Teleport", new game_object_TeleportParser() },
        { "game_object:text_box", new game_object_text_boxParser() },
        { "game_object:Vent", new game_object_VentParser() },
        { "game_object:VentType", new game_object_VentTypeParser() },
        { "game_object:BusStop", new game_object_BusStopParser() },
        { "hud:meter:font", new hud_meter_fontParser() },
        { "hud:meter:unit", new hud_meter_unitParser() },
        { "hud:model", new hud_modelParser() },
        { "hud:text", new hud_textParser() },
        { "JSP Extra Data", new JSPExtraDataParser() },
        { "logic:reference", new logic_referenceParser() },
        { "pointer", new pointerParser() },
        { "Scene Properties", new ScenePropertiesParser() },
    };

    public static Dictionary<string, string> DYNAFolderToInternalName = new()
    {
        { "Lightning", "effect:Lightning" },
        { "RumbleSphericalEmitter", "effect:Rumble Spherical Emitter" },
        { "Rumble", "effect:Rumble" },
        { "ScreenFade", "effect:ScreenFade" },
        { "SmokeEmitter", "effect:smoke_emitter" },
        { "Spotlight", "effect:spotlight" },
        { "WaterBody", "effect:water_body" },
        { "Enemy", "Enemy:SB" },
        { "BoulderGenerator", "game_object:BoulderGenerator" },
        { "BungeeDrop", "game_object:bungee_drop" },
        { "BungeeHook", "game_object:bungee_hook" },
        { "BusStop", "game_object:BusStop" },
        { "CameraTweak", "game_object:Camera_Tweak" },
        { "FlythroughObject", "game_object:Flythrough" },
        { "Mindy", "Enemy:SB" },
        { "EnemySB", "Enemy:SB" },
        { "Critter", "Enemy:SB" },
        { "Dennis", "Enemy:SB" },
        { "Spawner", "Enemy:SB" },
        { "CastNCrew", "Enemy:SB" },
        { "FrogFish", "Enemy:SB" },
        { "Crate", "Enemy:SB" },
        { "Turret", "Enemy:SB" },
        { "Neptune", "Enemy:SB" },
        { "NPCSettingsObject", "game_object:NPCSettings" },
        { "RaceTimer", "game_object:RaceTimer" },
        { "Ring", "game_object:Ring" },
        { "RingControl", "game_object:RingControl" },
        { "TalkBox", "game_object:talk_box" },
        { "TaskBox", "game_object:task_box" },
        { "Taxi", "game_object:Taxi" },
        { "Teleport", "game_object:Teleport" },
        { "TextBox", "game_object:text_box" },
        { "Vent", "game_object:Vent" },
        { "VentType", "game_object:VentType" },
        { "HUDMeterFont", "hud:meter:font" },
        { "HUDMeterUnit", "hud:meter:unit" },
        { "HUDModel", "hud:model" },
        { "HUDText", "hud:text" },
        { "JSPExtraData", "JSP Extra Data" },
        { "LogicReference", "logic:reference" },
        { "Pointer", "pointer" },
        { "SceneProperties", "Scene Properties" },
    };

    public static bool TryGetDYNAParser(string typeName, out AbstractDYNAParser parser)
    {
        if (DYNAToParser.TryGetValue(typeName, out parser))
            return true;

        if (typeName.StartsWith("Enemy:SB:"))
            return DYNAToParser.TryGetValue("Enemy:SB", out parser);

        parser = null!;
        return false;
    }
}
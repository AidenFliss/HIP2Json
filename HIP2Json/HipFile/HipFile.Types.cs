using System;
using System.Collections.Generic;

namespace HIP2Json;

public enum AssetType
{
    Null,
    AnimationList,
    Animation,
    AnimationTable,
    AttackTable,
    BinkVideo,
    Boulder,
    BSP,
    Button,
    Camera,
    CameraCurve,
    Counter,
    CollisionTable,
    Conditional,
    Credits,
    Cutscene,
    CutsceneManager,
    CutsceneStreamingSound,
    CutsceneTableOfContents,
    Destructible,
    Dispatcher,
    DiscoFloor,
    DestructibleObject,
    DashTrack,
    Duplicator,
    ElectricArcGenerator,
    Environment,
    Flythrough,
    Fog,
    GrassMesh,
    Group,
    Gust,
    Hangable,
    JawDataTable,
    JSP,
    JSPInfo,
    Light,
    LightKit,
    LobMaster,
    LevelOfDetailTable,
    SurfaceMapper,
    ModelInfo,
    Model,
    MorphTarget,
    Marker,
    MovePoint,
    NavigationMesh,
    Villain,
    NPCSettings,
    OneLiner,
    ParticleEmitter,
    ParticleProperties,
    ParticleSystem,
    Pendulum,
    ProgressScript,
    PickupTable,
    PipeInfoTable,
    Pickup,
    Platform,
    Player,
    Portal,
    Projectile,
    ReactiveAnimation,
    RawImage,
    Texture,
    Script,
    SDFX,
    SFX,
    SoundGroup,
    ShadowTable,
    Shrapnel,
    SimpleObject,
    SlideProperty,
    Sound,
    SoundInfo,
    SoundStream,
    Spline,
    SplinePath,
    SceneSettings,
    Subtitles,
    Surface,
    TextureStream,
    Text,
    Timer,
    PickupTypes,
    Track,
    Trigger,
    ThrowableTable,
    UserInterface,
    UIFN,
    UserInterfaceFont,
    UserInterfaceMotion,
    NPC,
    NPCProperties,
    Volume,
    WireframeModel,
    ZipLine,
    AnalogDeflection,
    AnalogDirection,
    AudioConversation,
    Bomber,
    BossBrain,
    BossUnderminerDrill,
    BossUnderminerUM,
    BoulderGenerator,
    BulletMark,
    BulletTime,
    BungeeDrop,
    BungeeHook,
    BusStop,
    CameraBinaryPoi,
    CameraParamAsset,
    CameraPreset,
    CameraTransitionPath,
    CameraTransitionTime,
    CameraTweak,
    CarryableObject,
    CarryablePropertyGeneric,
    CarryablePropertyAttract,
    CarryablePropertyRepel,
    CarryablePropertySwipe,
    CastNCrew,
    Checkpoint,
    Chicken,
    Crate,
    Critter,
    DashCameraSpline,
    Dennis,
    Driller,
    Enemy,
    EnemyLeftArm,
    EnemyRightArm,
    EnemySB,
    EnemySwarmBug,
    EnemySwarmOwl,
    EnemyThief,
    EnemyWaiter,
    Enforcer,
    FlameEmitter,
    Flamethrower,
    FlythroughObject,
    FreezableObject,
    FrogFish,
    LogicFunctionGenerator,
    NPCGate,
    Grapple,
    Grass,
    HUDCompassObject,
    HUDCompassSystem,
    HangableObject,
    HUDImage,
    HUDMeterFont,
    HUDMeterUnit,
    HUDModel,
    HUDText,
    Humanoid,
    IncrediblesIcon,
    IncrediblesPickup,
    InteractionIceBridge,
    InteractionLaunch,
    InteractionLift,
    InteractionSwitchLever,
    InteractionTurn,
    InterestPointer,
    JSPExtraData,
    LaserBeam,
    LensFlareElement,
    LensFlareSource,
    LightEffect,
    LightEffectFlicker,
    LightEffectStrobe,
    Lightning,
    LogicMission,
    LogicReference,
    LogicTask,
    Mindy,
    NPCCoverPoint,
    NPCCustomAV,
    NPCGroup,
    NPCSettingsObject,
    Neptune,
    ParticleGenerator,
    Pointer,
    PoleSwing,
    PourWidget,
    RaceTimer,
    Rat,
    RbandCameraAsset,
    Ring,
    RingControl,
    RobotTank,
    RubbleGenerator,
    Rumble,
    RumbleBoxEmitter,
    RumbleSphericalEmitter,
    SceneProperties,
    Scientist,
    ScreenFade,
    ScreenWarp,
    Shooter,
    SmokeEmitter,
    SparkEmitter,
    Spawner,
    Splash,
    Spotlight,
    Springboard,
    TalkBox,
    TaskBox,
    Taxi,
    TeleportBox,
    TextBox,
    Tightrope,
    TrainCar,
    TrainJunction,
    Turret,
    TurretObject,
    Twiddler,
    UberLaser,
    UserInterfaceBox,
    UserInterfaceController,
    UserInterfaceImage,
    UserInterfaceModel,
    UserInterfaceText,
    UserInterfaceTextUserString,
    Vent,
    VentType,
    NPCWalls,
    WaterBody,
    WaterHose,
    Unknown_EBC04E7B,
}

public enum DynaType : uint
{
    Null = 0,
    audio__conversation = 0x3A335FCF,
    Checkpoint = 0x2DE7AB98,
    Effect__particle_generator = 0x4AF4ABC7,
    Incredibles__Icon = 0xD6093241,
    Interest_Pointer = 0x1F662B3C,
    JSPExtraData = 0x204D6ADB,
    SceneProperties = 0xFABDB3B3,
    pointer = 0x2196C135,
    camera__binary_poi = 0xFA0E4015,
    camera__preset = 0xCDAB9190,
    camera__transition_path = 0xBBA5036A,
    camera__transition_time = 0xBC304E86,
    effect__BossBrain = 0xDEC6DFF0,
    effect__Flamethrower = 0xFB1179F5,
    effect__LensFlareElement = 0x2CD29541,
    effect__LensFlareSource = 0xA072A4DA,
    effect__LightEffectFlicker = 0x53CE3CA4,
    effect__LightEffectStrobe = 0x96727F69,
    effect__Lightning = 0x94B8EF2D,
    effect__Rumble = 0x2A59443A,
    effect__RumbleBoxEmitter = 0x56F5D96F,
    effect__RumbleSphericalEmitter = 0x1337E641,
    effect__ScreenFade = 0x9535DB9D,
    effect__ScreenWarp = 0xC2783A7F,
    effect__Splash = 0xCDF6730C,
    effect__Waterhose = 0x03E64AEB,
    effect__grass = 0x081A3629,
    effect__light = 0x5EAB97E1,
    effect__smoke_emitter = 0x0903FBB9,
    effect__spark_emitter = 0xA7039867,
    effect__spotlight = 0x6AA8BF67,
    effect__uber_laser = 0xA866726F,
    effect__water_body = 0x90D4BA5B,
    Enemy__SB = 0x5B1CC119,
    Enemy__SB__BucketOTron = 0xD2D6A1E5,
    Enemy__SB__CastNCrew = 0x1F9D54BB,
    Enemy__SB__Critter = 0x45B73B62,
    Enemy__SB__Dennis = 0xCE41C144,
    Enemy__SB__FrogFish = 0x11FCF451,
    Enemy__SB__Mindy = 0xC92170B2,
    Enemy__SB__Neptune = 0xBE8C5CAC,
    Enemy__SB__Standard = 0x44EA147A,
    Enemy__SB__SupplyCrate = 0x495BFF9B,
    Enemy__SB__Turret = 0x9FEC1E09,
    Enemy__IN2__Bomber = 0xC6C76EEE,
    Enemy__IN2__BossUnderminerDrill = 0x4EE03B24,
    Enemy__IN2__BossUnderminerUM = 0xCDB57387,
    Enemy__IN2__Chicken = 0x460F4FB2,
    Enemy__IN2__Driller = 0xCF21DB89,
    Enemy__IN2__Enforcer = 0xE5D82D97,
    Enemy__IN2__Humanoid = 0x2743B85C,
    Enemy__IN2__Rat = 0x9F234F8E,
    Enemy__IN2__RobotTank = 0xAD7CB421,
    Enemy__IN2__Scientist = 0xE2301EA9,
    Enemy__IN2__Shooter = 0xFC2951C1,
    game_object__BoulderGenerator = 0xBB4864D8,
    game_object__BusStop = 0x8F012778,
    game_object__Camera_Tweak = 0x9092FB14,
    game_object__Flythrough = 0x85BFDF34,
    game_object__FreezableObject = 0x35D19631,
    game_object__Grapple = 0xE7928821,
    game_object__Hangable = 0x1D3C54EE,
    game_object__IN_Pickup = 0x832E4208,
    game_object__NPCSettings = 0x8768334A,
    game_object__RaceTimer = 0x844BCF76,
    game_object__Ring = 0x4D81C1EE,
    game_object__RingControl = 0x18028CA7,
    game_object__RubbleGenerator = 0x3D0D5121,
    game_object__Taxi = 0x4DC449FC,
    game_object__Teleport = 0x70ADB7F9,
    game_object__Turret = 0x798A7982,
    game_object__Vent = 0x4E09EC43,
    game_object__VentType = 0x5E5B5165,
    game_object__bullet_mark = 0x381232B4,
    game_object__bullet_time = 0x390467A4,
    game_object__bungee_drop = 0x574749A4,
    game_object__bungee_hook = 0x57CFB6F0,
    game_object__camera_param_asset = 0xE44DCEBA,
    game_object__dash_camera_spline = 0x571A5DBC,
    game_object__flame_emitter = 0xE6120704,
    game_object__laser_beam = 0xBBCB17C1,
    game_object__rband_camera_asset = 0x945F2E84,
    game_object__talk_box = 0x0934B196,
    game_object__task_box = 0xE9D2C1BB,
    game_object__text_box = 0x442E1337,
    game_object__train_car = 0xC279D693,
    game_object__train_junction = 0xEA7B28D9,
    hud__image = 0xB8DA553C,
    hud__meter__font = 0x8B3E732F,
    hud__meter__unit = 0x8D40B9AC,
    hud__model = 0xFF5691D2,
    hud__text = 0x687ED0B0,
    interaction__IceBridge = 0xF7E8697A,
    interaction__Launch = 0x4B03B4F7,
    interaction__Lift = 0x4C1F2B57,
    interaction__SwitchLever = 0x28478E46,
    interaction__Turn = 0x4D34C2B9,
    logic__FunctionGenerator = 0x4494F483,
    logic__reference = 0xF98698FF,
    npc__CoverPoint = 0x48C0D3A6,
    npc__NPC_Custom_AV = 0xFF7E4CFC,
    npc__group = 0x2326640A,
    ui__box = 0x8C2D107D,
    ui__controller = 0xE8753BAE,
    ui__image = 0x337BCB31,
    ui__model = 0x79F807C7,
    ui__text = 0xBD7646D7,
    ui__text__userstring = 0xFB50BACB,
    Unknown_EBC04E7B = 0xEBC04E7B,
    AnalogDeflection = 0x16B0A88D,
    AnalogDirection = 0xC0288F1F,
    Carrying_CarryableProperty_GenericUseProperty = 0x35F3B22A,
    Carrying_CarryableProperty_UsePropertyAttract = 0x45F261C6,
    Carrying_CarryableProperty_UsePropertyRepel = 0x0A21FFAD,
    Carrying_CarryableProperty_UsePropertySwipe = 0x1E175B3F,
    Carrying_CarryableObject = 0x284375FD,
    ContextObject_PoleSwing = 0xD9CA96BC,
    ContextObject_Springboard = 0x2D0D198B,
    ContextObject_Tightrope = 0x105DFF22,
    Enemy__NPC_Gate = 0x175ED698,
    Enemy__NPC_Walls = 0x0E612078,
    Enemy__RATS__LeftArm = 0xB34B0083,
    Enemy__RATS__RightArm = 0x89F5441A,
    Enemy__RATS__Swarm__Bug = 0x544AA34C,
    Enemy__RATS__Swarm__Owl = 0x544E0BCC,
    Enemy__RATS__Thief = 0xEF5FD10C,
    Enemy__RATS__Waiter = 0xF5B8CC9C,
    HUD_Compass_Object = 0x50B5E94C,
    HUD_Compass_System = 0xD3BB2158,
    logic__Mission = 0x890EB71C,
    logic__Task = 0x1D40CE5D,
    Pour_Widget = 0x2DDFA8F4,
    Twiddler = 0x01A49323,
}

internal static class HipTypes
{
    static readonly Dictionary<AssetType, string> CodeByName = new Dictionary<AssetType, string>
    {
        { AssetType.Null, "NULL" },
        { AssetType.AnimationList, "ALST" },
        { AssetType.Animation, "ANIM" },
        { AssetType.AnimationTable, "ATBL" },
        { AssetType.AttackTable, "ATKT" },
        { AssetType.BinkVideo, "BINK" },
        { AssetType.Boulder, "BOUL" },
        { AssetType.BSP, "BSP" },
        { AssetType.Button, "BUTN" },
        { AssetType.Camera, "CAM" },
        { AssetType.CameraCurve, "CCRV" },
        { AssetType.Counter, "CNTR" },
        { AssetType.CollisionTable, "COLL" },
        { AssetType.Conditional, "COND" },
        { AssetType.Credits, "CRDT" },
        { AssetType.Cutscene, "CSN" },
        { AssetType.CutsceneManager, "CSNM" },
        { AssetType.CutsceneStreamingSound, "CSSS" },
        { AssetType.CutsceneTableOfContents, "CTOC" },
        { AssetType.Destructible, "DEST" },
        { AssetType.Dispatcher, "DPAT" },
        { AssetType.DiscoFloor, "DSCO" },
        { AssetType.DestructibleObject, "DSTR" },
        { AssetType.DashTrack, "DTRK" },
        { AssetType.Duplicator, "DUPC" },
        { AssetType.ElectricArcGenerator, "EGEN" },
        { AssetType.Environment, "ENV" },
        { AssetType.Flythrough, "FLY" },
        { AssetType.Fog, "FOG" },
        { AssetType.GrassMesh, "GRSM" },
        { AssetType.Group, "GRUP" },
        { AssetType.Gust, "GUST" },
        { AssetType.Hangable, "HANG" },
        { AssetType.JawDataTable, "JAW" },
        { AssetType.JSP, "JSP" },
        { AssetType.JSPInfo, "JSP" },
        { AssetType.Light, "LITE" },
        { AssetType.LightKit, "LKIT" },
        { AssetType.LobMaster, "LOBM" },
        { AssetType.LevelOfDetailTable, "LODT" },
        { AssetType.SurfaceMapper, "MAPR" },
        { AssetType.ModelInfo, "MINF" },
        { AssetType.Model, "MODL" },
        { AssetType.MorphTarget, "MPHT" },
        { AssetType.Marker, "MRKR" },
        { AssetType.MovePoint, "MVPT" },
        { AssetType.NavigationMesh, "NGMS" },
        { AssetType.Villain, "NPC" },
        { AssetType.NPCSettings, "NPCS" },
        { AssetType.OneLiner, "ONEL" },
        { AssetType.ParticleEmitter, "PARE" },
        { AssetType.ParticleProperties, "PARP" },
        { AssetType.ParticleSystem, "PARS" },
        { AssetType.Pendulum, "PEND" },
        { AssetType.ProgressScript, "PGRS" },
        { AssetType.PickupTable, "PICK" },
        { AssetType.PipeInfoTable, "PIPT" },
        { AssetType.Pickup, "PKUP" },
        { AssetType.Platform, "PLAT" },
        { AssetType.Player, "PLYR" },
        { AssetType.Portal, "PORT" },
        { AssetType.Projectile, "PRJT" },
        { AssetType.ReactiveAnimation, "RANM" },
        { AssetType.RawImage, "RAW" },
        { AssetType.Texture, "RWTX" },
        { AssetType.Script, "SCRP" },
        { AssetType.SDFX, "SDFX" },
        { AssetType.SFX, "SFX" },
        { AssetType.SoundGroup, "SGRP" },
        { AssetType.ShadowTable, "SHDW" },
        { AssetType.Shrapnel, "SHRP" },
        { AssetType.SimpleObject, "SIMP" },
        { AssetType.SlideProperty, "SLID" },
        { AssetType.Sound, "SND" },
        { AssetType.SoundInfo, "SNDI" },
        { AssetType.SoundStream, "SNDS" },
        { AssetType.Spline, "SPLN" },
        { AssetType.SplinePath, "SPLP" },
        { AssetType.SceneSettings, "SSET" },
        { AssetType.Subtitles, "SUBT" },
        { AssetType.Surface, "SURF" },
        { AssetType.TextureStream, "TEXS" },
        { AssetType.Text, "TEXT" },
        { AssetType.Timer, "TIMR" },
        { AssetType.Track, "TRCK" },
        { AssetType.PickupTypes, "TPIK" },
        { AssetType.Trigger, "TRIG" },
        { AssetType.ThrowableTable, "TRWT" },
        { AssetType.UserInterface, "UI" },
        { AssetType.UIFN, "UIFN" },
        { AssetType.UserInterfaceFont, "UIFT" },
        { AssetType.UserInterfaceMotion, "UIM" },
        { AssetType.NPC, "VIL" },
        { AssetType.NPCProperties, "VILP" },
        { AssetType.Volume, "VOLU" },
        { AssetType.WireframeModel, "WIRE" },
        { AssetType.ZipLine, "ZLIN" },
    };

    static readonly Dictionary<DynaType, AssetType> AssetByDyna = new Dictionary<DynaType, AssetType>
    {
        { DynaType.AnalogDeflection, AssetType.AnalogDeflection },
        { DynaType.AnalogDirection, AssetType.AnalogDirection },
        { DynaType.Carrying_CarryableObject, AssetType.CarryableObject },
        { DynaType.Carrying_CarryableProperty_GenericUseProperty, AssetType.CarryablePropertyGeneric },
        { DynaType.Carrying_CarryableProperty_UsePropertyAttract, AssetType.CarryablePropertyAttract },
        { DynaType.Carrying_CarryableProperty_UsePropertyRepel, AssetType.CarryablePropertyRepel },
        { DynaType.Carrying_CarryableProperty_UsePropertySwipe, AssetType.CarryablePropertySwipe },
        { DynaType.Checkpoint, AssetType.Checkpoint },
        { DynaType.ContextObject_PoleSwing, AssetType.PoleSwing },
        { DynaType.ContextObject_Springboard, AssetType.Springboard },
        { DynaType.ContextObject_Tightrope, AssetType.Tightrope },
        { DynaType.Effect__particle_generator, AssetType.ParticleGenerator },
        { DynaType.Enemy__IN2__Bomber, AssetType.Bomber },
        { DynaType.Enemy__IN2__BossUnderminerDrill, AssetType.BossUnderminerDrill },
        { DynaType.Enemy__IN2__BossUnderminerUM, AssetType.BossUnderminerUM },
        { DynaType.Enemy__IN2__Chicken, AssetType.Chicken },
        { DynaType.Enemy__IN2__Driller, AssetType.Driller },
        { DynaType.Enemy__IN2__Enforcer, AssetType.Enforcer },
        { DynaType.Enemy__IN2__Humanoid, AssetType.Humanoid },
        { DynaType.Enemy__IN2__Rat, AssetType.Rat },
        { DynaType.Enemy__IN2__RobotTank, AssetType.RobotTank },
        { DynaType.Enemy__IN2__Scientist, AssetType.Scientist },
        { DynaType.Enemy__IN2__Shooter, AssetType.Shooter },
        { DynaType.Enemy__NPC_Gate, AssetType.NPCGate },
        { DynaType.Enemy__NPC_Walls, AssetType.NPCWalls },
        { DynaType.Enemy__RATS__LeftArm, AssetType.EnemyLeftArm },
        { DynaType.Enemy__RATS__RightArm, AssetType.EnemyRightArm },
        { DynaType.Enemy__RATS__Swarm__Bug, AssetType.EnemySwarmBug },
        { DynaType.Enemy__RATS__Swarm__Owl, AssetType.EnemySwarmOwl },
        { DynaType.Enemy__RATS__Thief, AssetType.EnemyThief },
        { DynaType.Enemy__RATS__Waiter, AssetType.EnemyWaiter },
        { DynaType.Enemy__SB, AssetType.EnemySB },
        { DynaType.Enemy__SB__BucketOTron, AssetType.Spawner },
        { DynaType.Enemy__SB__CastNCrew, AssetType.CastNCrew },
        { DynaType.Enemy__SB__Critter, AssetType.Critter },
        { DynaType.Enemy__SB__Dennis, AssetType.Dennis },
        { DynaType.Enemy__SB__FrogFish, AssetType.FrogFish },
        { DynaType.Enemy__SB__Mindy, AssetType.Mindy },
        { DynaType.Enemy__SB__Neptune, AssetType.Neptune },
        { DynaType.Enemy__SB__Standard, AssetType.Enemy },
        { DynaType.Enemy__SB__SupplyCrate, AssetType.Crate },
        { DynaType.Enemy__SB__Turret, AssetType.Turret },
        { DynaType.HUD_Compass_Object, AssetType.HUDCompassObject },
        { DynaType.HUD_Compass_System, AssetType.HUDCompassSystem },
        { DynaType.Incredibles__Icon, AssetType.IncrediblesIcon },
        { DynaType.Interest_Pointer, AssetType.InterestPointer },
        { DynaType.JSPExtraData, AssetType.JSPExtraData },
        { DynaType.Pour_Widget, AssetType.PourWidget },
        { DynaType.SceneProperties, AssetType.SceneProperties },
        { DynaType.Twiddler, AssetType.Twiddler },
        { DynaType.Unknown_EBC04E7B, AssetType.Unknown_EBC04E7B },
        { DynaType.audio__conversation, AssetType.AudioConversation },
        { DynaType.camera__binary_poi, AssetType.CameraBinaryPoi },
        { DynaType.camera__preset, AssetType.CameraPreset },
        { DynaType.camera__transition_path, AssetType.CameraTransitionPath },
        { DynaType.camera__transition_time, AssetType.CameraTransitionTime },
        { DynaType.effect__BossBrain, AssetType.BossBrain },
        { DynaType.effect__Flamethrower, AssetType.Flamethrower },
        { DynaType.effect__LensFlareElement, AssetType.LensFlareElement },
        { DynaType.effect__LensFlareSource, AssetType.LensFlareSource },
        { DynaType.effect__LightEffectFlicker, AssetType.LightEffectFlicker },
        { DynaType.effect__LightEffectStrobe, AssetType.LightEffectStrobe },
        { DynaType.effect__Lightning, AssetType.Lightning },
        { DynaType.effect__Rumble, AssetType.Rumble },
        { DynaType.effect__RumbleBoxEmitter, AssetType.RumbleBoxEmitter },
        { DynaType.effect__RumbleSphericalEmitter, AssetType.RumbleSphericalEmitter },
        { DynaType.effect__ScreenFade, AssetType.ScreenFade },
        { DynaType.effect__ScreenWarp, AssetType.ScreenWarp },
        { DynaType.effect__Splash, AssetType.Splash },
        { DynaType.effect__Waterhose, AssetType.WaterHose },
        { DynaType.effect__grass, AssetType.Grass },
        { DynaType.effect__light, AssetType.LightEffect },
        { DynaType.effect__smoke_emitter, AssetType.SmokeEmitter },
        { DynaType.effect__spark_emitter, AssetType.SparkEmitter },
        { DynaType.effect__spotlight, AssetType.Spotlight },
        { DynaType.effect__uber_laser, AssetType.UberLaser },
        { DynaType.effect__water_body, AssetType.WaterBody },
        { DynaType.game_object__BoulderGenerator, AssetType.BoulderGenerator },
        { DynaType.game_object__BusStop, AssetType.BusStop },
        { DynaType.game_object__Camera_Tweak, AssetType.CameraTweak },
        { DynaType.game_object__Flythrough, AssetType.FlythroughObject },
        { DynaType.game_object__FreezableObject, AssetType.FreezableObject },
        { DynaType.game_object__Grapple, AssetType.Grapple },
        { DynaType.game_object__Hangable, AssetType.HangableObject },
        { DynaType.game_object__IN_Pickup, AssetType.IncrediblesPickup },
        { DynaType.game_object__NPCSettings, AssetType.NPCSettingsObject },
        { DynaType.game_object__RaceTimer, AssetType.RaceTimer },
        { DynaType.game_object__Ring, AssetType.Ring },
        { DynaType.game_object__RingControl, AssetType.RingControl },
        { DynaType.game_object__RubbleGenerator, AssetType.RubbleGenerator },
        { DynaType.game_object__Taxi, AssetType.Taxi },
        { DynaType.game_object__Teleport, AssetType.TeleportBox },
        { DynaType.game_object__Turret, AssetType.TurretObject },
        { DynaType.game_object__Vent, AssetType.Vent },
        { DynaType.game_object__VentType, AssetType.VentType },
        { DynaType.game_object__bullet_mark, AssetType.BulletMark },
        { DynaType.game_object__bullet_time, AssetType.BulletTime },
        { DynaType.game_object__bungee_drop, AssetType.BungeeDrop },
        { DynaType.game_object__bungee_hook, AssetType.BungeeHook },
        { DynaType.game_object__camera_param_asset, AssetType.CameraParamAsset },
        { DynaType.game_object__dash_camera_spline, AssetType.DashCameraSpline },
        { DynaType.game_object__flame_emitter, AssetType.FlameEmitter },
        { DynaType.game_object__laser_beam, AssetType.LaserBeam },
        { DynaType.game_object__rband_camera_asset, AssetType.RbandCameraAsset },
        { DynaType.game_object__talk_box, AssetType.TalkBox },
        { DynaType.game_object__task_box, AssetType.TaskBox },
        { DynaType.game_object__text_box, AssetType.TextBox },
        { DynaType.game_object__train_car, AssetType.TrainCar },
        { DynaType.game_object__train_junction, AssetType.TrainJunction },
        { DynaType.hud__image, AssetType.HUDImage },
        { DynaType.hud__meter__font, AssetType.HUDMeterFont },
        { DynaType.hud__meter__unit, AssetType.HUDMeterUnit },
        { DynaType.hud__model, AssetType.HUDModel },
        { DynaType.hud__text, AssetType.HUDText },
        { DynaType.interaction__IceBridge, AssetType.InteractionIceBridge },
        { DynaType.interaction__Launch, AssetType.InteractionLaunch },
        { DynaType.interaction__Lift, AssetType.InteractionLift },
        { DynaType.interaction__SwitchLever, AssetType.InteractionSwitchLever },
        { DynaType.interaction__Turn, AssetType.InteractionTurn },
        { DynaType.logic__FunctionGenerator, AssetType.LogicFunctionGenerator },
        { DynaType.logic__Mission, AssetType.LogicMission },
        { DynaType.logic__Task, AssetType.LogicTask },
        { DynaType.logic__reference, AssetType.LogicReference },
        { DynaType.npc__CoverPoint, AssetType.NPCCoverPoint },
        { DynaType.npc__NPC_Custom_AV, AssetType.NPCCustomAV },
        { DynaType.npc__group, AssetType.NPCGroup },
        { DynaType.pointer, AssetType.Pointer },
        { DynaType.ui__box, AssetType.UserInterfaceBox },
        { DynaType.ui__controller, AssetType.UserInterfaceController },
        { DynaType.ui__image, AssetType.UserInterfaceImage },
        { DynaType.ui__model, AssetType.UserInterfaceModel },
        { DynaType.ui__text, AssetType.UserInterfaceText },
        { DynaType.ui__text__userstring, AssetType.UserInterfaceTextUserString },
    };

    public static string GetCode(this AssetType assetType)
    {
        if (CodeByName.TryGetValue(assetType, out string code))
            return code;
        return "DYNA";
    }

    public static bool IsDyna(this AssetType assetType) => GetCode(assetType) == "DYNA";

    public static AssetType ToAssetType(DynaType dynaType)
    {
        if (AssetByDyna.TryGetValue(dynaType, out AssetType assetType))
            return assetType;
        throw new Exception("Unknown DYNA type: " + ((uint)dynaType).ToString("X8"));
    }

    public static AssetType AssetTypeFromCode(string code, Platform platform, byte[] data)
    {
        switch (code.Trim())
        {
            case "ALST": return AssetType.AnimationList;
            case "ANIM": return AssetType.Animation;
            case "ATBL": return AssetType.AnimationTable;
            case "ATKT": return AssetType.AttackTable;
            case "BINK": return AssetType.BinkVideo;
            case "BOUL": return AssetType.Boulder;
            case "BSP": return AssetType.BSP;
            case "BUTN": return AssetType.Button;
            case "CAM": return AssetType.Camera;
            case "CCRV": return AssetType.CameraCurve;
            case "CNTR": return AssetType.Counter;
            case "COLL": return AssetType.CollisionTable;
            case "COND": return AssetType.Conditional;
            case "CRDT": return AssetType.Credits;
            case "CSN": return AssetType.Cutscene;
            case "CSNM": return AssetType.CutsceneManager;
            case "CSSS": return AssetType.CutsceneStreamingSound;
            case "CTOC": return AssetType.CutsceneTableOfContents;
            case "DEST": return AssetType.Destructible;
            case "DPAT": return AssetType.Dispatcher;
            case "DSCO": return AssetType.DiscoFloor;
            case "DSTR": return AssetType.DestructibleObject;
            case "DTRK": return AssetType.DashTrack;
            case "DUPC": return AssetType.Duplicator;
            case "DYNA": return GetDynaType(platform, data);
            case "EGEN": return AssetType.ElectricArcGenerator;
            case "ENV": return AssetType.Environment;
            case "FLY": return AssetType.Flythrough;
            case "FOG": return AssetType.Fog;
            case "GRSM": return AssetType.GrassMesh;
            case "GRUP": return AssetType.Group;
            case "GUST": return AssetType.Gust;
            case "HANG": return AssetType.Hangable;
            case "JAW": return AssetType.JawDataTable;
            case "JSP": return AssetType.JSP;
            case "LITE": return AssetType.Light;
            case "LKIT": return AssetType.LightKit;
            case "LOBM": return AssetType.LobMaster;
            case "LODT": return AssetType.LevelOfDetailTable;
            case "MAPR": return AssetType.SurfaceMapper;
            case "MINF": return AssetType.ModelInfo;
            case "MODL": return AssetType.Model;
            case "MPHT": return AssetType.MorphTarget;
            case "MRKR": return AssetType.Marker;
            case "MVPT": return AssetType.MovePoint;
            case "NGMS": return AssetType.NavigationMesh;
            case "NPC": return AssetType.Villain;
            case "NPCS": return AssetType.NPCSettings;
            case "ONEL": return AssetType.OneLiner;
            case "PARE": return AssetType.ParticleEmitter;
            case "PARP": return AssetType.ParticleProperties;
            case "PARS": return AssetType.ParticleSystem;
            case "PEND": return AssetType.Pendulum;
            case "PGRS": return AssetType.ProgressScript;
            case "PICK": return AssetType.PickupTable;
            case "PIPT": return AssetType.PipeInfoTable;
            case "PKUP": return AssetType.Pickup;
            case "PLAT": return AssetType.Platform;
            case "PLYR": return AssetType.Player;
            case "PORT": return AssetType.Portal;
            case "PRJT": return AssetType.Projectile;
            case "RANM": return AssetType.ReactiveAnimation;
            case "RAW": return AssetType.RawImage;
            case "RWTX": return AssetType.Texture;
            case "SCRP": return AssetType.Script;
            case "SDFX": return AssetType.SDFX;
            case "SFX": return AssetType.SFX;
            case "SGRP": return AssetType.SoundGroup;
            case "SHDW": return AssetType.ShadowTable;
            case "SHRP": return AssetType.Shrapnel;
            case "SIMP": return AssetType.SimpleObject;
            case "SLID": return AssetType.SlideProperty;
            case "SND": return AssetType.Sound;
            case "SNDI": return AssetType.SoundInfo;
            case "SNDS": return AssetType.SoundStream;
            case "SPLN": return AssetType.Spline;
            case "SPLP": return AssetType.SplinePath;
            case "SSET": return AssetType.SceneSettings;
            case "SUBT": return AssetType.Subtitles;
            case "SURF": return AssetType.Surface;
            case "TEXS": return AssetType.TextureStream;
            case "TEXT": return AssetType.Text;
            case "TIMR": return AssetType.Timer;
            case "TPIK": return AssetType.PickupTypes;
            case "TRCK": return AssetType.Track;
            case "TRIG": return AssetType.Trigger;
            case "TRWT": return AssetType.ThrowableTable;
            case "UI": return AssetType.UserInterface;
            case "UIFN": return AssetType.UIFN;
            case "UIFT": return AssetType.UserInterfaceFont;
            case "UIM": return AssetType.UserInterfaceMotion;
            case "VIL": return AssetType.NPC;
            case "VILP": return AssetType.NPCProperties;
            case "VOLU": return AssetType.Volume;
            case "WIRE": return AssetType.WireframeModel;
            case "ZLIN": return AssetType.ZipLine;
            default:
                throw new Exception("Unknown asset type: " + code);
        }
    }

    static AssetType GetDynaType(Platform platform, byte[] data)
    {
        uint hash = platform == Platform.GameCube
            ? (uint)(data[0x8] << 24 | data[0x9] << 16 | data[0xA] << 8 | data[0xB])
            : BitConverter.ToUInt32(data, 8);
        return ToAssetType((DynaType)hash);
    }

    public static int GetCompareValue(Section_AHDR ahdr, Game game)
    {
        switch (ahdr.assetType)
        {
            case AssetType.Texture: return 1;
            case AssetType.TextureStream: return 1;
            case AssetType.BinkVideo: return 2;
            case AssetType.WireframeModel: return 3;
            case AssetType.BSP: return 4;
            case AssetType.JSP: return 5;
            case AssetType.JSPInfo: return 6;
            case AssetType.Model: return 7;
            case AssetType.Player: return 10;
            case AssetType.Villain: return 15;
            case AssetType.NPC: return 20;
            case AssetType.NPCProperties: return 30;
            case AssetType.Duplicator: return 35;
            case AssetType.Pickup: return 40;
            case AssetType.Trigger: return 50;
            case AssetType.Camera: return ahdr.ADBG.assetName == "STARTCAM" ? 90 : 100;
            case AssetType.CameraCurve: return 101;
            case AssetType.Spline: return 102;
            case AssetType.Environment: return 110;
            case AssetType.Timer: return 120;
            case AssetType.Portal: return 130;
            case AssetType.Text: return 131;
            case AssetType.Subtitles: return 132;
            case AssetType.MovePoint: return 160;
            case AssetType.Marker: return 170;
            case AssetType.Group: return 180;
            case AssetType.RawImage: return 190;
            case AssetType.Counter: return 200;
            case AssetType.Hangable: return 204;
            case AssetType.Pendulum: return 206;
            case AssetType.SFX: return 210;
            case AssetType.SDFX: return 215;
            case AssetType.Platform: return PlatformCompareValue(ahdr, game);
            case AssetType.Track: return 298;
            case AssetType.SimpleObject: return 299;
            case AssetType.Button: return 300;
            case AssetType.SlideProperty: return 304;
            case AssetType.ZipLine: return 305;
            case AssetType.Surface: return 310;
            case AssetType.DestructibleObject: return 320;
            case AssetType.Gust: return 321;
            case AssetType.Volume: return 322;
            case AssetType.Dispatcher: return 330;
            case AssetType.Conditional: return 340;
            case AssetType.UserInterface: return 350;
            case AssetType.UserInterfaceFont: return 360;
            case AssetType.Projectile: return 361;
            case AssetType.LobMaster: return 362;
            case AssetType.Fog: return 370;
            case AssetType.Light: return 375;
            case AssetType.ParticleProperties: return 380;
            case AssetType.ParticleEmitter: return 390;
            case AssetType.ParticleSystem: return 400;
            case AssetType.CutsceneManager: return 410;
            case AssetType.ElectricArcGenerator: return 420;
            case AssetType.AnimationList: return 430;
            case AssetType.Boulder: return 440;
            case AssetType.LightKit: return 450;
            case AssetType.AttackTable: return 451;
            case AssetType.NPCSettings: return 452;
            case AssetType.OneLiner: return 453;
            case AssetType.UserInterfaceMotion: return 454;
            case AssetType.Script: return 455;
            case AssetType.ProgressScript: return 456;
            case AssetType.SplinePath: return 457;
            case AssetType.Credits: return 460;
            case AssetType.DiscoFloor: return 470;
            case AssetType.AudioConversation: return 480;
            case AssetType.CameraBinaryPoi: return 481;
            case AssetType.CameraPreset: return 482;
            case AssetType.CameraTransitionPath: return 483;
            case AssetType.CameraTransitionTime: return 484;
            case AssetType.AnalogDeflection: return 485;
            case AssetType.AnalogDirection: return 486;
            case AssetType.CarryableObject: return 490;
            case AssetType.CarryablePropertyGeneric: return 491;
            case AssetType.CarryablePropertyAttract: return 492;
            case AssetType.CarryablePropertyRepel: return 493;
            case AssetType.CarryablePropertySwipe: return 494;
            case AssetType.Checkpoint: return 495;
            case AssetType.PoleSwing: return 496;
            case AssetType.Springboard: return 497;
            case AssetType.Tightrope: return 500;
            case AssetType.BossBrain: return 501;
            case AssetType.Flamethrower: return 502;
            case AssetType.Grass: return 503;
            case AssetType.LensFlareElement: return 504;
            case AssetType.LensFlareSource: return 505;
            case AssetType.LightEffect: return 506;
            case AssetType.LightEffectFlicker: return 507;
            case AssetType.LightEffectStrobe: return 508;
            case AssetType.Lightning: return 509;
            case AssetType.ParticleGenerator: return 510;
            case AssetType.Rumble: return 511;
            case AssetType.RumbleBoxEmitter: return 512;
            case AssetType.RumbleSphericalEmitter: return 513;
            case AssetType.ScreenFade: return 514;
            case AssetType.ScreenWarp: return 515;
            case AssetType.SmokeEmitter: return 516;
            case AssetType.SparkEmitter: return 517;
            case AssetType.NPCGate: return 518;
            case AssetType.NPCWalls: return 519;
            case AssetType.EnemySwarmBug: return 520;
            case AssetType.EnemySwarmOwl: return 521;
            case AssetType.EnemyThief: return 522;
            case AssetType.EnemyLeftArm: return 523;
            case AssetType.EnemyRightArm: return 524;
            case AssetType.EnemyWaiter: return 525;
            case AssetType.Splash: return 526;
            case AssetType.Spotlight: return 527;
            case AssetType.UberLaser: return 528;
            case AssetType.WaterHose: return 529;
            case AssetType.WaterBody: return 530;
            case AssetType.Bomber: return 531;
            case AssetType.BossUnderminerDrill: return 532;
            case AssetType.BossUnderminerUM: return 533;
            case AssetType.Chicken: return 534;
            case AssetType.Driller: return 535;
            case AssetType.Enforcer: return 536;
            case AssetType.Humanoid: return 537;
            case AssetType.Rat: return 538;
            case AssetType.RobotTank: return 539;
            case AssetType.Scientist: return 540;
            case AssetType.Shooter: return 541;
            case AssetType.EnemySB: return 542;
            case AssetType.Spawner: return 543;
            case AssetType.CastNCrew: return 544;
            case AssetType.Critter: return 545;
            case AssetType.Dennis: return 546;
            case AssetType.FrogFish: return 547;
            case AssetType.Mindy: return 548;
            case AssetType.Neptune: return 549;
            case AssetType.Enemy: return 550;
            case AssetType.Crate: return 551;
            case AssetType.Turret: return 552;
            case AssetType.BoulderGenerator: return 553;
            case AssetType.BulletMark: return 554;
            case AssetType.BulletTime: return 555;
            case AssetType.BungeeDrop: return 556;
            case AssetType.BungeeHook: return 557;
            case AssetType.BusStop: return 558;
            case AssetType.CameraParamAsset: return 559;
            case AssetType.CameraTweak: return 560;
            case AssetType.DashCameraSpline: return 561;
            case AssetType.FlameEmitter: return 562;
            case AssetType.FlythroughObject: return 563;
            case AssetType.FreezableObject: return 564;
            case AssetType.Grapple: return 565;
            case AssetType.HangableObject: return 566;
            case AssetType.IncrediblesPickup: return 567;
            case AssetType.LaserBeam: return 568;
            case AssetType.NPCSettingsObject: return 570;
            case AssetType.RaceTimer: return 571;
            case AssetType.RbandCameraAsset: return 572;
            case AssetType.Ring: return 573;
            case AssetType.RingControl: return 574;
            case AssetType.RubbleGenerator: return 575;
            case AssetType.TalkBox: return 576;
            case AssetType.TaskBox: return 577;
            case AssetType.Taxi: return 578;
            case AssetType.TeleportBox: return 579;
            case AssetType.TextBox: return 580;
            case AssetType.TrainCar: return 581;
            case AssetType.TrainJunction: return 582;
            case AssetType.TurretObject: return 583;
            case AssetType.Vent: return 584;
            case AssetType.VentType: return 585;
            case AssetType.HUDImage: return 586;
            case AssetType.HUDMeterFont: return 587;
            case AssetType.HUDMeterUnit: return 588;
            case AssetType.HUDModel: return 589;
            case AssetType.HUDText: return 590;
            case AssetType.HUDCompassObject: return 591;
            case AssetType.HUDCompassSystem: return 592;
            case AssetType.IncrediblesIcon: return 600;
            case AssetType.InteractionIceBridge: return 601;
            case AssetType.InteractionLaunch: return 602;
            case AssetType.InteractionLift: return 603;
            case AssetType.InteractionSwitchLever: return 604;
            case AssetType.InteractionTurn: return 605;
            case AssetType.InterestPointer: return 606;
            case AssetType.JSPExtraData: return 607;
            case AssetType.LogicFunctionGenerator: return 608;
            case AssetType.LogicMission: return 609;
            case AssetType.LogicReference: return 610;
            case AssetType.LogicTask: return 611;
            case AssetType.NPCCoverPoint: return 612;
            case AssetType.NPCGroup: return 613;
            case AssetType.NPCCustomAV: return 614;
            case AssetType.Pointer: return 615;
            case AssetType.PourWidget: return 616;
            case AssetType.SceneProperties: return 617;
            case AssetType.Twiddler: return 618;
            case AssetType.UserInterfaceBox: return 619;
            case AssetType.UserInterfaceController: return 620;
            case AssetType.UserInterfaceImage: return 621;
            case AssetType.UserInterfaceModel: return 621;
            case AssetType.UserInterfaceText: return 622;
            case AssetType.UserInterfaceTextUserString: return 623;
            case AssetType.Unknown_EBC04E7B: return 624;
            case AssetType.PickupTypes: return 642;
            case AssetType.ThrowableTable: return 643;
            case AssetType.ReactiveAnimation: return 644;
            case AssetType.SceneSettings: return 645;
            case AssetType.Flythrough: return 650;
            case AssetType.NavigationMesh: return 653;
            case AssetType.GrassMesh: return 654;
            case AssetType.MorphTarget: return 655;
            case AssetType.Animation: return 656;
            case AssetType.AnimationTable: return 660;
            case AssetType.Shrapnel: return 670;
            case AssetType.PickupTable: return 680;
            case AssetType.ModelInfo: return 690;
            case AssetType.SoundGroup: return 695;
            case AssetType.Destructible: return 699;
            case AssetType.LevelOfDetailTable: return 700;
            case AssetType.CollisionTable: return 710;
            case AssetType.ShadowTable: return 720;
            case AssetType.PipeInfoTable: return 730;
            case AssetType.JawDataTable: return 740;
            case AssetType.SurfaceMapper: return 750;
            case AssetType.Cutscene: return 755;
            case AssetType.Sound: return 760;
            case AssetType.SoundStream: return 770;
            case AssetType.CutsceneStreamingSound: return 780;
            case AssetType.SoundInfo: return 790;
            default: return 0;
        }
    }

    static int PlatformCompareValue(Section_AHDR ahdr, Game game)
    {
        byte type = ahdr.data[game == Game.BFBB ? 0x54 : 0x50];
        switch (type)
        {
            case 0: // ExtendRetract
            case 1: // Orbit
            case 2: // Spline
            case 3: // MovePoint
            case 13: // FullyManipulable
                return 150;
            case 6: return 220; // ConveyorBelt
            case 9: return 230; // BreakawayPlatform
            case 10: return 240; // Springboard
            case 11: return 250; // TeeterTotter
            case 4: return 260; // Mechanism
            case 12: return 270; // Paddle
            case 5: // Pendulum
            case 7: // FallingPlatform
            case 8: // FR
            default:
                return 0;
        }
    }
}
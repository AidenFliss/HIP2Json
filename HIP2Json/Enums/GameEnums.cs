using System;
using System.Text.Json.Serialization;

namespace HIP2Json;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BaseFlags : ushort
{
    None = 0,
    Enabled = 0x01,
    Persistent = 0x02,
    Valid = 0x04,
    VisibleDuringCutscenes = 0x08,
    ReceiveShadows = 0x10,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntFlags : byte
{
    None = 0,
    Visible = 0x01,
    Stackable = 0x02,
    Unused04 = 0x04,
    Unknown08 = 0x08,
    Unused10 = 0x10,
    Unused20 = 0x20,
    NoShadow = 0x40,
    Unused80 = 0x80,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntFlagsMore : byte
{
    None = 0,
    Unused01 = 0x01,
    PreciseCollision = 0x02,
    Unknown04 = 0x04,
    Grabbable = 0x08,
    Hittable = 0x10,
    AnimateCollision = 0x20,
    Unused40 = 0x40,
    LedgeGrab = 0x80,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MotionType : byte
{
    ExtendRetract = 0,
    Orbit = 1,
    Spline = 2,
    MovePoint = 3,
    Mechanism = 4,
    Pendulum = 5,
    None = 6,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MechanismType : byte
{
    Slide = 0,
    Rotate = 1,
    SlideAndRotate = 2,
    SlideThenRotate = 3,
    RotateThenSlide = 4,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Axis : byte
{
    X = 0,
    Y = 1,
    Z = 2,
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ButtonType
{
    Button = 0,
    PressurePlate = 1,
}

[Flags]
[JsonConverter(typeof(ButtonHitmaskConverter))]
public enum ButtonHitmask : uint
{
    None = 0,
    BubbleSpinSliding = 0x000001,
    BubbleBounce = 0x000002,
    BubbleBash = 0x000004,
    BoulderBubbleBowl = 0x000008,
    CruiseBubble = 0x000010,
    Bungee = 0x000020,
    ThrownEnemyTiki = 0x000040,
    ThrowFruit = 0x000080,
    PatrickSlam = 0x000100,
    Unknown = 0x000200,
    PressurePlatePlayerStand = 0x000400,
    PressurePlateEnemyStand = 0x000800,
    PressurePlateBoulderBubbleBowl = 0x001000,
    PressurePlateStoneTiki = 0x002000,
    SandyMeleeSliding = 0x004000,
    PatrickMeleeSliding = 0x008000,
    PressurePlateThrowFruit = 0x010000,
    PatrickCartwheel = 0x020000,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CamType : byte
{
    Follow = 0,
    Shoulder = 1,
    Static = 2,
    Path = 3,
    StaticFollow = 4,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransitionType
{
    None = 0,
    Interp1 = 1,
    Interp2 = 2,
    Interp3 = 3,
    Interp4 = 4,
    Linear = 5,
    Interp1Rev = 6,
    Interp2Rev = 7,
    Interp3Rev = 8,
    Interp4Rev = 9,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Operation : int
{
    EQUAL_TO = 0,
    GREATER_THAN = 1,
    LESS_THAN = 2,
    GREATER_THAN_OR_EQUAL_TO = 3,
    LESS_THAN_OR_EQUAL_TO = 4,
    NOT_EQUAL_TO = 5,
    UNKNOWN = 255,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionalVariableBFBB : uint
{
    SoundMode = 0x29600EB0,
    MusicVolume = 0x84D4A26D,
    SfxVolume = 0x1E0EEB55,
    MemoryCardAvailable = 0x42453758,
    VibrationEnabled = 0x3B93C93F,
    SceneLetter = 0x704D04A9,
    Room = 0x0B11B427,
    CurrentLevelCollectable = 0x9653DA31,
    PatsSocks = 0x18249056,
    TotalPatsSocks = 0xD1FEEEE2,
    ShinyObjects = 0xD6FCCFE7,
    GoldenSpatulas = 0xC7E0F71C,
    CurrentDate = 0x9482683D,
    CurrentHour = 0x950F49B7,
    CurrentMinute = 0xBD2884E7,
    CounterValue = 0x4329EFFD,
    IsEnabled = 0xA6956B3F,
    IsVisible = 0x1E42996C,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionalVariableTSSM : uint
{
    SoundMode = 0x29600EB0,
    MusicVolume = 0x84D4A26D,
    SfxVolume = 0x1E0EEB55,
    MemoryCardAvailable = 0x42453758,
    VibrationEnabled = 0x3B93C93F,
    SubtitlesEnabled = 0xD1A7DE2C,
    SceneLetter = 0x704D04A9,
    Room = 0x0B11B427,
    CurrentDate = 0x9482683D,
    CurrentHour = 0x950F49B7,
    CurrentMinute = 0xBD2884E7,
    CounterValue = 0x4329EFFD,
    IsEnabled = 0xA6956B3F,
    IsVisible = 0x1E42996C,
    TimerSecondsLeft = 0x6897B48B,
    TimerMillisecondsLeft = 0xF4FE2282,
    IsMnus = 0x649FA12A,
    DemoType = 0x0B9F22CF,
    GoofyGooberTokens = 0x43DD1E00,
    ManlinessPoints = 0xD8A29291,
    LevelTreasureChests = 0xFE31C583,
    PlayerCurrentHealth = 0x25CD9F4A,
    IsReferenceNull = 0x1F5BAA4D,
    AlwaysPortal = 0x5B85F809,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DiscoFloorFlags : uint
{
    None = 0,
    Loop = 0x1,
    Enabled = 0x2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupEventMode : short
{
    SendToAll = 0,
    SendToRandom = 1,
    SendSequential = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightType : byte
{
    Point = 0,
    Spot = 1,
    Point2 = 2,
    Point3 = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightEffect : byte
{
    None = 0,
    NoneAlt = 1,
    FlickerSlow = 2,
    Flicker = 3,
    FlickerErratic = 4,
    StrobeSlow = 5,
    Strobe = 6,
    StrobeFast = 7,
    DimSlow = 8,
    Dim = 9,
    DimFast = 10,
    HalfDimSlow = 11,
    HalfDim = 12,
    HalfDimFast = 13,
    RandomColorSlow = 14,
    RandomColor = 15,
    RandomColorFast = 16,
    Cauldron = 17,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LightFlags : uint
{
    None = 0,
    Environment = 0x08,
    On = 0x20,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OnType : byte
{
    Arena = 0,
    Zone = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EmitType : byte
{
    Point = 0,
    CircleEdge = 1,
    Circle = 2,
    RectEdge = 3,
    Rect = 4,
    Line = 5,
    Volume = 6,
    SphereEdge = 7,
    Sphere = 8,
    OffsetPoint = 9,
    SphereEdge2 = 10,
    SphereEdge3 = 11,
    VCylEdge = 12,
    OCircleEdge = 13,
    OCircle = 14,
    EntityBone = 15,
    EntityBound = 16,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PickupFlags : short //funky math stuff yay
{
    None = 0,
    ReappearAfterCollecting = 1 << 0,
    EnabledOnStart = 1 << 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PickupType : byte
{
    Artwork = 0x10,
    Underwear = 0x13,
    RedManlinessPoint = 0x17,
    YellowManlinessPoint = 0x5A,
    GreenManlinessPoint = 0xD9,
    BlueManlinessPoint = 0x4C,
    PurpleManlinessPointOrExtra = 0x8A,
    KrabbyPatty = 0xD1,
    GoofyGooberToken = 0xB7,
    Sock = 0x24,
    SteeringWheel = 0x27,
    Clue = 0x28,
    GoldenUnderwear = 0x2E,
    GreenShinyObject = 0x34,
    YellowShinyObject = 0x3B,
    RedShinyObject = 0x3E,
    SpongeBall = 0x40,
    Savepoint = 0x5C,
    Shovel = 0x80,
    BlueShinyObject = 0x81,
    Snackgate = 0x86,
    PowerCrystal = 0xBB,
    ScoobySnack = 0xBC,
    PurpleShinyObject = 0xCB,
    GoldenSpatula = 0xDD,
    ScoobySnackBox = 0xEC,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlatformType : byte
{
    ExtendRetract = 0,
    Orbit = 1,
    Spline = 2,
    MovePoint = 3,
    Mechanism = 4,
    Pendulum = 5,
    Conveyor = 6,
    Falling = 7,
    FR = 8,
    Breakaway = 9,
    Springboard = 10,
    Teeter = 11,
    Paddle = 12,
    FullyManipulable = 13,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SDFXFlags : int
{
    Normal = 0,
    Unknown1 = 1,
    Unknown2 = 2,
    Unknown3 = 3,
    PlayFromEntity = 4,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CollisionType : byte //weird bit stuff again, yay
{
    None = 0,
    Trigger = 1 << 0,
    Static = 1 << 1,
    Dynamic = 1 << 2,
    NPC = 1 << 3,
    Player = 1 << 4,
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
    Unknown40 = 0x40,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
enum TriggerType : byte
{
    Box = 0,
    Sphere = 1,
    Cylinder = 2,
    Unknown = 255,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UIMCommandType : uint
{
    Move = 0,
    Scale = 1,
    Rotate = 2,
    Opacity = 3,
    AbsoluteScale = 4,
    Brightness = 5,
    Color = 6,
    UVScroll = 7,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum en_spawnmode : int
{
    NME_SPAWNMODE_CONTINUOUS = 0,
    NME_SPAWNMODE_WAVES = 1,
    NME_SPAWNMODE_AMBUSHWAVE = 2,
    NME_SPAWNMODE_AMBUSHCONT = 3,
    NME_SPAWNMODE_NOMORE = 4,
    NME_SPAWNMODE_FORCE = 5,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MindyCharacter : uint
{
    Spongebob = 0,
    Patrick = 1,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EnemyFlags : uint
{
    None = 0,
    PrepareForScare = 0x01,
    Unknown02 = 0x02,
    WalkOnPLATs = 0x04,
    WalkOnSIMPs = 0x08,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BasisType : uint
{
    None = 0,
    EvilRobot = 1,
    FriendlyRobot = 2,
    LovingCitizen = 3,
    GrumpyCitizen = 4,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DuploWaveMode : uint
{
    Continuous = 0,
    Discreet = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggerBoundsType : uint
{
    HalfSizeShadow = 0,
    FullSizeShadow = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextFont : uint
{
    Default = 0,
    Arial = 1,
    System = 2,
    Numbers = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextJustify
{
    Left = 0,
    Center = 1,
    Right = 2,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TextExpandMode
{
    Up = 0,
    Center = 1,
    Down = 2,
    Clip = 3,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackdropType : uint
{
    SolidColor = 0,
    Texture = 1,
    None = 100,
}

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VentFlags : uint
{
    None = 0,
    BreakBoulders = 0x1,
    Automatic = 0x2,
    DamageSpongeBall = 0x4,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PlayableCharacter
{
    Patrick = 0,
    Sandy = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MeterFillDirection : uint
{
    RightToLeft = 0,
    LeftToRight = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConstructFuncBFBB : uint
{
    zEntPlayer_AnimTable = 0,
    ZNPC_AnimTable_Common = 1,
    zPatrick_AnimTable = 2,
    zSandy_AnimTable = 3,
    ZNPC_AnimTable_Villager = 4,
    zSpongeBobTongue_AnimTable = 5,
    ZNPC_AnimTable_LassoGuide = 6,
    ZNPC_AnimTable_Hammer = 7,
    ZNPC_AnimTable_TarTar = 8,
    ZNPC_AnimTable_GLove = 9,
    ZNPC_AnimTable_Monsoon = 10,
    ZNPC_AnimTable_SleepyTime = 11,
    ZNPC_AnimTable_ArfDog = 12,
    ZNPC_AnimTable_ArfArf = 13,
    ZNPC_AnimTable_Chuck = 14,
    ZNPC_AnimTable_Tubelet = 15,
    ZNPC_AnimTable_Slick = 16,
    ZNPC_AnimTable_Ambient = 17,
    ZNPC_AnimTable_Tiki = 18,
    ZNPC_AnimTable_Fodder = 19,
    ZNPC_AnimTable_Duplotron = 20,
    ZNPC_AnimTable_Jelly = 21,
    ZNPC_AnimTable_Test = 22,
    ZNPC_AnimTable_Neptune = 23,
    ZNPC_AnimTable_KingJelly = 24,
    ZNPC_AnimTable_Dutchman = 25,
    ZNPC_AnimTable_Prawn = 26,
    ZNPC_AnimTable_BossSandy = 27,
    ZNPC_AnimTable_BossPatrick = 28,
    ZNPC_AnimTable_BossSB1 = 29,
    ZNPC_AnimTable_BossSB2 = 30,
    ZNPC_AnimTable_BossSBobbyArm = 31,
    ZNPC_AnimTable_BossPlankton = 32,
    zEntPlayer_BoulderVehicleAnimTable = 33,
    ZNPC_AnimTable_BossSandyHead = 34,
    ZNPC_AnimTable_BalloonBoy = 35,
    xEnt_AnimTable_AutoEventSmall = 36,
    ZNPC_AnimTable_SlickShield = 37,
    ZNPC_AnimTable_SuperFriend = 38,
    ZNPC_AnimTable_ThunderCloud = 39,
    XHUD_AnimTable_Idle = 40,
    ZNPC_AnimTable_NightLight = 41,
    ZNPC_AnimTable_HazardStd = 42,
    ZNPC_AnimTable_FloatDevice = 43,
    cruise_bubble__anim_table = 44,
    ZNPC_AnimTable_BossSandyScoreboard = 45,
    zEntPlayer_TreeDomeSBAnimTable = 46,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConstructFuncTSSM : uint
{
    CruiseBubble = 0,
    NME_TYPE_COMMON = 1,
    NME_TYPE_TIKI = 2,
    NME_TYPE_TIKI_WOOD = 3,
    NME_TYPE_TIKI_LOVEY = 4,
    NME_TYPE_TIKI_QUIET = 5,
    NME_TYPE_TIKI_THUNDER = 6,
    NME_TYPE_TIKI_STONE = 7,
    NME_TYPE_FIRST_SEE_TYPE = 8,
    NME_TYPE_CRITTER = 9,
    NME_TYPE_CRITBASIC = 10,
    NME_TYPE_CRITJELLY = 11,
    NME_TYPE_BUCKETJELLY = 12,
    NME_TYPE_TURRET = 13,
    NME_TYPE_TURBARREL = 14,
    NME_TYPE_TURBARREL_V1 = 15,
    NME_TYPE_TURBARREL_V2 = 16,
    NME_TYPE_TURBARREL_V3 = 17,
    NME_TYPE_TURSPIRAL = 18,
    NME_TYPE_TURPOPUP = 19,
    NME_TYPE_TURTURNER = 20,
    NME_TYPE_TURARTY = 21,
    NME_TYPE_TURTRACE = 22,
    NME_TYPE_STANDARD = 23,
    NME_TYPE_FOGGER = 24,
    NME_TYPE_FOGGER_V1 = 25,
    NME_TYPE_FOGGER_V2 = 26,
    NME_TYPE_FOGGER_V3 = 27,
    NME_TYPE_SLAMMER = 28,
    NME_TYPE_SLAMMER_V1 = 29,
    NME_TYPE_SLAMMER_V2 = 30,
    NME_TYPE_SLAMMER_V3 = 31,
    NME_TYPE_FLINGER = 32,
    NME_TYPE_FLINGER_V1 = 33,
    NME_TYPE_FLINGER_V2 = 34,
    NME_TYPE_FLINGER_V3 = 35,
    NME_TYPE_SPINNER = 36,
    NME_TYPE_SPINNER_V1 = 37,
    NME_TYPE_SPINNER_V2 = 38,
    NME_TYPE_SPINNER_V3 = 39,
    NME_TYPE_POPPER = 40,
    NME_TYPE_POPPER_V1 = 41,
    NME_TYPE_POPPER_V2 = 42,
    NME_TYPE_POPPER_V3 = 43,
    NME_TYPE_ZAPPER_V1 = 44,
    NME_TYPE_ZAPPER_V2 = 45,
    NME_TYPE_ZAPPER_V3 = 46,
    NME_TYPE_MERVYN = 47,
    NME_TYPE_MERVYN_V1 = 48,
    NME_TYPE_MERVYN_V2 = 49,
    NME_TYPE_MERVYN_V3 = 50,
    NME_TYPE_BUCKOTRON = 51,
    NME_TYPE_BUCKOTRON_V1 = 52,
    NME_TYPE_BUCKOTRON_V2 = 53,
    NME_TYPE_BUCKOTRON_V3 = 54,
    NME_TYPE_BUCKOTRON_V4 = 55,
    NME_TYPE_BUCKOTRON_V5 = 56,
    NME_TYPE_BUCKOTRON_V6 = 57,
    NME_TYPE_BUCKOTRON_V7 = 58,
    NME_TYPE_LAST_SEE_TYPE = 59,
    NME_TYPE_FROGFISH = 60,
    NME_TYPE_DENNIS = 61,
    NME_TYPE_DENNIS_V1 = 62,
    NME_TYPE_DENNIS_V2 = 63,
    NME_TYPE_NEPTUNE = 64,
    NME_TYPE_SBBAT = 65,
    NME_TYPE_TONGUESPIN = 66,
    NME_TYPE_MINDY = 67,
    NME_TYPE_NPC_PAT = 68,
    NME_TYPE_NPC_BOB = 69,
    PlayerCar = 70,
    PlayerPat = 71,
    PlayerSB = 72,
    PlayerSBTongue = 73,
    PlayerSlide = 74,
    SpongeBall = 75,
    xHUD = 76,
}
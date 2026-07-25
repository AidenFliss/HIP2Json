namespace HIP2Json;

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

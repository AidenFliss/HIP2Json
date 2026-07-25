namespace HIP2Json;

public struct xVec2
{
    public float x,
        y;

    public xVec2(float x, float y)
    {
        this.x = x;
        this.y = y;
    }

    public override string ToString() => $"({x}, {y})";
}

public struct xVec3
{
    public float x,
        y,
        z;

    public xVec3(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override string ToString() => $"({x}, {y}, {z})";
}

public struct xVec4
{
    public float x,
        y,
        z,
        w;

    public xVec4(float x, float y, float z, float w)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.w = w;
    }

    public override string ToString() => $"({x}, {y}, {z}, {w})";
}

public struct xColor
{
    public float r,
        g,
        b,
        a;

    public xColor(float r, float g, float b, float a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public override string ToString() => $"({r}, {g}, {b}, {a})";
}

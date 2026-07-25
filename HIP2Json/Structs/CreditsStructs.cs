namespace HIP2Json;

public class CRDTPreset
{
    public ushort num { get; set; }
    public ushort align { get; set; }
    public float delay { get; set; }
    public float innerspace { get; set; }
    public CRDTTextBox textStyle { get; set; }
    public CRDTTextBox backdropStyle { get; set; }
    public CRDTTexture textureFront { get; set; }
    public CRDTTexture textureBack { get; set; }
}

public class CRDTHunk
{
    public uint hunk_size { get; set; }
    public uint preset { get; set; }
    public float t0 { get; set; }
    public float t1 { get; set; }
    public string text1 { get; set; }
}

public class CRDTTextBox
{
    public TextFont font { get; set; }
    public xColor color { get; set; }
    public float charWidth { get; set; }
    public float charHeight { get; set; }
    public float spacingX { get; set; }
    public float spacingY { get; set; }
    public float maxWidth { get; set; }
    public float maxHeight { get; set; }
}

public class CRDTTexture
{
    public uint textureAssetID { get; set; }
    public xColor color { get; set; }
    public float posX { get; set; }
    public float posY { get; set; }
    public float width { get; set; }
    public float height { get; set; }
    public uint texture { get; set; }
}

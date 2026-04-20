namespace HWKUltra.Vision.Abstractions
{
    /// <summary>
    /// RGB color used for defect visualization. Replaces legacy WD.AVI.Common.AVIColor.
    /// </summary>
    public readonly record struct VisionColor(byte R, byte G, byte B);
}

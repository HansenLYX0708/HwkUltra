namespace HWKUltra.Core
{
    /// <summary>
    /// Axis-aligned bounding box in integer pixel coordinates.
    /// Geometric primitive shared across Vision, TestRun reports, and any consumer
    /// that needs a rectangular region description.
    /// </summary>
    public record BoundingBox(int X1, int Y1, int X2, int Y2);
}

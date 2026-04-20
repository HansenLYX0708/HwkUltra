namespace HWKUltra.Core
{
    /// <summary>
    /// Immutable 3D point. Geometric primitive used by Vision algorithms
    /// (laser datum, slider center, row-bar center) and potentially Motion.
    /// </summary>
    public struct Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString() => $"({X}, {Y}, {Z})";
    }
}

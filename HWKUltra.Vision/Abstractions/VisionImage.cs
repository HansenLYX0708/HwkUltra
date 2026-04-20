namespace HWKUltra.Vision.Abstractions
{
    /// <summary>
    /// Unified image container for vision algorithms.
    /// Shape mirrors HWKUltra.Camera.Abstractions.CameraImageEventArgs
    /// so a camera grab can be passed directly to vision code.
    /// </summary>
    public sealed class VisionImage
    {
        public byte[] Data { get; }
        public int Width { get; }
        public int Height { get; }
        public bool IsColor { get; }

        public VisionImage(byte[] data, int width, int height, bool isColor)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
            IsColor = isColor;
        }
    }
}

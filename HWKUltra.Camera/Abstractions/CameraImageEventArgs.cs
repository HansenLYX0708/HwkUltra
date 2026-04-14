namespace HWKUltra.Camera.Abstractions
{
    /// <summary>
    /// Event arguments for camera image grabbed events.
    /// </summary>
    public class CameraImageEventArgs : EventArgs
    {
        /// <summary>
        /// Logical camera name that produced this image.
        /// </summary>
        public string CameraName { get; }

        /// <summary>
        /// Image width in pixels.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Image height in pixels.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Raw image data bytes.
        /// </summary>
        public byte[] ImageData { get; }

        /// <summary>
        /// Whether the image is color (true) or monochrome (false).
        /// </summary>
        public bool IsColor { get; }

        /// <summary>
        /// Timestamp when the image was grabbed.
        /// </summary>
        public DateTime Timestamp { get; }

        public CameraImageEventArgs(
            string cameraName, int width, int height,
            byte[] imageData, bool isColor)
        {
            CameraName = cameraName;
            Width = width;
            Height = height;
            ImageData = imageData;
            IsColor = isColor;
            Timestamp = DateTime.Now;
        }
    }
}

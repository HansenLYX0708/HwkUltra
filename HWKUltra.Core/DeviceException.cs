namespace HWKUltra.Core
{
    public abstract class DeviceException : Exception
    {
        public string DeviceName { get; }

        protected DeviceException(string device, string message, Exception inner = null)
            : base(message, inner)
        {
            DeviceName = device;
        }
    }

    public class MotionException : DeviceException
    {
        public string Axis { get; }

        public MotionException(string axis, string message, Exception inner = null)
            : base("Motion", message, inner)
        {
            Axis = axis;
        }
    }

    public class CameraException : DeviceException
    {
        public string CameraId { get; }

        public CameraException(string id, string message, Exception inner = null)
            : base("Camera", message, inner)
        {
            CameraId = id;
        }
    }
}

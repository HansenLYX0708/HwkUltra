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

    public class IODeviceException : DeviceException
    {
        public string PointName { get; }

        public IODeviceException(string pointName, string message, Exception inner = null)
            : base("IO", message, inner)
        {
            PointName = pointName;
        }
    }

    public class LightSourceException : DeviceException
    {
        public string ChannelName { get; }

        public LightSourceException(string channelName, string message, Exception inner = null)
            : base("LightSource", message, inner)
        {
            ChannelName = channelName;
        }
    }

    public class AutoFocusException : DeviceException
    {
        public string InstanceName { get; }

        public AutoFocusException(string instanceName, string message, Exception inner = null)
            : base("AutoFocus", message, inner)
        {
            InstanceName = instanceName;
        }
    }

    public class MeasurementException : DeviceException
    {
        public string InstanceName { get; }

        public MeasurementException(string instanceName, string message, Exception inner = null)
            : base("Measurement", message, inner)
        {
            InstanceName = instanceName;
        }
    }
}

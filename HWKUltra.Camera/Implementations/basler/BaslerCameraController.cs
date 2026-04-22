using System.Diagnostics;
using System.Runtime.InteropServices;
using Basler.Pylon;
using HWKUltra.Camera.Abstractions;
using HWKUltra.Core;

namespace HWKUltra.Camera.Implementations.basler
{
    /// <summary>
    /// Basler Pylon camera controller implementation.
    /// Manages multiple Basler cameras by logical name, preserving original AVICamera functionality.
    /// </summary>
    public class BaslerCameraController : ICameraController
    {
        private readonly BaslerCameraControllerConfig _config;
        private readonly Dictionary<string, CameraConfig> _cameraConfigs;
        private readonly Dictionary<string, Basler.Pylon.Camera> _cameras = new();
        private readonly Dictionary<string, PixelDataConverter> _converters = new();
        private readonly Dictionary<string, IntPtr> _frameAddresses = new();
        private readonly Dictionary<string, Stopwatch> _stopwatches = new();
        private readonly Dictionary<string, bool> _isColorFlags = new();
        private readonly Dictionary<string, int> _imageWidths = new();
        private readonly Dictionary<string, int> _imageHeights = new();
        private bool _isOpen;

        private static readonly Version Sfnc2_0_0 = new Version(2, 0, 0);

        /// <summary>
        /// Event raised when an image is grabbed from any camera.
        /// </summary>
        public event EventHandler<CameraImageEventArgs>? ImageGrabbed;

        public BaslerCameraController(BaslerCameraControllerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _cameraConfigs = config.Cameras.ToDictionary(c => c.Name, c => c);
        }

        /// <summary>
        /// Open and initialize all configured cameras.
        /// </summary>
        public void Open()
        {
            if (_isOpen) return;

            var allDeviceInfos = EnumerateDevices();

            foreach (var camConfig in _config.Cameras)
            {
                var camera = FindAndCreateCamera(camConfig, allDeviceInfos);
                _cameras[camConfig.Name] = camera;
                _converters[camConfig.Name] = new PixelDataConverter();
                _frameAddresses[camConfig.Name] = IntPtr.Zero;
                _stopwatches[camConfig.Name] = new Stopwatch();
                _isColorFlags[camConfig.Name] = false;

                OpenSingleCamera(camConfig.Name, camConfig);
            }

            _isOpen = true;
        }

        /// <summary>
        /// Close all cameras and release resources.
        /// </summary>
        public void Close()
        {
            foreach (var kvp in _cameras)
            {
                try
                {
                    var name = kvp.Key;
                    var camera = kvp.Value;
                    camera.Close();

                    if (_frameAddresses.TryGetValue(name, out var addr) && addr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(addr);
                        _frameAddresses[name] = IntPtr.Zero;
                    }
                }
                catch { }
            }

            _cameras.Clear();
            _converters.Clear();
            _stopwatches.Clear();
            _isOpen = false;
        }

        /// <summary>
        /// Grab a single frame from the specified camera.
        /// </summary>
        public bool GrabOne(string cameraName)
        {
            var camera = GetCamera(cameraName);
            try
            {
                if (camera.StreamGrabber.IsGrabbing)
                    return false;

                camera.Parameters[PLCamera.AcquisitionMode].SetValue("SingleFrame");
                camera.StreamGrabber.Start(1, GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
                _stopwatches[cameraName].Restart();
                return true;
            }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"GrabOne failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Start continuous grabbing on the specified camera.
        /// </summary>
        public bool StartGrabbing(string cameraName)
        {
            var camera = GetCamera(cameraName);
            try
            {
                if (camera.StreamGrabber.IsGrabbing)
                    return false;

                camera.Parameters[PLCamera.AcquisitionMode].SetValue(PLCamera.AcquisitionMode.Continuous);
                camera.StreamGrabber.Start(GrabStrategy.OneByOne, GrabLoop.ProvidedByStreamGrabber);
                _stopwatches[cameraName].Restart();
                return true;
            }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"StartGrabbing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Stop continuous grabbing on the specified camera.
        /// </summary>
        public void StopGrabbing(string cameraName)
        {
            var camera = GetCamera(cameraName);
            try
            {
                if (camera.StreamGrabber.IsGrabbing)
                    camera.StreamGrabber.Stop();
            }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"StopGrabbing failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set exposure time for the specified camera.
        /// </summary>
        public void SetExposureTime(string cameraName, long value)
        {
            var camera = GetCamera(cameraName);
            try
            {
                camera.Parameters[PLCamera.ExposureAuto].TrySetValue(PLCamera.ExposureAuto.Off);
                camera.Parameters[PLCamera.ExposureMode].TrySetValue(PLCamera.ExposureMode.Timed);

                if (camera.GetSfncVersion() < Sfnc2_0_0)
                {
                    long min = camera.Parameters[PLCamera.ExposureTimeRaw].GetMinimum();
                    long max = camera.Parameters[PLCamera.ExposureTimeRaw].GetMaximum();
                    long incr = camera.Parameters[PLCamera.ExposureTimeRaw].GetIncrement();
                    value = ClampWithIncrement(value, min, max, incr);
                    camera.Parameters[PLCamera.ExposureTimeRaw].SetValue(value);
                }
                else
                {
                    camera.Parameters[PLUsbCamera.ExposureTime].SetValue((double)value);
                }
            }
            catch (CameraException) { throw; }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"SetExposureTime failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current exposure time for the specified camera.
        /// </summary>
        public long GetExposureTime(string cameraName)
        {
            var camera = GetCamera(cameraName);
            try
            {
                camera.Parameters[PLCamera.ExposureAuto].TrySetValue(PLCamera.ExposureAuto.Off);
                camera.Parameters[PLCamera.ExposureMode].TrySetValue(PLCamera.ExposureMode.Timed);
                return (long)camera.Parameters[PLUsbCamera.ExposureTime].GetValue();
            }
            catch (CameraException) { throw; }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"GetExposureTime failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set gain for the specified camera.
        /// </summary>
        public void SetGain(string cameraName, long value)
        {
            var camera = GetCamera(cameraName);
            try
            {
                camera.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Off);

                if (camera.GetSfncVersion() < Sfnc2_0_0)
                {
                    long min = camera.Parameters[PLCamera.GainRaw].GetMinimum();
                    long max = camera.Parameters[PLCamera.GainRaw].GetMaximum();
                    long incr = camera.Parameters[PLCamera.GainRaw].GetIncrement();
                    value = ClampWithIncrement(value, min, max, incr);
                    camera.Parameters[PLCamera.GainRaw].SetValue(value);
                }
                else
                {
                    camera.Parameters[PLUsbCamera.Gain].SetValue(value);
                }
            }
            catch (CameraException) { throw; }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"SetGain failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get current gain for the specified camera.
        /// </summary>
        public long GetGain(string cameraName)
        {
            var camera = GetCamera(cameraName);
            try
            {
                camera.Parameters[PLCamera.GainAuto].TrySetValue(PLCamera.GainAuto.Off);
                return (long)camera.Parameters[PLUsbCamera.Gain].GetValue();
            }
            catch (CameraException) { throw; }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"GetGain failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Set trigger mode for the specified camera.
        /// </summary>
        public void SetTriggerMode(string cameraName, CameraTriggerMode mode)
        {
            var camera = GetCamera(cameraName);
            try
            {
                switch (mode)
                {
                    case CameraTriggerMode.Freerun:
                        SetFreerun(camera);
                        break;
                    case CameraTriggerMode.ExternalHardware:
                        SetExternTrigger(camera);
                        break;
                    case CameraTriggerMode.Software:
                        SetSoftwareTrigger(camera);
                        break;
                    default:
                        throw new ArgumentException($"Unknown trigger mode: {mode}");
                }
                _stopwatches[cameraName].Reset();
            }
            catch (CameraException) { throw; }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"SetTriggerMode failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Send a software trigger to the specified camera.
        /// </summary>
        public void SendSoftwareTrigger(string cameraName)
        {
            var camera = GetCamera(cameraName);
            try
            {
                if (camera.WaitForFrameTriggerReady(1000, TimeoutHandling.ThrowException))
                {
                    camera.ExecuteSoftwareTrigger();
                    _stopwatches[cameraName].Restart();
                }
            }
            catch (CameraException) { throw; }
            catch (Exception ex)
            {
                throw new CameraException(cameraName, $"SendSoftwareTrigger failed: {ex.Message}", ex);
            }
        }

        #region Internal helpers

        private List<ICameraInfo> EnumerateDevices()
        {
            List<ICameraInfo> devices;

            switch (_config.DeviceType.ToUpperInvariant())
            {
                case "USB":
                    devices = CameraFinder.Enumerate(DeviceType.Usb);
                    break;
                case "CXP":
                    devices = CameraFinder.Enumerate(DeviceType.BaslerGenTlCxpDeviceClass);
                    break;
                case "ALL":
                    devices = CameraFinder.Enumerate();
                    break;
                default: // "Auto"
                    devices = CameraFinder.Enumerate(DeviceType.Usb);
                    if (devices.Count == 0)
                        devices = CameraFinder.Enumerate(DeviceType.BaslerGenTlCxpDeviceClass);
                    break;
            }

            if (devices.Count == 0)
                throw new CameraException("", "No Basler cameras found on the system.");

            return devices;
        }

        private Basler.Pylon.Camera FindAndCreateCamera(CameraConfig camConfig, List<ICameraInfo> allDevices)
        {
            var deviceSNs = new List<string>();
            foreach (var info in allDevices)
            {
                var sn = info[CameraInfoKey.SerialNumber];
                deviceSNs.Add(sn);
                if (sn == camConfig.SerialNumber)
                    return new Basler.Pylon.Camera(camConfig.SerialNumber);
            }

            var available = string.Join(", ", deviceSNs);
            throw new CameraException(camConfig.Name,
                $"Camera serial number '{camConfig.SerialNumber}' not found. Available: [{available}]");
        }

        private void OpenSingleCamera(string cameraName, CameraConfig camConfig)
        {
            var camera = _cameras[cameraName];

            camera.Open();
            if (!camera.IsOpen)
                throw new CameraException(cameraName, "Failed to open camera.");

            camera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(_config.MaxBufferCount);
            camera.Parameters[PLCamera.Width].SetValue(camConfig.Width);
            camera.Parameters[PLCamera.Height].SetValue(camConfig.Height);

            // Offset mode
            if (camConfig.OffsetMode == 1)
            {
                long maxW = camera.Parameters[PLCamera.WidthMax].GetValue();
                long maxH = camera.Parameters[PLCamera.HeightMax].GetValue();
                camera.Parameters[PLCamera.OffsetX].SetValue((maxW - camConfig.Width) / 2);
                camera.Parameters[PLCamera.OffsetY].SetValue((maxH - camConfig.Height) / 2);
            }
            else
            {
                camera.Parameters[PLCamera.OffsetX].SetValue(0);
                camera.Parameters[PLCamera.OffsetY].SetValue(0);
            }

            // Set default exposure
            camera.Parameters[PLCamera.ExposureTime].SetValue(camConfig.DefaultExposure);

            _imageWidths[cameraName] = (int)camera.Parameters[PLCamera.Width].GetValue();
            _imageHeights[cameraName] = (int)camera.Parameters[PLCamera.Height].GetValue();

            // Subscribe to events
            camera.StreamGrabber.ImageGrabbed += (sender, e) => OnImageGrabbed(cameraName, e);
            camera.ConnectionLost += (sender, e) => OnConnectionLost(cameraName);
        }

        private void OnImageGrabbed(string cameraName, ImageGrabbedEventArgs e)
        {
            try
            {
                IGrabResult grabResult = e.GrabResult;
                if (!grabResult.GrabSucceeded)
                    return;

                var converter = _converters[cameraName];
                int width = _imageWidths[cameraName];
                int height = _imageHeights[cameraName];
                long payloadSize = (long)width * height;
                long len = converter.GetBufferSizeForConversion(grabResult);
                byte[] imageByteData = new byte[len];
                bool isColor = false;

                if (grabResult.PixelTypeValue == PixelType.Mono8)
                {
                    if (_frameAddresses[cameraName] == IntPtr.Zero)
                        _frameAddresses[cameraName] = Marshal.AllocHGlobal((int)payloadSize);

                    converter.OutputPixelFormat = PixelType.Mono8;
                    converter.Convert(_frameAddresses[cameraName], payloadSize, grabResult);
                    converter.Convert<byte>(imageByteData, grabResult);
                    isColor = false;
                }
                else if (grabResult.PixelTypeValue == PixelType.BGR8packed)
                {
                    if (_frameAddresses[cameraName] == IntPtr.Zero)
                        _frameAddresses[cameraName] = Marshal.AllocHGlobal((int)payloadSize * 3);

                    converter.OutputPixelFormat = PixelType.BGR8packed;
                    converter.Convert(_frameAddresses[cameraName], payloadSize * 3, grabResult);
                    converter.Convert<byte>(imageByteData, grabResult);
                    isColor = true;
                }
                else
                {
                    return;
                }

                _isColorFlags[cameraName] = isColor;

                ImageGrabbed?.Invoke(this, new CameraImageEventArgs(
                    cameraName, width, height, imageByteData, isColor));
            }
            catch { }
            finally
            {
                e.DisposeGrabResultIfClone();
            }
        }

        private void OnConnectionLost(string cameraName)
        {
            try
            {
                if (!_cameras.TryGetValue(cameraName, out var camera))
                    return;

                Thread.Sleep(100);
                camera.Close();

                for (int i = 0; i < 1000; i++)
                {
                    try
                    {
                        camera.Open(20, TimeoutHandling.ThrowException);
                        if (camera.IsOpen) break;
                    }
                    catch { }
                }

                if (camera.IsOpen && camera.GetSfncVersion() < Sfnc2_0_0)
                {
                    camera.Parameters[PLGigECamera.GevHeartbeatTimeout].SetValue(5000);
                }

                _imageWidths[cameraName] = (int)camera.Parameters[PLCamera.Width].GetValue();
                _imageHeights[cameraName] = (int)camera.Parameters[PLCamera.Height].GetValue();
            }
            catch { }
        }

        private static void SetFreerun(Basler.Pylon.Camera camera)
        {
            if (camera.Parameters[PLCamera.TriggerMode].GetValue() == PLCamera.TriggerMode.Off)
                return;

            camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
        }

        private static void SetExternTrigger(Basler.Pylon.Camera camera)
        {
            camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
            camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
            camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
            camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Line1);
            camera.Parameters[PLCameraInstance.MaxNumBuffer].SetValue(50);
            camera.Parameters[PLCamera.TriggerDelay].SetValue(0);
            camera.Parameters[PLCamera.LineSelector].TrySetValue(PLCamera.LineSelector.Line1);
            camera.Parameters[PLCamera.LineMode].TrySetValue(PLCamera.LineMode.Input);
        }

        private static void SetSoftwareTrigger(Basler.Pylon.Camera camera)
        {
            if (camera.GetSfncVersion() < Sfnc2_0_0)
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.AcquisitionStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                }
            }
            else
            {
                if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart))
                {
                    if (camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart))
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.Off);
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                    else
                    {
                        camera.Parameters[PLCamera.TriggerSelector].TrySetValue(PLCamera.TriggerSelector.FrameBurstStart);
                        camera.Parameters[PLCamera.TriggerMode].TrySetValue(PLCamera.TriggerMode.On);
                        camera.Parameters[PLCamera.TriggerSource].TrySetValue(PLCamera.TriggerSource.Software);
                    }
                }
            }
        }

        private Basler.Pylon.Camera GetCamera(string cameraName)
        {
            if (!_cameras.TryGetValue(cameraName, out var camera))
                throw new CameraException(cameraName,
                    $"Camera not found: {cameraName}. Available: [{string.Join(", ", _cameras.Keys)}]");
            return camera;
        }

        private static long ClampWithIncrement(long value, long min, long max, long incr)
        {
            if (value < min) return min;
            if (value > max) return max;
            return min + (((value - min) / incr) * incr);
        }

        #endregion
    }
}

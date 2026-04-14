using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using HWKUltra.AutoFocus.Abstractions;
using HWKUltra.Core;

namespace HWKUltra.AutoFocus.Implementations.laf
{
    /// <summary>
    /// LAF auto focus controller - TCP-based communication with LAF motor controller.
    /// Preserves core functionality from the original LAFMotorLibrary while supporting
    /// multi-instance, configurable IP/port, and status monitoring.
    /// </summary>
    public class LafAutoFocusController : IAutoFocusController, IDisposable
    {
        private readonly LafAutoFocusControllerConfig _config;
        private readonly Dictionary<string, LafInstance> _instances = new();
        private bool _disposed;

        public event EventHandler<AutoFocusStatusEventArgs>? StatusChanged;

        public LafAutoFocusController(LafAutoFocusControllerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Open connections to all configured AF instances.
        /// </summary>
        public void Open()
        {
            foreach (var cfg in _config.Instances)
            {
                if (_instances.ContainsKey(cfg.Name))
                {
                    Console.WriteLine($"[AutoFocus] Instance '{cfg.Name}' already opened, skipping");
                    continue;
                }

                var instance = new LafInstance(cfg);
                if (instance.Connect())
                {
                    _instances[cfg.Name] = instance;
                    Console.WriteLine($"[AutoFocus] Instance '{cfg.Name}' connected to {cfg.IpAddress}:{cfg.Port}");
                    RaiseStatusChanged(cfg.Name, instance);
                }
                else
                {
                    Console.WriteLine($"[AutoFocus] WARNING: Failed to connect instance '{cfg.Name}' to {cfg.IpAddress}:{cfg.Port}");
                }
            }
        }

        /// <summary>
        /// Close all AF connections and release resources.
        /// </summary>
        public void Close()
        {
            foreach (var kvp in _instances)
            {
                try
                {
                    kvp.Value.Disconnect();
                    Console.WriteLine($"[AutoFocus] Instance '{kvp.Key}' disconnected");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoFocus] Error closing instance '{kvp.Key}': {ex.Message}");
                }
            }
            _instances.Clear();
        }

        /// <summary>
        /// Enable auto focus tracking (tracklaser 1).
        /// </summary>
        public void EnableAutoFocus(string name)
        {
            var inst = GetInstance(name);
            var response = inst.SendCommand("tracklaser 1");
            inst.Status.IsAutoFocusEnabled = true;
            Console.WriteLine($"[AutoFocus] EnableAutoFocus '{name}': {response}");
            RaiseStatusChanged(name, inst);
        }

        /// <summary>
        /// Disable auto focus tracking (tracklaser 0).
        /// </summary>
        public void DisableAutoFocus(string name)
        {
            var inst = GetInstance(name);
            var response = inst.SendCommand("tracklaser 0");
            inst.Status.IsAutoFocusEnabled = false;
            Console.WriteLine($"[AutoFocus] DisableAutoFocus '{name}': {response}");
            RaiseStatusChanged(name, inst);
        }

        /// <summary>
        /// Turn laser on (lasergate 1).
        /// </summary>
        public void LaserOn(string name)
        {
            var inst = GetInstance(name);
            var response = inst.SendCommand("lasergate 1");
            inst.Status.IsLaserOn = true;
            Console.WriteLine($"[AutoFocus] LaserOn '{name}': {response}");
            RaiseStatusChanged(name, inst);
        }

        /// <summary>
        /// Turn laser off (lasergate 0).
        /// </summary>
        public void LaserOff(string name)
        {
            var inst = GetInstance(name);
            var response = inst.SendCommand("lasergate 0");
            inst.Status.IsLaserOn = false;
            Console.WriteLine($"[AutoFocus] LaserOff '{name}': {response}");
            RaiseStatusChanged(name, inst);
        }

        /// <summary>
        /// Get current focus value (st_focus).
        /// </summary>
        public double GetFocusValue(string name)
        {
            var inst = GetInstance(name);
            var response = inst.SendCommand("st_focus");
            if (double.TryParse(response, out double val))
            {
                inst.Status.FocusValue = val;
                return val;
            }
            return -9999;
        }

        /// <summary>
        /// Get current motor position (st_mpos).
        /// </summary>
        public double GetMotorPosition(string name)
        {
            var inst = GetInstance(name);
            var response = inst.SendCommand("st_mpos");
            if (double.TryParse(response, out double val))
            {
                inst.Status.MotorPosition = val;
                return val;
            }
            return -9999;
        }

        /// <summary>
        /// Reset axis to zero position (movegoto1 0).
        /// </summary>
        public void ResetAxis(string name)
        {
            var inst = GetInstance(name);
            var response = inst.SendCommand("movegoto1 0");
            Console.WriteLine($"[AutoFocus] ResetAxis '{name}': {response}");
        }

        /// <summary>
        /// Send a custom command to the specified instance.
        /// </summary>
        public string SendCommand(string name, string command)
        {
            var inst = GetInstance(name);
            return inst.SendCommand(command);
        }

        private LafInstance GetInstance(string name)
        {
            if (!_instances.TryGetValue(name, out var inst))
                throw new AutoFocusException(name,
                    $"Instance not found: {name}. Available: {string.Join(", ", _instances.Keys)}");
            if (!inst.Status.IsConnected)
                throw new AutoFocusException(name, $"Instance '{name}' is not connected");
            return inst;
        }

        private void RaiseStatusChanged(string name, LafInstance instance)
        {
            StatusChanged?.Invoke(this, new AutoFocusStatusEventArgs(name, instance.Status));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal class managing a single LAF TCP connection.
        /// Preserves the original LAFMotorLibrary async TCP communication pattern.
        /// </summary>
        private class LafInstance
        {
            private readonly AutoFocusConfig _config;
            private Socket? _socket;
            private readonly byte[] _buffer = new byte[4096];
            private string _received = "";
            private string? _response;
            private Regex _rx = new("");
            private readonly ManualResetEvent _waitHandle = new(false);
            private readonly ManualResetEvent _connected = new(false);

            public AutoFocusStatus Status { get; } = new();

            public LafInstance(AutoFocusConfig config)
            {
                _config = config;
            }

            /// <summary>
            /// Connect to the LAF controller via TCP.
            /// </summary>
            public bool Connect()
            {
                try
                {
                    var ipAddress = IPAddress.Parse(_config.IpAddress);
                    var remoteEP = new IPEndPoint(ipAddress, _config.Port);

                    _response = null;
                    _received = "";

                    _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    _connected.Reset();
                    _socket.BeginConnect(remoteEP, ConnectCallback, null);
                    if (!_connected.WaitOne(_config.TimeoutMs))
                    {
                        Status.IsConnected = false;
                        return false;
                    }

                    _rx = new Regex("(.*)\n");
                    _waitHandle.Reset();
                    BeginReceive();

                    if (_waitHandle.WaitOne(_config.TimeoutMs))
                    {
                        Console.WriteLine($"[AutoFocus] Connected to '{_response}'");
                        Status.IsConnected = true;
                        return true;
                    }

                    Status.IsConnected = false;
                    return false;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoFocus] Connection error: {ex.Message}");
                    Status.IsConnected = false;
                    return false;
                }
            }

            /// <summary>
            /// Disconnect from the LAF controller.
            /// </summary>
            public void Disconnect()
            {
                if (_socket != null)
                {
                    try
                    {
                        _socket.Shutdown(SocketShutdown.Both);
                    }
                    catch { }
                    finally
                    {
                        _socket.Dispose();
                        _socket = null;
                    }
                }
                Status.IsConnected = false;
                Status.IsAutoFocusEnabled = false;
                Status.IsLaserOn = false;
            }

            /// <summary>
            /// Send a command and wait for response.
            /// </summary>
            public string SendCommand(string command)
            {
                if (_socket == null || !Status.IsConnected)
                    throw new InvalidOperationException("Not connected");

                _received = "";
                _response = null;

                _rx = new Regex(Regex.Escape(command) + " ?(.*)\n");

                _waitHandle.Reset();
                Send(command + "\n");

                if (_waitHandle.WaitOne(_config.TimeoutMs))
                {
                    return _response ?? "";
                }

                throw new TimeoutException($"Command '{command}' timed out after {_config.TimeoutMs}ms");
            }

            private void BeginReceive()
            {
                _socket?.BeginReceive(_buffer, 0, _buffer.Length, 0, ReceiveCallback, _socket);
            }

            private void ConnectCallback(IAsyncResult ar)
            {
                try
                {
                    _socket?.EndConnect(ar);
                    _connected.Set();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoFocus] ConnectCallback error: {ex.Message}");
                }
            }

            private void ReceiveCallback(IAsyncResult ar)
            {
                try
                {
                    if (_socket == null) return;
                    int bytesRead = _socket.EndReceive(ar);
                    if (bytesRead > 0)
                    {
                        _received += Encoding.ASCII.GetString(_buffer, 0, bytesRead);
                        Match match = _rx.Match(_received);
                        if (match.Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                _response = match.Groups[1].Value;
                            }
                            _waitHandle.Set();
                        }
                        BeginReceive();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Socket was closed, ignore
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoFocus] ReceiveCallback error: {ex.Message}");
                }
            }

            private void Send(string data)
            {
                byte[] byteData = Encoding.ASCII.GetBytes(data);
                _socket?.BeginSend(byteData, 0, byteData.Length, 0, SendCallback, _socket);
            }

            private void SendCallback(IAsyncResult ar)
            {
                try
                {
                    _socket?.EndSend(ar);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoFocus] SendCallback error: {ex.Message}");
                }
            }
        }
    }
}

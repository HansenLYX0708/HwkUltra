using System.Net;
using System.Net.Sockets;
using System.Text;
using HWKUltra.Core;
using HWKUltra.LightSource.Abstractions;

namespace HWKUltra.LightSource.Implementations.ccs
{
    /// <summary>
    /// CCS light source controller implementation via TCP socket.
    /// Preserves the original CCS protocol commands (@00F, @00L, @00PM, etc.).
    /// </summary>
    public class CcsLightSourceController : ILightSourceController
    {
        private readonly CcsLightSourceControllerConfig _config;
        private readonly Dictionary<int, int> _intensities = new();
        private readonly Dictionary<int, bool> _onStates = new();
        private bool _isOpen;

        // CCS protocol constants
        private const string ResponseOk = "@00O\r\n";

        public CcsLightSourceController(CcsLightSourceControllerConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            foreach (var ch in config.Channels)
            {
                _intensities[ch.ChannelIndex] = 0;
                _onStates[ch.ChannelIndex] = false;
            }
        }

        public void Open()
        {
            _isOpen = true;
        }

        public void Close()
        {
            // Turn off all channels on close
            foreach (var ch in _config.Channels)
            {
                try { TurnOff(ch.ChannelIndex); } catch { }
            }
            _isOpen = false;
        }

        public void TurnOn(int channel)
        {
            EnsureOpen();
            var cmd = FormatCommand(channel, "L1");
            SendCommand(cmd);
            _onStates[channel] = true;
        }

        public void TurnOff(int channel)
        {
            EnsureOpen();
            var cmd = FormatCommand(channel, "L0");
            SendCommand(cmd);
            _onStates[channel] = false;
        }

        public void SetIntensity(int channel, int intensity)
        {
            EnsureOpen();
            var chConfig = GetChannelConfig(channel);
            intensity = Math.Clamp(intensity, 0, chConfig.MaxIntensity);
            var cmd = FormatCommand(channel, $"F{intensity:D4}");
            SendCommand(cmd);
            _intensities[channel] = intensity;
        }

        public void SetPulseMode(int channel, LightPulseMode mode)
        {
            EnsureOpen();
            var cmd = FormatCommand(channel, $"PM{(int)mode}");
            SendCommand(cmd);
        }

        public int GetIntensity(int channel)
        {
            return _intensities.TryGetValue(channel, out var v) ? v : 0;
        }

        public bool IsOn(int channel)
        {
            return _onStates.TryGetValue(channel, out var v) && v;
        }

        #region Internal TCP communication

        /// <summary>
        /// Format a CCS protocol command string.
        /// </summary>
        private static string FormatCommand(int channel, string command)
        {
            return $"@{channel:D2}{command}\r\n";
        }

        /// <summary>
        /// Send a command to the CCS controller and verify the response.
        /// </summary>
        private void SendCommand(string command)
        {
            var ip = IPAddress.Parse(_config.IpAddress);
            var endpoint = new IPEndPoint(ip, _config.Port);
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.ReceiveTimeout = _config.ConnectionTimeoutMs;
                socket.SendTimeout = _config.ConnectionTimeoutMs;
                socket.Connect(endpoint);

                var sendBytes = Encoding.ASCII.GetBytes(command);
                socket.Send(sendBytes, sendBytes.Length, SocketFlags.None);

                var recvBuffer = new byte[1024];
                int received = socket.Receive(recvBuffer, recvBuffer.Length, SocketFlags.None);
                var response = Encoding.ASCII.GetString(recvBuffer, 0, received);

                if (response != ResponseOk)
                {
                    throw new LightSourceException("CCS",
                        $"Command '{command.TrimEnd()}' failed, response: '{response.TrimEnd()}'");
                }
            }
            catch (LightSourceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LightSourceException("CCS",
                    $"TCP communication failed for command '{command.TrimEnd()}'", ex);
            }
        }

        private void EnsureOpen()
        {
            if (!_isOpen)
                throw new LightSourceException("CCS", "Controller is not open. Call Open() first.");
        }

        private LightChannelConfig GetChannelConfig(int channel)
        {
            var cfg = _config.Channels.Find(c => c.ChannelIndex == channel);
            if (cfg == null)
                throw new LightSourceException("CCS", $"Channel {channel} not found in configuration.");
            return cfg;
        }

        #endregion
    }
}

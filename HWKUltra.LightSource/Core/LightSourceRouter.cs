using HWKUltra.Core;
using HWKUltra.LightSource.Abstractions;
using HWKUltra.LightSource.Implementations;

namespace HWKUltra.LightSource.Core
{
    /// <summary>
    /// Light source router (corresponds to MotionRouter).
    /// Provides name-based channel access, hiding low-level channel index details.
    /// </summary>
    public class LightSourceRouter
    {
        private readonly ILightSourceController _controller;
        private readonly Dictionary<string, LightChannelConfig> _channelMap;

        public LightSourceRouter(
            ILightSourceController controller,
            Dictionary<string, LightChannelConfig> channelMap)
        {
            _controller = controller;
            _channelMap = channelMap;
        }

        public void Open() => _controller.Open();

        public void Close() => _controller.Close();

        /// <summary>
        /// Turn on the specified channel by name.
        /// </summary>
        public void TurnOn(string channelName)
        {
            var ch = GetChannel(channelName);
            _controller.TurnOn(ch.ChannelIndex);
        }

        /// <summary>
        /// Turn off the specified channel by name.
        /// </summary>
        public void TurnOff(string channelName)
        {
            var ch = GetChannel(channelName);
            _controller.TurnOff(ch.ChannelIndex);
        }

        /// <summary>
        /// Set intensity for the specified channel by name.
        /// </summary>
        public void SetIntensity(string channelName, int intensity)
        {
            var ch = GetChannel(channelName);
            _controller.SetIntensity(ch.ChannelIndex, intensity);
        }

        /// <summary>
        /// Set trigger mode: Turn off -> Set intensity -> Set external pulse mode -> Turn on.
        /// Preserves the original SetTriggerMode sequence from the legacy LightControl.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="intensity">Intensity value (0 to MaxIntensity)</param>
        public void SetTriggerMode(string channelName, int intensity)
        {
            var ch = GetChannel(channelName);
            try
            {
                _controller.TurnOff(ch.ChannelIndex);
                _controller.SetIntensity(ch.ChannelIndex, intensity);
                _controller.SetPulseMode(ch.ChannelIndex, LightPulseMode.External);
                _controller.TurnOn(ch.ChannelIndex);
            }
            catch (LightSourceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LightSourceException(channelName, "SetTriggerMode failed", ex);
            }
        }

        /// <summary>
        /// Set continuous mode: Turn off -> Set low intensity -> Set pulse mode off -> Turn on.
        /// Preserves the original SetContinueMode sequence from the legacy LightControl.
        /// </summary>
        /// <param name="channelName">Channel name</param>
        /// <param name="intensity">Optional intensity override (default: 1 for minimum brightness)</param>
        public void SetContinuousMode(string channelName, int intensity = 1)
        {
            var ch = GetChannel(channelName);
            try
            {
                _controller.TurnOff(ch.ChannelIndex);
                Thread.Sleep(100); // Brief delay between off and configuration (matches original)
                _controller.SetIntensity(ch.ChannelIndex, intensity);
                _controller.SetPulseMode(ch.ChannelIndex, LightPulseMode.Off);
                _controller.TurnOn(ch.ChannelIndex);
            }
            catch (LightSourceException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new LightSourceException(channelName, "SetContinuousMode failed", ex);
            }
        }

        /// <summary>
        /// Get the current intensity of the specified channel.
        /// </summary>
        public int GetIntensity(string channelName)
        {
            var ch = GetChannel(channelName);
            return _controller.GetIntensity(ch.ChannelIndex);
        }

        /// <summary>
        /// Check if the specified channel is currently on.
        /// </summary>
        public bool IsOn(string channelName)
        {
            var ch = GetChannel(channelName);
            return _controller.IsOn(ch.ChannelIndex);
        }

        /// <summary>
        /// Check if a channel exists by name.
        /// </summary>
        public bool HasChannel(string channelName) => _channelMap.ContainsKey(channelName);

        /// <summary>
        /// Get all channel names.
        /// </summary>
        public IReadOnlyCollection<string> ChannelNames => _channelMap.Keys;

        private LightChannelConfig GetChannel(string channelName)
        {
            if (!_channelMap.TryGetValue(channelName, out var ch))
                throw new LightSourceException(channelName,
                    $"Channel not found: {channelName}. Available: {string.Join(", ", _channelMap.Keys)}");
            return ch;
        }
    }
}

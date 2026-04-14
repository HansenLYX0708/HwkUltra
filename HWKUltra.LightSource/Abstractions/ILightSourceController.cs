namespace HWKUltra.LightSource.Abstractions
{
    /// <summary>
    /// Light source controller abstraction interface (corresponds to IMotionController).
    /// Different light source vendors implement this interface.
    /// </summary>
    public interface ILightSourceController
    {
        /// <summary>
        /// Open connection to the light source controller.
        /// </summary>
        void Open();

        /// <summary>
        /// Close connection to the light source controller.
        /// </summary>
        void Close();

        /// <summary>
        /// Turn on the specified light channel.
        /// </summary>
        /// <param name="channel">Channel index (0-based)</param>
        void TurnOn(int channel);

        /// <summary>
        /// Turn off the specified light channel.
        /// </summary>
        /// <param name="channel">Channel index (0-based)</param>
        void TurnOff(int channel);

        /// <summary>
        /// Set light intensity for the specified channel.
        /// </summary>
        /// <param name="channel">Channel index (0-based)</param>
        /// <param name="intensity">Intensity value (0 to MaxIntensity)</param>
        void SetIntensity(int channel, int intensity);

        /// <summary>
        /// Set pulse mode for the specified channel.
        /// </summary>
        /// <param name="channel">Channel index (0-based)</param>
        /// <param name="mode">Pulse mode</param>
        void SetPulseMode(int channel, LightPulseMode mode);

        /// <summary>
        /// Get current intensity of the specified channel.
        /// </summary>
        int GetIntensity(int channel);

        /// <summary>
        /// Check if the specified channel is currently on.
        /// </summary>
        bool IsOn(int channel);
    }

    /// <summary>
    /// Light source pulse mode.
    /// </summary>
    public enum LightPulseMode
    {
        /// <summary>
        /// Pulse mode off (continuous lighting).
        /// </summary>
        Off = 0,

        /// <summary>
        /// Internal trigger pulse mode.
        /// </summary>
        Internal = 1,

        /// <summary>
        /// External trigger pulse mode.
        /// </summary>
        External = 2
    }
}

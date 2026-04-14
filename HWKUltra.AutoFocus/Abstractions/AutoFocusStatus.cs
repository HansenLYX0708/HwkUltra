namespace HWKUltra.AutoFocus.Abstractions
{
    /// <summary>
    /// Auto focus status data class.
    /// </summary>
    public class AutoFocusStatus
    {
        /// <summary>
        /// Current focus value from the AF sensor.
        /// </summary>
        public double FocusValue { get; set; } = -9999;

        /// <summary>
        /// Current motor position.
        /// </summary>
        public double MotorPosition { get; set; } = -9999;

        /// <summary>
        /// Whether the AF controller is connected.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// Whether auto focus tracking is enabled.
        /// </summary>
        public bool IsAutoFocusEnabled { get; set; }

        /// <summary>
        /// Whether the laser is on.
        /// </summary>
        public bool IsLaserOn { get; set; }
    }
}

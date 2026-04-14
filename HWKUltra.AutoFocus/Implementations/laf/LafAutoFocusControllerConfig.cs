namespace HWKUltra.AutoFocus.Implementations.laf
{
    /// <summary>
    /// LAF auto focus controller configuration (corresponds to BaslerCameraControllerConfig).
    /// </summary>
    public class LafAutoFocusControllerConfig
    {
        /// <summary>
        /// List of AF instances managed by this controller.
        /// </summary>
        public List<AutoFocusConfig> Instances { get; set; } = new();
    }
}

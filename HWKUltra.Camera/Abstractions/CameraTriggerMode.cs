namespace HWKUltra.Camera.Abstractions
{
    /// <summary>
    /// Camera trigger mode enumeration.
    /// </summary>
    public enum CameraTriggerMode
    {
        /// <summary>
        /// Free-run mode (no trigger, continuous acquisition).
        /// </summary>
        Freerun = 0,

        /// <summary>
        /// External hardware trigger (e.g., Line1 input).
        /// </summary>
        ExternalHardware = 1,

        /// <summary>
        /// Software trigger mode.
        /// </summary>
        Software = 2
    }
}

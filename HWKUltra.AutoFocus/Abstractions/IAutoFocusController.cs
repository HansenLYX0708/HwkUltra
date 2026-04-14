namespace HWKUltra.AutoFocus.Abstractions
{
    /// <summary>
    /// Vendor-agnostic auto focus controller interface.
    /// Different AF vendors (LAF, etc.) implement this interface.
    /// </summary>
    public interface IAutoFocusController
    {
        /// <summary>
        /// Open connections to all configured AF instances.
        /// </summary>
        void Open();

        /// <summary>
        /// Close all AF connections and release resources.
        /// </summary>
        void Close();

        /// <summary>
        /// Enable auto focus tracking on the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        void EnableAutoFocus(string name);

        /// <summary>
        /// Disable auto focus tracking on the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        void DisableAutoFocus(string name);

        /// <summary>
        /// Turn laser on for the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        void LaserOn(string name);

        /// <summary>
        /// Turn laser off for the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        void LaserOff(string name);

        /// <summary>
        /// Get current focus value from the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        /// <returns>Focus value, or -9999 on error</returns>
        double GetFocusValue(string name);

        /// <summary>
        /// Get current motor position from the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        /// <returns>Motor position, or -9999 on error</returns>
        double GetMotorPosition(string name);

        /// <summary>
        /// Reset axis to zero position on the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        void ResetAxis(string name);

        /// <summary>
        /// Send a custom command to the specified instance.
        /// </summary>
        /// <param name="name">Logical AF instance name</param>
        /// <param name="command">Raw command string</param>
        /// <returns>Response string from the controller</returns>
        string SendCommand(string name, string command);

        /// <summary>
        /// Event raised when status changes on any instance.
        /// </summary>
        event EventHandler<AutoFocusStatusEventArgs>? StatusChanged;
    }
}

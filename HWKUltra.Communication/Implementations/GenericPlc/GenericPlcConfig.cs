using System.Collections.Generic;

namespace HWKUltra.Communication.Implementations.GenericPlc
{
    /// <summary>
    /// Configuration for the generic PLC controller.
    /// Supports multiple protocols (Mock, ModbusTcp, Fins) via pluggable transport.
    /// </summary>
    public class GenericPlcConfig
    {
        /// <summary>Transport/protocol name: "Mock", "ModbusTcp", "Fins".</summary>
        public string Protocol { get; set; } = "Mock";

        /// <summary>Target IP address (ignored for Mock).</summary>
        public string IpAddress { get; set; } = "127.0.0.1";

        /// <summary>Target TCP port (ignored for Mock).</summary>
        public int Port { get; set; } = 502;

        /// <summary>Default command timeout in milliseconds.</summary>
        public int DefaultCommandTimeoutMs { get; set; } = 15000;

        /// <summary>Polling interval while waiting for command feedback.</summary>
        public int PollIntervalMs { get; set; } = 50;

        /// <summary>Auto-connect on controller construction.</summary>
        public bool AutoConnect { get; set; } = false;

        /// <summary>Named command map: logical name -> PLC write/read protocol.</summary>
        public Dictionary<string, PlcCommandDef> CommandMap { get; set; } = new();

        /// <summary>Named bit-address map (e.g. LeftTrayState -> "M100.0").</summary>
        public Dictionary<string, string> BitMap { get; set; } = new();

        /// <summary>Named register-address map (e.g. Temperature -> "D200").</summary>
        public Dictionary<string, string> RegisterMap { get; set; } = new();
    }

    /// <summary>
    /// Definition of a named PLC command.
    /// When SendCommand is invoked, the controller writes TriggerValue to TriggerAddress,
    /// then polls SuccessAddress expecting SuccessValue (or FailureAddress expecting FailureValue).
    /// </summary>
    public class PlcCommandDef
    {
        /// <summary>Address to write the trigger to (e.g. "M10.0" or "D100").</summary>
        public string TriggerAddress { get; set; } = string.Empty;

        /// <summary>Value to write for trigger (bit: true/false, register: int).</summary>
        public string TriggerValue { get; set; } = "true";

        /// <summary>Address to poll for success signal (bit address).</summary>
        public string SuccessAddress { get; set; } = string.Empty;

        /// <summary>Expected value at SuccessAddress for success (default true).</summary>
        public bool SuccessValue { get; set; } = true;

        /// <summary>Optional address to poll for failure signal; leave empty to ignore.</summary>
        public string FailureAddress { get; set; } = string.Empty;

        /// <summary>Expected value at FailureAddress for failure (default true).</summary>
        public bool FailureValue { get; set; } = true;

        /// <summary>Optional address to write when command ack received, to reset trigger (empty = skip).</summary>
        public string ResetAddress { get; set; } = string.Empty;

        /// <summary>Timeout override in milliseconds for this specific command; 0 = use DefaultCommandTimeoutMs.</summary>
        public int TimeoutMs { get; set; } = 0;
    }
}

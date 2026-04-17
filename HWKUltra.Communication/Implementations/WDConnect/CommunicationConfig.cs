namespace HWKUltra.Communication.Implementations.WDConnect
{
    /// <summary>
    /// Configuration for the WDConnect-based communication controller (CamStar/MES).
    /// </summary>
    public class WDConnectCommunicationControllerConfig
    {
        /// <summary>
        /// Path to the WDConnect tool model XML file (relative to exe directory).
        /// </summary>
        public string ToolModelPath { get; set; } = "Equipment/ToolModel.xml";

        /// <summary>
        /// Whether to auto-connect on initialization.
        /// </summary>
        public bool AutoConnect { get; set; } = false;
    }

    /// <summary>
    /// Placeholder configuration for future PLC communication controller.
    /// </summary>
    public class PlcCommunicationControllerConfig
    {
        // Reserved for future PLC communication implementation.
        // Example fields: IpAddress, Port, Protocol, etc.
    }
}

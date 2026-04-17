namespace HWKUltra.Communication.Abstractions
{
    /// <summary>
    /// Types of messages that can be received from the host.
    /// </summary>
    public enum CommunicationMessageType
    {
        ScanResponse,
        CompleteResponse,
        LoadResponse,
        UnloadResponse,
        LoginResponse,
        Error,
        PrimaryIn,
        Unknown
    }

    /// <summary>
    /// Base event args for all communication messages.
    /// </summary>
    public class CommunicationEventArgs : EventArgs
    {
        public CommunicationMessageType MessageType { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string RawCommandId { get; set; } = string.Empty;

        /// <summary>
        /// Scan response data (populated when MessageType == ScanResponse).
        /// </summary>
        public ScanResponseData? ScanData { get; set; }

        /// <summary>
        /// Login response data (populated when MessageType == LoginResponse).
        /// </summary>
        public LoginResponseData? LoginData { get; set; }

        /// <summary>
        /// Unload response data (populated when MessageType == UnloadResponse).
        /// </summary>
        public UnloadResponseData? UnloadData { get; set; }
    }

    /// <summary>
    /// Data received from host after a StartScan request.
    /// </summary>
    public class ScanResponseData
    {
        public string ToolId { get; set; } = string.Empty;
        public string ExecutedBy { get; set; } = string.Empty;
        public string LoadLock { get; set; } = string.Empty;
        public string TrayId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string HeadType { get; set; } = string.Empty;
        public string LotId { get; set; } = string.Empty;
        public bool IsAbort { get; set; }
        public List<SliderInfo> TrayMap { get; set; } = new();
    }

    /// <summary>
    /// Individual slider information from the host tray map.
    /// </summary>
    public class SliderInfo
    {
        public string ContainerId { get; set; } = string.Empty;
        public string SliderSN { get; set; } = string.Empty;
        public string PosX { get; set; } = string.Empty;
        public string PosY { get; set; } = string.Empty;
        public string DefectCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Login response data from the host.
    /// </summary>
    public class LoginResponseData
    {
        public bool IsValidUser { get; set; }
    }

    /// <summary>
    /// Unload response data from the host.
    /// </summary>
    public class UnloadResponseData
    {
        public string LoadLock { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data required to send a CompleteRequest to the host.
    /// Decoupled from detection result — only contains defect slider info needed by the protocol.
    /// </summary>
    public class CommunicationCompleteData
    {
        public string TrayId { get; set; } = string.Empty;
        public string LoadLock { get; set; } = string.Empty;
        public string EmpId { get; set; } = string.Empty;
        public List<SliderDefectInfo> DefectSliders { get; set; } = new();
    }

    /// <summary>
    /// Defect information for a single slider, sent to the host during CompleteRequest.
    /// DefectCode is a string (loaded from JSON config, not a hardcoded enum).
    /// </summary>
    public class SliderDefectInfo
    {
        public string ContainerId { get; set; } = string.Empty;
        public string SliderSN { get; set; } = string.Empty;
        public int Row { get; set; }
        public int Col { get; set; }
        public string DefectCode { get; set; } = string.Empty;
    }
}

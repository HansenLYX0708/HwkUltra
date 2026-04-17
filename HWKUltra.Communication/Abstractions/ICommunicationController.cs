namespace HWKUltra.Communication.Abstractions
{
    /// <summary>
    /// Interface for factory host communication controllers (MES, PLC, etc.).
    /// Supports multiple communication protocol implementations.
    /// </summary>
    public interface ICommunicationController
    {
        /// <summary>
        /// Open the communication channel.
        /// </summary>
        void Open();

        /// <summary>
        /// Close the communication channel.
        /// </summary>
        void Close();

        /// <summary>
        /// Whether the communication channel is connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Request to start scanning a tray.
        /// </summary>
        void StartScan(string trayId, string loadLock, string empId);

        /// <summary>
        /// Request to load a tray.
        /// </summary>
        void Load(string loadLock, string empId);

        /// <summary>
        /// Request to unload a tray.
        /// </summary>
        void Unload(string loadLock, string empId);

        /// <summary>
        /// Report inspection completion with defect data.
        /// </summary>
        void CompleteRequest(CommunicationCompleteData data);

        /// <summary>
        /// Abort the current operation.
        /// </summary>
        void Abort(string trayId, string loadLock, string empId);

        /// <summary>
        /// Authenticate a user against the host system.
        /// </summary>
        void UserAuthentication(string userId, string password);

        /// <summary>
        /// Unified message event raised when the host sends a response.
        /// </summary>
        event EventHandler<CommunicationEventArgs>? MessageReceived;
    }
}

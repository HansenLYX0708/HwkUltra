using HWKUltra.Communication.Abstractions;
using HWKUltra.Core;

namespace HWKUltra.Communication.Core
{
    /// <summary>
    /// Routes communication operations to the underlying controller.
    /// Currently single-instance (one MES connection), but extensible for multiple channels.
    /// </summary>
    public class CommunicationRouter
    {
        private readonly ICommunicationController _controller;

        public event EventHandler<CommunicationEventArgs>? MessageReceived;

        public bool IsConnected => _controller.IsConnected;

        public CommunicationRouter(ICommunicationController controller)
        {
            _controller = controller;
            _controller.MessageReceived += (s, e) => MessageReceived?.Invoke(this, e);
        }

        /// <summary>
        /// Access the underlying controller as a generic PLC controller, or null if unsupported.
        /// </summary>
        public IGenericPlcController? Plc => _controller as IGenericPlcController;

        public void Open()
        {
            _controller.Open();
        }

        public void Close()
        {
            _controller.Close();
        }

        public void StartScan(string trayId, string loadLock, string empId)
        {
            _controller.StartScan(trayId, loadLock, empId);
        }

        public void Load(string loadLock, string empId)
        {
            _controller.Load(loadLock, empId);
        }

        public void Unload(string loadLock, string empId)
        {
            _controller.Unload(loadLock, empId);
        }

        public void CompleteRequest(CommunicationCompleteData data)
        {
            _controller.CompleteRequest(data);
        }

        public void Abort(string trayId, string loadLock, string empId)
        {
            _controller.Abort(trayId, loadLock, empId);
        }

        public void UserAuthentication(string userId, string password)
        {
            _controller.UserAuthentication(userId, password);
        }
    }
}

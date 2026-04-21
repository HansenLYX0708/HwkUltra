using HWKUltra.Communication.Abstractions;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.Communication.Core
{
    /// <summary>
    /// Subscribes to an <see cref="ITestRunStore"/> and, when a run completes
    /// successfully, maps its <see cref="TrayAoiReport"/> into a
    /// <see cref="CommunicationCompleteData"/> and forwards it via a
    /// <see cref="CommunicationRouter"/>.
    /// <para>
    /// This is the integration point that replaces the old coupling between
    /// `WD.AVI.Common.TrayDetectionResult` and `CommunicationLib.CompleteRequest`.
    /// </para>
    /// </summary>
    public sealed class TestRunCompletionBridge : IDisposable
    {
        private readonly ITestRunStore _store;
        private readonly CommunicationRouter _router;
        private readonly bool _requireConnected;
        private bool _disposed;

        /// <summary>Raised for every handled completion, regardless of success.</summary>
        public event EventHandler<TestRunCompletionBridgeEventArgs>? CompletionForwarded;

        public TestRunCompletionBridge(ITestRunStore store, CommunicationRouter router, bool requireConnected = true)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _requireConnected = requireConnected;
            _store.RunCompleted += OnRunCompleted;
        }

        private void OnRunCompleted(object? sender, TestRunEventArgs e)
        {
            try
            {
                if (e.Status != TestRunStatus.Completed)
                {
                    Fire(e.RunKey, forwarded: false, reason: $"status={e.Status}");
                    return;
                }
                if (e.Report is not TrayAoiReport tray)
                {
                    Fire(e.RunKey, forwarded: false, reason: $"report type={e.Report?.GetType().Name ?? "null"}");
                    return;
                }
                if (_requireConnected && !_router.IsConnected)
                {
                    Fire(e.RunKey, forwarded: false, reason: "router not connected");
                    return;
                }

                var data = Map(tray);
                _router.CompleteRequest(data);
                Fire(e.RunKey, forwarded: true, reason: $"defects={data.DefectSliders.Count}");
            }
            catch (Exception ex)
            {
                Fire(e.RunKey, forwarded: false, reason: $"error: {ex.Message}");
            }
        }

        /// <summary>
        /// Pure mapper — exposed for testing. Produces one <see cref="SliderDefectInfo"/>
        /// per row in <see cref="TrayAoiReport.Defects"/>; looks up SN and container id
        /// from the tray's per-slot arrays (1-based row/col in DefectDetail).
        /// </summary>
        public static CommunicationCompleteData Map(TrayAoiReport tray)
        {
            var data = new CommunicationCompleteData
            {
                TrayId = tray.Session.SerialNumber,
                LoadLock = tray.LoadLock,
                EmpId = tray.Session.OperatorId
            };

            foreach (var d in tray.Defects)
            {
                int r = d.Row - 1;
                int c = d.Col - 1;
                string sn = "";
                string containerId = "";
                if (r >= 0 && r < tray.Rows && c >= 0 && c < tray.Cols)
                {
                    sn = tray.SliderSN[r, c] ?? "";
                    containerId = tray.ContainerIds[r, c] ?? "";
                }

                data.DefectSliders.Add(new SliderDefectInfo
                {
                    Row = d.Row,
                    Col = d.Col,
                    SliderSN = sn,
                    ContainerId = containerId,
                    DefectCode = d.DefectCode
                });
            }
            return data;
        }

        private void Fire(string runKey, bool forwarded, string reason)
            => CompletionForwarded?.Invoke(this, new TestRunCompletionBridgeEventArgs(runKey, forwarded, reason));

        public void Dispose()
        {
            if (_disposed) return;
            _store.RunCompleted -= OnRunCompleted;
            _disposed = true;
        }
    }

    public sealed class TestRunCompletionBridgeEventArgs : EventArgs
    {
        public string RunKey { get; }
        public bool Forwarded { get; }
        public string Reason { get; }
        public TestRunCompletionBridgeEventArgs(string runKey, bool forwarded, string reason)
        { RunKey = runKey; Forwarded = forwarded; Reason = reason; }
    }
}

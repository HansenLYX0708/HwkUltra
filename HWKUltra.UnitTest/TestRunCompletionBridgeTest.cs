using HWKUltra.Communication.Abstractions;
using HWKUltra.Communication.Core;
using HWKUltra.Core;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.TestRun.Core;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Tests for the Communication ↔ TestRun bridge: the pure mapper and the
    /// end-to-end event-driven forwarding path (with a stub ICommunicationController).
    /// </summary>
    public static class TestRunCompletionBridgeTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("  TestRunCompletionBridge Tests");
            Console.WriteLine("=======================================");

            int pass = 0, fail = 0;
            var tests = new (string, Action)[]
            {
                ("Test 1: Map produces one defect entry per DefectDetail", Test_Map_Basic),
                ("Test 2: Map looks up SN and ContainerId from slot grids", Test_Map_SlotLookup),
                ("Test 3: End-to-end forwarding on RunCompleted", Test_EndToEnd_Forward),
                ("Test 4: Bridge skips when router not connected", Test_Skip_When_Disconnected),
                ("Test 5: Bridge fires CompletionForwarded for non-Completed statuses", Test_NonCompleted_Fires_NotForwarded)
            };

            foreach (var (name, test) in tests)
            {
                try { test(); pass++; Console.WriteLine($"  [PASS] {name}"); }
                catch (Exception ex) { fail++; Console.WriteLine($"  [FAIL] {name}: {ex.Message}"); }
            }
            Console.WriteLine($"\nBridge: {pass} passed, {fail} failed");
            if (fail > 0) throw new Exception($"{fail} bridge tests failed");
        }

        private static TrayAoiReport BuildTray()
        {
            var tray = new TrayAoiReport(2, 3);
            tray.Session.SerialNumber = "TRAY-42";
            tray.Session.OperatorId = "OP1";
            tray.LoadLock = "LL-A";
            tray.SliderSN[0, 1] = "SN-01-B";
            tray.ContainerIds[0, 1] = "C-01-B";
            tray.Defects.Add(new DefectDetail
            {
                Row = 1, Col = 2, DefectCode = "A2", Confidence = 0.9f,
                Region = new BoundingBox(1, 2, 3, 4)
            });
            return tray;
        }

        public static void Test_Map_Basic()
        {
            var data = TestRunCompletionBridge.Map(BuildTray());
            if (data.TrayId != "TRAY-42") throw new Exception("TrayId mismatch");
            if (data.EmpId != "OP1") throw new Exception("EmpId mismatch");
            if (data.LoadLock != "LL-A") throw new Exception("LoadLock mismatch");
            if (data.DefectSliders.Count != 1) throw new Exception($"expected 1 defect, got {data.DefectSliders.Count}");
            if (data.DefectSliders[0].DefectCode != "A2") throw new Exception("DefectCode mismatch");
        }

        public static void Test_Map_SlotLookup()
        {
            var data = TestRunCompletionBridge.Map(BuildTray());
            var d = data.DefectSliders[0];
            if (d.Row != 1 || d.Col != 2) throw new Exception("row/col mismatch");
            if (d.SliderSN != "SN-01-B") throw new Exception($"SN mismatch: {d.SliderSN}");
            if (d.ContainerId != "C-01-B") throw new Exception($"ContainerId mismatch: {d.ContainerId}");
        }

        public static void Test_EndToEnd_Forward()
        {
            var store = new TestRunStore();
            var stub = new StubCommController { IsConnected = true };
            var router = new CommunicationRouter(stub);
            using var bridge = new TestRunCompletionBridge(store, router, requireConnected: true);

            int forwardedCount = 0;
            bridge.CompletionForwarded += (_, e) => { if (e.Forwarded) forwardedCount++; };

            var run = store.Start("run-1", BuildTray());
            run.Complete(TestRunStatus.Completed);

            if (stub.LastCompleteData is null) throw new Exception("CompleteRequest never called");
            if (stub.LastCompleteData.DefectSliders.Count != 1) throw new Exception("defect not mapped");
            if (forwardedCount != 1) throw new Exception($"expected 1 forwarded event, got {forwardedCount}");
        }

        public static void Test_Skip_When_Disconnected()
        {
            var store = new TestRunStore();
            var stub = new StubCommController { IsConnected = false };
            var router = new CommunicationRouter(stub);
            using var bridge = new TestRunCompletionBridge(store, router, requireConnected: true);

            string? reason = null;
            bridge.CompletionForwarded += (_, e) => { if (!e.Forwarded) reason = e.Reason; };

            var run = store.Start("run-2", BuildTray());
            run.Complete(TestRunStatus.Completed);

            if (stub.LastCompleteData is not null) throw new Exception("should not have forwarded");
            if (reason is null || !reason.Contains("not connected")) throw new Exception($"expected disconnect reason, got: {reason}");
        }

        public static void Test_NonCompleted_Fires_NotForwarded()
        {
            var store = new TestRunStore();
            var stub = new StubCommController { IsConnected = true };
            var router = new CommunicationRouter(stub);
            using var bridge = new TestRunCompletionBridge(store, router);

            bool sawNotForwarded = false;
            bridge.CompletionForwarded += (_, e) => { if (!e.Forwarded && e.Reason.StartsWith("status=")) sawNotForwarded = true; };

            var run = store.Start("run-3", BuildTray());
            run.Complete(TestRunStatus.Failed);

            if (!sawNotForwarded) throw new Exception("expected non-forwarded event for Failed status");
            if (stub.LastCompleteData is not null) throw new Exception("should not have forwarded a Failed run");
        }

        // --- Stub controller ---
        private sealed class StubCommController : ICommunicationController
        {
            public bool IsConnected { get; set; }
            public CommunicationCompleteData? LastCompleteData { get; private set; }

            public event EventHandler<CommunicationEventArgs>? MessageReceived;

            public void Open() { IsConnected = true; }
            public void Close() { IsConnected = false; }
            public void StartScan(string trayId, string loadLock, string empId) { }
            public void Load(string loadLock, string empId) { }
            public void Unload(string loadLock, string empId) { }
            public void CompleteRequest(CommunicationCompleteData data) { LastCompleteData = data; }
            public void Abort(string trayId, string loadLock, string empId) { }
            public void UserAuthentication(string userId, string password) { }

            // Silence unused-event warning
            private void _unused() => MessageReceived?.Invoke(this, new CommunicationEventArgs());
        }
    }
}

using HWKUltra.Core;
using HWKUltra.TestRun.Abstractions;
using HWKUltra.TestRun.Core;
using HWKUltra.TestRun.Reports;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// Tests for the HWKUltra.TestRun runtime-session layer:
    /// store lifecycle, thread-safe mutation, event firing, CSV export.
    /// </summary>
    public static class TestRunStoreTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("  TestRun Store Tests");
            Console.WriteLine("=======================================");

            int pass = 0, fail = 0;
            var tests = new (string, Action)[]
            {
                ("Test 1: Start + Get + active runs", Test_StartAndGet),
                ("Test 2: Mutate raises Updated event", Test_Mutate_RaisesEvent),
                ("Test 3: Concurrent mutations are serialized", Test_ConcurrentMutations),
                ("Test 4: Complete raises Completed event + blocks mutation", Test_Complete_Blocks),
                ("Test 5: Duplicate Start throws", Test_Duplicate_Start_Throws),
                ("Test 6: CSV export writes headers + defect rows", Test_CsvExport)
            };

            foreach (var (name, test) in tests)
            {
                try { test(); pass++; Console.WriteLine($"  [PASS] {name}"); }
                catch (Exception ex) { fail++; Console.WriteLine($"  [FAIL] {name}: {ex.Message}"); }
            }
            Console.WriteLine($"\nTestRun: {pass} passed, {fail} failed");
            if (fail > 0) throw new Exception($"{fail} TestRun tests failed");
        }

        public static void Test_StartAndGet()
        {
            var store = new TestRunStore();
            var report = new TrayAoiReport(8, 30);
            var run = store.Start("tray1", report);
            if (run.RunKey != "tray1") throw new Exception("run key mismatch");
            if (run.Status != TestRunStatus.Running) throw new Exception("expected Running");
            if (store.Get("tray1") is null) throw new Exception("Get returned null");
            if (store.ActiveRuns.Count != 1) throw new Exception($"expected 1 active run, got {store.ActiveRuns.Count}");
            store.Remove("tray1");
            if (store.ActiveRuns.Count != 0) throw new Exception("expected 0 after remove");
        }

        public static void Test_Mutate_RaisesEvent()
        {
            var store = new TestRunStore();
            int eventCount = 0;
            store.RunUpdated += (_, _) => Interlocked.Increment(ref eventCount);
            var run = store.Start("t", new TrayAoiReport(2, 2));
            run.Mutate<TrayAoiReport>(r => r.Defects.Add(new DefectDetail { Row = 1, Col = 1, DefectCode = "A2" }));
            if (eventCount != 1) throw new Exception($"expected 1 update event, got {eventCount}");
        }

        public static void Test_ConcurrentMutations()
        {
            var store = new TestRunStore();
            var run = store.Start("t", new TrayAoiReport(10, 10));
            const int iterations = 1000;
            var tasks = new List<Task>();
            for (int i = 0; i < 4; i++)
            {
                tasks.Add(Task.Run(() =>
                {
                    for (int k = 0; k < iterations; k++)
                        run.Mutate<TrayAoiReport>(r => r.Defects.Add(new DefectDetail { Row = 1, Col = 1 }));
                }));
            }
            Task.WaitAll(tasks.ToArray());
            int expected = 4 * iterations;
            var actual = ((TrayAoiReport)run.Report).Defects.Count;
            if (actual != expected) throw new Exception($"expected {expected} defects, got {actual}");
        }

        public static void Test_Complete_Blocks()
        {
            var store = new TestRunStore();
            int completedCount = 0;
            store.RunCompleted += (_, _) => Interlocked.Increment(ref completedCount);
            var run = store.Start("t", new TrayAoiReport(2, 2));
            run.Complete(TestRunStatus.Completed);
            if (completedCount != 1) throw new Exception("expected 1 RunCompleted event");
            if (run.Status != TestRunStatus.Completed) throw new Exception("status not Completed");
            bool threw = false;
            try { run.Mutate<TrayAoiReport>(_ => { }); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("expected Mutate to throw after Complete");
        }

        public static void Test_Duplicate_Start_Throws()
        {
            var store = new TestRunStore();
            store.Start("t", new TrayAoiReport(2, 2));
            bool threw = false;
            try { store.Start("t", new TrayAoiReport(2, 2)); }
            catch (InvalidOperationException) { threw = true; }
            if (!threw) throw new Exception("expected duplicate Start to throw");
        }

        public static void Test_CsvExport()
        {
            var report = new TrayAoiReport(2, 2);
            report.Session.SerialNumber = "TRAY-UT";
            report.Session.StartTime = DateTime.Now;
            report.Session.EndTime = DateTime.Now.AddSeconds(5);
            report.SlotDefectCodes[0, 0] = "Pass";
            report.SlotDefectCodes[0, 1] = "A2";
            report.SliderSN[0, 0] = "SN1";
            report.SliderSN[0, 1] = "SN2";
            report.Defects.Add(new DefectDetail
            {
                Row = 1, Col = 2, DefectCode = "A2", Confidence = 0.9f,
                Region = new BoundingBox(10, 20, 100, 200)
            });

            var path = Path.Combine(Path.GetTempPath(), $"testrun_csv_{Guid.NewGuid():N}.csv");
            try
            {
                TrayAoiCsvExporter.Save(report, path);
                var content = File.ReadAllText(path);
                if (!content.Contains("trayID: TRAY-UT")) throw new Exception("trayID missing");
                if (!content.Contains("A2,10,20,100,200")) throw new Exception("defect row missing/wrong");
                if (!content.Contains("index,row,column,Slider Serial number")) throw new Exception("CSV header missing");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}

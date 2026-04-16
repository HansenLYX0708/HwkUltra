using System.Text.Json;
using HWKUltra.Core;
using HWKUltra.Tray.Abstractions;

namespace HWKUltra.Tray.Implementations
{
    /// <summary>
    /// Tray controller managing multiple named tray instances.
    /// Each instance contains a grid of pockets with 3D positions and slot states.
    /// </summary>
    public class TrayController : ITrayController
    {
        private readonly Dictionary<string, TrayInstance> _instances = new();

        public event EventHandler<TrayStatusEventArgs>? StatusChanged;

        public IReadOnlyList<string> InstanceNames => _instances.Keys.ToList();

        public bool HasInstance(string name) => _instances.ContainsKey(name);

        public TrayController(TrayControllerConfig config)
        {
            foreach (var cfg in config.Instances)
            {
                var instance = new TrayInstance(cfg.Name, cfg.Rows, cfg.Cols);
                _instances[cfg.Name] = instance;

                // Auto-load positions if path is configured and file exists
                if (!string.IsNullOrEmpty(cfg.PositionDataPath) && File.Exists(cfg.PositionDataPath))
                {
                    try
                    {
                        LoadPositionsInternal(instance, cfg.PositionDataPath);
                    }
                    catch
                    {
                        // Ignore load errors during construction
                    }
                }
            }
        }

        public void SetShape(string name, int rows, int cols)
        {
            var inst = GetInstance(name);
            inst.Reshape(rows, cols);
            RaiseStatusChanged(name, inst);
        }

        public void InitPositions(string name, AxisPosition leftTop, AxisPosition rightTop, AxisPosition leftBottom, AxisPosition rightBottom)
        {
            var inst = GetInstance(name);
            var leftList = GetEquallyDividedList(leftTop, leftBottom, inst.Rows);
            var rightList = GetEquallyDividedList(rightTop, rightBottom, inst.Rows);

            for (int row = 0; row < inst.Rows; row++)
            {
                var rowPoints = GetEquallyDividedList(leftList[row], rightList[row], inst.Cols);
                for (int col = 0; col < inst.Cols; col++)
                {
                    inst.Pockets[row, col] = rowPoints[col];
                }
            }
            inst.PositionsInitialized = true;
            RaiseStatusChanged(name, inst);
        }

        public AxisPosition GetPocketPosition(string name, int row, int col)
        {
            var inst = GetInstance(name);
            ValidateRowCol(inst, row, col);
            return inst.Pockets[row, col];
        }

        public SlotState GetSlotState(string name, int row, int col)
        {
            var inst = GetInstance(name);
            ValidateRowCol(inst, row, col);
            return inst.SlotStates[row, col];
        }

        public void SetSlotState(string name, int row, int col, SlotState state)
        {
            var inst = GetInstance(name);
            ValidateRowCol(inst, row, col);
            inst.SlotStates[row, col] = state;
            RaiseStatusChanged(name, inst);
        }

        public TrayTestState GetTestState(string name)
        {
            var inst = GetInstance(name);
            return inst.TestState;
        }

        public void SetTestState(string name, TrayTestState state)
        {
            var inst = GetInstance(name);
            inst.TestState = state;
            RaiseStatusChanged(name, inst);
        }

        public void ResetTray(string name)
        {
            var inst = GetInstance(name);
            inst.Reset();
            RaiseStatusChanged(name, inst);
        }

        public TrayInfo GetTrayInfo(string name)
        {
            var inst = GetInstance(name);
            return inst.BuildInfo();
        }

        public void SavePositions(string name, string filePath)
        {
            var inst = GetInstance(name);
            SavePositionsInternal(inst, filePath);
        }

        public void LoadPositions(string name, string filePath)
        {
            var inst = GetInstance(name);
            LoadPositionsInternal(inst, filePath);
            RaiseStatusChanged(name, inst);
        }

        #region Internal helpers

        private TrayInstance GetInstance(string name)
        {
            if (!_instances.TryGetValue(name, out var inst))
                throw new TrayException($"Tray instance '{name}' not found");
            return inst;
        }

        private static void ValidateRowCol(TrayInstance inst, int row, int col)
        {
            if (row < 0 || row >= inst.Rows)
                throw new TrayException($"Row {row} out of range [0, {inst.Rows - 1}]");
            if (col < 0 || col >= inst.Cols)
                throw new TrayException($"Col {col} out of range [0, {inst.Cols - 1}]");
        }

        /// <summary>
        /// Interpolate equally spaced positions between start and end.
        /// Ported from original TrayControl.GetEquallyDividedList.
        /// </summary>
        private static AxisPosition[] GetEquallyDividedList(AxisPosition start, AxisPosition end, int num)
        {
            if (num < 2)
                throw new TrayException("num must be >= 2 for equally divided list");

            var result = new AxisPosition[num];
            var (sx, sy, sz) = start.ToXYZ();
            var (ex, ey, ez) = end.ToXYZ();
            double stepX = (ex - sx) / (num - 1);
            double stepY = (ey - sy) / (num - 1);
            double stepZ = (ez - sz) / (num - 1);

            for (int i = 0; i < num - 1; i++)
            {
                result[i] = Pos.XYZ(
                    sx + i * stepX,
                    sy + i * stepY,
                    sz + i * stepZ);
            }
            result[num - 1] = end;
            return result;
        }

        private static void SavePositionsInternal(TrayInstance inst, string filePath)
        {
            var data = new Dictionary<string, double>[inst.Rows * inst.Cols];
            for (int r = 0; r < inst.Rows; r++)
                for (int c = 0; c < inst.Cols; c++)
                    data[r * inst.Cols + c] = new Dictionary<string, double>(inst.Pockets[r, c].Values);

            var wrapper = new PocketDataWrapper
            {
                Rows = inst.Rows,
                Cols = inst.Cols,
                Positions = data
            };

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(wrapper, TrayJsonContext.Default.PocketDataWrapper);
            File.WriteAllText(filePath, json);
        }

        private static void LoadPositionsInternal(TrayInstance inst, string filePath)
        {
            if (!File.Exists(filePath))
                throw new TrayException($"Position data file not found: {filePath}");

            var json = File.ReadAllText(filePath);
            var wrapper = JsonSerializer.Deserialize(json, TrayJsonContext.Default.PocketDataWrapper);
            if (wrapper == null)
                throw new TrayException("Failed to deserialize position data");

            if (wrapper.Rows != inst.Rows || wrapper.Cols != inst.Cols)
                throw new TrayException($"Position data shape mismatch: file({wrapper.Rows}x{wrapper.Cols}) vs tray({inst.Rows}x{inst.Cols})");

            for (int r = 0; r < inst.Rows; r++)
                for (int c = 0; c < inst.Cols; c++)
                    inst.Pockets[r, c] = new AxisPosition(wrapper.Positions[r * inst.Cols + c]);

            inst.PositionsInitialized = true;
        }

        private void RaiseStatusChanged(string name, TrayInstance inst)
        {
            StatusChanged?.Invoke(this, new TrayStatusEventArgs(name, inst.BuildInfo()));
        }

        #endregion

        #region Inner classes

        /// <summary>
        /// Internal class representing a single tray instance.
        /// </summary>
        private class TrayInstance
        {
            public string Name { get; }
            public int Rows { get; private set; }
            public int Cols { get; private set; }
            public AxisPosition[,] Pockets { get; private set; }
            public SlotState[,] SlotStates { get; private set; }
            public TrayTestState TestState { get; set; } = TrayTestState.Idle;
            public bool PositionsInitialized { get; set; }

            public TrayInstance(string name, int rows, int cols)
            {
                Name = name;
                Rows = rows;
                Cols = cols;
                Pockets = new AxisPosition[rows, cols];
                SlotStates = new SlotState[rows, cols];
                InitializeDefaults();
            }

            public void Reshape(int rows, int cols)
            {
                Rows = rows;
                Cols = cols;
                Pockets = new AxisPosition[rows, cols];
                SlotStates = new SlotState[rows, cols];
                InitializeDefaults();
            }

            public void Reset()
            {
                TestState = TrayTestState.Idle;
                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                        SlotStates[r, c] = SlotState.Empty;
            }

            public TrayInfo BuildInfo()
            {
                var info = new TrayInfo
                {
                    Name = Name,
                    Rows = Rows,
                    Cols = Cols,
                    TestState = TestState
                };

                for (int r = 0; r < Rows; r++)
                {
                    for (int c = 0; c < Cols; c++)
                    {
                        var s = SlotStates[r, c];
                        if (s != SlotState.Empty)
                            info.TestedCount++;
                        if (s == SlotState.Pass)
                            info.PassCount++;
                        else if (s == SlotState.Fail)
                            info.FailCount++;
                        else if (s == SlotState.Error)
                            info.ErrorCount++;
                    }
                }
                return info;
            }

            private void InitializeDefaults()
            {
                for (int r = 0; r < Rows; r++)
                    for (int c = 0; c < Cols; c++)
                    {
                        Pockets[r, c] = Pos.XYZ(0, 0, 0);
                        SlotStates[r, c] = SlotState.Empty;
                    }
                PositionsInitialized = false;
                TestState = TrayTestState.Idle;
            }
        }

        /// <summary>
        /// Wrapper for JSON serialization of pocket position data.
        /// </summary>
        public class PocketDataWrapper
        {
            public int Rows { get; set; }
            public int Cols { get; set; }
            public Dictionary<string, double>[] Positions { get; set; } = Array.Empty<Dictionary<string, double>>();
        }

        #endregion
    }
}

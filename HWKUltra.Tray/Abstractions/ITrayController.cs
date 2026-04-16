using HWKUltra.Core;

namespace HWKUltra.Tray.Abstractions
{
    /// <summary>
    /// Interface for tray controller operations.
    /// Manages multiple named tray instances, each with a grid of pockets.
    /// </summary>
    public interface ITrayController
    {
        /// <summary>
        /// Initialize a tray instance with the given shape (rows x cols).
        /// </summary>
        void SetShape(string name, int rows, int cols);

        /// <summary>
        /// Teach pocket positions using 4-corner interpolation.
        /// Corners: [0]=leftTop, [1]=rightTop, [2]=leftBottom, [3]=rightBottom.
        /// </summary>
        void InitPositions(string name, AxisPosition leftTop, AxisPosition rightTop, AxisPosition leftBottom, AxisPosition rightBottom);

        /// <summary>
        /// Get pocket position at (row, col) for a tray instance.
        /// Row and col are 0-based.
        /// </summary>
        AxisPosition GetPocketPosition(string name, int row, int col);

        /// <summary>
        /// Get the slot state at (row, col).
        /// </summary>
        SlotState GetSlotState(string name, int row, int col);

        /// <summary>
        /// Set the slot state at (row, col).
        /// </summary>
        void SetSlotState(string name, int row, int col, SlotState state);

        /// <summary>
        /// Get the current test state of a tray.
        /// </summary>
        TrayTestState GetTestState(string name);

        /// <summary>
        /// Set the test state of a tray.
        /// </summary>
        void SetTestState(string name, TrayTestState state);

        /// <summary>
        /// Reset all slot states and test state for a tray.
        /// </summary>
        void ResetTray(string name);

        /// <summary>
        /// Get tray statistical information.
        /// </summary>
        TrayInfo GetTrayInfo(string name);

        /// <summary>
        /// Save pocket positions to a JSON file.
        /// </summary>
        void SavePositions(string name, string filePath);

        /// <summary>
        /// Load pocket positions from a JSON file.
        /// </summary>
        void LoadPositions(string name, string filePath);

        /// <summary>
        /// Get all instance names.
        /// </summary>
        IReadOnlyList<string> InstanceNames { get; }

        /// <summary>
        /// Check if a named instance exists.
        /// </summary>
        bool HasInstance(string name);

        /// <summary>
        /// Status changed event.
        /// </summary>
        event EventHandler<TrayStatusEventArgs>? StatusChanged;
    }
}

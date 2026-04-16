namespace HWKUltra.Tray.Abstractions
{
    /// <summary>
    /// Tray test state enumeration.
    /// </summary>
    public enum TrayTestState
    {
        Idle = 0,
        Testing = 1,
        Complete = 2,
        Error = 3
    }

    /// <summary>
    /// Slot state enumeration for individual pocket in a tray.
    /// </summary>
    public enum SlotState
    {
        Empty = 0,
        Present = 1,
        Pass = 2,
        Fail = 3,
        Error = 4,
        Unknown = 5
    }

    /// <summary>
    /// Tray statistical information.
    /// </summary>
    public class TrayInfo
    {
        public string Name { get; set; } = "";
        public int Rows { get; set; }
        public int Cols { get; set; }
        public int TotalSlots => Rows * Cols;
        public int TestedCount { get; set; }
        public int PassCount { get; set; }
        public int FailCount { get; set; }
        public int ErrorCount { get; set; }
        public TrayTestState TestState { get; set; } = TrayTestState.Idle;
    }

    /// <summary>
    /// Tray status event arguments.
    /// </summary>
    public class TrayStatusEventArgs : EventArgs
    {
        public string InstanceName { get; }
        public TrayInfo Info { get; }

        public TrayStatusEventArgs(string instanceName, TrayInfo info)
        {
            InstanceName = instanceName;
            Info = info;
        }
    }
}

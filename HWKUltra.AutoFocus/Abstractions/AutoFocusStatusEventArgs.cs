namespace HWKUltra.AutoFocus.Abstractions
{
    /// <summary>
    /// Event arguments for auto focus status changes.
    /// </summary>
    public class AutoFocusStatusEventArgs : EventArgs
    {
        /// <summary>
        /// Logical AF instance name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Current status snapshot.
        /// </summary>
        public AutoFocusStatus Status { get; }

        /// <summary>
        /// Timestamp of the status change.
        /// </summary>
        public DateTime Timestamp { get; }

        public AutoFocusStatusEventArgs(string name, AutoFocusStatus status)
        {
            Name = name;
            Status = status;
            Timestamp = DateTime.Now;
        }
    }
}

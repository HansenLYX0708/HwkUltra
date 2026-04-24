namespace HWKUltra.Communication.Abstractions
{
    /// <summary>
    /// Generic PLC controller interface. Extends ICommunicationController with
    /// low-level PLC primitives (bit/register read/write) and named command dispatch.
    /// Designed for integrating with robot arms, interlock systems, and other PLC-driven equipment.
    /// </summary>
    public interface IGenericPlcController : ICommunicationController
    {
        /// <summary>
        /// Send a named command (pre-configured via CommandMap) and wait for success feedback.
        /// Returns true on success, false on timeout/failure. Diagnostic message is provided via out.
        /// </summary>
        bool SendCommand(string commandName, int timeoutMs, out string message);

        /// <summary>
        /// Read a single boolean bit by configured bit name.
        /// </summary>
        bool ReadBit(string bitName);

        /// <summary>
        /// Write a single boolean bit by configured bit name.
        /// </summary>
        void WriteBit(string bitName, bool value);

        /// <summary>
        /// Read a 32-bit integer register by configured register name.
        /// </summary>
        int ReadRegister(string registerName);

        /// <summary>
        /// Write a 32-bit integer register by configured register name.
        /// </summary>
        void WriteRegister(string registerName, int value);
    }
}

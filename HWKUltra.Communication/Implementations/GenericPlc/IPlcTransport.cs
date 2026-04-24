namespace HWKUltra.Communication.Implementations.GenericPlc
{
    /// <summary>
    /// Low-level PLC transport abstraction. Different protocols (Modbus TCP, Fins, Mock) implement this.
    /// </summary>
    public interface IPlcTransport
    {
        /// <summary>Whether transport is connected.</summary>
        bool IsConnected { get; }

        /// <summary>Open / connect.</summary>
        void Open();

        /// <summary>Close / disconnect.</summary>
        void Close();

        /// <summary>Read a bit from the given protocol address (e.g. "M10.0").</summary>
        bool ReadBit(string address);

        /// <summary>Write a bit to the given protocol address.</summary>
        void WriteBit(string address, bool value);

        /// <summary>Read a 32-bit register from the given protocol address (e.g. "D100").</summary>
        int ReadRegister(string address);

        /// <summary>Write a 32-bit register to the given protocol address.</summary>
        void WriteRegister(string address, int value);
    }
}

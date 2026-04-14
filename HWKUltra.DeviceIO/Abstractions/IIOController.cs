namespace HWKUltra.DeviceIO.Abstractions
{
    /// <summary>
    /// IO controller abstraction interface (corresponds to IMotionController).
    /// Different IO card vendors implement this interface.
    /// </summary>
    public interface IIOController
    {
        /// <summary>
        /// Open connection to the IO controller.
        /// </summary>
        void Open();

        /// <summary>
        /// Close connection to the IO controller.
        /// </summary>
        void Close();

        /// <summary>
        /// Set an output bit.
        /// </summary>
        /// <param name="cardIndex">Card index (0-based)</param>
        /// <param name="bitIndex">Bit index</param>
        /// <param name="value">true=On, false=Off</param>
        void SetOutput(int cardIndex, int bitIndex, bool value);

        /// <summary>
        /// Read output bit state.
        /// </summary>
        bool GetOutput(int cardIndex, int bitIndex);

        /// <summary>
        /// Read input bit state.
        /// </summary>
        bool GetInput(int cardIndex, int bitIndex);

        /// <summary>
        /// Batch-read an input bank from the specified card.
        /// </summary>
        /// <param name="cardIndex">Card index</param>
        /// <param name="bankIndex">Bank number (0, 1, ...)</param>
        /// <returns>Raw integer value of the bank (each bit represents one IO point)</returns>
        int ReadInputBank(int cardIndex, int bankIndex);

        /// <summary>
        /// Batch-read an output bank from the specified card.
        /// </summary>
        int ReadOutputBank(int cardIndex, int bankIndex);
    }
}

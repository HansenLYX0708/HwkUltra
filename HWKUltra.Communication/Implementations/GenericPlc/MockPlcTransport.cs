using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace HWKUltra.Communication.Implementations.GenericPlc
{
    /// <summary>
    /// In-memory mock PLC transport for simulation and unit testing.
    /// When a write to a configured trigger-address occurs, if <see cref="AutoAckSuccess"/> is true
    /// a background task auto-sets the mapped success bit after <see cref="AutoAckDelayMs"/>.
    /// </summary>
    public class MockPlcTransport : IPlcTransport
    {
        private readonly ConcurrentDictionary<string, bool> _bits = new();
        private readonly ConcurrentDictionary<string, int> _regs = new();

        public bool IsConnected { get; private set; }

        /// <summary>When true, mock writes to any bit immediately schedule a success-ack callback (see TriggerAckMap).</summary>
        public bool AutoAckSuccess { get; set; } = true;

        /// <summary>Delay before auto-setting the success bit.</summary>
        public int AutoAckDelayMs { get; set; } = 100;

        /// <summary>Map of trigger-address -> success-address that should be set true after an auto-ack delay.</summary>
        public ConcurrentDictionary<string, string> TriggerAckMap { get; } = new();

        /// <summary>Map of trigger-address -> bit that should be toggled (e.g. TrayState) to mimic physical change.</summary>
        public ConcurrentDictionary<string, (string address, bool value)> SideEffectMap { get; } = new();

        public void Open() { IsConnected = true; }
        public void Close() { IsConnected = false; }

        public bool ReadBit(string address) => _bits.TryGetValue(address, out var v) && v;

        public void WriteBit(string address, bool value)
        {
            _bits[address] = value;
            if (value && AutoAckSuccess && TriggerAckMap.TryGetValue(address, out var successAddr))
            {
                Task.Run(async () =>
                {
                    await Task.Delay(AutoAckDelayMs).ConfigureAwait(false);
                    if (SideEffectMap.TryGetValue(address, out var side))
                        _bits[side.address] = side.value;
                    _bits[successAddr] = true;
                });
            }
        }

        public int ReadRegister(string address) => _regs.TryGetValue(address, out var v) ? v : 0;

        public void WriteRegister(string address, int value) { _regs[address] = value; }

        /// <summary>Direct set for test setup.</summary>
        public void SetBit(string address, bool value) => _bits[address] = value;
    }
}

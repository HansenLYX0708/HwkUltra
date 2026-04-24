using System;
using System.Diagnostics;
using System.Threading;
using HWKUltra.Communication.Abstractions;
using HWKUltra.Core;

namespace HWKUltra.Communication.Implementations.GenericPlc
{
    /// <summary>
    /// Generic PLC controller. Implements ICommunicationController (MES methods are not supported
    /// and will throw NotSupportedException) and IGenericPlcController (PLC primitives).
    /// Actual protocol is delegated to an IPlcTransport.
    /// </summary>
    public class GenericPlcController : IGenericPlcController
    {
        private readonly GenericPlcConfig _config;
        private readonly IPlcTransport _transport;

        public event EventHandler<CommunicationEventArgs>? MessageReceived;

        public GenericPlcController(GenericPlcConfig config, IPlcTransport? transport = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _transport = transport ?? CreateDefaultTransport(config);
            if (_config.AutoConnect) Open();
        }

        private static IPlcTransport CreateDefaultTransport(GenericPlcConfig config)
        {
            return config.Protocol?.ToLowerInvariant() switch
            {
                "mock" or "" or null => new MockPlcTransport(),
                _ => new MockPlcTransport() // TODO: implement ModbusTcp/Fins transports; fall back to mock for now
            };
        }

        public bool IsConnected => _transport.IsConnected;

        public void Open() { _transport.Open(); }
        public void Close() { _transport.Close(); }

        public bool SendCommand(string commandName, int timeoutMs, out string message)
        {
            if (!_config.CommandMap.TryGetValue(commandName, out var def))
            {
                message = $"Command '{commandName}' not found in CommandMap";
                return false;
            }

            var effectiveTimeout = timeoutMs > 0 ? timeoutMs
                                 : def.TimeoutMs > 0 ? def.TimeoutMs
                                 : _config.DefaultCommandTimeoutMs;

            try
            {
                // Write trigger
                if (IsBoolValue(def.TriggerValue, out var bVal))
                    _transport.WriteBit(def.TriggerAddress, bVal);
                else if (int.TryParse(def.TriggerValue, out var iVal))
                    _transport.WriteRegister(def.TriggerAddress, iVal);
                else
                {
                    message = $"Invalid TriggerValue '{def.TriggerValue}' for command '{commandName}'";
                    return false;
                }

                // Poll success / failure
                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < effectiveTimeout)
                {
                    if (!string.IsNullOrEmpty(def.FailureAddress)
                        && _transport.ReadBit(def.FailureAddress) == def.FailureValue)
                    {
                        ResetTrigger(def);
                        message = $"Command '{commandName}' reported failure";
                        RaiseMessage(CommunicationMessageType.Error, false, message, commandName);
                        return false;
                    }

                    if (!string.IsNullOrEmpty(def.SuccessAddress)
                        && _transport.ReadBit(def.SuccessAddress) == def.SuccessValue)
                    {
                        ResetTrigger(def);
                        message = $"Command '{commandName}' completed";
                        RaiseMessage(CommunicationMessageType.CompleteResponse, true, message, commandName);
                        return true;
                    }

                    Thread.Sleep(_config.PollIntervalMs);
                }

                ResetTrigger(def);
                message = $"Command '{commandName}' timed out after {effectiveTimeout} ms";
                RaiseMessage(CommunicationMessageType.Error, false, message, commandName);
                return false;
            }
            catch (Exception ex)
            {
                message = $"Command '{commandName}' transport error: {ex.Message}";
                RaiseMessage(CommunicationMessageType.Error, false, message, commandName);
                return false;
            }
        }

        private void ResetTrigger(PlcCommandDef def)
        {
            if (string.IsNullOrEmpty(def.ResetAddress)) return;
            try { _transport.WriteBit(def.ResetAddress, false); } catch { /* best effort */ }
        }

        public bool ReadBit(string bitName)
        {
            var addr = ResolveBitAddress(bitName);
            return _transport.ReadBit(addr);
        }

        public void WriteBit(string bitName, bool value)
        {
            var addr = ResolveBitAddress(bitName);
            _transport.WriteBit(addr, value);
        }

        public int ReadRegister(string registerName)
        {
            var addr = ResolveRegisterAddress(registerName);
            return _transport.ReadRegister(addr);
        }

        public void WriteRegister(string registerName, int value)
        {
            var addr = ResolveRegisterAddress(registerName);
            _transport.WriteRegister(addr, value);
        }

        private string ResolveBitAddress(string name)
        {
            if (_config.BitMap.TryGetValue(name, out var addr)) return addr;
            return name;
        }

        private string ResolveRegisterAddress(string name)
        {
            if (_config.RegisterMap.TryGetValue(name, out var addr)) return addr;
            return name;
        }

        private static bool IsBoolValue(string s, out bool value)
        {
            return bool.TryParse(s, out value);
        }

        private void RaiseMessage(CommunicationMessageType type, bool success, string message, string commandId)
        {
            MessageReceived?.Invoke(this, new CommunicationEventArgs
            {
                MessageType = type,
                Success = success,
                Message = message,
                RawCommandId = commandId
            });
        }

        // === ICommunicationController MES methods: not supported by generic PLC ===
        public void StartScan(string trayId, string loadLock, string empId)
            => throw new NotSupportedException("GenericPlcController does not support MES StartScan");

        public void Load(string loadLock, string empId)
            => throw new NotSupportedException("GenericPlcController does not support MES Load");

        public void Unload(string loadLock, string empId)
            => throw new NotSupportedException("GenericPlcController does not support MES Unload");

        public void CompleteRequest(CommunicationCompleteData data)
            => throw new NotSupportedException("GenericPlcController does not support MES CompleteRequest");

        public void Abort(string trayId, string loadLock, string empId)
            => throw new NotSupportedException("GenericPlcController does not support MES Abort");

        public void UserAuthentication(string userId, string password)
            => throw new NotSupportedException("GenericPlcController does not support MES UserAuthentication");

        /// <summary>Expose transport for advanced scenarios / tests.</summary>
        public IPlcTransport Transport => _transport;
    }
}

using System.Runtime.InteropServices;
using HWKUltra.Measurement.Abstractions;
using HWKUltra.Core;

namespace HWKUltra.Measurement.Implementations.Keyence
{
    /// <summary>
    /// Keyence CL3-IF laser displacement sensor controller.
    /// Supports multiple device instances identified by name.
    /// Communicates via CL3_IF.dll native SDK over USB.
    /// </summary>
    public class KeyenceMeasurementController : IMeasurementController, IDisposable
    {
        private readonly Dictionary<string, KeyenceInstance> _instances = new();

        public event EventHandler<MeasurementStatusEventArgs>? StatusChanged;

        public KeyenceMeasurementController(KeyenceMeasurementControllerConfig config)
        {
            foreach (var cfg in config.Instances)
            {
                if (string.IsNullOrWhiteSpace(cfg.Name))
                    throw new MeasurementException("", "Instance name cannot be empty");
                if (_instances.ContainsKey(cfg.Name))
                    throw new MeasurementException(cfg.Name, $"Duplicate instance name: {cfg.Name}");
                _instances[cfg.Name] = new KeyenceInstance(cfg);
            }
        }

        public void Open(string name)
        {
            var inst = GetInstance(name);
            if (inst.IsConnected) return;

            try
            {
                int rc = KeyenceNativeMethods.CL3IF_OpenUsbCommunication(inst.Config.DeviceId, (uint)inst.Config.TimeoutMs);
                CheckResult("CL3IF_OpenUsbCommunication", name, rc);

                byte programNo;
                KeyenceNativeMethods.CL3IF_GetProgramNo(inst.Config.DeviceId, out programNo);

                var samplingCycle = MapSamplingCycle(inst.Config.DefaultSamplingCycleUs);
                KeyenceNativeMethods.CL3IF_SetSamplingCycle(inst.Config.DeviceId, programNo, samplingCycle);

                var filterAvg = MapFilterAverage(inst.Config.DefaultFilterAverage);
                KeyenceNativeMethods.CL3IF_SetFilter(inst.Config.DeviceId, programNo, 0,
                    CL3IF_FILTERMODE.CL3IF_FILTERMODE_MOVING_AVERAGE, (ushort)filterAvg);

                inst.IsConnected = true;
                RaiseStatusChanged(name, inst);
            }
            catch (MeasurementException) { throw; }
            catch (Exception ex)
            {
                throw new MeasurementException(name, $"Failed to open device: {ex.Message}", ex);
            }
        }

        public void Close(string name)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected) return;

            try
            {
                int rc = KeyenceNativeMethods.CL3IF_CloseCommunication(inst.Config.DeviceId);
                CheckResult("CL3IF_CloseCommunication", name, rc);
            }
            catch (Exception ex)
            {
                throw new MeasurementException(name, $"Failed to close device: {ex.Message}", ex);
            }
            finally
            {
                inst.IsConnected = false;
                RaiseStatusChanged(name, inst);
            }
        }

        public double GetMeasurementValue(string name)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected) return -9999;

            byte[] buffer = new byte[inst.Config.MaxRequestDataLength];
            using (var pin = new PinnedObject(buffer))
            {
                int rc = KeyenceNativeMethods.CL3IF_GetMeasurementData(inst.Config.DeviceId, pin.Pointer);
                CheckResult("CL3IF_GetMeasurementData", name, rc);

                var addInfo = (CL3IF_ADD_INFO)Marshal.PtrToStructure(pin.Pointer, typeof(CL3IF_ADD_INFO))!;
                int readPos = Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                var outData = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPos, typeof(CL3IF_OUTMEASUREMENT_DATA))!;

                double value = (double)outData.measurementValue / 10000;
                inst.LastValue = value;
                return value;
            }
        }

        public uint GetTrendIndex(string name)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected) return 0;

            uint index;
            int rc = KeyenceNativeMethods.CL3IF_GetTrendIndex(inst.Config.DeviceId, out index);
            CheckResult("CL3IF_GetTrendIndex", name, rc);
            return index;
        }

        public double[] GetTrendData(string name, uint startIndex, uint endIndex)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected || endIndex <= startIndex) return Array.Empty<double>();

            return ReadTrendDataAsDouble(inst, startIndex, endIndex);
        }

        public double[] GetAllTrendData(string name, uint startIndex, uint endIndex)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected || endIndex <= startIndex) return Array.Empty<double>();

            uint count = endIndex - startIndex;
            double[] data = new double[count];
            byte[] buffer = new byte[inst.Config.MaxRequestDataLength];
            uint maxPerBatch = (uint)(inst.Config.MaxRequestDataLength / (16 * 8)) - 1;
            uint lastBatchCount = count % maxPerBatch;
            uint batches = count / maxPerBatch + 1;
            int idx = 0;

            for (uint batch = 0; batch < batches; batch++)
            {
                uint batchStart = batch * maxPerBatch + startIndex;
                uint batchLen = (batch < batches - 1) ? maxPerBatch : lastBatchCount;
                if (batchLen == 0) continue;

                uint nextInd, obtained;
                CL3IF_OUTNO outTarget;
                using (var pin = new PinnedObject(buffer))
                {
                    KeyenceNativeMethods.CL3IF_GetTrendData(inst.Config.DeviceId, batchStart, batchLen,
                        out nextInd, out obtained, out outTarget, pin.Pointer);

                    var outTargetList = ConvertOutTargetList(outTarget);
                    int readPos = 0;
                    for (uint i = 0; i < obtained && idx < data.Length; i++)
                    {
                        readPos += Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                        var outData = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPos, typeof(CL3IF_OUTMEASUREMENT_DATA))!;
                        readPos += Marshal.SizeOf(typeof(CL3IF_OUTMEASUREMENT_DATA)) * outTargetList.Count;
                        data[idx++] = (double)outData.measurementValue / 10000;
                    }
                }
            }
            return data;
        }

        public int GetTrendIndexData(string name, uint index)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected) return 0;

            byte[] buffer = new byte[inst.Config.MaxRequestDataLength];
            uint nextInd, obtained;
            CL3IF_OUTNO outTarget;
            using (var pin = new PinnedObject(buffer))
            {
                KeyenceNativeMethods.CL3IF_GetTrendData(inst.Config.DeviceId, index, 1,
                    out nextInd, out obtained, out outTarget, pin.Pointer);

                if (obtained == 0) return 0;
                var outTargetList = ConvertOutTargetList(outTarget);
                int readPos = Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                var outData = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPos, typeof(CL3IF_OUTMEASUREMENT_DATA))!;
                return outData.measurementValue;
            }
        }

        public void StartStorage(string name)
        {
            var inst = GetInstance(name);
            EnsureConnected(inst, name);
            int rc = KeyenceNativeMethods.CL3IF_StartStorage(inst.Config.DeviceId);
            CheckResult("CL3IF_StartStorage", name, rc);
        }

        public void StopStorage(string name)
        {
            var inst = GetInstance(name);
            EnsureConnected(inst, name);
            int rc = KeyenceNativeMethods.CL3IF_StopStorage(inst.Config.DeviceId);
            CheckResult("CL3IF_StopStorage", name, rc);
        }

        public void ClearStorage(string name)
        {
            var inst = GetInstance(name);
            EnsureConnected(inst, name);
            int rc = KeyenceNativeMethods.CL3IF_ClearStorageData(inst.Config.DeviceId);
            CheckResult("CL3IF_ClearStorageData", name, rc);
        }

        public uint GetStorageIndex(string name)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected) return 0;

            uint oldest, newest;
            int rc = KeyenceNativeMethods.CL3IF_GetStorageIndex(inst.Config.DeviceId, CL3IF_SELECTED_INDEX.CL3IF_SELECTED_INDEX_OLDEST, out oldest);
            KeyenceNativeMethods.CL3IF_GetStorageIndex(inst.Config.DeviceId, CL3IF_SELECTED_INDEX.CL3IF_SELECTED_INDEX_NEWEST, out newest);
            CheckResult("CL3IF_GetStorageIndex", name, rc);
            return oldest;
        }

        public int[] GetStorageData(string name, uint startIndex, uint endIndex)
        {
            var inst = GetInstance(name);
            if (!inst.IsConnected || endIndex <= startIndex) return Array.Empty<int>();

            uint count = endIndex - startIndex;
            int[] data = new int[count];
            byte[] buffer = new byte[inst.Config.MaxRequestDataLength];
            uint maxPerBatch = (uint)(inst.Config.MaxRequestDataLength / (16 * 8)) - 1;
            uint lastBatchCount = count % maxPerBatch;
            uint batches = count / maxPerBatch + 1;
            int idx = 0;

            for (uint batch = 0; batch < batches; batch++)
            {
                uint batchStart = batch * maxPerBatch + startIndex;
                uint batchLen = (batch < batches - 1) ? maxPerBatch : lastBatchCount;
                if (batchLen == 0) continue;

                uint nextInd, obtained;
                CL3IF_OUTNO outTarget;
                using (var pin = new PinnedObject(buffer))
                {
                    KeyenceNativeMethods.CL3IF_GetTrendData(inst.Config.DeviceId, batchStart, batchLen,
                        out nextInd, out obtained, out outTarget, pin.Pointer);

                    var outTargetList = ConvertOutTargetList(outTarget);
                    int readPos = 0;
                    for (uint i = 0; i < obtained && idx < data.Length; i++)
                    {
                        readPos += Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                        var outData = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPos, typeof(CL3IF_OUTMEASUREMENT_DATA))!;
                        readPos += Marshal.SizeOf(typeof(CL3IF_OUTMEASUREMENT_DATA)) * outTargetList.Count;
                        data[idx++] = outData.measurementValue;
                    }
                }
            }
            return data;
        }

        public void MeasureControl(string name, bool enable)
        {
            var inst = GetInstance(name);
            EnsureConnected(inst, name);
            int rc = KeyenceNativeMethods.CL3IF_MeasurementControl(inst.Config.DeviceId, enable);
            CheckResult("CL3IF_MeasurementControl", name, rc);
        }

        public void SetSamplingCycle(string name, int cycleUs)
        {
            var inst = GetInstance(name);
            EnsureConnected(inst, name);
            var cycle = MapSamplingCycle(cycleUs);
            byte programNo;
            KeyenceNativeMethods.CL3IF_GetProgramNo(inst.Config.DeviceId, out programNo);
            int rc = KeyenceNativeMethods.CL3IF_SetSamplingCycle(inst.Config.DeviceId, programNo, cycle);
            CheckResult("CL3IF_SetSamplingCycle", name, rc);
        }

        public void SetFilterAverage(string name, int averageCount)
        {
            var inst = GetInstance(name);
            EnsureConnected(inst, name);
            var avg = MapFilterAverage(averageCount);
            byte programNo;
            KeyenceNativeMethods.CL3IF_GetProgramNo(inst.Config.DeviceId, out programNo);
            int rc = KeyenceNativeMethods.CL3IF_SetFilter(inst.Config.DeviceId, programNo, 0,
                CL3IF_FILTERMODE.CL3IF_FILTERMODE_MOVING_AVERAGE, (ushort)avg);
            CheckResult("CL3IF_SetFilter", name, rc);
        }

        public void Dispose()
        {
            foreach (var kvp in _instances)
            {
                if (kvp.Value.IsConnected)
                {
                    try { Close(kvp.Key); } catch { }
                }
            }
            _instances.Clear();
        }

        // ==================== Private helpers ====================

        private KeyenceInstance GetInstance(string name)
        {
            if (!_instances.TryGetValue(name, out var inst))
                throw new MeasurementException(name, $"Unknown measurement instance: {name}");
            return inst;
        }

        private static void EnsureConnected(KeyenceInstance inst, string name)
        {
            if (!inst.IsConnected)
                throw new MeasurementException(name, $"Measurement instance '{name}' is not connected");
        }

        private static void CheckResult(string command, string name, int rc)
        {
            if (rc != KeyenceNativeMethods.CL3IF_RC_OK)
                throw new MeasurementException(name, $"{command} failed with error code {rc}");
        }

        private void RaiseStatusChanged(string name, KeyenceInstance inst)
        {
            StatusChanged?.Invoke(this, new MeasurementStatusEventArgs(name, new MeasurementStatus
            {
                IsConnected = inst.IsConnected,
                ConnectionType = inst.Config.ConnectionType,
                LastValue = inst.LastValue
            }));
        }

        private double[] ReadTrendDataAsDouble(KeyenceInstance inst, uint startIndex, uint endIndex)
        {
            var result = new List<double>();
            byte[] buffer = new byte[inst.Config.MaxRequestDataLength];
            uint maxPerBatch = (uint)(inst.Config.MaxRequestDataLength / (16 * 8)) - 1;
            uint totalCount = endIndex - startIndex;
            uint lastBatchCount = totalCount % maxPerBatch;
            uint batches = totalCount / maxPerBatch + 1;

            CL3IF_MEASUREMENT_DATA? lastOne = null;
            bool flag = false;

            for (uint batch = 0; batch < batches; batch++)
            {
                uint batchStart = batch * maxPerBatch + startIndex;
                uint batchLen = (batch < batches - 1) ? maxPerBatch : lastBatchCount;
                if (batchLen == 0) continue;

                uint nextInd, obtained;
                CL3IF_OUTNO outTarget;
                using (var pin = new PinnedObject(buffer))
                {
                    KeyenceNativeMethods.CL3IF_GetTrendData(inst.Config.DeviceId, batchStart, batchLen,
                        out nextInd, out obtained, out outTarget, pin.Pointer);

                    var outTargetList = ConvertOutTargetList(outTarget);
                    int readPos = 0;
                    for (int i = 0; i < (int)obtained; i++)
                    {
                        var measureData = new CL3IF_MEASUREMENT_DATA();
                        measureData.outMeasurementData = new CL3IF_OUTMEASUREMENT_DATA[outTargetList.Count];
                        measureData.addInfo = (CL3IF_ADD_INFO)Marshal.PtrToStructure(pin.Pointer + readPos, typeof(CL3IF_ADD_INFO))!;
                        readPos += Marshal.SizeOf(typeof(CL3IF_ADD_INFO));
                        for (int j = 0; j < outTargetList.Count; j++)
                        {
                            measureData.outMeasurementData[j] = (CL3IF_OUTMEASUREMENT_DATA)Marshal.PtrToStructure(pin.Pointer + readPos, typeof(CL3IF_OUTMEASUREMENT_DATA))!;
                            readPos += Marshal.SizeOf(typeof(CL3IF_OUTMEASUREMENT_DATA));
                        }

                        if (!flag)
                        {
                            flag = true;
                            if (measureData.outMeasurementData[0].judgeResult == 2 ||
                                measureData.outMeasurementData[0].measurementValue == -999999)
                            {
                                result.Add((double)measureData.outMeasurementData[0].measurementValue / 10000);
                            }
                            lastOne = measureData;
                        }
                        else
                        {
                            if ((measureData.outMeasurementData[0].judgeResult == 2 ||
                                 measureData.outMeasurementData[0].measurementValue == -999999) &&
                                lastOne!.Value.outMeasurementData[0].judgeResult == 0)
                            {
                                result.Add((double)measureData.outMeasurementData[0].measurementValue / 10000);
                            }
                            lastOne = measureData;
                        }
                    }
                }
            }
            return result.ToArray();
        }

        private static List<int> ConvertOutTargetList(CL3IF_OUTNO outTarget)
        {
            byte mask = 1;
            var outList = new List<int>();
            for (int i = 0; i < KeyenceNativeMethods.CL3IF_MAX_OUT_COUNT; i++)
            {
                if (((ushort)outTarget & mask) != 0)
                    outList.Add(i + 1);
                mask = (byte)(mask << 1);
            }
            return outList;
        }

        private static CL3IF_SAMPLINGCYCLE MapSamplingCycle(int cycleUs) => cycleUs switch
        {
            100 => CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_100USEC,
            200 => CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_200USEC,
            500 => CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_500USEC,
            1000 => CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_1000USEC,
            _ => CL3IF_SAMPLINGCYCLE.CL3IF_SAMPLINGCYCLE_100USEC
        };

        private static CL3IF_FILTERPARAM_AVERAGE MapFilterAverage(int count) => count switch
        {
            1 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_1,
            2 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_2,
            4 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_4,
            8 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_8,
            16 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_16,
            32 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_32,
            64 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_64,
            256 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_256,
            1024 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_1024,
            4096 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_4096,
            16384 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_16384,
            65536 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_65536,
            262144 => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_262144,
            _ => CL3IF_FILTERPARAM_AVERAGE.CL3IF_FILTERPARAM_AVERAGE_4
        };

        /// <summary>
        /// Internal state for each Keyence device instance.
        /// </summary>
        private class KeyenceInstance
        {
            public MeasurementConfig Config { get; }
            public bool IsConnected { get; set; }
            public double LastValue { get; set; } = -9999;

            public KeyenceInstance(MeasurementConfig config)
            {
                Config = config;
            }
        }
    }
}

using System.Runtime.InteropServices;

namespace HWKUltra.Measurement.Implementations.Keyence
{
    /// <summary>
    /// P/Invoke declarations for the Keyence CL3-IF laser displacement sensor SDK (CL3_IF.dll).
    /// </summary>
    internal static class KeyenceNativeMethods
    {
        // ==================== Return codes ====================
        public const int CL3IF_RC_OK = 0;
        public const int CL3IF_RC_ERR_INITIALIZE = 100;
        public const int CL3IF_RC_ERR_NOT_PARAM = 101;
        public const int CL3IF_RC_ERR_USB = 102;
        public const int CL3IF_RC_ERR_ETHERNET = 103;
        public const int CL3IF_RC_ERR_CONNECT = 105;
        public const int CL3IF_RC_ERR_TIMEOUT = 106;
        public const int CL3IF_RC_ERR_CHECKSUM = 110;
        public const int CL3IF_RC_ERR_LIMIT_CONTROL_ERROR = 120;
        public const int CL3IF_RC_ERR_UNKNOWN = 127;
        public const int CL3IF_RC_ERR_STATE_ERROR = 81;
        public const int CL3IF_RC_ERR_PARAMETER_NUMBER_ERROR = 82;
        public const int CL3IF_RC_ERR_PARAMETER_RANGE_ERROR = 83;
        public const int CL3IF_RC_ERR_UNIQUE_ERROR1 = 84;
        public const int CL3IF_RC_ERR_UNIQUE_ERROR2 = 85;
        public const int CL3IF_RC_ERR_UNIQUE_ERROR3 = 86;

        // ==================== Constants ====================
        public const int CL3IF_MAX_OUT_COUNT = 8;
        public const int CL3IF_MAX_HEAD_COUNT = 6;
        public const int CL3IF_MAX_DEVICE_COUNT = 3;
        public const int CL3IF_ALL_SETTINGS_DATA_LENGTH = 16612;
        public const int CL3IF_PROGRAM_SETTINGS_DATA_LENGTH = 1724;
        public const int CL3IF_LIGHT_WAVE_DATA_LENGTH = 512;
        public const int CL3IF_MAX_LIGHT_WAVE_COUNT = 4;

        private const string DllName = @"CL3_IF.dll";

        // ==================== Communication ====================
        [DllImport(DllName)]
        internal static extern CL3IF_VERSION_INFO CL3IF_GetVersion();

        [DllImport(DllName)]
        internal static extern int CL3IF_OpenUsbCommunication(int deviceId, uint timeout);

        [DllImport(DllName)]
        internal static extern int CL3IF_OpenEthernetCommunication(int deviceId, ref CL3IF_ETHERNET_SETTING ethernetSetting, uint timeout);

        [DllImport(DllName)]
        internal static extern int CL3IF_CloseCommunication(int deviceId);

        // ==================== System ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_GetSystemConfiguration(int deviceId, out byte deviceCount, IntPtr deviceTypeList);

        [DllImport(DllName)]
        internal static extern int CL3IF_ReturnToFactoryDefaultSetting(int deviceId);

        // ==================== Measurement data ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_GetMeasurementData(int deviceId, IntPtr measurementData);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetTrendIndex(int deviceId, out uint index);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetTrendData(int deviceId, uint index, uint requestDataCount, out uint nextIndex, out uint obtainedDataCount, out CL3IF_OUTNO outTarget, IntPtr measurementData);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetStorageIndex(int deviceId, CL3IF_SELECTED_INDEX selectedIndex, out uint index);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetStorageData(int deviceId, uint index, uint requestDataCount, out uint nextIndex, out uint obtainedDataCount, out CL3IF_OUTNO outTarget, IntPtr measurementData);

        // ==================== Control ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_AutoZeroSingle(int deviceId, byte outNo, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_AutoZeroMulti(int deviceId, CL3IF_OUTNO outNo, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_AutoZeroGroup(int deviceId, CL3IF_ZERO_GROUP group, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_TimingSingle(int deviceId, byte outNo, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_TimingMulti(int deviceId, CL3IF_OUTNO outNo, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_TimingGroup(int deviceId, CL3IF_TIMING_GROUP group, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_ResetSingle(int deviceId, byte outNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_ResetMulti(int deviceId, CL3IF_OUTNO outNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_ResetGroup(int deviceId, CL3IF_RESET_GROUP group);

        [DllImport(DllName)]
        internal static extern int CL3IF_LightControl(int deviceId, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_MeasurementControl(int deviceId, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_SwitchProgram(int deviceId, byte programNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetProgramNo(int deviceId, out byte programNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_LockPanel(int deviceId, bool onOff);

        // ==================== Storage ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_StartStorage(int deviceId);

        [DllImport(DllName)]
        internal static extern int CL3IF_StopStorage(int deviceId);

        [DllImport(DllName)]
        internal static extern int CL3IF_ClearStorageData(int deviceId);

        // ==================== Terminal / Pulse ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_GetTerminalStatus(int deviceId, out ushort inputTerminalStatus, out ushort outputTerminalStatus);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetPulseCount(int deviceId, out int pulseCount);

        [DllImport(DllName)]
        internal static extern int CL3IF_ResetPulseCount(int deviceId);

        // ==================== Light / Calibration ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_GetLightWaveform(int deviceId, byte headNo, CL3IF_PEAKNO peakNo, IntPtr waveData);

        [DllImport(DllName)]
        internal static extern int CL3IF_StartLightIntensityTuning(int deviceId, byte headNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_StopLightIntensityTuning(int deviceId, byte headNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_CancelLightIntensityTuning(int deviceId, byte headNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_StartCalibration(int deviceId, byte headNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_StopCalibration(int deviceId, byte headNo);

        [DllImport(DllName)]
        internal static extern int CL3IF_CancelCalibration(int deviceId, byte headNo);

        // ==================== Settings (Set) ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_SetSettings(int deviceId, byte[] settings);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetProgram(int deviceId, byte programNo, byte[] program);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetSamplingCycle(int deviceId, byte programNo, CL3IF_SAMPLINGCYCLE samplingCycle);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetMutualInterferencePrevention(int deviceId, byte programNo, bool onOff, ushort group);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetAmbientLightFilter(int deviceId, byte programNo, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetMedianFilter(int deviceId, byte programNo, byte headNo, CL3IF_MEDIANFILTER medianFilter);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetThreshold(int deviceId, byte programNo, byte headNo, CL3IF_MODE mode, byte value);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetMask(int deviceId, byte programNo, byte headNo, bool onOff, int position1, int position2);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetLightIntensityControl(int deviceId, byte programNo, byte headNo, CL3IF_MODE mode, byte upperLimit, byte lowerLimit);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetPeakShapeFilter(int deviceId, byte programNo, byte headNo, bool onOff, CL3IF_INTENSITY intensity);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetLightIntensityIntegration(int deviceId, byte programNo, byte headNo, CL3IF_INTEGRATION_NUMBER integrationNumber);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetQuadProcessing(int deviceId, byte programNo, byte headNo, CL3IF_QUADPROCESSING processing, byte quadValidPoint);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetMeasurementPeaks(int deviceId, byte programNo, byte headNo, byte peaks);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetCheckingNumberOfPeaks(int deviceId, byte programNo, byte headNo, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetMultiLightIntensityControl(int deviceId, byte programNo, byte headNo, bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetRefractiveIndexCorrection(int deviceId, byte programNo, byte headNo, bool onOff, CL3IF_MATERIAL layer1, CL3IF_MATERIAL layer2, CL3IF_MATERIAL layer3);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetMeasurementMethod(int deviceId, byte programNo, byte outNo, CL3IF_MEASUREMENTMETHOD method, CL3IF_MEASUREMENTMETHOD_PARAM param);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetScaling(int deviceId, byte programNo, byte outNo, int inputValue1, int outputValue1, int inputValue2, int outputValue2);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetOffset(int deviceId, byte programNo, byte outNo, int offset);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetTolerance(int deviceId, byte programNo, byte outNo, int upperLimit, int lowerLimit, int hysteresis);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetFilter(int deviceId, byte programNo, byte outNo, CL3IF_FILTERMODE filterMode, ushort filterParam);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetHold(int deviceId, byte programNo, byte outNo, CL3IF_HOLDMODE holdMode, CL3IF_HOLDMODE_PARAM param);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetInvalidDataProcessing(int deviceId, byte programNo, byte outNo, ushort invalidationNumber, ushort recoveryNumber);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetDisplayUnit(int deviceId, byte programNo, byte outNo, CL3IF_DISPLAYUNIT displayUnit);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetTerminalAllocation(int deviceId, byte programNo, byte outNo, CL3IF_TIMINGRESET timingReset, CL3IF_ZERO zero);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetJudgmentOutput(int deviceId, byte programNo, CL3IF_JUDGMENT_OUTPUT[] judgmentOutput);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetStorageNumber(int deviceId, byte programNo, byte overwrite, uint storageNumber);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetStorageTiming(int deviceId, byte programNo, byte storageTiming, CL3IF_STORAGETIMING_PARAM param);

        [DllImport(DllName)]
        internal static extern int CL3IF_SetStorageTarget(int deviceId, byte programNo, CL3IF_OUTNO outNo);

        // ==================== Settings (Get) ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_GetSettings(int deviceId, IntPtr settings);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetProgram(int deviceId, byte programNo, IntPtr program);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetSamplingCycle(int deviceId, byte programNo, out CL3IF_SAMPLINGCYCLE samplingCycle);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetMutualInterferencePrevention(int deviceId, byte programNo, out bool onOff, out ushort group);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetAmbientLightFilter(int deviceId, byte programNo, out bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetMedianFilter(int deviceId, byte programNo, byte headNo, out CL3IF_MEDIANFILTER medianFilter);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetThreshold(int deviceId, byte programNo, byte headNo, out CL3IF_MODE mode, out byte value);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetMask(int deviceId, byte programNo, byte headNo, out bool onOff, out int position1, out int position2);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetLightIntensityControl(int deviceId, byte programNo, byte headNo, out CL3IF_MODE mode, out byte upperLimit, out byte lowerLimit);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetPeakShapeFilter(int deviceId, byte programNo, byte headNo, out bool onOff, out CL3IF_INTENSITY intensity);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetLightIntensityIntegration(int deviceId, byte programNo, byte headNo, out CL3IF_INTEGRATION_NUMBER integrationNumber);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetQuadProcessing(int deviceId, byte programNo, byte headNo, out CL3IF_QUADPROCESSING processing, out byte quadValidPoint);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetMeasurementPeaks(int deviceId, byte programNo, byte headNo, out byte peaks);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetCheckingNumberOfPeaks(int deviceId, byte programNo, byte headNo, out bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetMultiLightIntensityControl(int deviceId, byte programNo, byte headNo, out bool onOff);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetRefractiveIndexCorrection(int deviceId, byte programNo, byte headNo, out bool onOff, out CL3IF_MATERIAL layer1, out CL3IF_MATERIAL layer2, out CL3IF_MATERIAL layer3);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetMeasurementMethod(int deviceId, byte programNo, byte outNo, out CL3IF_MEASUREMENTMETHOD method, out CL3IF_MEASUREMENTMETHOD_PARAM param);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetScaling(int deviceId, byte programNo, byte outNo, out int inputValue1, out int outputValue1, out int inputValue2, out int outputValue2);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetOffset(int deviceId, byte programNo, byte outNo, out int offset);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetTolerance(int deviceId, byte programNo, byte outNo, out int upperLimit, out int lowerLimit, out int hysteresis);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetFilter(int deviceId, byte programNo, byte outNo, out CL3IF_FILTERMODE filterMode, out ushort filterParam);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetHold(int deviceId, byte programNo, byte outNo, out CL3IF_HOLDMODE holdMode, out CL3IF_HOLDMODE_PARAM param);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetInvalidDataProcessing(int deviceId, byte programNo, byte outNo, out ushort invalidationNumber, out ushort recoveryNumber);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetDisplayUnit(int deviceId, byte programNo, byte outNo, out CL3IF_DISPLAYUNIT displayUnit);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetTerminalAllocation(int deviceId, byte programNo, byte outNo, out CL3IF_TIMINGRESET timingReset, out CL3IF_ZERO zero);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetJudgmentOutput(int deviceId, byte programNo, IntPtr judgmentOutput);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetStorageNumber(int deviceId, byte programNo, out byte overwrite, out uint storageNumber);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetStorageTiming(int deviceId, byte programNo, out byte storageTiming, out CL3IF_STORAGETIMING_PARAM param);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetStorageTarget(int deviceId, byte programNo, out CL3IF_OUTNO outNo);

        // ==================== Mode transition ====================
        [DllImport(DllName)]
        internal static extern int CL3IF_TransitToMeasurementMode(int deviceId);

        [DllImport(DllName)]
        internal static extern int CL3IF_TransitToSettingMode(int deviceId);
    }
}

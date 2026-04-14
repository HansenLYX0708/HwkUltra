using System;
using System.Runtime.InteropServices;

namespace WD.AVI.LaserLib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_VERSION_INFO
    {
        public int majorNumber;
        public int minorNumber;
        public int revisionNumber;
        public int buildNumber;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_ETHERNET_SETTING
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ipAddress;
        public ushort portNo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserved;
    };

    public enum CL3IF_DEVICETYPE
    {
        CL3IF_DEVICETYPE_INVALID = 0x0000,
        CL3IF_DEVICETYPE_CONTROLLER = 0x0001,
        CL3IF_DEVICETYPE_OPTICALUNIT1 = 0x0011,
        CL3IF_DEVICETYPE_OPTICALUNIT2 = 0x0012,
        CL3IF_DEVICETYPE_OPTICALUNIT3 = 0x0013,
        CL3IF_DEVICETYPE_OPTICALUNIT4 = 0x0014,
        CL3IF_DEVICETYPE_OPTICALUNIT5 = 0x0015,
        CL3IF_DEVICETYPE_OPTICALUNIT6 = 0x0016,
        CL3IF_DEVICETYPE_EXUNIT1 = 0x0041,     // Extensional Unit 1
        CL3IF_DEVICETYPE_EXUNIT2 = 0x0042      // Extensional Unit 2
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_ADD_INFO
    {
        public uint triggerCount;
        public int pulseCount;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_OUTMEASUREMENT_DATA
    {
        public int measurementValue;
        public byte valueInfo;
        public byte judgeResult;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserved;

    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_MEASUREMENT_DATA
    {
        public CL3IF_ADD_INFO addInfo;
        public CL3IF_OUTMEASUREMENT_DATA[] outMeasurementData;
    };

    public enum CL3IF_VALUE_INFO
    {
        CL3IF_VALUE_INFO_VALID,
        CL3IF_VALUE_INFO_JUDGMENTSTANDBY,
        CL3IF_VALUE_INFO_INVALID,
        CL3IF_VALUE_INFO_OVERDISPRANGE_P,
        CL3IF_VALUE_INFO_OVERDISPRANGE_N
    };

    [Flags]
    public enum CL3IF_JUDGE_RESULT
    {
        CL3IF_JUDGE_RESULT_HI = 0x01,
        CL3IF_JUDGE_RESULT_GO = 0x02,
        CL3IF_JUDGE_RESULT_LO = 0x04
    };

    [Flags]
    public enum CL3IF_OUTNO
    {
        CL3IF_OUTNO_01 = 0x0001, // OUT1
        CL3IF_OUTNO_02 = 0x0002, // OUT2
        CL3IF_OUTNO_03 = 0x0004, // OUT3
        CL3IF_OUTNO_04 = 0x0008, // OUT4
        CL3IF_OUTNO_05 = 0x0010, // OUT5
        CL3IF_OUTNO_06 = 0x0020, // OUT6
        CL3IF_OUTNO_07 = 0x0040, // OUT7
        CL3IF_OUTNO_08 = 0x0080, // OUT8
        CL3IF_OUTNO_ALL = 0x00FF // ALL
    } ;

    public enum CL3IF_SELECTED_INDEX
    {
        CL3IF_SELECTED_INDEX_OLDEST,
        CL3IF_SELECTED_INDEX_NEWEST
    };

    [Flags]
    public enum CL3IF_ZERO_GROUP
    {
        CL3IF_ZERO_GROUP_01 = 0x0001, // Group01
        CL3IF_ZERO_GROUP_02 = 0x0002  // Group02
    };

    [Flags]
    public enum CL3IF_TIMING_GROUP
    {
        CL3IF_TIMING_GROUP_01 = 0x0001, // Group01
        CL3IF_TIMING_GROUP_02 = 0x0002  // Group02
    };

    [Flags]
    public enum CL3IF_RESET_GROUP
    {
        CL3IF_RESET_GROUP_01 = 0x0001, // Group01
        CL3IF_RESET_GROUP_02 = 0x0002  // Group02
    };

    [Flags]
    public enum CL3IF_PEAKNO
    {
        CL3IF_PEAKNO_01 = 0x0001,
        CL3IF_PEAKNO_02 = 0x0002,
        CL3IF_PEAKNO_03 = 0x0004,
        CL3IF_PEAKNO_04 = 0x0008
    };

    public enum CL3IF_SAMPLINGCYCLE
    {
        CL3IF_SAMPLINGCYCLE_100USEC,
        CL3IF_SAMPLINGCYCLE_200USEC,
        CL3IF_SAMPLINGCYCLE_500USEC,
        CL3IF_SAMPLINGCYCLE_1000USEC
    };

    public enum CL3IF_MEDIANFILTER
    {
        CL3IF_MEDIANFILTER_OFF, // OFF
        CL3IF_MEDIANFILTER_7,   // 7
        CL3IF_MEDIANFILTER_15,  // 15
        CL3IF_MEDIANFILTER_31   // 31
    };

    public enum CL3IF_MODE
    {
        CL3IF_MODE_AUTO,    //AUTO
        CL3IF_MODE_MANUAL   //MANUAL
    };

    public enum CL3IF_INTENSITY
    {
        CL3IF_INTENSITY_1, // 1
        CL3IF_INTENSITY_2, // 2
        CL3IF_INTENSITY_3, // 3
        CL3IF_INTENSITY_4, // 4
        CL3IF_INTENSITY_5  // 5
    };

    public enum CL3IF_INTEGRATION_NUMBER
    {
        CL3IF_INTEGRATION_NUMBER_OFF, // OFF
        CL3IF_INTEGRATION_NUMBER_4,   // 4
        CL3IF_INTEGRATION_NUMBER_16,  // 16
        CL3IF_INTEGRATION_NUMBER_64,  // 64
        CL3IF_INTEGRATION_NUMBER_256  // 256
    };

    public enum CL3IF_QUADPROCESSING
    {
        CL3IF_QUADPROCESSING_AVERAGE,
        CL3IF_QUADPROCESSING_MULTIPLE
    };

    public enum CL3IF_MATERIAL
    {
        CL3IF_MATERIAL_VACUUM,
        CL3IF_MATERIAL_QUARTZ,
        CL3IF_MATERIAL_OPTICAL_GLASS,
        CL3IF_MATERIAL_ACRYLIC,
        CL3IF_MATERIAL_PMMA,
        CL3IF_MATERIAL_PMMI,
        CL3IF_MATERIAL_PS,
        CL3IF_MATERIAL_PC,
        CL3IF_MATERIAL_WHITE_FLAT_GLASS,
        CL3IF_MATERIAL_RESERVED1,
        CL3IF_MATERIAL_RESERVED2,
        CL3IF_MATERIAL_RESERVED3,
        CL3IF_MATERIAL_RESERVED4,
        CL3IF_MATERIAL_RESERVED5,
        CL3IF_MATERIAL_RESERVED6,
        CL3IF_MATERIAL_RESERVED7,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL1,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL2,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL3,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL4,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL5,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL6,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL7,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL8,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL9,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL10,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL11,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL12,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL13,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL14,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL15,
        CL3IF_MATERIAL_ADDITIONAL_MATERIAL16
    };

    public enum CL3IF_MEASUREMENTMETHOD
    {
        CL3IF_MEASUREMENTMETHOD_DISPLACEMENT,
        CL3IF_MEASUREMENTMETHOD_DISPLACEMENT_FOR_TRANSPARENT,
        CL3IF_MEASUREMENTMETHOD_THICKNESS_FOR_TRANSPARENT,
        CL3IF_MEASUREMENTMETHOD_THICKNESS_2HEADS,
        CL3IF_MEASUREMENTMETHOD_HEIGHTDIFFERENCE_2HEADS,
        CL3IF_MEASUREMENTMETHOD_FORMULA,
        CL3IF_MEASUREMENTMETHOD_AVERAGE,
        CL3IF_MEASUREMENTMETHOD_PEAK_TO_PEAK,
        CL3IF_MEASUREMENTMETHOD_MAX,
        CL3IF_MEASUREMENTMETHOD_MIN,
        CL3IF_MEASUREMENTMETHOD_NO_CALCULATION
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT
    {
        [FieldOffset(0)]
        public byte headNo;
        [FieldOffset(1)]
        public byte reserved_1;
        [FieldOffset(2)]
        public byte reserved_2;
        [FieldOffset(3)]
        public byte reserved_3;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT_FOR_TRANSPARENT
    {
        [FieldOffset(0)]
        public byte headNo;
        [FieldOffset(1)]
        public byte reserved1;
        [FieldOffset(2)]
        public byte peak;
        [FieldOffset(3)]
        public byte reserved2;
    };

    public enum CL3IF_TRANSPARENTPEAK
    {
        CL3IF_TRANSPARENTPEAK_PLUS1,  // +1
        CL3IF_TRANSPARENTPEAK_PLUS2,  // +2
        CL3IF_TRANSPARENTPEAK_PLUS3,  // +3
        CL3IF_TRANSPARENTPEAK_PLUS4,  // +4
        CL3IF_TRANSPARENTPEAK_MINUS1, // -1
        CL3IF_TRANSPARENTPEAK_MINUS2, // -2
        CL3IF_TRANSPARENTPEAK_MINUS3, // -3
        CL3IF_TRANSPARENTPEAK_MINUS4  // -4
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_FOR_TRANSPARENT
    {
        [FieldOffset(0)]
        public byte headNo;
        [FieldOffset(1)]
        public byte reserved;
        [FieldOffset(2)]
        public byte peak1;
        [FieldOffset(3)]
        public byte peak2;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_2HEADS
    {
        [FieldOffset(0)]
        public byte headNo1;
        [FieldOffset(1)]
        public byte headNo2;
        [FieldOffset(2)]
        public byte reserved_1;
        [FieldOffset(3)]
        public byte reserved_2;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_HEIGHTDIFFERENCE_2HEADS
    {
        [FieldOffset(0)]
        public byte headNo1;
        [FieldOffset(1)]
        public byte headNo2;
        [FieldOffset(2)]
        public byte reserved_1;
        [FieldOffset(3)]
        public byte reserved_2;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_FORMULA
    {
        [FieldOffset(0)]
        public int factorA;
        [FieldOffset(4)]
        public int factorB;
        [FieldOffset(8)]
        public int factorC;
        [FieldOffset(12)]
        public byte targetOutX;
        [FieldOffset(13)]
        public byte targetOutY;
        [FieldOffset(14)]
        public byte reserved_1;
        [FieldOffset(15)]
        public byte reserved_2;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_OUT_OPERATION
    {
        [FieldOffset(0)]
        public ushort targetOut;
        [FieldOffset(2)]
        public byte reserved_1;
        [FieldOffset(3)]
        public byte reserved_2;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_NO_CALCULATION
    {
        [FieldOffset(0)]
        public byte reserved_1;
        [FieldOffset(1)]
        public byte reserved_2;
        [FieldOffset(2)]
        public byte reserved_3;
        [FieldOffset(3)]
        public byte reserved_4;
    };


    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM
    {
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT paramDisplacement;
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT_FOR_TRANSPARENT paramDisplacementForTransparent;
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_FOR_TRANSPARENT paramThicknessForTransparent;
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_2HEADS paramThickness2Heads;
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_HEIGHTDIFFERENCE_2HEADS paramHeightDifference2Heads;
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_FORMULA paramFormula;
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_OUT_OPERATION paramOutOperation;
        [FieldOffset(0)]
        public CL3IF_MEASUREMENTMETHOD_PARAM_NO_CALCULATION paramNoCalculation;
    };

    public enum CL3IF_FILTERMODE
    {
        CL3IF_FILTERMODE_MOVING_AVERAGE, // Moving average
        CL3IF_FILTERMODE_LOWPASS,        // Low pass filter
        CL3IF_FILTERMODE_HIGHPASS        // High pass filter
    };

    public enum CL3IF_FILTERPARAM_AVERAGE
    {
        CL3IF_FILTERPARAM_AVERAGE_1,      // 1 time
        CL3IF_FILTERPARAM_AVERAGE_2,
        CL3IF_FILTERPARAM_AVERAGE_4,
        CL3IF_FILTERPARAM_AVERAGE_8,
        CL3IF_FILTERPARAM_AVERAGE_16,
        CL3IF_FILTERPARAM_AVERAGE_32,
        CL3IF_FILTERPARAM_AVERAGE_64,
        CL3IF_FILTERPARAM_AVERAGE_256,
        CL3IF_FILTERPARAM_AVERAGE_1024,
        CL3IF_FILTERPARAM_AVERAGE_4096,
        CL3IF_FILTERPARAM_AVERAGE_16384,
        CL3IF_FILTERPARAM_AVERAGE_65536,
        CL3IF_FILTERPARAM_AVERAGE_262144  // 262144 times
    } ;

    public enum CL3IF_FILTERPARAM_CUTOFF
    {
        CL3IF_FILTERPARAM_CUTOFF_1000,      // 1000Hz
        CL3IF_FILTERPARAM_CUTOFF_300,
        CL3IF_FILTERPARAM_CUTOFF_100,
        CL3IF_FILTERPARAM_CUTOFF_30,
        CL3IF_FILTERPARAM_CUTOFF_10,
        CL3IF_FILTERPARAM_CUTOFF_3,
        CL3IF_FILTERPARAM_CUTOFF_1,
        CL3IF_FILTERPARAM_CUTOFF_0_3,
        CL3IF_FILTERPARAM_CUTOFF_0_1        // 0.1Hz
    } ;

    public enum CL3IF_HOLDMODE
    {
        CL3IF_HOLDMODE_NORMAL,          // Normal
        CL3IF_HOLDMODE_PEAK,            // Peak hold
        CL3IF_HOLDMODE_BOTTOM,          // Bottom hold
        CL3IF_HOLDMODE_PEAK_TO_PEAK,    // Peak to peak hold
        CL3IF_HOLDMODE_SAMPLE,          // Sample hold
        CL3IF_HOLDMODE_AVERAGE,         // Average hold
        CL3IF_HOLDMODE_AUTOPEAK,        // Auto Peak hold
        CL3IF_HOLDMODE_AUTOBOTTOM       // Auto bottom hold
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM_NORMAL
    {
        [FieldOffset(0)]
        public byte reserved_1;
        [FieldOffset(1)]
        public byte reserved_2;
        [FieldOffset(2)]
        public byte reserved_3;
        [FieldOffset(3)]
        public byte reserved_4;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM_HOLD
    {
        [FieldOffset(0)]
        public byte updateCondition;
        [FieldOffset(1)]
        public byte reserved;
        [FieldOffset(2)]
        public ushort numberOfSamplings;
    };

    public enum CL3IF_UPDATECONDITION
    {
        CL3IF_UPDATECONDITION_EXTERNAL1,    // External trigger 1
        CL3IF_UPDATECONDITION_EXTERNAL2,    // External trigger 2
        CL3IF_UPDATECONDITION_INTERNAL      // Internal trigger
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM_AUTOHOLD
    {
        [FieldOffset(0)]
        public int level;
        [FieldOffset(4)]
        public int hysteresis;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM
    {
        [FieldOffset(0)]
        public CL3IF_HOLDMODE_PARAM_NORMAL paramNormal;
        [FieldOffset(0)]
        public CL3IF_HOLDMODE_PARAM_HOLD paramHold;
        [FieldOffset(0)]
        public CL3IF_HOLDMODE_PARAM_AUTOHOLD paramAutoHold;
    };

    public enum CL3IF_DISPLAYUNIT
    { 
        CL3IF_DISPLAYUNIT_0_01MM,       // 0.01mm
        CL3IF_DISPLAYUNIT_0_001MM,      // 0.001mm
        CL3IF_DISPLAYUNIT_0_0001MM,     // 0.0001mm
        CL3IF_DISPLAYUNIT_0_00001MM,    // 0.00001mm
        CL3IF_DISPLAYUNIT_0_1UM,        // 0.1um
        CL3IF_DISPLAYUNIT_0_01UM,       // 0.01um
        CL3IF_DISPLAYUNIT_0_001UM       // 0.001um
    };

    public enum CL3IF_TIMINGRESET
    {
        CL3IF_TIMINGRESET_NONE, // None
        CL3IF_TIMINGRESET_1,    // Timing1/Reset1
        CL3IF_TIMINGRESET_2     // Timing2/Reset2
    };

    public enum CL3IF_ZERO
    {
        CL3IF_ZERO_NONE,
        CL3IF_ZERO_1,
        CL3IF_ZERO_2
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_JUDGMENT_OUTPUT
    {
        public byte logic;
        public byte strobe;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserved1;
        public ushort hi;
        public ushort go;
        public ushort lo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserved2;
    }

    public enum CL3IF_LOGIC
    { 
        CL3IF_LOGIC_AND,
        CL3IF_LOGIC_OR
    };

    public enum CL3IF_STROBE
    { 
        CL3IF_STROBE_NO,
        CL3IF_STROBE_STROBE1,
        CL3IF_STROBE_STROBE2
    };

    public enum CL3IF_STORAGETIMING
    {
        CL3IF_STORAGETIMING_MEASUREMENT,
        CL3IF_STORAGETIMING_JUDGMENT
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_STORAGETIMING_PARAM_MEASUREMENT
    {
        [FieldOffset(0)]
        public ushort storageCycle;
        [FieldOffset(2)]
        public byte reserved_1;
        [FieldOffset(3)]
        public byte reserved_2;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_STORAGETIMING_PARAM_JUDGMENT
    {
        [FieldOffset(0)]
        public byte logic;
        [FieldOffset(1)]
        public byte reserved1_1;
        [FieldOffset(2)]
        public byte reserved1_2;
        [FieldOffset(3)]
        public byte reserved1_3;
        [FieldOffset(4)]
        public ushort hi;
        [FieldOffset(6)]
        public ushort go;
        [FieldOffset(8)]
        public ushort lo;
        [FieldOffset(10)]
        public byte reserved2_1;
        [FieldOffset(11)]
        public byte reserved2_2;
    };

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_STORAGETIMING_PARAM
    {
        [FieldOffset(0)]
        public CL3IF_STORAGETIMING_PARAM_MEASUREMENT paramMeasurement;
        [FieldOffset(0)]
        public CL3IF_STORAGETIMING_PARAM_JUDGMENT paramJudgment;
    };

    public class NativeMethods
    {
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

        public const int CL3IF_MAX_OUT_COUNT = 8;
        public const int CL3IF_MAX_HEAD_COUNT = 6;
        public const int CL3IF_MAX_DEVICE_COUNT = 3;
        public const int CL3IF_ALL_SETTINGS_DATA_LENGTH = 16612;
        public const int CL3IF_PROGRAM_SETTINGS_DATA_LENGTH = 1724;
        public const int CL3IF_LIGHT_WAVE_DATA_LENGTH = 512;
        public const int CL3IF_MAX_LIGHT_WAVE_COUNT = 4;

        private const string DllName = @"CL3_IF.dll";

        [DllImport(DllName)]
        internal static extern CL3IF_VERSION_INFO CL3IF_GetVersion();

        [DllImport(DllName)]
        internal static extern int CL3IF_OpenUsbCommunication(int deviceId, uint timeout);
        [DllImport(DllName)]
        internal static extern int CL3IF_OpenEthernetCommunication(int deviceId, ref CL3IF_ETHERNET_SETTING ethernetSetting, uint timeout);
        [DllImport(DllName)]
        internal static extern int CL3IF_CloseCommunication(int deviceId);

        [DllImport(DllName)]
        internal static extern int CL3IF_GetSystemConfiguration(int deviceId, out byte deviceCount, IntPtr deviceTypeList);
        [DllImport(DllName)]
        internal static extern int CL3IF_ReturnToFactoryDefaultSetting(int deviceId);

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
        [DllImport(DllName)]
        internal static extern int CL3IF_StartStorage(int deviceId);
        [DllImport(DllName)]
        internal static extern int CL3IF_StopStorage(int deviceId);
        [DllImport(DllName)]
        internal static extern int CL3IF_ClearStorageData(int deviceId);
        [DllImport(DllName)]
        internal static extern int CL3IF_GetTerminalStatus(int deviceId, out ushort inputTerminalStatus, out ushort outputTerminalStatus);
        [DllImport(DllName)]
        internal static extern int CL3IF_GetPulseCount(int deviceId, out int pulseCount);
        [DllImport(DllName)]
        internal static extern int CL3IF_ResetPulseCount(int deviceId);
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

        [DllImport(DllName)]
        internal static extern int CL3IF_TransitToMeasurementMode(int deviceId);
        [DllImport(DllName)]
        internal static extern int CL3IF_TransitToSettingMode(int deviceId);
    }
}

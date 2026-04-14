using System.Runtime.InteropServices;

namespace HWKUltra.Measurement.Implementations.Keyence
{
    // ==================== Structs ====================

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_VERSION_INFO
    {
        public int majorNumber;
        public int minorNumber;
        public int revisionNumber;
        public int buildNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_ETHERNET_SETTING
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ipAddress;
        public ushort portNo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CL3IF_ADD_INFO
    {
        public uint triggerCount;
        public int pulseCount;
    }

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
    }

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

    // ==================== Measurement method parameter structs ====================

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT
    {
        [FieldOffset(0)] public byte headNo;
        [FieldOffset(1)] public byte reserved_1;
        [FieldOffset(2)] public byte reserved_2;
        [FieldOffset(3)] public byte reserved_3;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT_FOR_TRANSPARENT
    {
        [FieldOffset(0)] public byte headNo;
        [FieldOffset(1)] public byte reserved1;
        [FieldOffset(2)] public byte peak;
        [FieldOffset(3)] public byte reserved2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_FOR_TRANSPARENT
    {
        [FieldOffset(0)] public byte headNo;
        [FieldOffset(1)] public byte reserved;
        [FieldOffset(2)] public byte peak1;
        [FieldOffset(3)] public byte peak2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_2HEADS
    {
        [FieldOffset(0)] public byte headNo1;
        [FieldOffset(1)] public byte headNo2;
        [FieldOffset(2)] public byte reserved_1;
        [FieldOffset(3)] public byte reserved_2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_HEIGHTDIFFERENCE_2HEADS
    {
        [FieldOffset(0)] public byte headNo1;
        [FieldOffset(1)] public byte headNo2;
        [FieldOffset(2)] public byte reserved_1;
        [FieldOffset(3)] public byte reserved_2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_FORMULA
    {
        [FieldOffset(0)] public int factorA;
        [FieldOffset(4)] public int factorB;
        [FieldOffset(8)] public int factorC;
        [FieldOffset(12)] public byte targetOutX;
        [FieldOffset(13)] public byte targetOutY;
        [FieldOffset(14)] public byte reserved_1;
        [FieldOffset(15)] public byte reserved_2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_OUT_OPERATION
    {
        [FieldOffset(0)] public ushort targetOut;
        [FieldOffset(2)] public byte reserved_1;
        [FieldOffset(3)] public byte reserved_2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM_NO_CALCULATION
    {
        [FieldOffset(0)] public byte reserved_1;
        [FieldOffset(1)] public byte reserved_2;
        [FieldOffset(2)] public byte reserved_3;
        [FieldOffset(3)] public byte reserved_4;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_MEASUREMENTMETHOD_PARAM
    {
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT paramDisplacement;
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_DISPLACEMENT_FOR_TRANSPARENT paramDisplacementForTransparent;
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_FOR_TRANSPARENT paramThicknessForTransparent;
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_THICKNESS_2HEADS paramThickness2Heads;
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_HEIGHTDIFFERENCE_2HEADS paramHeightDifference2Heads;
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_FORMULA paramFormula;
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_OUT_OPERATION paramOutOperation;
        [FieldOffset(0)] public CL3IF_MEASUREMENTMETHOD_PARAM_NO_CALCULATION paramNoCalculation;
    }

    // ==================== Hold mode parameter structs ====================

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM_NORMAL
    {
        [FieldOffset(0)] public byte reserved_1;
        [FieldOffset(1)] public byte reserved_2;
        [FieldOffset(2)] public byte reserved_3;
        [FieldOffset(3)] public byte reserved_4;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM_HOLD
    {
        [FieldOffset(0)] public byte updateCondition;
        [FieldOffset(1)] public byte reserved;
        [FieldOffset(2)] public ushort numberOfSamplings;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM_AUTOHOLD
    {
        [FieldOffset(0)] public int level;
        [FieldOffset(4)] public int hysteresis;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_HOLDMODE_PARAM
    {
        [FieldOffset(0)] public CL3IF_HOLDMODE_PARAM_NORMAL paramNormal;
        [FieldOffset(0)] public CL3IF_HOLDMODE_PARAM_HOLD paramHold;
        [FieldOffset(0)] public CL3IF_HOLDMODE_PARAM_AUTOHOLD paramAutoHold;
    }

    // ==================== Storage timing parameter structs ====================

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_STORAGETIMING_PARAM_MEASUREMENT
    {
        [FieldOffset(0)] public ushort storageCycle;
        [FieldOffset(2)] public byte reserved_1;
        [FieldOffset(3)] public byte reserved_2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_STORAGETIMING_PARAM_JUDGMENT
    {
        [FieldOffset(0)] public byte logic;
        [FieldOffset(1)] public byte reserved1_1;
        [FieldOffset(2)] public byte reserved1_2;
        [FieldOffset(3)] public byte reserved1_3;
        [FieldOffset(4)] public ushort hi;
        [FieldOffset(6)] public ushort go;
        [FieldOffset(8)] public ushort lo;
        [FieldOffset(10)] public byte reserved2_1;
        [FieldOffset(11)] public byte reserved2_2;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct CL3IF_STORAGETIMING_PARAM
    {
        [FieldOffset(0)] public CL3IF_STORAGETIMING_PARAM_MEASUREMENT paramMeasurement;
        [FieldOffset(0)] public CL3IF_STORAGETIMING_PARAM_JUDGMENT paramJudgment;
    }

    // ==================== Enums ====================

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
        CL3IF_DEVICETYPE_EXUNIT1 = 0x0041,
        CL3IF_DEVICETYPE_EXUNIT2 = 0x0042
    }

    public enum CL3IF_VALUE_INFO
    {
        CL3IF_VALUE_INFO_VALID,
        CL3IF_VALUE_INFO_JUDGMENTSTANDBY,
        CL3IF_VALUE_INFO_INVALID,
        CL3IF_VALUE_INFO_OVERDISPRANGE_P,
        CL3IF_VALUE_INFO_OVERDISPRANGE_N
    }

    [Flags]
    public enum CL3IF_JUDGE_RESULT
    {
        CL3IF_JUDGE_RESULT_HI = 0x01,
        CL3IF_JUDGE_RESULT_GO = 0x02,
        CL3IF_JUDGE_RESULT_LO = 0x04
    }

    [Flags]
    public enum CL3IF_OUTNO
    {
        CL3IF_OUTNO_01 = 0x0001,
        CL3IF_OUTNO_02 = 0x0002,
        CL3IF_OUTNO_03 = 0x0004,
        CL3IF_OUTNO_04 = 0x0008,
        CL3IF_OUTNO_05 = 0x0010,
        CL3IF_OUTNO_06 = 0x0020,
        CL3IF_OUTNO_07 = 0x0040,
        CL3IF_OUTNO_08 = 0x0080,
        CL3IF_OUTNO_ALL = 0x00FF
    }

    public enum CL3IF_SELECTED_INDEX
    {
        CL3IF_SELECTED_INDEX_OLDEST,
        CL3IF_SELECTED_INDEX_NEWEST
    }

    [Flags]
    public enum CL3IF_ZERO_GROUP
    {
        CL3IF_ZERO_GROUP_01 = 0x0001,
        CL3IF_ZERO_GROUP_02 = 0x0002
    }

    [Flags]
    public enum CL3IF_TIMING_GROUP
    {
        CL3IF_TIMING_GROUP_01 = 0x0001,
        CL3IF_TIMING_GROUP_02 = 0x0002
    }

    [Flags]
    public enum CL3IF_RESET_GROUP
    {
        CL3IF_RESET_GROUP_01 = 0x0001,
        CL3IF_RESET_GROUP_02 = 0x0002
    }

    [Flags]
    public enum CL3IF_PEAKNO
    {
        CL3IF_PEAKNO_01 = 0x0001,
        CL3IF_PEAKNO_02 = 0x0002,
        CL3IF_PEAKNO_03 = 0x0004,
        CL3IF_PEAKNO_04 = 0x0008
    }

    public enum CL3IF_SAMPLINGCYCLE
    {
        CL3IF_SAMPLINGCYCLE_100USEC,
        CL3IF_SAMPLINGCYCLE_200USEC,
        CL3IF_SAMPLINGCYCLE_500USEC,
        CL3IF_SAMPLINGCYCLE_1000USEC
    }

    public enum CL3IF_MEDIANFILTER
    {
        CL3IF_MEDIANFILTER_OFF,
        CL3IF_MEDIANFILTER_7,
        CL3IF_MEDIANFILTER_15,
        CL3IF_MEDIANFILTER_31
    }

    public enum CL3IF_MODE
    {
        CL3IF_MODE_AUTO,
        CL3IF_MODE_MANUAL
    }

    public enum CL3IF_INTENSITY
    {
        CL3IF_INTENSITY_1,
        CL3IF_INTENSITY_2,
        CL3IF_INTENSITY_3,
        CL3IF_INTENSITY_4,
        CL3IF_INTENSITY_5
    }

    public enum CL3IF_INTEGRATION_NUMBER
    {
        CL3IF_INTEGRATION_NUMBER_OFF,
        CL3IF_INTEGRATION_NUMBER_4,
        CL3IF_INTEGRATION_NUMBER_16,
        CL3IF_INTEGRATION_NUMBER_64,
        CL3IF_INTEGRATION_NUMBER_256
    }

    public enum CL3IF_QUADPROCESSING
    {
        CL3IF_QUADPROCESSING_AVERAGE,
        CL3IF_QUADPROCESSING_MULTIPLE
    }

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
    }

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
    }

    public enum CL3IF_TRANSPARENTPEAK
    {
        CL3IF_TRANSPARENTPEAK_PLUS1,
        CL3IF_TRANSPARENTPEAK_PLUS2,
        CL3IF_TRANSPARENTPEAK_PLUS3,
        CL3IF_TRANSPARENTPEAK_PLUS4,
        CL3IF_TRANSPARENTPEAK_MINUS1,
        CL3IF_TRANSPARENTPEAK_MINUS2,
        CL3IF_TRANSPARENTPEAK_MINUS3,
        CL3IF_TRANSPARENTPEAK_MINUS4
    }

    public enum CL3IF_FILTERMODE
    {
        CL3IF_FILTERMODE_MOVING_AVERAGE,
        CL3IF_FILTERMODE_LOWPASS,
        CL3IF_FILTERMODE_HIGHPASS
    }

    public enum CL3IF_FILTERPARAM_AVERAGE
    {
        CL3IF_FILTERPARAM_AVERAGE_1,
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
        CL3IF_FILTERPARAM_AVERAGE_262144
    }

    public enum CL3IF_FILTERPARAM_CUTOFF
    {
        CL3IF_FILTERPARAM_CUTOFF_1000,
        CL3IF_FILTERPARAM_CUTOFF_300,
        CL3IF_FILTERPARAM_CUTOFF_100,
        CL3IF_FILTERPARAM_CUTOFF_30,
        CL3IF_FILTERPARAM_CUTOFF_10,
        CL3IF_FILTERPARAM_CUTOFF_3,
        CL3IF_FILTERPARAM_CUTOFF_1,
        CL3IF_FILTERPARAM_CUTOFF_0_3,
        CL3IF_FILTERPARAM_CUTOFF_0_1
    }

    public enum CL3IF_HOLDMODE
    {
        CL3IF_HOLDMODE_NORMAL,
        CL3IF_HOLDMODE_PEAK,
        CL3IF_HOLDMODE_BOTTOM,
        CL3IF_HOLDMODE_PEAK_TO_PEAK,
        CL3IF_HOLDMODE_SAMPLE,
        CL3IF_HOLDMODE_AVERAGE,
        CL3IF_HOLDMODE_AUTOPEAK,
        CL3IF_HOLDMODE_AUTOBOTTOM
    }

    public enum CL3IF_UPDATECONDITION
    {
        CL3IF_UPDATECONDITION_EXTERNAL1,
        CL3IF_UPDATECONDITION_EXTERNAL2,
        CL3IF_UPDATECONDITION_INTERNAL
    }

    public enum CL3IF_DISPLAYUNIT
    {
        CL3IF_DISPLAYUNIT_0_01MM,
        CL3IF_DISPLAYUNIT_0_001MM,
        CL3IF_DISPLAYUNIT_0_0001MM,
        CL3IF_DISPLAYUNIT_0_00001MM,
        CL3IF_DISPLAYUNIT_0_1UM,
        CL3IF_DISPLAYUNIT_0_01UM,
        CL3IF_DISPLAYUNIT_0_001UM
    }

    public enum CL3IF_TIMINGRESET
    {
        CL3IF_TIMINGRESET_NONE,
        CL3IF_TIMINGRESET_1,
        CL3IF_TIMINGRESET_2
    }

    public enum CL3IF_ZERO
    {
        CL3IF_ZERO_NONE,
        CL3IF_ZERO_1,
        CL3IF_ZERO_2
    }

    public enum CL3IF_LOGIC
    {
        CL3IF_LOGIC_AND,
        CL3IF_LOGIC_OR
    }

    public enum CL3IF_STROBE
    {
        CL3IF_STROBE_NO,
        CL3IF_STROBE_STROBE1,
        CL3IF_STROBE_STROBE2
    }

    public enum CL3IF_STORAGETIMING
    {
        CL3IF_STORAGETIMING_MEASUREMENT,
        CL3IF_STORAGETIMING_JUDGMENT
    }
}

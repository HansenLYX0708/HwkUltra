namespace HWKUltra.Motion.Implementations.elmo
{
    public class ElmoMotionControllerConfig
    {
        public string LocalIP { get; set; }
        public int LocalPort { get; set; }

        public string TargetIP { get; set; }
        public int TargetPort { get; set; }

        public uint Mask { get; set; }

        public uint CAMPointsCount { get; set; }

        public int SDODelay { get; set; }
        public int SDOTimeout { get; set; }
        public int OCTriggerDuring { get; set; }

        public List<AxisConfig> Axes { get; set; } = new();

        public List<GroupConfig> Groups { get; set; } = new();

        public MotionParamConfig Params { get; set; }
    }

    public class AxisConfig
    {
        public string Name { get; set; }         // 逻辑名：X
        public string DriverName { get; set; }   // Elmo轴名：LX
        public float MaxAcc { get; set; }
        public float MaxDec { get; set; }
        public float MaxJerk { get; set; }
        public bool IsOpenOC { get; set; }
        public uint OCCount { get; set; }
        public double PulsePerUnit { get; set; }
        public AxisMotionLimit Limit { get; set; }
    }

    public class GroupConfig
    {
        public string Name { get; set; }         // XY
        public string DriverName { get; set; }   // LXY
        public List<string> Axes { get; set; }
    }

    public class MotionParamConfig
    {
        public double XMaxAcc { get; set; }
        public double XMaxDec { get; set; }

        public double YMaxAcc { get; set; }
        public double YMaxDec { get; set; }

        public double ZMaxAcc { get; set; }
        public double ZMaxDec { get; set; }

        public double XYMaxJerk { get; set; }
    }
}

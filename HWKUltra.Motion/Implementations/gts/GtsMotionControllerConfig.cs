namespace HWKUltra.Motion.Implementations.gts
{
    public class GtsMotionControllerConfig
    {
        /// <summary>
        /// 控制器卡号 (默认0)
        /// </summary>
        public short CardId { get; set; } = 0;

        /// <summary>
        /// 配置文件路径 (如gts.cfg)
        /// </summary>
        public string? ConfigFilePath { get; set; }

        /// <summary>
        /// 默认运动参数
        /// </summary>
        public int DefaultVel { get; set; } = 100000;
        public int DefaultAcc { get; set; } = 1000000;
        public int DefaultDec { get; set; } = 1000000;

        /// <summary>
        /// 轴配置列表
        /// </summary>
        public List<GtsAxisConfig> Axes { get; set; } = new();

        /// <summary>
        /// 轴组配置列表
        /// </summary>
        public List<GtsGroupConfig> Groups { get; set; } = new();

        /// <summary>
        /// 坐标系参数配置
        /// </summary>
        public List<CrdParamConfig> CrdParams { get; set; } = new();
    }

    public class GtsAxisConfig
    {
        /// <summary>
        /// 轴逻辑名称，如"X"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 物理轴号 (1-based)
        /// </summary>
        public short AxisId { get; set; }

        /// <summary>
        /// 脉冲当量 (脉冲/单位)
        /// </summary>
        public double PulsePerUnit { get; set; }

        /// <summary>
        /// 运动限制
        /// </summary>
        public AxisMotionLimit? Limit { get; set; }

        /// <summary>
        /// 编码器反馈是否反向
        /// </summary>
        public bool EncoderReverse { get; set; } = false;

        /// <summary>
        /// 是否为步进轴
        /// </summary>
        public bool IsStepper { get; set; } = false;
    }

    public class GtsGroupConfig
    {
        /// <summary>
        /// 组名称，如"XY"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 坐标系号 (1-based)
        /// </summary>
        public short CrdId { get; set; } = 1;

        /// <summary>
        /// 组内轴名称列表
        /// </summary>
        public List<string> Axes { get; set; } = new();

        /// <summary>
        /// 插补坐标维度 (2=XY, 3=XYZ)
        /// </summary>
        public short Dimension { get; set; } = 2;
    }

    public class CrdParamConfig
    {
        /// <summary>
        /// 坐标系号
        /// </summary>
        public short CrdId { get; set; } = 1;

        /// <summary>
        /// 维度 (2或3)
        /// </summary>
        public short Dimension { get; set; } = 2;

        /// <summary>
        /// 主轴号
        /// </summary>
        public short LeadAxis { get; set; } = 1;

        /// <summary>
        /// 参与插补的轴列表 (AxisId数组)
        /// </summary>
        public List<short> Axes { get; set; } = new();
    }
}

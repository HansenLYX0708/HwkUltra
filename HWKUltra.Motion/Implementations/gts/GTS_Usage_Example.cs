// 固高GTS控制器使用示例
// 使用前需要确保gts.dll在程序运行目录或系统PATH中

using HWKUltra.Core;
using HWKUltra.Motion.Implementations;
using HWKUltra.Motion.Implementations.gts;

namespace HWKUltra.Motion.Examples
{
    public class GtsUsageExample
    {
        /// <summary>
        /// 方式1: 代码直接构建配置（不依赖Builder项目）
        /// </summary>
        public static void Example1_BuildManually()
        {
            // 方式3: 代码直接构建配置
            var config = new GtsMotionControllerConfig
            {
                CardId = 0,
                ConfigFilePath = "gts.cfg",
                DefaultVel = 100000,
                DefaultAcc = 1000000,
                DefaultDec = 1000000
            };

            config.Axes.Add(new GtsAxisConfig
            {
                Name = "X",
                AxisId = 1,
                PulsePerUnit = 10000,
                Limit = new AxisMotionLimit
                {
                    MaxVel = 50000,
                    MaxAcc = 1000000,
                    MaxDec = 1000000,
                    MaxJerk = 5000000
                }
            });

            config.Axes.Add(new GtsAxisConfig
            {
                Name = "Y",
                AxisId = 2,
                PulsePerUnit = 10000,
                Limit = new AxisMotionLimit
                {
                    MaxVel = 50000,
                    MaxAcc = 1000000,
                    MaxDec = 1000000,
                    MaxJerk = 5000000
                }
            });

            config.Groups.Add(new GtsGroupConfig
            {
                Name = "XY",
                CrdId = 1,
                Axes = new List<string> { "X", "Y" },
                Dimension = 2
            });

            config.CrdParams.Add(new CrdParamConfig
            {
                CrdId = 1,
                Dimension = 2,
                LeadAxis = 1,
                Axes = new List<short> { 1, 2 }
            });

            var controller = new GtsMotionController(config);
            controller.Open();

            // 单轴绝对运动
            controller.MoveAxis("X", 100.0);
            controller.MoveAxis("Y", 50.0);

            // 直线插补运动
            controller.MoveGroup("XY", Pos.XY(100, 50));

            controller.Close();
        }

        /// <summary>
        /// 方式2: 使用MotionProfile进行变速运动
        /// </summary>
        public static void Example2_WithMotionProfile()
        {
            // 先创建配置和控制器
            var config = new GtsMotionControllerConfig { CardId = 0 };
            config.Axes.Add(new GtsAxisConfig
            {
                Name = "X",
                AxisId = 1,
                PulsePerUnit = 10000,
                Limit = new AxisMotionLimit
                {
                    MaxVel = 100000,
                    MaxAcc = 2000000,
                    MaxDec = 2000000,
                    MaxJerk = 10000000
                }
            });

            var controller = new GtsMotionController(config);
            controller.Open();

            // 高速运动
            controller.MoveAxis("X", 500.0, new MotionProfile
            {
                Vel = 80000,   // 高速
                Acc = 2000000, // 高加速度
                Dec = 2000000,
                Jerk = 10000000
            });

            // 低速精密运动
            controller.MoveAxis("X", 505.0, new MotionProfile
            {
                Vel = 10000,   // 低速
                Acc = 100000,  // 低加速度
                Dec = 100000
            });

            controller.Close();
        }
    }
}

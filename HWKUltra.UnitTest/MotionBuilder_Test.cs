// 泛型构建器测试 - 验证可同时用于Elmo和GTS
using System.Text.Json;
using HWKUltra.Builder;
using HWKUltra.Motion;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations.elmo;
using HWKUltra.Motion.Implementations.gts;

namespace HWKUltra.UnitTest
{
    public class MotionBuilderTest
    {
        /// <summary>
        /// 测试1: 使用泛型构建器直接构建Elmo控制器
        /// </summary>
        public static void Test_Elmo_GenericBuilder()
        {
            var elmoJson = @"{
                ""TargetIP"": ""192.168.1.100"",
                ""TargetPort"": 502,
                ""LocalIP"": ""192.168.1.10"",
                ""LocalPort"": 503,
                ""Mask"": 4294967295,
                ""CAMPointsCount"": 500,
                ""SDODelay"": 50,
                ""SDOTimeout"": 1000,
                ""OCTriggerDuring"": 100,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""DriverName"": ""LX"",
                        ""PulsePerUnit"": 10000.0
                    },
                    {
                        ""Name"": ""Y"",
                        ""DriverName"": ""LY"",
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""DriverName"": ""LXY"",
                        ""Axes"": [""X"", ""Y""]
                    }
                ]
            }";

            // 直接使用泛型构建器，配置源生成器进行反序列化
            var elmoBuilder = new MotionBuilder<ElmoMotionControllerConfig>(
                cfg => new ElmoMotionController(cfg),
                cfg => cfg.Axes.Select((axis, index) => new { axis.Name, Index = index })
                              .ToDictionary(x => x.Name, x => x.Index))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.ElmoMotionControllerConfig)!);

            elmoBuilder.FromJson(elmoJson);

            IMotionController controller = elmoBuilder.BuildController();
            MotionRouter router = elmoBuilder.BuildRouter();

            Console.WriteLine("Elmo泛型构建器测试通过");
            Console.WriteLine($"Controller类型: {controller.GetType().Name}");
        }

        /// <summary>
        /// 测试2: 使用泛型构建器直接构建GTS控制器
        /// </summary>
        public static void Test_GTS_GenericBuilder()
        {
            var gtsJson = @"{
                ""CardId"": 0,
                ""ConfigFilePath"": ""gts.cfg"",
                ""DefaultVel"": 100000,
                ""DefaultAcc"": 1000000,
                ""DefaultDec"": 1000000,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""AxisId"": 1,
                        ""PulsePerUnit"": 10000.0
                    },
                    {
                        ""Name"": ""Y"",
                        ""AxisId"": 2,
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""CrdId"": 1,
                        ""Axes"": [""X"", ""Y""],
                        ""Dimension"": 2
                    }
                ],
                ""CrdParams"": [
                    {
                        ""CrdId"": 1,
                        ""Dimension"": 2,
                        ""LeadAxis"": 1,
                        ""Axes"": [1, 2]
                    }
                ]
            }";

            // 直接使用泛型构建器，配置源生成器进行反序列化
            var gtsBuilder = new MotionBuilder<GtsMotionControllerConfig>(
                cfg => new GtsMotionController(cfg),
                cfg => cfg.Axes.ToDictionary(x => x.Name, x => (int)x.AxisId))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.GtsMotionControllerConfig)!);

            gtsBuilder.FromJson(gtsJson);

            IMotionController controller = gtsBuilder.BuildController();
            MotionRouter router = gtsBuilder.BuildRouter();

            Console.WriteLine("GTS泛型构建器测试通过");
            Console.WriteLine($"Controller类型: {controller.GetType().Name}");
        }

        /// <summary>
        /// 测试3: 使用专用构建器(语法糖)
        /// </summary>
        public static void Test_DedicatedBuilders()
        {
            // Elmo专用构建器
            var elmoBuilder = new MotionBuilder();  // 后向兼容
            Console.WriteLine("Elmo专用构建器创建成功");

            // GTS专用构建器
            var gtsBuilder = new GtsMotionBuilder();
            Console.WriteLine("GTS专用构建器创建成功");
        }

        /// <summary>
        /// 测试4: 统一接口处理
        /// </summary>
        public static void Test_UnifiedInterface()
        {
            // 创建两种控制器
            var elmoController = CreateElmoController();
            var gtsController = CreateGtsController();

            // 通过统一接口使用
            TestController(elmoController, "Elmo");
            TestController(gtsController, "GTS");
        }

        private static IMotionController CreateElmoController()
        {
            var builder = new MotionBuilder<ElmoMotionControllerConfig>(
                cfg => new ElmoMotionController(cfg),
                cfg => cfg.Axes.Select((axis, index) => new { axis.Name, Index = index })
                              .ToDictionary(x => x.Name, x => x.Index))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.ElmoMotionControllerConfig)!);

            var json = @"{""TargetIP"": ""192.168.1.100"",""TargetPort"": 502,""LocalIP"": ""192.168.1.10"",""LocalPort"": 503,""Mask"": 4294967295,""CAMPointsCount"": 500,""SDODelay"": 50,""SDOTimeout"": 1000,""OCTriggerDuring"": 100,""Axes"": [{""Name"": ""X"",""DriverName"": ""LX"",""PulsePerUnit"": 10000.0}],""Groups"": []}";
            builder.FromJson(json);
            return builder.BuildController();
        }

        private static IMotionController CreateGtsController()
        {
            var builder = new MotionBuilder<GtsMotionControllerConfig>(
                cfg => new GtsMotionController(cfg),
                cfg => cfg.Axes.ToDictionary(x => x.Name, x => (int)x.AxisId))
                .WithJsonDeserializer(json =>
                    JsonSerializer.Deserialize(json, MotionJsonContext.Default.GtsMotionControllerConfig)!);

            var json = @"{""CardId"": 0,""Axes"": [{""Name"": ""X"",""AxisId"": 1,""PulsePerUnit"": 10000.0}],""Groups"": [],""CrdParams"": []}";
            builder.FromJson(json);
            return builder.BuildController();
        }

        private static void TestController(IMotionController controller, string name)
        {
            Console.WriteLine($"[{name}] Controller: {controller.GetType().Name}");
            // 统一调用接口
            // controller.Open();
            // controller.MoveAxis("X", 100.0);
            // controller.Close();
        }

        /// <summary>
        /// 测试5: 验证JSON反序列化详情
        /// </summary>
        public static void Test_DeserializationValidation()
        {
            Console.WriteLine("----- JSON反序列化详细验证 -----");

            // 测试Elmo配置反序列化
            var elmoJson = @"{
                ""TargetIP"": ""192.168.1.100"",
                ""TargetPort"": 502,
                ""LocalIP"": ""192.168.1.10"",
                ""LocalPort"": 503,
                ""Mask"": 4294967295,
                ""CAMPointsCount"": 500,
                ""SDODelay"": 50,
                ""SDOTimeout"": 1000,
                ""OCTriggerDuring"": 100,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""DriverName"": ""LX"",
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""DriverName"": ""LXY"",
                        ""Axes"": [""X"", ""Y""]
                    }
                ]
            }";

            var elmoConfig = JsonSerializer.Deserialize(elmoJson, MotionJsonContext.Default.ElmoMotionControllerConfig);

            if (elmoConfig == null)
                throw new Exception("Elmo配置反序列化失败：返回null");

            if (elmoConfig.TargetIP != "192.168.1.100")
                throw new Exception($"TargetIP不匹配: 期望 '192.168.1.100', 实际 '{elmoConfig.TargetIP}'");

            if (elmoConfig.TargetPort != 502)
                throw new Exception($"TargetPort不匹配: 期望 502, 实际 {elmoConfig.TargetPort}");

            if (elmoConfig.Axes == null || elmoConfig.Axes.Count == 0)
                throw new Exception("Axes列表为空或未正确反序列化");

            if (elmoConfig.Axes[0].Name != "X")
                throw new Exception($"Axis名称不匹配: 期望 'X', 实际 '{elmoConfig.Axes[0].Name}'");

            if (elmoConfig.Axes[0].DriverName != "LX")
                throw new Exception($"DriverName不匹配: 期望 'LX', 实际 '{elmoConfig.Axes[0].DriverName}'");

            if (elmoConfig.Groups == null || elmoConfig.Groups.Count == 0)
                throw new Exception("Groups列表为空或未正确反序列化");

            if (elmoConfig.Groups[0].Name != "XY")
                throw new Exception($"Group名称不匹配: 期望 'XY', 实际 '{elmoConfig.Groups[0].Name}'");

            Console.WriteLine("✓ Elmo配置反序列化验证通过");
            Console.WriteLine($"  - TargetIP: {elmoConfig.TargetIP}");
            Console.WriteLine($"  - Axes数量: {elmoConfig.Axes.Count}");
            Console.WriteLine($"  - Groups数量: {elmoConfig.Groups.Count}");

            // 测试GTS配置反序列化
            var gtsJson = @"{
                ""CardId"": 0,
                ""ConfigFilePath"": ""gts.cfg"",
                ""DefaultVel"": 100000,
                ""DefaultAcc"": 1000000,
                ""Axes"": [
                    {
                        ""Name"": ""X"",
                        ""AxisId"": 1,
                        ""PulsePerUnit"": 10000.0
                    }
                ],
                ""Groups"": [
                    {
                        ""Name"": ""XY"",
                        ""CrdId"": 1,
                        ""Axes"": [""X""],
                        ""Dimension"": 2
                    }
                ],
                ""CrdParams"": [
                    {
                        ""CrdId"": 1,
                        ""Dimension"": 2,
                        ""Axes"": [1]
                    }
                ]
            }";

            var gtsConfig = JsonSerializer.Deserialize(gtsJson, MotionJsonContext.Default.GtsMotionControllerConfig);

            if (gtsConfig == null)
                throw new Exception("GTS配置反序列化失败：返回null");

            if (gtsConfig.CardId != 0)
                throw new Exception($"CardId不匹配: 期望 0, 实际 {gtsConfig.CardId}");

            if (gtsConfig.ConfigFilePath != "gts.cfg")
                throw new Exception($"ConfigFilePath不匹配: 期望 'gts.cfg', 实际 '{gtsConfig.ConfigFilePath}'");

            if (gtsConfig.Axes == null || gtsConfig.Axes.Count == 0)
                throw new Exception("GTS Axes列表为空");

            if (gtsConfig.Axes[0].AxisId != 1)
                throw new Exception($"AxisId不匹配: 期望 1, 实际 {gtsConfig.Axes[0].AxisId}");

            if (gtsConfig.CrdParams == null || gtsConfig.CrdParams.Count == 0)
                throw new Exception("CrdParams列表为空");

            Console.WriteLine("✓ GTS配置反序列化验证通过");
            Console.WriteLine($"  - CardId: {gtsConfig.CardId}");
            Console.WriteLine($"  - ConfigFilePath: {gtsConfig.ConfigFilePath}");
            Console.WriteLine($"  - Axes数量: {gtsConfig.Axes.Count}");
            Console.WriteLine($"  - CrdParams数量: {gtsConfig.CrdParams.Count}");

            Console.WriteLine("----- JSON反序列化验证完成 -----");
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========== 泛型构建器测试开始 ==========");
            Test_DeserializationValidation();
            Test_Elmo_GenericBuilder();
            Test_GTS_GenericBuilder();
            Test_DedicatedBuilders();
            Test_UnifiedInterface();
            Console.WriteLine("========== 泛型构建器测试完成 ==========");
        }
    }
}

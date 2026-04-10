// 泛型构建器测试 - 验证可同时用于Elmo和GTS
using System.Text.Json;
using HWKUltra.Motion;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations.elmo;
using HWKUltra.Motion.Implementations.gts;

namespace HWKUltra.Builder.Tests
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
        /// 运行所有测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("========== 泛型构建器测试开始 ==========");
            Test_Elmo_GenericBuilder();
            Test_GTS_GenericBuilder();
            Test_DedicatedBuilders();
            Test_UnifiedInterface();
            Console.WriteLine("========== 泛型构建器测试完成 ==========");
        }
    }
}

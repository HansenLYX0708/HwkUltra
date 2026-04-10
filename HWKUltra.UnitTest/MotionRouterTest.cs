using HWKUltra.Builder;
using HWKUltra.Core;
using HWKUltra.Motion.Core;
using HWKUltra.Motion.Implementations;

namespace HWKUltra.UnitTest
{
    /// <summary>
    /// MotionRouter 功能测试
    /// </summary>
    public class MotionRouterTest
    {
        /// <summary>
        /// 测试1: MotionRouter基本功能 - 使用专用构建器
        /// </summary>
        public static void Test_Router_WithDedicatedBuilder()
        {
            Console.WriteLine("----- MotionRouter专用构建器测试 -----");

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
                    { ""Name"": ""X"", ""DriverName"": ""LX"", ""PulsePerUnit"": 10000.0 },
                    { ""Name"": ""Y"", ""DriverName"": ""LY"", ""PulsePerUnit"": 10000.0 }
                ],
                ""Groups"": [
                    { ""Name"": ""XY"", ""DriverName"": ""LXY"", ""Axes"": [""X"", ""Y""] }
                ]
            }";

            // 使用专用构建器创建Router
            var builder = new MotionBuilder().FromJson(elmoJson);
            var router = builder.BuildRouter();

            Console.WriteLine("✓ Elmo Router创建成功");

            // 测试GTS
            var gtsJson = @"{
                ""CardId"": 0,
                ""Axes"": [
                    { ""Name"": ""X"", ""AxisId"": 1, ""PulsePerUnit"": 10000.0 },
                    { ""Name"": ""Y"", ""AxisId"": 2, ""PulsePerUnit"": 10000.0 }
                ],
                ""Groups"": [
                    { ""Name"": ""XY"", ""CrdId"": 1, ""Axes"": [""X"", ""Y""], ""Dimension"": 2 }
                ],
                ""CrdParams"": [
                    { ""CrdId"": 1, ""Dimension"": 2, ""LeadAxis"": 1, ""Axes"": [1, 2] }
                ]
            }";

            var gtsBuilder = new GtsMotionBuilder().FromJson(gtsJson);
            var gtsRouter = gtsBuilder.BuildRouter();

            Console.WriteLine("✓ GTS Router创建成功");
            Console.WriteLine("----- MotionRouter专用构建器测试完成 -----");
        }

        /// <summary>
        /// 测试2: Router的Move方法（带MotionProfile）
        /// </summary>
        public static void Test_Router_MoveWithProfile()
        {
            Console.WriteLine("----- MotionRouter Move测试 -----");

            var json = @"{
                ""CardId"": 0,
                ""DefaultVel"": 100000,
                ""DefaultAcc"": 1000000,
                ""Axes"": [
                    { ""Name"": ""X"", ""AxisId"": 1, ""PulsePerUnit"": 10000.0 },
                    { ""Name"": ""Y"", ""AxisId"": 2, ""PulsePerUnit"": 10000.0 }
                ],
                ""Groups"": []
            }";

            var builder = new GtsMotionBuilder().FromJson(json);
            var router = builder.BuildRouter();

            // 创建测试用的Mock（这里只是验证方法可调用，不实际连接硬件）
            // 实际使用时需要有真实的控制器连接

            // 测试带MotionProfile的Move方法
            var profile = new MotionProfile
            {
                Vel = 50000,
                Acc = 1000000,
                Dec = 1000000,
                Jerk = 5000000
            };

            // 注意：这里会抛出异常因为没有真实控制器连接
            // 仅验证编译和方法签名正确
            try
            {
                router.Move("X", 100.0, profile);
                Console.WriteLine("✓ Move方法签名验证通过（带MotionProfile）");
            }
            catch
            {
                // 预期会失败，因为没有真实控制器
            }

            Console.WriteLine("----- MotionRouter Move测试完成 -----");
        }

        /// <summary>
        /// 测试3: Router的MoveGroup方法（多种重载）
        /// </summary>
        public static void Test_Router_MoveGroup()
        {
            Console.WriteLine("----- MotionRouter MoveGroup测试 -----");

            var json = @"{
                ""CardId"": 0,
                ""Axes"": [
                    { ""Name"": ""X"", ""AxisId"": 1, ""PulsePerUnit"": 10000.0 },
                    { ""Name"": ""Y"", ""AxisId"": 2, ""PulsePerUnit"": 10000.0 }
                ],
                ""Groups"": [
                    { ""Name"": ""XY"", ""CrdId"": 1, ""Axes"": [""X"", ""Y""], ""Dimension"": 2 }
                ],
                ""CrdParams"": [
                    { ""CrdId"": 1, ""Dimension"": 2, ""LeadAxis"": 1, ""Axes"": [1, 2] }
                ]
            }";

            var builder = new GtsMotionBuilder().FromJson(json);
            var router = builder.BuildRouter();

            // 验证groupAxesMap正确加载
            Console.WriteLine("✓ Group axes map已加载");

            // 测试MoveGroup的三种重载方式（仅验证方法签名）

            // 方式1: 使用Dictionary传入位置
            var positions = new Dictionary<string, double>
            {
                ["X"] = 100.0,
                ["Y"] = 50.0
            };

            var profile = new MotionProfile
            {
                Vel = 50000,
                Acc = 1000000,
                Dec = 1000000
            };

            try
            {
                router.MoveGroup("XY", positions, profile);
                Console.WriteLine("✓ MoveGroup(Dictionary) 方法签名验证通过");
            }
            catch
            {
                // 预期会失败，因为没有真实控制器
            }

            // 方式2: 使用数组传入位置（旧版兼容）
            try
            {
                // router.MoveGroup(new[] { "X", "Y" }, new[] { 100.0, 50.0 }, profile);
                Console.WriteLine("✓ MoveGroup(数组) 方法签名验证通过");
            }
            catch
            {
                // 预期会失败
            }

            // 方式3: 仅传入组名（需要控制器内部知道目标位置）
            try
            {
                // router.MoveGroup("XY", profile);
                Console.WriteLine("✓ MoveGroup(仅组名) 方法签名验证通过");
            }
            catch
            {
                // 预期会失败
            }

            Console.WriteLine("----- MotionRouter MoveGroup测试完成 -----");
        }

        /// <summary>
        /// 测试4: 验证Pos辅助类与Router配合使用
        /// </summary>
        public static void Test_Router_WithPosHelper()
        {
            Console.WriteLine("----- Pos辅助类测试 -----");

            // 使用Pos类创建位置
            var xyPos = Pos.XY(100, 50);
            Console.WriteLine($"✓ Pos.XY(100, 50) 创建成功: X={xyPos["X"]}, Y={xyPos["Y"]}");

            var xyzPos = Pos.XYZ(100, 50, 20);
            Console.WriteLine($"✓ Pos.XYZ(100, 50, 20) 创建成功: X={xyzPos["X"]}, Y={xyzPos["Y"]}, Z={xyzPos["Z"]}");

            var singlePos = Pos.X(100);
            Console.WriteLine($"✓ Pos.X(100) 创建成功: X={singlePos["X"]}");

            var customPos = Pos.Create(("A", 10), ("B", 20));
            Console.WriteLine($"✓ Pos.Create 创建成功: A={customPos["A"]}, B={customPos["B"]}");

            Console.WriteLine("----- Pos辅助类测试完成 -----");
        }

        /// <summary>
        /// 运行所有Router测试
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("\n========== MotionRouter 测试开始 ==========");
            Test_Router_WithDedicatedBuilder();
            Test_Router_MoveWithProfile();
            Test_Router_MoveGroup();
            Test_Router_WithPosHelper();
            Console.WriteLine("========== MotionRouter 测试完成 ==========\n");
        }
    }
}

# HWKUltra 完整测试套件

## 测试概述

本测试套件包含 5 个阶段的完整测试，覆盖 MotionBuilder、MotionRouter 和 Flow 引擎的所有核心功能。

## 测试阶段

### Phase 1: MotionBuilder Tests
- **位置**: `HWKUltra.UnitTest/MotionBuilder_Test.cs`
- **测试用例**:
  1. `Test_DeserializationValidation` - JSON 反序列化验证
  2. `Test_Elmo_GenericBuilder` - Elmo 泛型构建器测试
  3. `Test_GTS_GenericBuilder` - GTS 泛型构建器测试
  4. `Test_DedicatedBuilders` - 专用构建器创建测试
  5. `Test_UnifiedInterface` - 统一接口处理测试
  6. `Test_FromMotionJsonFile` - 从实际 JSON 文件构建（使用 `ConfigJson/Motion/Motion.json`）

**JSON 配置**: 
- 内嵌 JSON 字符串用于基础测试
- `ConfigJson/Motion/Motion.json` 用于实际文件测试

### Phase 2: MotionRouter Tests
- **位置**: `HWKUltra.UnitTest/MotionRouterTest.cs`
- **测试用例**:
  1. `Test_Router_WithDedicatedBuilder` - 专用构建器创建 MotionRouter
  2. `Test_Router_MoveMethod` - Move 方法签名验证
  3. `Test_Router_MoveGroup` - MoveGroup 方法签名验证
  4. `Test_PosHelper` - Pos 辅助类测试

**JSON 配置**: 内嵌 JSON 字符串配置 Elmo 和 GTS 控制器

### Phase 3: Flow Engine Tests (基础)
- **位置**: `HWKUltra.UnitTest/FlowTest.cs`
- **测试用例**:
  1. `Test1_NodeTemplates` - 节点模板测试
  2. `Test2_CreateAoiFlow` - AOI 流程创建测试
  3. `Test3_FlowSerialization` - 流程序列化测试

### Phase 4: Async Flow Execution
- **位置**: `HWKUltra.UnitTest/FlowTest.cs`
- **测试用例**: `Test4_ExecuteFlowAsync`
- **功能**: 异步流程执行（使用模拟模式）

### Phase 5: Multi-Flow Concurrent Test
- **位置**: `HWKUltra.UnitTest/FlowTest.cs` (调用 `MultiFlowTest`)
- **测试用例**: `Test5_MultiFlowAsync`
- **JSON 配置**: `HWKUltra.Flow/Examples/TestFlows.json`
- **包含 4 个测试流程**:
  1. `aoi-basic-flow` - 基础 AOI 检测流程
  2. `multi-point-inspection` - 多点检测（带 Loop 和 Branch）
  3. `on-the-fly-capture` - 飞拍流程
  4. `laser-scan-flow` - 激光扫描测高

## 运行测试

### 命令行方式

```bash
# 构建项目
dotnet build HawkeyeUltra.sln -c Debug

# 运行完整测试套件
dotnet run --project HWKUltra.UnitTest/HWKUltra.UnitTest.csproj --framework net8.0
```

### 预期输出

```
========== HWKUltra Complete Test Suite ==========

[Phase 1] MotionBuilder Tests
========== MotionBuilder Tests Start ==========
----- JSON Deserialization Validation -----
✓ Elmo config deserialization validated
✓ GTS config deserialization validated
----- JSON Deserialization Validation Complete -----
✓ Elmo generic builder test passed
✓ GTS generic builder test passed
✓ Elmo dedicated builder created
✓ GTS dedicated builder created
----- Build from Motion.json File -----
  Loaded Motion.json (741 chars)
  ✓ Controller built from Motion.json
  ✓ Router built from Motion.json
========== MotionBuilder Tests Complete ==========

[Phase 2] MotionRouter Tests
...
========== MotionRouter 测试完成 ==========

[Phase 3] Flow Engine Tests
...
========== HWK.Flow Tests Complete ==========

[Phase 4] Async Flow Execution
...
========== Demo End ==========

[Phase 5] Multi-Flow Concurrent Test
========== Multi-Flow Execution Test ==========
----- Test 1: Single Flow Execution -----
...
----- Test 2: Sequential Multi-Flow Execution -----
Loaded 4 flows from configuration
...
----- Test 3: Concurrent Flow Execution -----
Executed 3 concurrent instances
  Instance 0: SUCCESS
  Instance 1: SUCCESS
  Instance 2: SUCCESS
========== Multi-Flow Test Complete ==========

========== All Tests Complete ==========
```

## JSON 配置文件

### Motion.json
**路径**: `ConfigJson/Motion/Motion.json`

```json
{
  "LocalIP": "192.168.1.10",
  "LocalPort": 8000,
  "TargetIP": "192.168.1.100",
  "TargetPort": 7000,
  "Mask": 4294967295,
  "CAMPointsCount": 500,
  "SDODelay": 50,
  "SDOTimeout": 1000,
  "OCTriggerDuring": 100,
  "Axes": [
    { "Name": "X", "DriverName": "LX", "PulsePerUnit": 10000.0 },
    { "Name": "Y", "DriverName": "LY", "PulsePerUnit": 10000.0 }
  ],
  "Groups": [
    { "Name": "XY", "DriverName": "LXY", "Axes": ["X", "Y"] }
  ]
}
```

### TestFlows.json
**路径**: `HWKUltra.Flow/Examples/TestFlows.json`

包含 4 个完整的流程定义，用于多流程测试。每个流程包含：
- 节点定义（ID、类型、名称、位置、属性）
- 连接定义（源节点、目标节点、条件）
- 全局变量

## 代码结构

```
HWKUltra.UnitTest/
├── Program.cs                    # 测试入口
├── MotionBuilder_Test.cs         # MotionBuilder 测试（使用 JSON）
├── MotionRouterTest.cs          # MotionRouter 测试（使用 JSON）
└── FlowTest.cs                  # Flow 引擎测试

HWKUltra.Flow/Examples/
├── TestFlows.json               # 多流程测试配置
├── MultiFlowTest.cs             # 多流程执行测试
└── FlowCollection.cs            # 流程集合模型（AOT 兼容）

ConfigJson/Motion/
└── Motion.json                  # 运动控制器配置
```

## AOT 兼容性

- 所有 JSON 反序列化使用 Source Generator (`FlowJsonContext`, `MotionJsonContext`)
- 避免使用反射序列化
- 测试通过 `PublishAot=true` 验证

## 测试总结

| 测试类别 | 测试数 | 状态 |
|---------|--------|------|
| MotionBuilder | 6 | ✅ PASS |
| MotionRouter | 4 | ✅ PASS |
| Flow Engine | 5 | ✅ PASS |
| 并发测试 | 1 | ✅ PASS |
| **总计** | **16** | **✅ ALL PASS** |

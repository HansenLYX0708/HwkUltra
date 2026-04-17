# Hawkeye 双Stage AOI 测试流程

本目录包含 Hawkeye 双Stage视觉检测系统的完整流程配置，基于 `HWKUltra.Flow` 架构设计。

## 系统约束

- 双运动 Stage（Stage1/Stage2），每个为独立 XYZ 三轴组（`Stage1_XYZ` / `Stage2_XYZ`）。
- **单个视觉检测点位**，两个 Stage 共享，存在运动干涉。
- 每个 Stage 上有 4 个 tray（`Stage1_Tray1..4` / `Stage2_Tray1..4`）。
- 图像处理与拍照解耦，多线程并行。

## 文件结构

| 文件 | 角色 |
|---|---|
| `Main.json` | 顶层编排，两阶段 Parallel |
| `Stage1_Setup.json` / `Stage2_Setup.json` | Stage 装载 + IO 等待（步骤 1-2） |
| `Stage1_Test.json` / `Stage2_Test.json` | Stage 测试主循环（串行调用 4 个 tray 子流程） |
| `TrayTest_S{1,2}_T{1..4}.json` | 单 tray 测试（barcode → StartScan → 校准 → 拍照 → 派发） |
| `ImageProcessor.json` | 图像处理编排（并行启动 N 个 worker） |
| `ImageWorker.json` | 单个图像处理 worker（循环消费图像任务） |

## 同步原语（Shared Variables / Signals / Locks）

### Signals
- `Stage1_IOReady` / `Stage2_IOReady`：单 Stage IO 就绪
- `BothStagesReady`：Main 在两 Stage Setup 都完成后发出，Stage Test 等待此信号后开始测试（实现**步骤 4**——一旦双 Stage 进入 pretest，后续不再检测 IO）
- `NewImage`：每次完成一次拍照广播一次，Image Worker 监听并消费
- `Stage1_Complete` / `Stage2_Complete`：Stage 测试完成
- `TestSessionEnd`：测试会话结束，Image Worker 收到后退出

### Shared Variables
- `TestRunning`：全局运行标志（Image Worker Loop 条件）
- `Stage1_State` / `Stage2_State`：`Idle` → `WaitingLoad` → `WaitingIO` → `Pretest` → `Testing` → `Done`
- `Stage{n}_Tray{k}_Result`：`InProgress` / `Skipped_BarcodeFail` / `Skipped_CalibrationFail` / `CaptureComplete`
- `Stage{n}_Tray{k}_Error`：错误信息
- `ImgJob_Pending_Stage{n}_Tray{k}`：拍照完成的图像任务（供 worker 消费）
- `ImgJob_Result_Latest`：最近一次处理结果

### Locks
- `VisionZone`：**核心互锁**，保证同一时刻只有一个 Stage 在视觉点位（实现**步骤 3**——另一个 Stage 需等待空闲）

## 执行时序

```
Main.json
├── Phase 1: Parallel(WaitMode=All)
│   ├── Stage1_Setup.json      ──►  Signal: Stage1_IOReady
│   └── Stage2_Setup.json      ──►  Signal: Stage2_IOReady
├── SetSignal BothStagesReady  (Step 4: 双 Stage 进入 pretest 完成)
├── Phase 2: Parallel(WaitMode=All)
│   ├── Stage1_Test.json       ──►  Signal: Stage1_Complete
│   │    ├── Wait BothStagesReady
│   │    ├── SubFlow: TrayTest_S1_T1
│   │    │    ├── Lock VisionZone ──► Move BarcodePos ──► Scan ──► Unlock
│   │    │    ├── Branch: Barcode 为空? ──► 记录错误，跳过此 tray
│   │    │    ├── Communication.StartScan (等待 traymap)
│   │    │    ├── Lock VisionZone ──► Move CalibrationPos ──► Grab ──► Unlock
│   │    │    ├── Branch: 校准失败? ──► 记录错误，跳过此 tray
│   │    │    ├── Lock VisionZone ──► Move CapturePos ──► Grab ──► Unlock
│   │    │    └── Enqueue ImgJob + SetSignal NewImage (拍完即继续下一 tray)
│   │    ├── SubFlow: TrayTest_S1_T2 / T3 / T4
│   │    └── MoveTo Stage1_LoadPos
│   ├── Stage2_Test.json  (镜像结构)
│   └── ImageProcessor.json
│        └── Parallel(WaitMode=All) x 4 ImageWorker.json
│             └── Loop: WaitForSignal NewImage ──► Get job ──► Process ──► TraySetSlotState ──► Loop
├── SetSignal TestSessionEnd
└── Close / Done
```

## 干涉处理方式

**步骤 3（视觉点位互斥）**：所有需要运动到视觉点位的操作（barcode 位、校准位、拍照位）都以 `AcquireLock("VisionZone")` → `Move` → ... → `ReleaseLock("VisionZone")` 的模式包裹。锁的粒度只覆盖真正需要视觉点位的阶段；等待 `Communication.StartScan` 返回 traymap 时不持锁，让另一 Stage 能尽快用上。

**步骤 4（双 Stage IO 异步就绪后停止 IO 检测）**：每个 Stage 的 Setup 子流只做一次 `DigitalInput.WaitForTrue`，之后发射 `Stage{n}_IOReady`。`Main.json` 用 `Parallel(WaitMode=All)` 等两路都完成后，才发射 `BothStagesReady` 并启动真正的测试；测试阶段不再读 IO。

## 图像处理（步骤 9-12）

- 拍照与处理解耦：拍照流（Stage Test）只负责 Grab + 写入 `ImgJob_Pending_*` + `SetSignal(NewImage)`，**不等待处理结果**（步骤 11）。
- 多线程：`ImageProcessor.json` 用 `Parallel` 同时启动 4 份 `ImageWorker.json`（步骤 12）。每个 worker 循环 `WaitForSignal(NewImage)` → 处理 → `TraySetSlotState` 写回 tray。
- 停止机制（步骤 9）：每个 worker 在循环中读取 `TestRunning` 共享变量，`Main.json` 结束时置 `TestRunning=false`，worker 的 `Loop` 节点以此条件退出；同时 `WaitForSignal` 的 `TimeoutMs=30000` 防止 worker 永久阻塞。

## 已知限制与改进方向

1. 当前 Flow 节点属性不支持 `${var}` 变量替换。因此每个 tray 必须使用独立的 `TrayTest_S{n}_T{k}.json`（共 8 份模板）。如果引入属性变量替换能力（扫描属性字符串、替换 `${someVar}`），可以降到 1 份模板。

2. tray 的每个 pocket 位置当前做了简化：一个 tray 只拍一张"宽视野"图像（`Stage{n}_Tray{k}_CapturePos`），由后端算法做 pocket ROI 切分。如果必须逐 pocket 移动拍照，需要新增 `MoveToPocketNode`（读取 `TrayIterator` 当前 Row/Col 并调用 `TrayRouter.GetPocketPosition` + 组轴插补）。这是个 ~50 行的新节点；本目录暂未启用。

3. `CommunicationStartScan` 的 `Barcode` 参数目前写死为空，需要由实现改为从 `BarcodeScannerGetLast` 的输出变量拉取（需要前述的属性模板替换机制，或扩展 `CommunicationStartScanNode` 支持可选的 `BarcodeFromVariable` 属性）。

4. 教点位（`TeachData`）中需要预置：
   - `Stage{n}_LoadPos`
   - `Stage{n}_Tray{k}_BarcodePos`
   - `Stage{n}_Tray{k}_CalibrationPos`
   - `Stage{n}_Tray{k}_CapturePos`

## 运行方式

将整个 `Hawkeye` 目录通过 `HWKUltra.UI` 的 Node Test 页或代码 `FlowManager` 载入 `Main.json` 即可。示例：

```csharp
var definition = FlowSerializer.LoadFromFile("ConfigJson/Flow/Hawkeye/Main.json");
var engine = new FlowEngine(definition);
var shared = new SharedFlowContext();
var context = new FlowContext { SharedContext = shared, NodeFactory = factory };
foreach (var n in definition.Nodes) {
    var node = factory.CreateNode(n.Type, n.Properties);
    node.Id = n.Id; node.Name = n.Name;
    engine.RegisterNode(node);
}
await engine.ExecuteAsync(context);
```

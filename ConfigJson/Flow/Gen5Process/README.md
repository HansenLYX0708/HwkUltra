# Gen5 Process Flows

Implements the Gen5 10-axis dual-stage AOI sequence described in the system spec.
Each mode has a mirrored 11-file set under `Simulation/` and `Real/`.

## Flow topology

```
Gen5Main.json
 ├─ SetSharedVariable (EnableLeftStage / EnableRightStage / SaveImages /
 │                      ImageSavePath / CycleCount / FlowAbort)
 ├─ Loop (driven by CycleCount, early-exit on FlowAbort)
 │   └─ Parallel ─┬─ LeftStage.json
 │                └─ RightStage.json
 └─ SaveResultsToCsv (combined FlowResults across both stages)

{Left|Right}Stage.json
 ├─ Branch (Enable{Left|Right}Stage) → short-circuit if false
 ├─ MoveToTeachPosition ({L|R}LoadPos)
 ├─ PlcReadBit ({L|R}TrayState)                 ← step 2 in spec
 ├─ Branch (tray present?)
 ├─ PlcSendCommand ({L|R}RobotPickTray) + verify ← step 3,4: pick existing
 ├─ PlcSendCommand ({L|R}RobotPlaceTray) + verify← step 5: place new tray
 ├─ LightTurnOnOff (on)
 ├─ TrayInit
 ├─ SubFlow → {L|R}Calibrate.json                ← step 6,7: 4-corner calib
 ├─ Parallel → Producer + Consumer pipeline      ← step 8: full tray scan
 ├─ LightTurnOnOff (off)
 ├─ MoveToTeachPosition ({L|R}LoadPos)           ← step 9: return
 └─ PlcSendCommand ({L|R}RobotPickTray)          ← step 9: operator picks

{Left|Right}Calibrate.json
 └─ 4 × [ Move → CameraStartCapture(1 frame) → FindDatum → SetSharedVariable X/Y ]
   → TrayTeach (apply corrected 4-corner positions)

{Left|Right}ScanProducer.json (runs in Parallel with ScanConsumer)
 └─ ImagePoolCreate → TrayIterator loop
     [ Move to pocket → CameraStartCapture(1) → AppendToList {L|R}PocketJobs ]
   → ImagePoolComplete

{Left|Right}ScanConsumer.json
 └─ Parallel (WorkPool) pulling from {L|R}ScanPool
     worker → {Left|Right}ScanWorker.json

{Left|Right}ScanWorker.json  (per image)
 └─ ListLookupByIndex {L|R}PocketJobs by CurrentIndex
   → GetSharpnessLaplacian
   → SaveBitmap (if SaveImages=true)
   → AppendToList FlowResults (Stage, Row, Col, Score, File)
   → DisposeImage
```

## Controllable shared variables (set by Gen5Main or externally before Run)

| Variable          | Purpose                                          |
|-------------------|--------------------------------------------------|
| EnableLeftStage   | `true`/`false` — include left stage in cycle     |
| EnableRightStage  | `true`/`false` — include right stage             |
| SaveImages        | `true`/`false` — save raw pocket bitmaps         |
| ImageSavePath     | directory template root for SaveBitmap           |
| CycleCount        | number of full cycles (consumed by Loop)         |
| FlowAbort         | set to `true` by any step on fatal error; Loop exits|

## Device names used in flows (configure in device JSON)

- Motion groups: `LXYZ`, `RXYZ` (each contains 5 axes per stage)
- Cameras: `LeftScanCam`, `RightScanCam`
- Light channels: `LeftTopLight`, `RightTopLight`
- Tray instances: `LeftTray`, `RightTray`
- Teach positions per stage: `{L|R}LoadPos`,
  `{L|R}DefaultTrayLeftTop`, `{L|R}DefaultTrayRightTop`,
  `{L|R}DefaultTrayLeftBottom`, `{L|R}DefaultTrayRightBottom`
- PLC BitMap: `LeftTrayState`, `RightTrayState`
- PLC CommandMap: `LeftRobotPickTray`, `LeftRobotPlaceTray`,
  `RightRobotPickTray`, `RightRobotPlaceTray`
  (see `ConfigJson/Communication/GenericPlc.json`)

## Assumptions / out-of-scope

- Device Open/Close is **not** part of these flows — wire devices externally.
- Pocket grid is 25 × 20 per the spec; `TrayIteratorNode` iterates all slots
  regardless of state (`FilterState=-1`). Adjust filter if re-scanning only
  defective slots.
- Calibration uses `FindDatum` (outputs `DatumX`/`DatumY` in pixels). For real
  deployment substitute `CalibrateCameraMpp` or a site-specific vision chain.

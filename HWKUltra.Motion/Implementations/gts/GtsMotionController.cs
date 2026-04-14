using HWKUltra.Core;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations.gts;
using System.Runtime.InteropServices;

namespace HWKUltra.Motion.Implementations.gts
{
    public class GtsMotionController : IMotionController
    {
        private readonly GtsMotionControllerConfig _config;
        private readonly Dictionary<string, AxisScale> _scales;
        private readonly Dictionary<string, AxisMotionLimit> _limits;
        private readonly Dictionary<string, List<string>> _groupAxisOrder;
        private readonly Dictionary<string, short> _axisIdMap;
        private bool _isInit = false;

        // 固高GTS DLL函数声明 (常见API)
        #region GTS API

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_Open(short cardNum, short channel, string param);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_Close();

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_Reset();

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_LoadConfig(string filePath);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_ClrSts(short card, short axis, short count);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_AxisOn(short card, short axis);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_AxisOff(short card, short axis);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_GetSts(short card, short axis, out int status);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_Stop(short card, short axis, short mode);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_PrfTrap(short card, short axis);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_SetVel(short card, short axis, double vel);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_SetAcc(short card, short axis, double acc);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_SetDec(short card, short axis, double dec);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_SetPos(short card, short axis, int pos);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_Update(short card, short axis);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_GetPrfPos(short card, short axis, out double pos);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_GetEncPos(short card, short axis, out double pos);

        // 插补运动相关
        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_SetCrdPrm(short card, short crd, ref TCrdPrm crdPrm);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_CrdClear(short card, short crd, short fifo);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_LnXY(short card, short crd, int x, int y, double vel, double acc, double dec, double velEnd, short fifo);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_LnXYZ(short card, short crd, int x, int y, int z, double vel, double acc, double dec, double velEnd, short fifo);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_CrdStart(short card, short mask1, short mask2);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_CrdStatus(short card, short crd, out short status, out short cmdIndex, out double cmdPos, out short remainingSpace);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_BufIO(short card, short crd, ushort ioType, ushort mask, ushort value, short fifo);

        [DllImport("gts.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern short GTN_BufDelay(short card, short crd, ushort delayTime, short fifo);

        [StructLayout(LayoutKind.Sequential)]
        private struct TCrdPrm
        {
            public short dimension;
            public short profile1;
            public short profile2;
            public short profile3;
            public short profile4;
            public short profile5;
            public short profile6;
            public short profile7;
            public short profile8;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public short[] syncAxises;
            public short evenTime;
        }

        #endregion

        public GtsMotionController(GtsMotionControllerConfig config)
        {
            _config = config;
            _scales = new Dictionary<string, AxisScale>();
            _limits = new Dictionary<string, AxisMotionLimit>();
            _groupAxisOrder = new Dictionary<string, List<string>>();
            _axisIdMap = new Dictionary<string, short>();

            // 初始化限制和轴映射
            foreach (var axis in config.Axes)
            {
                _limits[axis.Name] = axis.Limit ?? GetDefaultLimit();
                _scales[axis.Name] = new AxisScale(axis.PulsePerUnit);
                _axisIdMap[axis.Name] = axis.AxisId;
            }

            foreach (var group in config.Groups)
            {
                _groupAxisOrder[group.Name] = group.Axes;
            }
        }

        public void Open()
        {
            if (_isInit) return;

            try
            {
                // 打开控制器
                short rtn = GTN_Open(_config.CardId, 0, _config.ConfigFilePath ?? string.Empty);
                CheckError(rtn, "Open", "Failed to open GTS controller");

                // 加载配置文件
                if (!string.IsNullOrEmpty(_config.ConfigFilePath))
                {
                    rtn = GTN_LoadConfig(_config.ConfigFilePath);
                    CheckError(rtn, "LoadConfig", "Failed to load configuration file");
                }

                // 清除所有轴状态
                rtn = GTN_Reset();
                CheckError(rtn, "Reset", "Failed to reset controller");

                Thread.Sleep(100);

                // 初始化各轴
                foreach (var axis in _config.Axes)
                {
                    short axisId = axis.AxisId;

                    // 清除轴状态
                    rtn = GTN_ClrSts(_config.CardId, axisId, 1);
                    CheckError(rtn, $"ClrSts_{axis.Name}", "Failed to clear axis status");

                    // 上电
                    rtn = GTN_AxisOn(_config.CardId, axisId);
                    CheckError(rtn, $"AxisOn_{axis.Name}", "Failed to power on axis");

                    // 设置为点位模式
                    rtn = GTN_PrfTrap(_config.CardId, axisId);
                    CheckError(rtn, $"PrfTrap_{axis.Name}", "Failed to set trap mode");
                }

                // 初始化坐标系配置
                foreach (var crdParam in _config.CrdParams)
                {
                    var crdPrm = new TCrdPrm
                    {
                        dimension = crdParam.Dimension,
                        evenTime = 50
                    };

                    // 设置坐标轴映射
                    for (int i = 0; i < crdParam.Axes.Count && i < 8; i++)
                    {
                        short axisId = crdParam.Axes[i];
                        switch (i)
                        {
                            case 0: crdPrm.profile1 = axisId; break;
                            case 1: crdPrm.profile2 = axisId; break;
                            case 2: crdPrm.profile3 = axisId; break;
                            case 3: crdPrm.profile4 = axisId; break;
                            case 4: crdPrm.profile5 = axisId; break;
                            case 5: crdPrm.profile6 = axisId; break;
                            case 6: crdPrm.profile7 = axisId; break;
                            case 7: crdPrm.profile8 = axisId; break;
                        }
                    }

                    rtn = GTN_SetCrdPrm(_config.CardId, crdParam.CrdId, ref crdPrm);
                    CheckError(rtn, $"SetCrdPrm_{crdParam.CrdId}", "Failed to set coordinate parameters");
                }

                _isInit = true;
            }
            catch (Exception ex)
            {
                throw new MotionException("GTS", "Open failed", ex);
            }
        }

        public void Close()
        {
            if (!_isInit) return;

            try
            {
                // 所有轴下电
                foreach (var axis in _config.Axes)
                {
                    GTN_AxisOff(_config.CardId, axis.AxisId);
                }

                // 关闭控制器
                GTN_Close();
                _isInit = false;
            }
            catch (Exception ex)
            {
                throw new MotionException("GTS", "Close failed", ex);
            }
        }

        public void MoveAxis(string axisName, double pos, MotionProfile? profile = null)
        {
            if (!_isInit) return;

            if (!_axisIdMap.TryGetValue(axisName, out short axisId))
            {
                throw new MotionException(axisName, $"Axis {axisName} not found");
            }

            try
            {
                // 检查轴状态
                int status;
                short rtn = GTN_GetSts(_config.CardId, axisId, out status);
                CheckError(rtn, $"GetSts_{axisName}", "Failed to get axis status");

                // 获取限制并钳制参数
                var p = Clamp(axisName, profile);

                // 转换为脉冲
                int pulsePos = ToPulse(axisName, pos);

                // 设置运动参数
                rtn = GTN_SetVel(_config.CardId, axisId, p.Vel ?? _config.DefaultVel);
                CheckError(rtn, $"SetVel_{axisName}", "Failed to set velocity");

                rtn = GTN_SetAcc(_config.CardId, axisId, p.Acc ?? _config.DefaultAcc);
                CheckError(rtn, $"SetAcc_{axisName}", "Failed to set acceleration");

                rtn = GTN_SetDec(_config.CardId, axisId, p.Dec ?? _config.DefaultDec);
                CheckError(rtn, $"SetDec_{axisName}", "Failed to set deceleration");

                rtn = GTN_SetPos(_config.CardId, axisId, pulsePos);
                CheckError(rtn, $"SetPos_{axisName}", "Failed to set position");

                // 启动运动
                rtn = GTN_Update(_config.CardId, axisId);
                CheckError(rtn, $"Update_{axisName}", "Failed to start motion");

                // 等待运动完成
                WaitForAxisIdle(axisName, axisId);
            }
            catch (MotionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MotionException(axisName, "Move failed", ex);
            }
        }

        public void MoveAxisRelative(string axisName, double distance, MotionProfile? profile = null)
        {
            if (!_isInit) return;
            var currentPos = GetPosition(axisName);
            MoveAxis(axisName, currentPos + distance, profile);
        }

        public void MoveAxisVelocity(string axisName, double velocity, MotionProfile? profile = null)
        {
            if (!_isInit) return;

            if (!_axisIdMap.TryGetValue(axisName, out short axisId))
                throw new MotionException(axisName, $"Axis {axisName} not found");

            try
            {
                var p = Clamp(axisName, profile);
                short rtn = GTN_PrfTrap(_config.CardId, axisId);
                CheckError(rtn, $"PrfTrap_{axisName}", "Failed to set trap mode");

                rtn = GTN_SetVel(_config.CardId, axisId, Math.Abs(velocity));
                CheckError(rtn, $"SetVel_{axisName}", "Failed to set velocity");

                rtn = GTN_SetAcc(_config.CardId, axisId, p.Acc ?? _config.DefaultAcc);
                CheckError(rtn, $"SetAcc_{axisName}", "Failed to set acceleration");

                // 速度运动用极大位置模拟方向
                int direction = velocity >= 0 ? 1 : -1;
                rtn = GTN_SetPos(_config.CardId, axisId, direction * int.MaxValue / 2);
                CheckError(rtn, $"SetPos_{axisName}", "Failed to set position");

                rtn = GTN_Update(_config.CardId, axisId);
                CheckError(rtn, $"Update_{axisName}", "Failed to start motion");
            }
            catch (MotionException) { throw; }
            catch (Exception ex) { throw new MotionException(axisName, "Velocity move failed", ex); }
        }

        public void HomeAxis(string axisName)
        {
            if (!_isInit) return;

            if (!_axisIdMap.TryGetValue(axisName, out short axisId))
                throw new MotionException(axisName, $"Axis {axisName} not found");

            try
            {
                // TODO: 实际回零需根据硬件配置选择回零模式
                short rtn = GTN_ClrSts(_config.CardId, axisId, 1);
                CheckError(rtn, $"Home_ClrSts_{axisName}", "Failed to clear status for home");

                MoveAxis(axisName, 0);
                WaitForAxisIdle(axisName, axisId);
            }
            catch (MotionException) { throw; }
            catch (Exception ex) { throw new MotionException(axisName, "Home failed", ex); }
        }

        public double GetPosition(string axisName)
        {
            if (!_isInit) return 0;

            if (!_axisIdMap.TryGetValue(axisName, out short axisId))
                throw new MotionException(axisName, $"Axis {axisName} not found");

            try
            {
                double pos;
                short rtn = GTN_GetEncPos(_config.CardId, axisId, out pos);
                CheckError(rtn, $"GetEncPos_{axisName}", "Failed to get position");
                return FromPulse(axisName, (int)pos);
            }
            catch (MotionException) { throw; }
            catch (Exception ex) { throw new MotionException(axisName, "GetPosition failed", ex); }
        }

        public void StopAxis(string axisName)
        {
            if (!_axisIdMap.TryGetValue(axisName, out short axisId))
                throw new MotionException(axisName, $"Axis {axisName} not found");
            Stop(axisId);
        }

        public bool IsAxisBusy(string axisName)
        {
            if (!_axisIdMap.TryGetValue(axisName, out short axisId))
                return false;
            return IsBusy(axisId);
        }

        public void Stop(int axisId)
        {
            if (!_isInit) return;

            try
            {
                // 平滑停止 (mode=0)
                short rtn = GTN_Stop(_config.CardId, (short)axisId, 0);
                CheckError(rtn, "Stop", "Failed to stop axis");
            }
            catch (Exception ex)
            {
                throw new MotionException(axisId.ToString(), "Stop failed", ex);
            }
        }

        public bool IsBusy(int axisId)
        {
            if (!_isInit) return false;

            try
            {
                int status;
                short rtn = GTN_GetSts(_config.CardId, (short)axisId, out status);
                if (rtn != 0) return false;

                // 检查是否在运动中 (固高状态位定义)
                const int AXIS_STATUS_RUNNING = 0x400;  // 位10: 运动状态
                return (status & AXIS_STATUS_RUNNING) != 0;
            }
            catch
            {
                return false;
            }
        }

        public void MoveGroup(string group, AxisPosition pos, MotionProfile? profile = null)
        {
            if (!_isInit) return;

            if (!_groupAxisOrder.TryGetValue(group, out var axesList))
            {
                throw new MotionException(group, $"Group {group} not found");
            }

            var groupConfig = _config.Groups.FirstOrDefault(g => g.Name == group);
            if (groupConfig == null)
            {
                throw new MotionException(group, $"Group configuration {group} not found");
            }

            try
            {
                short crdId = groupConfig.CrdId;
                short fifo = 0;

                // 获取各轴的profile并取最小值
                var profiles = axesList.Select(a => Clamp(a, profile)).ToList();
                var final = new MotionProfile
                {
                    Vel = profiles.Min(p => p.Vel),
                    Acc = profiles.Min(p => p.Acc),
                    Dec = profiles.Min(p => p.Dec),
                    Jerk = profiles.Min(p => p.Jerk)
                };

                // 清除坐标系FIFO
                short rtn = GTN_CrdClear(_config.CardId, crdId, fifo);
                CheckError(rtn, $"CrdClear_{group}", "Failed to clear coordinate FIFO");

                // 转换位置为脉冲
                var positions = ToPulseArray(group, pos);

                // 根据维度选择不同的插补指令
                if (groupConfig.Dimension == 2 && positions.Length >= 2)
                {
                    rtn = GTN_LnXY(
                        _config.CardId,
                        crdId,
                        positions[0],
                        positions[1],
                        final.Vel ?? _config.DefaultVel,
                        final.Acc ?? _config.DefaultAcc,
                        final.Dec ?? _config.DefaultDec,
                        0,  // velEnd
                        fifo);
                    CheckError(rtn, $"LnXY_{group}", "Failed to set linear interpolation");
                }
                else if (groupConfig.Dimension == 3 && positions.Length >= 3)
                {
                    rtn = GTN_LnXYZ(
                        _config.CardId,
                        crdId,
                        positions[0],
                        positions[1],
                        positions[2],
                        final.Vel ?? _config.DefaultVel,
                        final.Acc ?? _config.DefaultAcc,
                        final.Dec ?? _config.DefaultDec,
                        0,  // velEnd
                        fifo);
                    CheckError(rtn, $"LnXYZ_{group}", "Failed to set linear interpolation");
                }
                else
                {
                    throw new MotionException(group, "Unsupported dimension or insufficient axes");
                }

                // 启动插补运动
                rtn = GTN_CrdStart(_config.CardId, (short)(1 << (crdId - 1)), 0);
                CheckError(rtn, $"CrdStart_{group}", "Failed to start coordinate motion");

                // 等待插补完成
                WaitForGroupIdle(group, crdId);
            }
            catch (MotionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MotionException(group, "Group move failed", ex);
            }
        }

        #region 内部方法

        private void WaitForAxisIdle(string axisName, short axisId)
        {
            const int timeoutMs = 30000;  // 30秒超时
            const int intervalMs = 50;
            int elapsedMs = 0;

            while (elapsedMs < timeoutMs)
            {
                int status;
                short rtn = GTN_GetSts(_config.CardId, axisId, out status);
                if (rtn != 0) break;

                // 检查是否运动完成 (固高位10表示运动中)
                const int AXIS_STATUS_RUNNING = 0x400;
                if ((status & AXIS_STATUS_RUNNING) == 0)
                    break;

                Thread.Sleep(intervalMs);
                elapsedMs += intervalMs;
            }

            if (elapsedMs >= timeoutMs)
            {
                throw new MotionException(axisName, "Wait for idle timeout");
            }
        }

        private void WaitForGroupIdle(string groupName, short crdId)
        {
            const int timeoutMs = 60000;  // 60秒超时
            const int intervalMs = 50;
            int elapsedMs = 0;

            while (elapsedMs < timeoutMs)
            {
                short status, cmdIndex;
                double cmdPos;
                short remainingSpace;
                short rtn = GTN_CrdStatus(_config.CardId, crdId, out status, out cmdIndex, out cmdPos, out remainingSpace);
                if (rtn != 0) break;

                // 检查插补是否完成 (status=0表示完成)
                if (status == 0)
                    break;

                Thread.Sleep(intervalMs);
                elapsedMs += intervalMs;
            }

            if (elapsedMs >= timeoutMs)
            {
                throw new MotionException(groupName, "Wait for group idle timeout");
            }
        }

        private int[] ToPulseArray(string group, AxisPosition pos)
        {
            var axes = _groupAxisOrder[group];
            var result = new int[axes.Count];

            for (int i = 0; i < axes.Count; i++)
            {
                var axis = axes[i];
                if (!pos.Values.ContainsKey(axis))
                {
                    throw new MotionException(axis, $"Missing axis {axis} in position");
                }
                result[i] = ToPulse(axis, pos[axis]);
            }

            return result;
        }

        private int ToPulse(string axis, double value)
        {
            var scale = _scales[axis].PulsePerUnit;
            return (int)(value * scale);
        }

        private double FromPulse(string axis, int pulse)
        {
            var scale = _scales[axis].PulsePerUnit;
            return pulse / scale;
        }

        private MotionProfile Clamp(string axis, MotionProfile? req)
        {
            var limit = _limits[axis];

            return new MotionProfile
            {
                Vel = Math.Min(req?.Vel ?? limit.MaxVel, limit.MaxVel),
                Acc = Math.Min(req?.Acc ?? limit.MaxAcc, limit.MaxAcc),
                Dec = Math.Min(req?.Dec ?? limit.MaxDec, limit.MaxDec),
                Jerk = Math.Min(req?.Jerk ?? limit.MaxJerk, limit.MaxJerk)
            };
        }

        private static AxisMotionLimit GetDefaultLimit()
        {
            return new AxisMotionLimit
            {
                MaxVel = 50000,
                MaxAcc = 1000000,
                MaxDec = 1000000,
                MaxJerk = 5000000
            };
        }

        private static void CheckError(short rtn, string operation, string message)
        {
            if (rtn != 0)
            {
                throw new Exception($"[{operation}] Error code: {rtn}, {message}");
            }
        }

        #endregion
    }
}

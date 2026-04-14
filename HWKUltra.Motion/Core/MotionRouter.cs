using HWKUltra.Core;
using HWKUltra.Motion.Abstractions;
using HWKUltra.Motion.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HWKUltra.Motion.Core
{
    public class MotionRouter
    {
        private readonly IMotionController _controller;
        private readonly Dictionary<string, int> _axisMap;
        private readonly Dictionary<string, ISingleAxis> _singleAxes;
        private readonly Dictionary<string, string[]> _groupAxesMap;

        public MotionRouter(
            IMotionController controller,
            Dictionary<string, int> axisMap,
            Dictionary<string, ISingleAxis> singleAxes,
            Dictionary<string, string[]>? groupAxesMap = null)
        {
            _controller = controller;
            _axisMap = axisMap;
            _singleAxes = singleAxes;
            _groupAxesMap = groupAxesMap ?? new Dictionary<string, string[]>();
        }

        public void Move(string axisName, double pos, MotionProfile? profile = null)
        {
            if (_singleAxes.TryGetValue(axisName, out var axis))
            {
                axis.MoveTo(pos);
                return;
            }

            if (_axisMap.ContainsKey(axisName))
            {
                _controller.MoveAxis(axisName, pos, profile);
                return;
            }

            throw new Exception($"Axis not found: {axisName}");
        }

        public void MoveRelative(string axisName, double distance, MotionProfile? profile = null)
        {
            if (_axisMap.ContainsKey(axisName))
            {
                _controller.MoveAxisRelative(axisName, distance, profile);
                return;
            }

            // ISingleAxis 不支持相对运动，用绝对运动模拟
            if (_singleAxes.TryGetValue(axisName, out var axis))
            {
                var current = _controller.GetPosition(axisName);
                axis.MoveTo(current + distance);
                return;
            }

            throw new Exception($"Axis not found: {axisName}");
        }

        public void MoveVelocity(string axisName, double velocity, MotionProfile? profile = null)
        {
            if (_axisMap.ContainsKey(axisName))
            {
                _controller.MoveAxisVelocity(axisName, velocity, profile);
                return;
            }

            throw new Exception($"Axis {axisName} does not support velocity move");
        }

        public void Home(string axisName)
        {
            if (_singleAxes.TryGetValue(axisName, out var axis))
            {
                axis.Init();
                return;
            }

            if (_axisMap.ContainsKey(axisName))
            {
                _controller.HomeAxis(axisName);
                return;
            }

            throw new Exception($"Axis not found: {axisName}");
        }

        public double GetPosition(string axisName)
        {
            if (_axisMap.ContainsKey(axisName))
                return _controller.GetPosition(axisName);

            // ISingleAxis 无 GetPosition，返回 0
            if (_singleAxes.ContainsKey(axisName))
                return 0;

            throw new Exception($"Axis not found: {axisName}");
        }

        public void Stop(string axisName)
        {
            if (_singleAxes.TryGetValue(axisName, out var axis))
            {
                axis.Stop();
                return;
            }

            if (_axisMap.ContainsKey(axisName))
            {
                _controller.StopAxis(axisName);
                return;
            }

            throw new Exception($"Axis not found: {axisName}");
        }

        public bool IsBusy(string axisName)
        {
            if (_singleAxes.TryGetValue(axisName, out var axis))
                return axis.IsBusy();

            if (_axisMap.ContainsKey(axisName))
                return _controller.IsAxisBusy(axisName);

            throw new Exception($"Axis not found: {axisName}");
        }

        /// <summary>
        /// 异步等待轴到位
        /// </summary>
        public async Task WaitForIdleAsync(string axisName, int timeoutMs = 30000, CancellationToken cancellationToken = default)
        {
            const int pollIntervalMs = 10;
            int elapsed = 0;

            while (elapsed < timeoutMs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsBusy(axisName))
                    return;
                await Task.Delay(pollIntervalMs, cancellationToken);
                elapsed += pollIntervalMs;
            }

            throw new MotionException(axisName, $"Wait for idle timeout ({timeoutMs}ms)");
        }

        /// <summary>
        /// 按组名进行插补运动
        /// </summary>
        /// <param name="groupName">组名称，如"XY"</param>
        /// <param name="pos">各轴目标位置</param>
        /// <param name="profile">运动参数，可选</param>
        public void MoveGroup(string groupName, AxisPosition pos, MotionProfile? profile = null)
        {
            if (!_groupAxesMap.TryGetValue(groupName, out _))
                throw new Exception($"Group {groupName} not found in group axes map");

            _controller.MoveGroup(groupName, pos, profile);
        }

        /// <summary>
        /// 按组名进行插补运动，指定各轴目标位置
        /// </summary>
        /// <param name="groupName">组名称</param>
        /// <param name="positions">轴名到位置的映射</param>
        /// <param name="profile">运动参数，可选</param>
        public void MoveGroup(string groupName, Dictionary<string, double> positions, MotionProfile? profile = null)
        {
            if (!_groupAxesMap.TryGetValue(groupName, out var axisNames))
                throw new Exception($"Group {groupName} not found in group axes map");

            // 验证所有组内轴都有位置数据
            foreach (var axisName in axisNames)
            {
                if (!positions.ContainsKey(axisName))
                    throw new Exception($"Position for axis {axisName} not provided in group {groupName}");
            }

            var axisPos = new AxisPosition(positions);
            _controller.MoveGroup(groupName, axisPos, profile);
        }

        /// <summary>
        /// 按组名进行插补运动（旧版兼容，使用轴名数组和位置数组）
        /// </summary>
        public void MoveGroup(string[] axisNames, double[] positions, MotionProfile? profile = null)
        {
            if (axisNames.Length != positions.Length)
                throw new Exception("Axis names and positions count mismatch");

            // 找到对应的组名
            string? groupName = null;
            foreach (var kvp in _groupAxesMap)
            {
                if (kvp.Value.SequenceEqual(axisNames))
                {
                    groupName = kvp.Key;
                    break;
                }
            }

            if (groupName == null)
                throw new Exception("No matching group found for specified axes");

            var posDict = new Dictionary<string, double>();
            for (int i = 0; i < axisNames.Length; i++)
            {
                posDict[axisNames[i]] = positions[i];
            }

            var axisPos = new AxisPosition(posDict);
            _controller.MoveGroup(groupName, axisPos, profile);
        }
    }
}

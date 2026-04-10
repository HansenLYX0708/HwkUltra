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

            if (_axisMap.TryGetValue(axisName, out var axisId))
            {
                _controller.MoveAxis(axisName, pos, profile);
                return;
            }

            throw new Exception($"Axis not found: {axisName}");
        }

        public void Stop(string axisName)
        {
            if (_singleAxes.TryGetValue(axisName, out var axis))
            {
                axis.Stop();
                return;
            }

            if (_axisMap.TryGetValue(axisName, out var axisId))
            {
                _controller.Stop(axisId);
                return;
            }

            throw new Exception($"Axis not found: {axisName}");
        }

        public bool IsBusy(string axisName)
        {
            if (_singleAxes.TryGetValue(axisName, out var axis))
                return axis.IsBusy();

            if (_axisMap.TryGetValue(axisName, out var axisId))
                return _controller.IsBusy(axisId);

            throw new Exception($"Axis not found: {axisName}");
        }

        /// <summary>
        /// 按组名进行插补运动
        /// </summary>
        /// <param name="groupName">组名称，如"XY"</param>
        /// <param name="profile">运动参数，可选</param>
        public void MoveGroup(string groupName, MotionProfile? profile = null)
        {
            if (!_groupAxesMap.TryGetValue(groupName, out var axisNames))
                throw new Exception($"Group {groupName} not found in group axes map");

            // 构造当前位置（从各轴获取实际位置）
            var posDict = new Dictionary<string, double>();
            foreach (var axisName in axisNames)
            {
                // 这里简化处理，实际应该从控制器获取当前位置或传入目标位置
                // 暂时假设调用者会先设置各轴目标位置
                posDict[axisName] = 0; // 占位
            }

            var axisPos = new AxisPosition(posDict);
            _controller.MoveGroup(groupName, axisPos, profile);
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

using HWKUltra.Motion.Abstractions;
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

        public MotionRouter(
            IMotionController controller,
            Dictionary<string, int> axisMap,
            Dictionary<string, ISingleAxis> singleAxes)
        {
            _controller = controller;
            _axisMap = axisMap;
            _singleAxes = singleAxes;
        }

        public void Move(string axisName, double pos)
        {
            if (_singleAxes.TryGetValue(axisName, out var axis))
            {
                axis.MoveTo(pos);
                return;
            }

            if (_axisMap.TryGetValue(axisName, out var axisId))
            {
                //_controller.MoveAxis(axisName, pos, 10, 100);
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

        public void MoveGroup(string[] axisNames, double[] positions)
        {
            var ids = new List<int>();

            foreach (var name in axisNames)
            {
                if (!_axisMap.ContainsKey(name))
                    throw new Exception($"Axis {name} not in controller, cannot group move");

                ids.Add(_axisMap[name]);
            }

            //_controller.MoveGroup(ids.ToArray(), positions);
        }
    }
}

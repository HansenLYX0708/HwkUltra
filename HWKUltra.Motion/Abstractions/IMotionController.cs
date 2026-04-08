using HWKUltra.Core;
using HWKUltra.Motion.Implementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HWKUltra.Motion.Abstractions
{
    public interface IMotionController
    {
        void Open();
        void Close();

        void MoveAxis(string axisName, double pos, MotionProfile profile = null);
        void Stop(int axisId);
        bool IsBusy(int axisId);

        void MoveGroup(string group, AxisPosition pos, MotionProfile profile = null);
    }
}

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
        void MoveAxisRelative(string axisName, double distance, MotionProfile profile = null);
        void MoveAxisVelocity(string axisName, double velocity, MotionProfile profile = null);
        void HomeAxis(string axisName);
        double GetPosition(string axisName);
        void StopAxis(string axisName);
        bool IsAxisBusy(string axisName);

        void Stop(int axisId);
        bool IsBusy(int axisId);

        void MoveGroup(string group, AxisPosition pos, MotionProfile profile = null);
    }
}

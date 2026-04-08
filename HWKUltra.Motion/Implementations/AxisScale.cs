using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HWKUltra.Motion.Implementations
{
    public class AxisScale
    {
        public AxisScale(double pulsePerUnit)
        {
            PulsePerUnit = pulsePerUnit;
        }

        public double PulsePerUnit { get; set; }  // 脉冲 / mm
    }
}

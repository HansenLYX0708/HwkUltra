using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HWKUltra.Motion.Implementations
{
    public class AxisMotionLimit
    {
        public float MaxVel { get; set; }
        public float MaxAcc { get; set; }
        public float MaxDec { get; set; }
        public float MaxJerk { get; set; }
    }
}

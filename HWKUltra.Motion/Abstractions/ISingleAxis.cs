using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HWKUltra.Motion.Abstractions
{
    public interface ISingleAxis
    {
        void Init();
        void MoveTo(double pos);
        void Stop();
        bool IsBusy();
    }
}

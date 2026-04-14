using System;
using System.Runtime.InteropServices;

namespace WD.AVI.LaserLib
{
    public sealed class PinnedObject : IDisposable
    {
        private GCHandle _Handle;

        public IntPtr Pointer
        {
            get { return _Handle.AddrOfPinnedObject(); }
        }

        public PinnedObject(object target)
        {
            _Handle = GCHandle.Alloc(target, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            _Handle.Free();
            _Handle = new GCHandle();
        }
    }
}

using System.Runtime.InteropServices;

namespace HWKUltra.Measurement.Implementations.Keyence
{
    /// <summary>
    /// Helper class for pinning managed objects to obtain a stable pointer
    /// for use with unmanaged (native) API calls.
    /// </summary>
    public sealed class PinnedObject : IDisposable
    {
        private GCHandle _handle;

        /// <summary>
        /// Gets the pointer to the pinned object.
        /// </summary>
        public IntPtr Pointer => _handle.AddrOfPinnedObject();

        public PinnedObject(object target)
        {
            _handle = GCHandle.Alloc(target, GCHandleType.Pinned);
        }

        public void Dispose()
        {
            if (_handle.IsAllocated)
            {
                _handle.Free();
            }
        }
    }
}

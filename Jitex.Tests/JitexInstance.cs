using Jitex.JIT;
using System;

namespace Jitex.Tests
{
    public class JitexInstance : IDisposable
    {
        private static readonly object InstanceLock = new object();
        private static ManagedJit _jit;

        public static ManagedJit GetInstance()
        {
            lock (InstanceLock)
            {
                return _jit ??= ManagedJit.GetInstance();
            }
        }

        public void Dispose()
        {
            lock (InstanceLock)
            {
                _jit?.Dispose();
            }
        }
    }
}

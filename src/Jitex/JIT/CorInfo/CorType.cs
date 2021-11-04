using System;

namespace Jitex.JIT.CorInfo
{
    public abstract class CorType
    {
        public IntPtr HInstance { get; }

        protected CorType(IntPtr hInstance) => HInstance = hInstance;
    }
}

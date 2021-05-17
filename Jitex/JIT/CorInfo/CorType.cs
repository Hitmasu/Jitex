using System;

namespace Jitex.JIT.CorInfo
{
    internal abstract class CorType
    {
        public IntPtr HInstance { get; }

        protected CorType(IntPtr hInstance) => HInstance = hInstance;
    }
}

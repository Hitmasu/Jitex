using System;

namespace Jitex.JIT.CorInfo
{
    internal abstract class CorType
    {
        public IntPtr HInstance { get; set; }

        protected CorType(IntPtr hInstance) => HInstance = hInstance;
    }
}

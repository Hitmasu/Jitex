using System;

namespace Jitex.Runtime
{
    public class NativeCode
    {
        /// <summary>
        /// Address of native code.
        /// </summary>
        public IntPtr Address { get; internal set; }

        /// <summary>
        /// Size of native code.
        /// </summary>
        public int Size { get; internal set; }

        internal NativeCode(IntPtr address, int size)
        {
            Address = address;
            Size = size;
        }
    }
}

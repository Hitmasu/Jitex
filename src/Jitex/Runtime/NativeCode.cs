using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Runtime
{
    public class NativeCode
    {
        public IntPtr Address { get; internal set; }
        public int Size { get; internal set; }

        internal NativeCode(IntPtr address, int size)
        {
            Address = address;
            Size = size;
        }
    }
}

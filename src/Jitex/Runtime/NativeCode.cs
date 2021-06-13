using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.Runtime
{
    public class NativeCode
    {
        public IntPtr Address { get; set; }
        public int Size { get; set; }

        internal NativeCode(IntPtr address, int size)
        {
            Address = address;
            Size = size;
        }
    }
}

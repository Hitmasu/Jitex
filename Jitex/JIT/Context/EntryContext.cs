using System;
using System.Collections.Generic;
using System.Text;

namespace Jitex.JIT.Context
{
    public class EntryContext
    {
        public IntPtr NativeEntry { get; set; }
        public int Size { get; set; }

        public EntryContext(IntPtr nativeEntry, int size)
        {
            NativeEntry = nativeEntry;
            Size = size;
        }
    }
}

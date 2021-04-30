using System;

namespace Jitex.JIT.Context
{
    internal class EntryContext
    {
        public IntPtr NativeEntry { get; }
        public int Size { get; }

        public EntryContext(IntPtr nativeEntry, int size)
        {
            NativeEntry = nativeEntry;
            Size = size;
        }
    }
}
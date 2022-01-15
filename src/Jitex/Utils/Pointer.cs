using System;

namespace Jitex.Utils
{
    public sealed unsafe class Pointer
    {
        internal void* Ptr;

        internal IntPtr Address => new(Ptr);

        internal Pointer(void* ptr)
        {
            Ptr = ptr;
        }

        public static Pointer Box(void* ptr) => new(ptr);
    }
}
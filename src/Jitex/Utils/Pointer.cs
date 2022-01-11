using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jitex.Utils
{
    public sealed unsafe class Pointer
    {
        private readonly void* _ptr;
            
        internal IntPtr Address => new IntPtr(_ptr);

        private Pointer(void* ptr)
        {
            _ptr = ptr;
        }

        public static object Box(void* ptr) => new Pointer(ptr);

        internal object Unbox(Type type) => MarshalHelper.GetObjectFromAddress(Address, type);
        internal ref T Unbox<T>() => ref Unsafe.AsRef<T>(_ptr);
    }
}
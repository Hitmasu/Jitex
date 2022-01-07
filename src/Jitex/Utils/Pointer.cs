using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Jitex.Utils
{
    public sealed unsafe class Pointer
    {
        private readonly void* _ptr;

        private Pointer(void* ptr)
        {
            _ptr = ptr;
        }

        public static object Box(void* ptr) => new Pointer(ptr);

        public static ref T UnBox<T>(object ptr) => ref Unsafe.AsRef<T>(GetPointer(ptr));

        public static void* GetPointer(object ptr)
        {
            if (ptr is not Pointer pointer)
                throw new ArgumentException($"Parameter {ptr.GetType()} is not a Pointer.");

            return pointer._ptr;
        }
    }
}

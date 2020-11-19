using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitex.Helpers
{
    internal static class Detour
    {
        public static void CreateDetour(MethodInfo methodOriginal, MethodInfo methodDetour)
        {
            // IntPtr method = 
        }

        public static IntPtr GetMethodAddress(MethodInfo method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);

            IntPtr methodPointer = handle.GetFunctionPointer();

            byte jmp = Marshal.ReadByte(methodPointer);

            if (jmp == 0xE9)
            {
                int jmpSize = Marshal.ReadInt32(methodPointer + 1);
                methodPointer = new IntPtr(methodPointer.ToInt64() + jmpSize + 5);
            }

            return methodPointer;
        }

        public static RuntimeMethodHandle GetMethodHandle(MethodInfo method)
        {
            if (method is DynamicMethod dynamicMethod)
            {
                return default;
            }
            else
            {
                return method.MethodHandle;
            }
        }
    }
}
using System;
using System.Runtime.InteropServices;
using static Jitex.Utils.Memory;

namespace Jitex.Utils
{
    internal static class RuntimeHelperExtension
    {
        public static void PrepareDelegate(Delegate del, params object[] parameters)
        {
            IntPtr delPtr = Marshal.GetFunctionPointerForDelegate(del);

            IntPtr trampolinePtr = AllocateTrampoline(delPtr);
            Delegate trampoline = Marshal.GetDelegateForFunctionPointer(trampolinePtr, del.GetType());

            trampoline.DynamicInvoke(parameters);

            FreeTrampoline(trampolinePtr);
        }
    }
}
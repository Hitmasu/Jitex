using System;
using System.Reflection;
using Jitex.Runtime;

namespace Jitex.Utils
{
    internal static class DetourHelper
    {
        public static byte[] CreateDetour(MethodBase methodDetour)
        {
            IntPtr detourAddress = RuntimeMethodCache.GetNativeAddress(methodDetour);
            return Trampoline.GetTrampoline(detourAddress);
        }

        public static byte[] CreateDetour(IntPtr address)
        {
            return Trampoline.GetTrampoline(address);
        }

        public static byte[] CreateDetour(Delegate del)
        {
            IntPtr detourAddress = RuntimeMethodCache.GetNativeAddress(del.Method);
            return Trampoline.GetTrampoline(detourAddress);
        }
    }
}
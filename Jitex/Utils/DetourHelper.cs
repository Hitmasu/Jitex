using System;
using System.Reflection;

namespace Jitex.Utils
{
    internal static class DetourHelper
    {
        public static byte[] CreateDetour(MethodBase methodDetour)
        {
            IntPtr detourAddress = MethodHelper.GetMethodAddress(methodDetour);
            return Trampoline.GetTrampoline(detourAddress);
        }

        public static byte[] CreateDetour(IntPtr address)
        {
            return Trampoline.GetTrampoline(address);
        }

        public static byte[] CreateDetour(Delegate del)
        {
            IntPtr detourAddress = MethodHelper.GetMethodAddress(del.Method);
            return Trampoline.GetTrampoline(detourAddress);
        }
    }
}
using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using static Jitex.Utils.Trampoline;

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

        public static Task InternalPrepareMethodAsync(MethodBase method)
        {
            return Task.Run(() =>
            {
                RuntimeHelpers.PrepareMethod(method.MethodHandle);
            });
        }
    }
}
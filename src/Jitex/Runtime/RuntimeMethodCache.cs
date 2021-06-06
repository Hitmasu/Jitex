using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Jitex.JIT;
using Jitex.Utils;
using Jitex.Utils.Comparer;

namespace Jitex.Runtime
{
    internal static class RuntimeMethodCache
    {
        private static readonly ConcurrentDictionary<IntPtr, NativeCode> NativeCache = new ConcurrentDictionary<IntPtr, NativeCode>();
        private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new ConcurrentBag<MethodCompiled>();

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            IntPtr methodHandle = MethodHelper.GetMethodHandle(methodCompiled.Method).Value;
            NativeCache.TryAdd(methodHandle, new NativeCode(methodCompiled.NativeCodeAddress, methodCompiled.NativeCodeSize));
            CompiledMethods.Add(methodCompiled);
        }

        public static NativeCode GetNativeCode(MethodBase method)
        {
            IntPtr methodHandle = MethodHelper.GetMethodHandle(method).Value;

            if (!NativeCache.TryGetValue(methodHandle, out NativeCode nativeCode))
            {
                if (!JitexManager.IsLoaded)
                    throw new Exception("Jitex is not installed!");

                RuntimeHelperExtension.InternalPrepareMethodAsync(method).Wait();

                while (!NativeCache.TryGetValue(methodHandle, out nativeCode))
                {
                    Thread.Sleep(50);

                    MethodCompiled? methodCompiled = CompiledMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, method));

                    if (methodCompiled != null)
                    {
                        nativeCode = new NativeCode(methodCompiled.NativeCodeAddress, methodCompiled.NativeCodeSize);
                        NativeCache.TryAdd(methodHandle, nativeCode);

                        return nativeCode;
                    }
                }
            }

            return nativeCode;
        }
    }
}
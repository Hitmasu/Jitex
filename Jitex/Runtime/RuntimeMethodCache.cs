using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Jitex.JIT;
using Jitex.Utils;

namespace Jitex.Runtime
{
    internal static class RuntimeMethodCache
    {
        private static readonly ConcurrentDictionary<IntPtr, NativeCode> NativeCache = new ConcurrentDictionary<IntPtr, NativeCode>();
        private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new ConcurrentBag<MethodCompiled>();

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            NativeCache.TryAdd(methodCompiled.Method.MethodHandle.Value, new NativeCode(methodCompiled.NativeCodeAddress, methodCompiled.NativeCodeSize));
            CompiledMethods.Add(methodCompiled);
        }

        public static NativeCode GetNativeCode(MethodBase method)
        {
            if (!NativeCache.TryGetValue(method.MethodHandle.Value, out NativeCode nativeCode))
            {
                if (!JitexManager.IsLoaded)
                    throw new Exception("Jitex is not installed!");

                RuntimeHelperExtension.InternalPrepareMethodAsync(method).Wait();

                while (!NativeCache.TryGetValue(method.MethodHandle.Value, out nativeCode))
                    Thread.Sleep(50);

                return nativeCode;
            }

            return nativeCode;
        }
    }
}
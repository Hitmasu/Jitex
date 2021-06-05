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

        //Future feature
        //private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new ConcurrentBag<MethodCompiled>();

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            IntPtr methodHandle = MethodHelper.GetMethodHandle(methodCompiled.Method).Value;
            NativeCache.TryAdd(methodHandle, new NativeCode(methodCompiled.NativeCodeAddress, methodCompiled.NativeCodeSize));
            //CompiledMethods.Add(methodCompiled);
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
                    Thread.Sleep(50);

                return nativeCode;
            }

            return nativeCode;
        }
    }
}
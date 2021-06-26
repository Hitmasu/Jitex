using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jitex.JIT;
using Jitex.Utils;

namespace Jitex.Runtime
{
    internal static class RuntimeMethodCache
    {
        private static readonly ConcurrentDictionary<IntPtr, MethodCompiled> CompiledMethods = new ConcurrentDictionary<IntPtr, MethodCompiled>();

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            CompiledMethods.TryAdd(methodCompiled.Handle, methodCompiled);
        }

        public static async Task<NativeCode> GetNativeCodeAsync(MethodBase method)
        {
            if (MethodHelper.HasCanon(method))
                method = MethodHelper.GetBaseMethodGeneric(method);

            MethodCompiled? methodCompiled = GetMethodCompiledInfo(method);

            if (methodCompiled == null)
            {
                await RuntimeHelperExtension.InternalPrepareMethodAsync(method);

                do
                {
                    Thread.Sleep(100);
                    methodCompiled = GetMethodCompiledInfo(method);
                } while (methodCompiled == null);
            }

            return new NativeCode(methodCompiled.NativeCodeAddress, methodCompiled.NativeCodeSize);
        }

        public static MethodCompiled? GetMethodCompiledInfo(MethodBase method)
        {
            IntPtr methodHandle = MethodHelper.GetMethodHandle(method).Value;

            if (CompiledMethods.TryGetValue(methodHandle, out MethodCompiled methodCompiled))
                return methodCompiled;

            return null;
        }
    }
}
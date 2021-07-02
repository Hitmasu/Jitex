using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jitex.JIT;
using Jitex.Utils;
using Jitex.Utils.Comparer;

namespace Jitex.Runtime
{
    internal static class RuntimeMethodCache
    {
        private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new();

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            CompiledMethods.Add(methodCompiled);
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
                    await Task.Delay(100);
                    methodCompiled = GetMethodCompiledInfo(method);
                } while (methodCompiled == null);
            }

            return new NativeCode(methodCompiled.NativeCodeAddress, methodCompiled.NativeCodeSize);
        }

        public static MethodCompiled? GetMethodCompiledInfo(MethodBase method)
        {
            return CompiledMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, method));
        }
    }
}
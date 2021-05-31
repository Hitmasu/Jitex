using System;
using System.Collections.Concurrent;
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
        private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new ConcurrentBag<MethodCompiled>();

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            CompiledMethods.Add(methodCompiled);
        }

        public static NativeCode GetNativeCode(MethodBase method)
        {
            MethodCompiled? methodCompiled = CompiledMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, method));

            if (methodCompiled == null)
            {
                if (!JitexManager.IsLoaded)
                    throw new Exception("Jitex is not installed!");

                RuntimeHelperExtension.InternalPrepareMethodAsync(method).Wait();

                while (true)
                {
                    methodCompiled = CompiledMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, method));

                    if (methodCompiled != null)
                        break;

                    Thread.Sleep(50);
                }
            }

            return new NativeCode(methodCompiled.NativeCodeAddress, methodCompiled.NativeCodeSize);
        }
    }
}
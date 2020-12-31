using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jitex.JIT;
using Jitex.Utils;

namespace Jitex.Runtime
{
    public static class RuntimeMethodCache
    {
        private static readonly Type CanonType;
        private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new ConcurrentBag<MethodCompiled>();
        private static readonly ConcurrentDictionary<IntPtr, MethodBase> HandleCache = new ConcurrentDictionary<IntPtr, MethodBase>();

        static RuntimeMethodCache()
        {
            CanonType = Type.GetType("System.__Canon");
        }

        public static void AddMethod(MethodCompiled methodCompiled)
        {
            CompiledMethods.Add(methodCompiled);
            HandleCache.TryAdd(methodCompiled.Handle, methodCompiled.Method);
        }

        public static IntPtr GetNativeAddress(MethodBase method)
        {
            if (method.IsGenericMethod && method is MethodInfo methodInfo)
            {
                Type[]? genericArguments = methodInfo.GetGenericArguments();

                bool hasCanon = false;

                for (int i = 0; i < genericArguments.Length; i++)
                {
                    Type genericArgument = genericArguments[i];

                    if (genericArgument.IsClass)
                    {
                        genericArguments[i] = CanonType;
                        hasCanon = true;
                    }
                }

                if (hasCanon)
                {
                    method = methodInfo.GetGenericMethodDefinition().MakeGenericMethod(genericArguments);
                }
            }

            MethodCompiled methodCompiled = CompiledMethods.FirstOrDefault(w => w.Method == method);

            if (methodCompiled == null)
            {
                RuntimeHelperExtension.InternalPrepareMethodAsync(method).Wait();

                while (true)
                {
                    methodCompiled = CompiledMethods.FirstOrDefault(w => w.Method == method);

                    if (methodCompiled != null)
                        break;

                    Thread.Sleep(50);
                }
            }

            return methodCompiled.NativeCodeAddress;
        }

        public static MethodBase? GetMethodFromHandle(IntPtr handle)
        {
            if (HandleCache.TryGetValue(handle, out MethodBase? method))
                return method;

            method = MethodHelper.GetMethodFromHandle(handle);

            if (method != null)
                HandleCache.TryAdd(handle, method);

            return method;
        }
    }
}

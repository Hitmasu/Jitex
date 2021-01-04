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
    public static class RuntimeMethodCache
    {

        private static readonly ConcurrentBag<MethodCompiled> CompiledMethods = new ConcurrentBag<MethodCompiled>();
        private static readonly ConcurrentDictionary<IntPtr, MethodBase> HandleCache = new ConcurrentDictionary<IntPtr, MethodBase>();
        private static volatile bool _isLinqCompiled;

        internal static void AddMethod(MethodCompiled methodCompiled)
        {
            CompiledMethods.Add(methodCompiled);
        }

        public static IntPtr GetNativeAddress(MethodBase method)
        {
            if (!_isLinqCompiled)
                PrepareLinq();

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

            return methodCompiled.NativeCodeAddress;
        }

        private static void PrepareLinq()
        {
            if (_isLinqCompiled)
                return;

            try
            {
                _ = CompiledMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, w.Method));

            }
            catch
            {
                ConcurrentBag<MethodCompiled> stubList = new ConcurrentBag<MethodCompiled>();
                stubList.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, w.Method));
                _isLinqCompiled = true;
            }
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
using System;
using System.Collections.Generic;
using System.Reflection;
using Jitex.Exceptions;
using Jitex.JIT.Context;
using Jitex.Utils;
using Jitex.Utils.Comparer;

namespace Jitex.Intercept
{
    internal class CallManager : IDisposable
    {
        private static readonly IDictionary<IntPtr, CallCache> Cache = new Dictionary<IntPtr, CallCache>(IntPtrEqualityComparer.Instance);
        private static readonly InterceptManager InterceptManager = InterceptManager.GetInstance();

        private readonly CallContext _context;

        public CallManager(IntPtr handle, in object[] parameters, bool isGeneric)
        {
            if (isGeneric)
                handle = (IntPtr)parameters[0];

            if (!Cache.TryGetValue(handle, out CallCache cache))
            {
                MethodBase? method = MethodHelper.GetMethodFromHandle(handle);
                if (method == null) throw new MethodNotFound(handle);

                InterceptContext? interceptContext = InterceptManager.GetInterceptContext(method);
                if (interceptContext == null) throw new InterceptNotFound(method);

                Delegate del = DelegateHelper.CreateDelegate(interceptContext.PrimaryNativeAddress, method);

                cache = new CallCache(handle, method, del);
                Cache.Add(handle, cache);
            }

            _context = new CallContext(cache.Method, cache.Delegate, parameters);
        }

        public IntPtr InterceptCall()
        {
            foreach (InterceptHandler.InterceptorHandler interceptor in InterceptManager.GetInterceptors())
                interceptor(_context);

            if (_context.ProceedCall)
                _context.ContinueFlow();

            return _context.HasReturn ? _context.ReturnAddress : IntPtr.Zero;
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        private class CallCache
        {
            public IntPtr MethodHandle { get; }
            public MethodBase Method { get; }
            public Delegate Delegate { get; }

            public CallCache(IntPtr methodHandle, MethodBase method, Delegate @delegate)
            {
                MethodHandle = methodHandle;
                Method = method;
                Delegate = @delegate;
            }
        }
    }
}

using Jitex.Exceptions;
using Jitex.JIT.Context;
using Jitex.Utils;
using Jitex.Utils.Comparer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Jitex.Intercept
{
    public class CallManager : IDisposable
    {
        private static readonly ConcurrentDictionary<IntPtr, CallCache> Cache = new ConcurrentDictionary<IntPtr, CallCache>();
        private static readonly InterceptManager InterceptManager = InterceptManager.GetInstance();

        private readonly CallContext _context;

        public CallManager(IntPtr handle, in object[] parameters, bool hasCanon, bool isGenericMethod, bool isStatic)
        {
            if (!Cache.TryGetValue(handle, out CallCache cache))
            {
                MethodBase? method;

                if (hasCanon)
                {
                    IntPtr tempHandle;

                    if (isStatic)
                        tempHandle = (IntPtr)parameters[0];
                    else
                        tempHandle = (IntPtr)parameters[1];

                    if (isGenericMethod)
                    {
                        method = MethodHelper.GetMethodFromHandle(tempHandle);
                        handle = tempHandle;
                    }
                    else
                    {
                        method = MethodHelper.GetMethodFromHandle(handle, tempHandle);
                    }
                }
                else
                {
                    method = MethodHelper.GetMethodFromHandle(handle);
                }


                if (method == null) throw new MethodNotFound(handle);

                InterceptContext? interceptContext = InterceptManager.GetInterceptContext(method);
                if (interceptContext == null) throw new InterceptNotFound(method);

                Delegate del = DelegateHelper.CreateDelegate(interceptContext.MethodOriginalAddress, method);

                cache = new CallCache(handle, method, del);
                Cache.TryAdd(handle, cache);
            }

            _context = new CallContext(cache.Method, cache.Delegate, hasCanon, parameters);
        }

        public async Task<IntPtr> InterceptCallAsync()
        {
            foreach (InterceptHandler.InterceptorHandler interceptor in InterceptManager.GetInterceptorsAsync())
                await interceptor(_context).ConfigureAwait(false);

            if (_context.ProceedCall)
                await _context.ContinueFlowAsync().ConfigureAwait(false);

            return _context.HasReturn ? _context.ReturnAddress : IntPtr.Zero;
        }

        public async Task<TResult?> InterceptCallAsync<TResult>()
        {
            foreach (InterceptHandler.InterceptorHandler interceptor in InterceptManager.GetInterceptorsAsync())
                await interceptor(_context).ConfigureAwait(false);

            if (_context.ProceedCall)
                await _context.ContinueFlowAsync().ConfigureAwait(false);

            if (_context.HasReturn && _context.ReturnValue != null)
                return (TResult)_context.ReturnValue;

            return default;
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
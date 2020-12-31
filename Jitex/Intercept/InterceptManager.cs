using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jitex.JIT.Context;
using Jitex.Runtime;
using Jitex.Utils;

namespace Jitex.Intercept
{
    public class InterceptHandler
    {
        public delegate void InterceptorHandler(CallContext context);
    }

    public class InterceptManager
    {
        private static InterceptManager? _instance;

        private readonly IDictionary<MethodBase, InterceptContext> _interceptedMethods = new Dictionary<MethodBase, InterceptContext>();

        private InterceptHandler.InterceptorHandler? _interceptors;

        internal void AddIntercept(InterceptContext detourContext)
        {
            _interceptedMethods.Add(detourContext.Method, detourContext);
            detourContext.WriteDetour();
        }

        internal void RemoveIntercept(MethodBase method)
        {
            if (_interceptedMethods.TryGetValue(method, out InterceptContext detourContext))
            {
                detourContext.RemoveDetour();
                _interceptedMethods.Remove(method);
            }
        }

        internal InterceptContext? GetInterceptContext(MethodBase method)
        {
            if (_interceptedMethods.TryGetValue(method, out InterceptContext interceptContext))
                return interceptContext;

            return null;
        }

        internal void AddInterceptor(InterceptHandler.InterceptorHandler inteceptor) => _interceptors += inteceptor;

        internal void RemoveInterceptor(InterceptHandler.InterceptorHandler inteceptor) => _interceptors -= inteceptor;

        internal bool HasInteceptor(InterceptHandler.InterceptorHandler inteceptor) => _interceptors != null && _interceptors.GetInvocationList().Any(del => del.Method == inteceptor.Method);

        private InterceptManager()
        {
        }

        public object InterceptCall(long handle, object[] args)
        {
            MethodBase method = RuntimeMethodCache.GetMethodFromHandle(new IntPtr(handle));
            InterceptContext interceptContext = GetInterceptContext(method);
            Delegate del = DelegateHelper.CreateDelegate(interceptContext.SecondaryNativeAddress, method);
            CallContext context = new CallContext(method, del, args);

            if (_interceptors != null)
            {
                Delegate[] interceptors = _interceptors.GetInvocationList();

                foreach (InterceptHandler.InterceptorHandler interceptor in interceptors)
                {
                    interceptor(context);
                }
            }

            if (!context.IsReturnSetted)
                context.Continue();

            return context.ReturnValue;
        }

        public static InterceptManager GetInstance()
        {
            return _instance ??= new InterceptManager();
        }
    }
}
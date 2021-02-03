using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Jitex.Exceptions;
using Jitex.JIT.Context;
using Jitex.Utils.Comparer;

namespace Jitex.Intercept
{
    /// <summary>
    /// Handlers to InterceptManager
    /// </summary>
    public class InterceptHandler
    {
        /// <summary>
        /// Handler from Interceptor method.
        /// </summary>
        /// <param name="context">Context of call.</param>
        public delegate void InterceptorHandler(CallContext context);
    }

    internal class InterceptManager : IDisposable
    {

        private static InterceptManager? _instance;

        private readonly ConcurrentBag<InterceptContext> _interceptedMethods = new();

        private InterceptHandler.InterceptorHandler? _interceptors;

        private InterceptManager()
        {
        }

        public void AddIntercept(InterceptContext detourContext)
        {
            _interceptedMethods.Add(detourContext);
            EnableIntercept(detourContext.Method);
        }

        public void EnableIntercept(MethodBase method)
        {
            InterceptContext? interceptContext = GetInterceptContext(method);

            if (interceptContext == null) throw new InterceptNotFound(method);

            interceptContext.WriteDetour();
        }

        public void RemoveIntercept(MethodBase method)
        {
            InterceptContext? interceptContext = GetInterceptContext(method);

            if (interceptContext == null) throw new InterceptNotFound(method);

            interceptContext.RemoveDetour();
        }

        public InterceptContext? GetInterceptContext(MethodBase method)
        {
            return _interceptedMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.Method, method));
        }

        public void AddCallInterceptor(InterceptHandler.InterceptorHandler inteceptor) => _interceptors += inteceptor;

        public void RemoveCallInterceptor(InterceptHandler.InterceptorHandler inteceptor) => _interceptors -= inteceptor;

        public bool HasCallInteceptor(InterceptHandler.InterceptorHandler inteceptor) => _interceptors != null && _interceptors.GetInvocationList().Any(del => del.Method == inteceptor.Method);

        public InterceptHandler.InterceptorHandler[] GetInterceptors()
        {
            if (_interceptors == null)
                return new InterceptHandler.InterceptorHandler[0];

            return _interceptors.GetInvocationList().Cast<InterceptHandler.InterceptorHandler>().ToArray();
        }

        public static InterceptManager GetInstance()
        {
            return _instance ??= new InterceptManager();
        }

        public void Dispose()
        {
        }
    }
}
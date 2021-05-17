using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Jitex.Exceptions;
using Jitex.JIT.Context;
using Jitex.Utils.Comparer;

namespace Jitex.Intercept
{
    /// <summary>
    /// Handlers to InterceptManager
    /// </summary>
    public static class InterceptHandler
    {
        /// <summary>
        /// Handler to intercept async methods.
        /// </summary>
        /// <param name="context">Context of call.</param>
        public delegate ValueTask InterceptorHandler(CallContext context);
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
            EnableIntercept(detourContext.MethodIntercepted);
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
            return _interceptedMethods.FirstOrDefault(w => MethodEqualityComparer.Instance.Equals(w.MethodIntercepted, method));
        }

        public void AddInterceptorCall(InterceptHandler.InterceptorHandler inteceptor) => _interceptors += inteceptor;

        public void RemoveInterceptorCall(InterceptHandler.InterceptorHandler inteceptor) => _interceptors -= inteceptor;

        public bool HasInteceptorCall(InterceptHandler.InterceptorHandler inteceptor) => _interceptors != null && GetInterceptorsAsync().Any(del => del.Method == inteceptor.Method);

        public InterceptHandler.InterceptorHandler[] GetInterceptorsAsync()
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
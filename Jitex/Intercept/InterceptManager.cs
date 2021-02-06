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
    public class InterceptHandler
    {
        /// <summary>
        /// Handler to intercept methods.
        /// </summary>
        /// <param name="context">Context of call.</param>
        public delegate void InterceptorHandler(CallContext context);

        /// <summary>
        /// Handler to intercept async methods.
        /// </summary>
        /// <param name="context">Context of call.</param>
        public delegate ValueTask InterceptorAsyncHandler(CallContext context);
    }

    internal class InterceptManager : IDisposable
    {
        private static InterceptManager? _instance;

        private readonly ConcurrentBag<InterceptContext> _interceptedMethods = new();

        private InterceptHandler.InterceptorAsyncHandler? _interceptorsAsync;

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

        public void AddInterceptorCall(InterceptHandler.InterceptorAsyncHandler inteceptor) => _interceptorsAsync += inteceptor;

        public void RemoveInterceptorCall(InterceptHandler.InterceptorAsyncHandler inteceptor) => _interceptorsAsync -= inteceptor;

        public bool HasInteceptorCall(InterceptHandler.InterceptorAsyncHandler inteceptor) => _interceptorsAsync != null && GetInterceptorsAsync().Any(del => del.Method == inteceptor.Method);

        public InterceptHandler.InterceptorAsyncHandler[] GetInterceptorsAsync()
        {
            if (_interceptorsAsync == null)
                return new InterceptHandler.InterceptorAsyncHandler[0];

            return _interceptorsAsync.GetInvocationList().Cast<InterceptHandler.InterceptorAsyncHandler>().ToArray();
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
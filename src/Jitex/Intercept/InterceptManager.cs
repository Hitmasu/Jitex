using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public delegate Task InterceptorHandler(CallContext context);
    }

    internal class InterceptManager : IDisposable
    {
        private static InterceptManager? _instance;

        private InterceptHandler.InterceptorHandler? _interceptors;

        private InterceptManager()
        {
        }

        public void AddInterceptorCall(InterceptHandler.InterceptorHandler inteceptor) => _interceptors += inteceptor;

        public void RemoveInterceptorCall(InterceptHandler.InterceptorHandler inteceptor) => _interceptors -= inteceptor;

        public bool HasInteceptorCall(InterceptHandler.InterceptorHandler inteceptor) => _interceptors != null && GetInterceptors().Any(del => del.Method == inteceptor.Method);

        public IEnumerable<InterceptHandler.InterceptorHandler> GetInterceptors()
        {
            if (_interceptors == null)
                return Enumerable.Empty<InterceptHandler.InterceptorHandler>();

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
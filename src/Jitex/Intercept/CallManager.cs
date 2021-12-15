using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Jitex.Intercept
{
    public class CallManager
    {
        private readonly CallContext _context;
        private Task _currentInterceptor;

        public CallManager(CallContext context)
        {
            _context = context;
        }

        public void CallInterceptors()
        {
            CallInterceptorsAsync();
        }

        private async Task CallInterceptorsAsync()
        {
            IEnumerable<InterceptHandler.InterceptorHandler> interceptors = InterceptManager.GetInstance().GetInterceptors();

            foreach (InterceptHandler.InterceptorHandler interceptor in interceptors)
            {
                _currentInterceptor = interceptor(_context);
                await _currentInterceptor;
            }
        }

        public void ReleaseTask()
        {
            if (!_context.IsWaitingForEnd)
                return;
            
            _context.ReleaseSignal();
            _currentInterceptor.GetAwaiter().GetResult();
        }
    }
}
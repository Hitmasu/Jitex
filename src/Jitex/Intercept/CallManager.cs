using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Jitex.Utils;

namespace Jitex.Intercept
{
    public class CallManager
    {
        private readonly CallContext _context;
        private Task? _currentInterceptor;

        public CallManager(CallContext context)
        {
            _context = context;
        }

        public void CallInterceptors()
        {
            CallInterceptorsAsync();
            _context.WaitToContinue();
        }

        private async Task CallInterceptorsAsync()
        {
            IEnumerable<InterceptHandler.InterceptorHandler> interceptors = InterceptManager.GetInstance().GetInterceptors();

            foreach (InterceptHandler.InterceptorHandler interceptor in interceptors)
            {
                _currentInterceptor = interceptor(_context);
                await _currentInterceptor;
            }

            _context.ContinueWithCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetReturnValue(Pointer pointer)
        {
            _context.SetReturnValue(pointer);
        }


        /// <summary>
        /// Get return value from context ignoring ref.
        /// </summary>
        /// <remarks>
        /// That's a trick to avoid logic implementation for ldind.* on InterceptorBuilder when return type if a ref.
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T? GetReturnValueNoRef<T>()
        {
            ref T? value = ref _context.GetReturnValue<T>();
            return Unsafe.IsNullRef(ref value) ? default : value;
        }

        public void ReleaseTask()
        {
            if (!_context.IsWaitingForEnd)
                return;

            _context.ReleaseSignal();
            _currentInterceptor?.GetAwaiter().GetResult();
        }
    }
}
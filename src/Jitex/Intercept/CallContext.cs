using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Pointer = Jitex.Utils.Pointer;

namespace Jitex.Intercept
{
    public class CallContext
    {
        private AutoResetEvent? _autoResetEvent;
        private SemaphoreSlim? _semaphoreSlim;

        public MethodBase Method { get; }
        public object[] Parameters { get; set; }
        public object? Result { get; set; }
        internal bool IsWaitingForEnd { get; private set; }

        public CallContext(params object[] parameters)
        {
            Parameters = parameters;
            Method = null;
        }

        public Task ContinueAsync()
        {
            _semaphoreSlim = new SemaphoreSlim(0);
            IsWaitingForEnd = true;

            ContinueWithCode();
            return _semaphoreSlim.WaitAsync();
        }

        public async Task<T?> ContinueAsync<T>()
        {
            await ContinueAsync();

            if (Result == null)
                return default;

            return (T)Result;
        }

        public object? GetParameter(int index)
        {
            if (index >= Parameters.Length || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            return Parameters[index];
        }

        public T? GetParameter<T>(int index)
        {
            object? parameterValue = GetParameter(index);

            if (parameterValue == null)
                return default;

            return (T)Parameters[index];
        }

        public ref T? GetParameterAsRef<T>(int index)
        {
            object? parameterValue = GetParameter(index);

            if (parameterValue == null)
                return ref Unsafe.NullRef<T?>();

            return ref Pointer.UnBox<T>(parameterValue)!;
        }

        public unsafe void* GetParameterAsPointer(int index)
        {
            object? parameterValue = GetParameter(index);

            if (parameterValue == null)
                return default;

            return Pointer.GetPointer(parameterValue);
        }

        internal void WaitToContinue()
        {
            if (_autoResetEvent == null)
                _autoResetEvent = new AutoResetEvent(false);

            _autoResetEvent.WaitOne();
        }

        internal void ContinueWithCode()
        {
            if (_autoResetEvent == null)
                _autoResetEvent = new AutoResetEvent(true);

            _autoResetEvent.Set();
        }

        internal void ReleaseSignal()
        {
            if (_semaphoreSlim == null)
                throw new InvalidOperationException("Interceptor is not waiting.");

            _semaphoreSlim.Release();
        }
    }
}
using System.Threading;
using System.Threading.Tasks;

namespace Jitex.Intercept
{
    public class CallContext
    {
        private AutoResetEvent _autoResetEvent;
        private SemaphoreSlim _semaphoreSlim;
        public object[] Parameters { get; set; }
        public object? Result { get; set; }
        internal bool IsWaitingForEnd { get; private set; }

        public CallContext(params object[] parameters)
        {
            Parameters = parameters;
        }

        public void WaitToContinue()
        {
            _autoResetEvent = new AutoResetEvent(false);
            _autoResetEvent.WaitOne();
        }

        public void ContinueWithCode()
        {
            _autoResetEvent.Set();
        }

        public Task ContinueAsync()
        {
            ContinueWithCode();
            _semaphoreSlim = new SemaphoreSlim(0);
            IsWaitingForEnd = true;
            return _semaphoreSlim.WaitAsync();
        }

        public async Task<T?> ContinueAsync<T>()
        {
            await ContinueAsync();

            if (Result == null)
                return default;

            return (T)Result;
        }

        internal void ReleaseSignal()
        {
            _semaphoreSlim.Release();
        }
    }
}
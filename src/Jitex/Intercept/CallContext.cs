using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jitex.Utils;
using Pointer = Jitex.Utils.Pointer;

namespace Jitex.Intercept
{
    public class CallContext
    {
        private object? _returnValue;
        private readonly Parameter[] _parameters;
        private AutoResetEvent? _autoResetEvent;
        private SemaphoreSlim? _semaphoreSlim;
        public MethodBase Method { get; }


        public object? ReturnValue
        {
            get => _returnValue;
            set
            {
                _returnValue = value;
                ProceedCall = false;
            }
        }

        public object Instance { get; }
        public bool ProceedCall { get; set; } = true;
        internal bool IsWaitingForEnd { get; private set; }

        public CallContext(long methodHandle, object instance, params object[] parameters)
        {
            _parameters = new Parameter[parameters.Length];
            Instance = instance;

            //TODO: Move to out from constructor
            Method = MethodHelper.GetMethodFromHandle(new IntPtr(methodHandle))!;

            Type[] types = Method.GetParameters().Select(w => w.ParameterType).ToArray();

            for (int i = 0; i < parameters.Length; i++)
                _parameters[i] = new Parameter(parameters[i], types[i]);
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

            if (ReturnValue == null)
                return default;

            return (T) ReturnValue;
        }

        public object? GetParameterValue(int index)
        {
            ValidateParameterIndex(index);

            Parameter parameter = _parameters[index];
            return parameter.GetValue();
        }

        public ref T? GetParameterValue<T>(int index)
        {
            ValidateParameterIndex(index);

            Parameter parameter = _parameters[index];
            return ref parameter.GetValueRef<T>();
        }

        public IntPtr? GetParameterAddress(int index)
        {
            ValidateParameterIndex(index);

            Parameter parameter = _parameters[index];
            return parameter.GetAddress();
        }

        public void SetParameterValue<T>(int index, ref T value)
        {
            ref T parameterValue = ref GetParameterValue<T>(index)!;
            parameterValue = value;
        }

        public void SetParameterValue<T>(int index, T value)
        {
            ref T parameterValue = ref GetParameterValue<T>(index)!;
            parameterValue = value;
        }

        public T GetReturnValue<T>()
        {
            if (ReturnValue == null)
                return default;

            return (T) ReturnValue;
        }

        public void SetReturnValue<T>(T value)
        {
            ReturnValue = value;
        }

        public T GetInstance<T>() => (T) Instance;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateParameterIndex(int index)
        {
            if (index > _parameters.Length - 1 || index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), "Index was out of range. Must be non-negative and less than the size of the collection.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateInstance()
        {
            if (Method.IsStatic)
                throw new InvalidOperationException("Method static don't have instance.");
        }
    }
}
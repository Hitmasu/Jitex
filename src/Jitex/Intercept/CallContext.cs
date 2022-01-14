using System;
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
        private readonly VariableInfo[] _parameters;
        private readonly Type? _returnType;
        private readonly VariableInfo? _instance;

        private VariableInfo? _returnValue;
        private AutoResetEvent? _autoResetEvent;
        private SemaphoreSlim? _semaphoreSlim;
        public MethodBase Method { get; }
        public bool ProceedCall { get; set; } = true;
        internal bool IsWaitingForEnd { get; private set; }

        public CallContext(long methodHandle, Pointer? instance, Pointer? returnValue, params Pointer[] parameters)
        {
            //TODO: Move to out from constructor
            Method = MethodHelper.GetMethodFromHandle(new IntPtr(methodHandle))!;

            _parameters = new VariableInfo[parameters.Length];

            if (instance != null)
                _instance = new VariableInfo(instance, Method.DeclaringType!);

            if (Method is MethodInfo methodInfo)
            {
                _returnType = methodInfo.ReturnType;
                _returnValue = new VariableInfo(returnValue!, _returnType);
            }

            Type[] types = Method.GetParameters().Select(w => w.ParameterType).ToArray();

            for (int i = 0; i < parameters.Length; i++)
                _parameters[i] = new VariableInfo(parameters[i], types[i]);
        }

        public async Task ContinueAsync()
        {
            _semaphoreSlim = new SemaphoreSlim(0);
            IsWaitingForEnd = true;

            ContinueWithCode();
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        }

        public async Task<T?> ContinueAsync<T>()
        {
            await ContinueAsync();

            if (_returnType == typeof(void))
                return default;

            T? returnValue = GetReturnValue<T>();

            if (returnValue == null)
                return default;

            return returnValue;
        }

        private VariableInfo GetParameter(int index)
        {
            ValidateParameterIndex(index);
            return _parameters[index];
        }

        public object? GetParameterValue(int index)
        {
            VariableInfo variableInfo = GetParameter(index);
            return variableInfo.GetValue();
        }

        public ref T? GetParameterValue<T>(int index)
        {
            VariableInfo variableInfo = GetParameter(index);

            ValidateType<T>(variableInfo.Type);

            return ref variableInfo.GetValueRef<T>();
        }

        public IntPtr GetParameterAddress(int index)
        {
            VariableInfo variableInfo = GetParameter(index);

            return variableInfo.GetAddress();
        }

        public void SetParameterValue<T>(int index, ref T value)
        {
            VariableInfo variableInfo = GetParameter(index);

            ValidateType<T>(variableInfo.Type);

            variableInfo.SetValue(ref value);
        }

        public void SetParameterValue<T>(int index, T value)
        {
            VariableInfo variableInfo = GetParameter(index);

            ValidateType<T>(variableInfo.Type);

            variableInfo.SetValue(value);
        }

        public object? GetReturnValue() => _returnValue?.GetValue();

        public unsafe void* GetReturnValuePointer() => _returnValue == null ? default : _returnValue.GetAddress().ToPointer();

        public ref T? GetReturnValue<T>()
        {
            if (_returnValue == null)
                return ref Unsafe.NullRef<T>()!;

            return ref _returnValue.GetValueRef<T>();
        }

        internal void SetReturnValue(Pointer value)
        {
            _returnValue = new VariableInfo(value, _returnType!);
        }

        public void SetReturnValue<T>(ref T value)
        {
            ValidateReturnType<T>();
            ValidateType<T>(_returnType!);

            _returnValue!.SetValue(ref value);
            ProceedCall = false;
        }

        public void SetReturnValue<T>(T value)
        {
            ValidateReturnType<T>();
            ValidateType<T>(_returnType!);

            _returnValue!.SetValue(value);
            ProceedCall = false;
        }

        public object GetInstance()
        {
            ValidateInstance();
            return _instance!.GetValue()!;
        }

        public ref T GetInstance<T>()
        {
            ValidateInstance();
            ValidateType<T>(Method.DeclaringType!);

            return ref _instance!.GetValueRef<T>()!;
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

        #region Validations

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateReturnType<T>()
        {
            if (_returnType == null)
                throw new InvalidOperationException($"Method {Method} doesn't have return.");

            if (_returnType == typeof(void))
                throw new InvalidOperationException($"Method {Method} is declared as void.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateType<T>(Type expectedType)
        {
            Type typeArgument = typeof(T);

            if (expectedType.IsByRef)
                expectedType = expectedType.GetElementType()!;

            if (typeArgument != expectedType)
                throw new ArgumentException($"Invalid type passed by argument. Expected: {expectedType!.FullName}, Passed: {typeArgument.FullName}");
        }

        #endregion
    }
}
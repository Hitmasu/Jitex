using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Jitex.Utils;
using Jitex.Utils.Extension;
using Pointer = Jitex.Utils.Pointer;

namespace Jitex.Intercept
{
    /// <summary>
    /// Context from call
    /// </summary>
    public class CallContext
    {
        private readonly VariableInfo? _instance;
        private readonly VariableInfo[] _parameters;
        private readonly Type _returnType;
        private readonly VariableInfo? _returnValue;

        private bool _alreadyCalled = false;

        private AutoResetEvent? _autoResetEvent;
        private SemaphoreSlim? _semaphoreSlim;

        /// <summary>
        /// Method from call
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// If should continue with original call. 
        /// </summary>
        public bool ProceedCall { get; set; } = true;

        /// <summary>
        /// If method has return value
        /// </summary>
        public bool HasReturn { get; }

        /// <summary>
        /// Number of parameters
        /// </summary>
        public int ParametersCount => _parameters.Length;

        /// <summary>
        /// If context is waiting for end of call
        /// </summary>
        /// <remarks>
        /// Is used to hold original call after call ContinueAsync. 
        /// </remarks>
        internal bool IsWaitingForEnd { get; private set; }

        /// <summary>
        /// Create a new context from call (Should not be called directly). 
        /// </summary>
        /// <param name="methodHandle">Handle from method called.</param>
        /// <param name="methodGenericArguments"></param>
        /// <param name="instance">Instance passed on call.</param>
        /// <param name="returnValue">Pointer to variable of return method.</param>
        /// <param name="parameters">Pointer for each parameter from call.</param>
        /// <param name="typeGenericArguments"></param>
        public CallContext(long methodHandle, Type[]? typeGenericArguments, Type[]? methodGenericArguments, Pointer? instance, Pointer? returnValue, params Pointer[] parameters)
        {
            Type[] types;

            //TODO: Move to out from constructor
            Method = MethodHelper.GetMethodFromHandle(new IntPtr(methodHandle))!;

            if (Method is MethodInfo methodInfo)
            {
                _returnType = methodInfo.ReturnType;
                _returnValue = new VariableInfo(returnValue!, _returnType);

                Method = MethodHelper.TryInitializeGenericMethod(Method, typeGenericArguments, methodGenericArguments);
                HasReturn = _returnType != typeof(void);
                types = Method.GetParameters().Select(w => w.ParameterType).ToArray();
            }
            else if (!Method.IsStatic) //Non-static constructor
            {
                _returnType = Method.DeclaringType;

                if (instance != null)
                    _returnValue = new VariableInfo(instance, Method.DeclaringType!);

                HasReturn = true;
                types = Method.GetParameters().Select(w => w.ParameterType).ToArray();
            }
            else
            {
                types = Array.Empty<Type>();
            }

            if (instance != null)
                _instance = new VariableInfo(instance, Method.DeclaringType!);

            _parameters = new VariableInfo[parameters.Length];


            for (int i = 0; i < parameters.Length; i++)
                _parameters[i] = new VariableInfo(parameters[i], types[i]);
        }

        /// <summary>
        /// Continue with original call.
        /// </summary>
        public async Task ContinueAsync()
        {
            if (_alreadyCalled)
                return;

            _semaphoreSlim = new SemaphoreSlim(0);
            IsWaitingForEnd = true;

            ContinueWithCode();
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
            _alreadyCalled = true;
        }

        /// <summary>
        /// Continue with original call and get return value of type <typeparamref name="T"/> (if has return).
        /// </summary>
        /// <param name="innerResult">If should consume task when return is a Task or ValueTask.</param>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with return type.</param>
        /// <typeparam name="T">Type from return value.</typeparam>
        /// <returns>Return value from call.</returns>
        public async Task<T?> ContinueAsync<T>(bool innerResult = true, bool validateType = true)
        {
            await ContinueAsync().ConfigureAwait(false);

            if (!HasReturn)
                return default;

            T? returnValue = await GetReturnValueAsync<T>(innerResult, validateType).ConfigureAwait(false);

            return returnValue ?? default;
        }

        /// <summary>
        /// Get instance used on call.
        /// </summary>
        /// <returns>Instance used on call.</returns>
        public object GetInstance()
        {
            ValidateInstance();
            return _instance!.GetValue()!;
        }

        /// <summary>
        /// Get instance of type <typeparamref name="T"/> used on call.
        /// </summary>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with return type.</param>
        /// <typeparam name="T">Type from instance.</typeparam>
        /// <returns>instance used on call.</returns>
        public ref T GetInstance<T>(bool validateType = true)
        {
            ValidateInstance();

            if (validateType)
                ValidateType<T>(Method.DeclaringType!);

            return ref _instance!.GetValueRef<T>()!;
        }

        /// <summary>
        /// Get address from parameter.
        /// </summary>
        /// <param name="index">Position from parameter.</param>
        /// <returns>Address from parameter.</returns>
        public IntPtr GetParameterAddress(int index)
        {
            VariableInfo variableInfo = GetParameter(index);

            return variableInfo.GetAddress();
        }

        /// <summary>
        /// Get value from parameter.
        /// </summary>
        /// <param name="index">Position from parameter.</param>
        /// <returns>Value from parameter.</returns>
        public object? GetParameterValue(int index)
        {
            VariableInfo variableInfo = GetParameter(index);
            return variableInfo.GetValue();
        }

        /// <summary>
        /// Get reference to a parameter value of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="index">Position from parameter.</param>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with parameter type.</param>
        /// <typeparam name="T">Type from parameter.</typeparam>
        /// <returns>A reference to a value of type <typeparamref name="T"/></returns>
        public ref T? GetParameterValue<T>(int index, bool validateType = true)
        {
            VariableInfo variableInfo = GetParameter(index);

            if (validateType)
                ValidateType<T>(variableInfo.Type);

            return ref variableInfo.GetValueRef<T>();
        }

        /// <summary>
        /// Get return value from call.
        /// </summary>
        /// <returns>Return value.</returns>
        public object? GetReturnValue() => _returnValue?.GetValue();


        /// <summary>
        /// Get return value as <typeparamref name="T"/>.
        /// </summary>
        /// <param name="innerResult">If should consume task when return is a Task or ValueTask.</param>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with return type.</param>
        /// <typeparam name="T">Type from return.</typeparam>
        /// <returns>Return value from method as <typeparamref name="T"/></returns>
        public async Task<T?> GetReturnValueAsync<T>(bool innerResult = true, bool validateType = true)
        {
            if (_returnValue == null)
                return default;

            if (innerResult && _returnType.IsAwaitable())
            {
                if (_returnType == typeof(Task) || _returnType == typeof(ValueTask))
                    return GetReturnValue<T>(validateType);

                dynamic task;

                if (_returnType.GetGenericTypeDefinition() == typeof(Task<>))
                    task = GetReturnValue<Task<T>>(validateType)!;
                else
                    task = GetReturnValue<ValueTask<T>>(validateType)!;

                return await task;
            }

            return GetReturnValue<T>(validateType);
        }

        /// <summary>
        /// Get reference to return value of type <typeparamref name="T"/> from method.
        /// </summary>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with return type.</param>
        /// <typeparam name="T">Type from return.</typeparam>
        /// <returns>A reference to a value of type <typeparamref name="T"/></returns>
        public ref T? GetReturnValue<T>(bool validateType = true)
        {
            if (validateType)
                ValidateType<T>(_returnType!);

            if (_returnValue == null)
                return ref Unsafe.NullRef<T>()!;

            return ref _returnValue.GetValueRef<T>();
        }


        /// <summary>
        /// Get address from variable of return value.
        /// </summary>
        /// <returns></returns>
        public IntPtr GetReturnValueAddress() => _returnValue?.GetAddress() ?? default;

        /// <summary>
        /// Set a reference to a value of type <typeparamref name="T"/> in a parameter.
        /// </summary>
        /// <param name="index">Position from parameter.</param>
        /// <param name="value">A reference of type <typeparamref name="T"/> to set.</param>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with parameter type.</param>
        /// <typeparam name="T">Type from parameter.</typeparam>
        public void SetParameterValue<T>(int index, ref T value, bool validateType = true)
        {
            VariableInfo variableInfo = GetParameter(index);

            if (validateType && value != null)
                ValidateType(value.GetType(), variableInfo.Type);

            variableInfo.SetValue(ref value);
        }

        /// <summary>
        /// Set a value of type <typeparamref name="T"/> in a parameter.
        /// </summary>
        /// <param name="index">Position from parameter.</param>
        /// <param name="value">A reference of type <typeparamref name="T"/> to set.</param>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with parameter type.</param>
        /// <typeparam name="T">Type from parameter.</typeparam>
        public void SetParameterValue<T>(int index, T value, bool validateType = true)
        {
            VariableInfo variableInfo = GetParameter(index);

            if (validateType && value != null)
                ValidateType(value.GetType(), variableInfo.Type);

            variableInfo.SetValue(value);
        }

        /// <summary>
        /// Set a reference to a value of type <typeparamref name="T"/> on return from call.
        /// </summary>
        /// <param name="value">A reference of type <typeparamref name="T"/> to set.</param>
        /// <param name="validateType"></param>
        /// <typeparam name="T">Type from return.</typeparam>
        public void SetReturnValue<T>(ref T value, bool validateType = true)
        {
            ValidateReturnValue();
            
            if (validateType)
            {
                ValidateReturnType<T>();

                if (value != null)
                    ValidateType(value.GetType(), _returnType);
            }

            _returnValue!.SetValue(ref value);
            ProceedCall = false;
        }

        /// <summary>
        /// Set value of type <typeparamref name="T"/> on return from call.
        /// </summary>
        /// <param name="value">A value of type <typeparamref name="T"/> to set.</param>
        /// <param name="validateType">If should validate type <typeparamref name="T"/> with return type.</param>
        /// <typeparam name="T">Type from return.</typeparam>
        public void SetReturnValue<T>(T value, bool validateType = true)
        {
            ValidateReturnValue();

            if (validateType)
            {
                ValidateReturnType<T>();

                if (value != null)
                    ValidateType(value.GetType(), _returnType);
            }

            _returnValue!.SetValue(value);
            ProceedCall = false;
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

        internal void WaitToContinue()
        {
            if (_autoResetEvent == null)
                _autoResetEvent = new AutoResetEvent(false);

            _autoResetEvent.WaitOne();
        }

        private VariableInfo GetParameter(int index)
        {
            ValidateParameterIndex(index);
            return _parameters[index];
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
        private void ValidateReturnValue()
        {
            if (Method.IsConstructor)
                throw new InvalidOperationException("Can't set return value on constructors.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateReturnType<T>()
        {
            if (!HasReturn)
                throw new InvalidOperationException($"Method {Method} doesn't have return.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ValidateType<T>(Type expectedType)
        {
            Type typeArgument = typeof(T);

            ValidateType(typeArgument, expectedType);
        }

        private void ValidateType(Type type, Type expectedType)
        {
            Type typeArgument = type;

            if (expectedType.IsByRef)
                expectedType = expectedType.GetElementType()!;

            if (expectedType == TypeHelper.CanonType && typeArgument.IsCanon())
                return;

            if (typeArgument != expectedType)
                throw new ArgumentException($"Invalid type passed by argument. Expected: {expectedType!.FullName}, Passed: {typeArgument.FullName}");
        }

        #endregion
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Jitex.Utils;

namespace Jitex.Intercept
{
    /// <summary>
    /// Context of call.
    /// </summary>
    /// <remarks>
    /// That should contains all information necessary to continue (or not) with call.
    /// </remarks>
    public class CallContext
    {
        private Type _returnType;
        private Parameter? _returnValue;
        private object _instanceValue;

        /// <summary>
        /// Generic method.
        /// </summary>
        private readonly IntPtr _methodHandle;

        /// <summary>
        /// Method source call.
        /// </summary>
        public MethodBase Method { get; }

        /// <summary>
        /// Delegate to original call.
        /// </summary>
        /// <remarks>
        /// This delegate is a pointer to our unmodified native code of original method.
        /// Original native code has be detoured and cant run "normal" again, so we should continue call in another address.
        /// </remarks>
        private Delegate Call { get; }

        /// <summary>
        /// Instance passed in call (non-static methods).
        /// </summary>
        public object? Instance
        {
            get => _instanceValue;
            set
            {
                if (value == null) throw new ArgumentNullException("Instance can not be null!");
                _instanceValue = value;
            }
        }

        public bool HasParameters => Parameters != null && Parameters.Any();

        /// <summary>
        /// Parameters passed in call.
        /// </summary>
        public Parameters? Parameters { get; }

        /// <summary>
        /// If Return value has been setted.
        /// </summary>
        public bool ContinueCall { get; set; } = true;

        /// <summary>
        /// Return value of call.
        /// </summary>
        public object? ReturnValue
        {
            get => _returnValue?.Value;

            set
            {
                if (value != null)
                    _returnValue = new Parameter(ref value, ((MethodInfo)Method).ReturnType);

                ContinueCall = false;
            }
        }

        public IntPtr ReturnAddress
        {
            get
            {
                if (_returnValue == null)
                    return IntPtr.Zero; ;

                return _returnValue.Address;
            }
        }

        /// <summary>
        /// Raw parameters from call.
        /// </summary>
        /// <remarks>
        /// That include all arguments passed: Instance, Parameters and Generic Arguments.
        /// </remarks>
        public IEnumerable<Parameter> RawParameters
        {
            get
            {
                List<Parameter> rawParameters = new List<Parameter>();

                if (Method.IsGenericMethod)
                    rawParameters.Add(new Parameter(_methodHandle, typeof(IntPtr), false));

                if (!Method.IsStatic)
                    rawParameters.Add(new Parameter(ref _instanceValue, Method.DeclaringType));

                if (Parameters != null && Parameters.Any())
                    rawParameters.AddRange(Parameters);

                return rawParameters;
            }
        }

        private object[] ParametersCall => RawParameters.Select(w => w.RealValue).ToArray();

        internal CallContext(MethodBase method, Delegate call, object[] parameters)
        {
            Method = method;
            Call = call;

            int startIndex = 0;

            if (Method.IsGenericMethod)
                _methodHandle = (IntPtr)parameters[startIndex++];

            if (!Method.IsConstructor && !Method.IsStatic)
            {
                IntPtr instanceAddress = (IntPtr)parameters[startIndex++];
                _instanceValue = TypeUtils.GetObjectFromReference(instanceAddress);
            }

            Parameter[] parametersInfo = new Parameter[parameters.Length - startIndex];

            Type[] parametersMethod = Method.GetParameters().Select(w => w.ParameterType).ToArray();

            for (int i = startIndex; i < parameters.Length; i++)
            {
                object parameter = parameters[i];
                Type parameterType = parametersMethod[i - startIndex];

                parametersInfo[i - startIndex] = new Parameter((IntPtr)parameter, parameterType);
            }

            Parameters = new Parameters(parametersInfo);

            if (method is MethodInfo methodInfo)
                _returnType = methodInfo.ReturnType;
        }

        /// <summary>
        /// Continue original call.
        /// </summary>
        internal void ContinueFlow()
        {
            object returnValue = Call.DynamicInvoke(ParametersCall);

            if (returnValue is IntPtr returnAddress)
            {
                _returnValue = new Parameter(returnAddress, _returnType);
                _returnValue.IsOriginalReturn = true;
            }
            else
            {
                ReturnValue = returnValue;
            }
        }

        public object? Continue()
        {
            object returnValue = Call.DynamicInvoke(ParametersCall);

            if (Method is MethodInfo methodInfo)
            {
                Type returnType = methodInfo.ReturnType;

                if (returnType == typeof(void))
                    return default;

                if (returnType.IsValueType)
                {
                    return returnValue;
                }

                IntPtr ptrReturn = (IntPtr)returnValue; //Address of instance/Value is returned.
                IntPtr refReturn;

                if (Marshal.ReadIntPtr(ptrReturn) == methodInfo.ReturnType.TypeHandle.Value)
                {
                    unsafe
                    {
                        refReturn = (IntPtr)(&ptrReturn); //It's necessary create a reference to address of instance/value.
                    }
                }
                else
                {
                    refReturn = ptrReturn;
                }

                returnValue = TypeUtils.GetObjectFromReference(refReturn);

                return returnValue;
            }

            //It's a constructor
            return default;
        }

        /// <summary>
        /// Continue original call and retrieve result.
        /// </summary>
        /// <typeparam name="TResult">Type expected from result.</typeparam>
        /// <returns>Result from original call (Returns default on void or constructor).</returns>
        public TResult Continue<TResult>()
        {
            object? returnValue = Continue();

            if (returnValue == null)
                return default;

            return (TResult)returnValue;
        }

        /// <summary>
        /// Disable method to be intercepted again.
        /// </summary>
        public void DisableIntercept()
        {
            JitexManager.DisableIntercept(Method);
        }
    }
}
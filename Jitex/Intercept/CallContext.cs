using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
        private object? _returnValue;
        private object? _instance;

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
            get => _instance;
            set => _instance = value;
        }

        /// <summary>
        /// Parameters passed in call.
        /// </summary>
        public object[]? Parameters { get; set; }

        /// <summary>
        /// If Return value has been setted.
        /// </summary>
        public bool IsReturnSetted { get; private set; }

        /// <summary>
        /// Return value of call.
        /// </summary>
        public object? ReturnValue
        {
            get => _returnValue;

            set
            {
                _returnValue = value;
                IsReturnSetted = true;
            }
        }

        /// <summary>
        /// Raw parameters from call.
        /// </summary>
        /// <remarks>
        /// That include all arguments passed: Instance, Parameters and Generic Arguments.
        /// </remarks>
        public object[] RawParameters
        {
            get
            {
                List<object> rawParameters = new List<object>();

                if (Method.IsGenericMethod)
                    rawParameters.Add(Method.MethodHandle.Value);

                if (!Method.IsStatic)
                    rawParameters.Add(GetAddressFromObj(ref _instance!));

                if (Parameters != null)
                    rawParameters.AddRange(Parameters);

                return rawParameters.ToArray();
            }
        }

        internal CallContext(MethodBase method, Delegate call, object[] parameters)
        {
            Method = method;
            Call = call;

            if (!Method.IsConstructor && !Method.IsStatic)
            {
                Instance = parameters[0];

                Parameters = new object[parameters.Length - 1];

                for (int i = 1; i < parameters.Length; i++)
                {
                    Parameters[i - 1] = parameters[i];
                }
            }
            else
            {
                Parameters = parameters;
            }
        }

        /// <summary>
        /// Continue original call.
        /// </summary>
        internal void ContinueFlow()
        {
            ReturnValue = Continue<object>();
        }

        /// <summary>
        /// Continue original call and retrieve result.
        /// </summary>
        /// <typeparam name="TResult">Type expected from result.</typeparam>
        /// <returns>Result from original call.</returns>
        public TResult Continue<TResult>()
        {
            return (TResult) Call.DynamicInvoke(RawParameters);
        }

        /// <summary>
        /// Continue original call.
        /// </summary>
        public void Continue()
        {
            Call.DynamicInvoke(RawParameters);
        }

        /// <summary>
        /// Disable method to be intercepted again.
        /// </summary>
        public void DisableIntercept()
        {
            JitexManager.DisableIntercept(Method);
        }

        private static unsafe IntPtr GetAddressFromObj(ref object obj)
        {
            TypedReference typeRef = __makeref(obj);
            return **(IntPtr**) (&typeRef);
        }
    }
}
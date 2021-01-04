using System;
using System.Collections.Generic;
using System.Reflection;

namespace Jitex.Intercept
{
    public class CallContext
    {
        private object? _returnValue;

        public MethodBase Method { get; }
        private Delegate Call { get; }
        public object? Instance { get; set; }
        public object[]? Parameters { get; set; }

        public bool IsReturnSetted { get; private set; }

        public object? ReturnValue
        {
            get => _returnValue;

            set
            {
                _returnValue = value;
                IsReturnSetted = true;
            }
        }

        public object[] RawParameters
        {
            get
            {
                List<object> rawParameters = new List<object>();

                if(Method.IsGenericMethod)
                    rawParameters.Add(Method.MethodHandle.Value);

                if (!Method.IsStatic)
                    rawParameters.Add(Instance!);

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

        internal void ContinueFlow()
        {
            ReturnValue = Continue<object>();
        }

        public TResult Continue<TResult>()
        {
            return (TResult)Call.DynamicInvoke(RawParameters);
        }

        public void Continue()
        {
            Call.DynamicInvoke(RawParameters);
        }

        public void DisableIntercept()
        {
            JitexManager.DisableIntercept(Method);
        }
    }
}
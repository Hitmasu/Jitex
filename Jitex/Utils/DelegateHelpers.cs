using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using IntPtr = System.IntPtr;

namespace Jitex.Utils
{
    /// <summary>
    /// Helpers to manage delegate
    /// </summary>
    internal static class DelegateHelper
    {
        private static readonly MethodInfo MakeNewCustomDelegate;

        static DelegateHelper()
        {
            MakeNewCustomDelegate = typeof(Expression).Assembly.GetType("System.Linq.Expressions.Compiler.DelegateHelpers").GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static);
        }

        /// <summary>
        /// Create a Type Non Generic delegate.
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/a/26700515
        /// </remarks>
        /// <param name="method"></param>
        /// <returns></returns>
        private static Type CreateTypeDelegate(MethodBase method)
        {
            List<Type> parameters = new List<Type>();

            if (!method.IsStatic)
                parameters.Add(typeof(IntPtr));

            if (method.IsGenericMethod)
                parameters.Add(typeof(IntPtr));

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                if (parameter.ParameterType.IsPrimitive)
                    parameters.Add(parameter.ParameterType);
                else
                    parameters.Add(typeof(IntPtr));
            }

            if (method is MethodInfo methodInfo && methodInfo.ReturnType != typeof(void))
            {
                if (!methodInfo.ReturnType.IsPrimitive)
                    parameters.Add(typeof(IntPtr));
                else
                    parameters.Add(methodInfo.ReturnType);
            }

            return (Type)MakeNewCustomDelegate.Invoke(null, new object[] { parameters.ToArray() });
        }

        public static Delegate CreateDelegate(IntPtr address, MethodBase method)
        {
            Type delegateType = CreateTypeDelegate(method);
            return Marshal.GetDelegateForFunctionPointer(address, delegateType);
        }
    }
}
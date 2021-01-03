using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Jitex.Utils
{
    /// <summary>
    /// Helpers to manage delegate
    /// </summary>
    public static class DelegateHelper
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
                parameters.Add(method.DeclaringType);

            if (method.IsGenericMethod)
                parameters.Add(typeof(IntPtr));

            parameters.AddRange(method.GetParameters().Select(w => w.ParameterType));

            if (method is MethodInfo methodInfo && methodInfo.ReturnType != typeof(void))
                parameters.Add(methodInfo.ReturnType);

            return (Type)MakeNewCustomDelegate.Invoke(null, new object[] { parameters.ToArray() });
        }

        public static Delegate CreateDelegate(IntPtr address, MethodBase method)
        {
            Type delegateType = CreateTypeDelegate(method);
            return Marshal.GetDelegateForFunctionPointer(address, delegateType);
        }
    }
}

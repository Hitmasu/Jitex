using System;
using System.Linq;
using System.Reflection;

namespace Jitex.Utils.Extension
{
    internal static class MethodExtension
    {
        /// <summary>
        /// Return if method is a awaitable method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static bool IsAwaitable(this MethodBase method)
        {
            if (method.IsConstructor)
                return false;

            MethodInfo methodInfo = (MethodInfo) method;
            Type returnType = methodInfo.ReturnType;

            return returnType.IsAwaitable();
        }
    }
}
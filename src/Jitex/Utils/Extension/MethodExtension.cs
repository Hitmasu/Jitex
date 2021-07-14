using System;
using System.Reflection;
using System.Runtime.CompilerServices;

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

        /// <summary>
        /// Get RID from a method.
        /// </summary>
        /// <param name="method">Method to get RID.</param>
        /// <returns>RID from method.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRID(this MethodBase method) => MethodHelper.GetRID(method);
    }
}
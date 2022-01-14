using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jitex.Utils.Extension
{
    internal static class MethodExtension
    {
        /// <summary>
        /// Get RID from a method.
        /// </summary>
        /// <param name="method">Method to get RID.</param>
        /// <returns>RID from method.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRID(this MethodBase method) => MethodHelper.GetRID(method);
    }
}
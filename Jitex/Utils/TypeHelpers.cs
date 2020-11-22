using System;
using System.Reflection;

namespace Jitex.Utils
{
    internal static class TypeHelpers
    {
        /// <summary>
        /// Get type from handle pointer.
        /// </summary>
        /// <param name="handle">Handle fo type.</param>
        /// <returns>Type found;</returns>
        public static Type GetTypeFromHandle(IntPtr handle)
        {
            MethodInfo method = typeof(Type).GetMethod("GetTypeFromHandleUnsafe", BindingFlags.Static | BindingFlags.NonPublic)!;
            return (Type)method.Invoke(null, new object[] { handle });
        }
    }
}

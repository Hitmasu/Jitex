using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Jitex.Utils
{
    internal static class MethodHelper
    {
        private static readonly ConstructorInfo? CtorHandle;
        private static readonly MethodInfo? GetMethodBase;

        private static readonly IDictionary<IntPtr, MethodBase?> Cache = new Dictionary<IntPtr, MethodBase?>();

        static MethodHelper()
        {
            Type? runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal");

            if (runtimeMethodHandleInternalType == null)
                throw new TypeLoadException("Type System.RuntimeMethodHandleInternal was not found!");

            Type? runtimeType = Type.GetType("System.RuntimeType");

            if (runtimeType == null)
                throw new TypeLoadException("Type System.RuntimeType was not found!");

            CtorHandle = runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IntPtr) }, null);

            if (CtorHandle == null)
                throw new MethodAccessException("Constructor from RuntimeMethodHandleInternal was not found!");

            GetMethodBase = runtimeType
                .GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, new[] { runtimeType, runtimeMethodHandleInternalType }, null);

            if (GetMethodBase == null)
                throw new MethodAccessException("Method GetMethodBase from RuntimeType was not found!");
        }

        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            MethodBase? method = GetFromCache(methodHandle);

            if (method == null)
            {
                object? handle = GetMethodHandleFromPointer(methodHandle);
                method = GetMethodBase.Invoke(null, new[] { null, handle }) as MethodBase;
                Cache.Add(methodHandle, method);
            }

            return method;
        }

        public static MethodBase? GetFromCache(IntPtr methodHandle)
        {
            return Cache.TryGetValue(methodHandle, out MethodBase? method) ? method : null;
        }

        private static object? GetMethodHandleFromPointer(IntPtr methodHandle)
        {
            return CtorHandle!.Invoke(new object?[] { methodHandle });
        }
    }
}

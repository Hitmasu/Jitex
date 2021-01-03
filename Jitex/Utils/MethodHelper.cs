using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Jitex.Utils
{
    internal static class MethodHelper
    {
        private static readonly ConstructorInfo CtorHandle;
        private static readonly MethodInfo GetMethodBase;
        private static readonly Type CanonType;

        static MethodHelper()
        {
            CanonType = Type.GetType("System.__Canon");

            Type? runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal");

            if (runtimeMethodHandleInternalType == null)
                throw new TypeLoadException("Type System.RuntimeMethodHandleInternal was not found!");

            Type? runtimeType = Type.GetType("System.RuntimeType");

            if (runtimeType == null)
                throw new TypeLoadException("Type System.RuntimeType was not found!");

            CtorHandle = runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IntPtr) }, null)
                         ?? throw new MethodAccessException("Constructor from RuntimeMethodHandleInternal was not found!");

            GetMethodBase = runtimeType
                .GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, new[] { runtimeType, runtimeMethodHandleInternalType }, null)
                ?? throw new MethodAccessException("Method GetMethodBase from RuntimeType was not found!");
        }

        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            object? handle = GetMethodHandleFromPointer(methodHandle);
            MethodBase? method = GetMethodBase.Invoke(null, new[] { null, handle }) as MethodBase;

            return method;
        }

        private static object? GetMethodHandleFromPointer(IntPtr methodHandle)
        {
            return CtorHandle!.Invoke(new object?[] { methodHandle });
        }

        public static MethodInfo GetMethodGeneric(MethodInfo method)
        {
            Type[]? genericArguments = method.GetGenericArguments();

            bool hasCanon = false;

            for (int i = 0; i < genericArguments.Length; i++)
            {
                Type genericArgument = genericArguments[i];

                if (genericArgument.IsClass)
                {
                    genericArguments[i] = CanonType;
                    hasCanon = true;
                }
            }

            return hasCanon ? method.GetGenericMethodDefinition().MakeGenericMethod(genericArguments) : method;
        }
    }
}

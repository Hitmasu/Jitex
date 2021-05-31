using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Jitex.Runtime;

namespace Jitex.Utils
{
    internal static class MethodHelper
    {
        private static readonly ConstructorInfo CtorHandle;
        private static readonly MethodInfo GetMethodBase;
        private static readonly Type? CanonType;
        private static readonly MethodInfo? GetMethodDescriptorInfo;
        private static readonly ConcurrentDictionary<IntPtr, MethodBase> HandleCache = new ConcurrentDictionary<IntPtr, MethodBase>();

        static MethodHelper()
        {
            CanonType = Type.GetType("System.__Canon");

            Type? runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal");

            if (runtimeMethodHandleInternalType == null)
                throw new TypeLoadException("Type System.RuntimeMethodHandleInternal was not found!");

            Type? runtimeType = Type.GetType("System.RuntimeType");

            if (runtimeType == null)
                throw new TypeLoadException("Type System.RuntimeType was not found!");

            CtorHandle = runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] {typeof(IntPtr)}, null)
                         ?? throw new MethodAccessException("Constructor from RuntimeMethodHandleInternal was not found!");

            GetMethodBase = runtimeType
                                .GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null, CallingConventions.Any, new[] {runtimeType, runtimeMethodHandleInternalType}, null)
                            ?? throw new MethodAccessException("Method GetMethodBase from RuntimeType was not found!");

            GetMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static object? GetRuntimeMethodHandle(IntPtr methodHandle)
        {
            return CtorHandle!.Invoke(new object?[] {methodHandle});
        }

        public static MethodInfo GetMethodGeneric(MethodInfo method)
        {
            Type[] genericArguments = method.GetGenericArguments();

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

        public static RuntimeMethodHandle GetMethodHandle(MethodBase method)
        {
            if (method is DynamicMethod)
                return (RuntimeMethodHandle) GetMethodDescriptorInfo.Invoke(method, null);

            return method.MethodHandle;
        }

        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            if (HandleCache.TryGetValue(methodHandle, out MethodBase? method))
                return method;

            object? handle = GetRuntimeMethodHandle(methodHandle);
            method = GetMethodBase.Invoke(null, new[] {null, handle}) as MethodBase;

            if (method != null)
                HandleCache.TryAdd(methodHandle, method);

            return method;
        }

        public static NativeCode GetNativeCode(MethodBase method) => RuntimeMethodCache.GetNativeCode(method);

        public static void PrepareMethod(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);
        }
    }
}
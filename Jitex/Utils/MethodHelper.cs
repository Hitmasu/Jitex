using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Jitex.Builder.IL.Resolver;
using Jitex.Runtime;
using Jitex.Utils.Extension;

namespace Jitex.Utils
{
    internal static class MethodHelper
    {
        private static readonly ConstructorInfo CtorHandle;
        private static readonly MethodInfo GetMethodBase;
        private static readonly Type CanonType;
        private static readonly MethodInfo? GetMethodDescriptorInfo;
        private static readonly ConcurrentDictionary<IntPtr, MethodBase> HandleCache = new ConcurrentDictionary<IntPtr, MethodBase>();

        static MethodHelper()
        {
            CanonType = Type.GetType("System.__Canon")!;

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

            GetMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static object? GetRuntimeMethodHandle(IntPtr methodHandle)
        {
            return CtorHandle!.Invoke(new object?[] { methodHandle });
        }

        public static MethodInfo GetBaseMethodGeneric(MethodInfo method)
        {
            bool hasCanon = false;

            Type originalType = method.DeclaringType;

            if (method.DeclaringType is { IsGenericType: true })
            {
                Type[]? genericTypeArguments = method.DeclaringType.GetGenericArguments();

                if (ReadGenericTypes(ref genericTypeArguments))
                    hasCanon = true;

                originalType = originalType.GetGenericTypeDefinition().MakeGenericType(genericTypeArguments);
            }

            if (method.IsGenericMethod)
            {
                Type[]? genericMethodArguments = method.GetGenericArguments();
                if (ReadGenericTypes(ref genericMethodArguments))
                    hasCanon = true;

                method = method.GetGenericMethodDefinition().MakeGenericMethod(genericMethodArguments);
            }

            if (!hasCanon)
                return method;

            return (MethodInfo)MethodBase.GetMethodFromHandle(method.MethodHandle, originalType.TypeHandle);

            static bool ReadGenericTypes(ref Type[] types)
            {
                bool hasCanon = false;

                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];

                    if (type.IsCanon())
                    {
                        types[i] = CanonType;
                        hasCanon = true;
                    }
                }

                return hasCanon;
            }
        }

        public static bool HasCannon(MethodBase method)
        {
            if (method is MethodInfo { IsGenericMethod: true } methodInfo)
                return methodInfo.GetGenericArguments().Any(w => w.IsCanon());

            return false;
        }

        public static RuntimeMethodHandle GetMethodHandle(MethodBase method)
        {
            if (method is DynamicMethod)
                return (RuntimeMethodHandle)GetMethodDescriptorInfo.Invoke(method, null);

            return method.MethodHandle;
        }

        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            if (HandleCache.TryGetValue(methodHandle, out MethodBase? method))
                return method;

            object? handle = GetRuntimeMethodHandle(methodHandle);
            method = GetMethodBase.Invoke(null, new[] { null, handle }) as MethodBase;

            if (method != null)
                HandleCache.TryAdd(methodHandle, method);

            return method;
        }

        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle, IntPtr typeHandle)
        {
            MethodBase? method = GetMethodFromHandle(methodHandle);

            if (method == null)
                return null;

            Type type = TypeHelper.GetTypeFromHandle(typeHandle);

            return MethodBase.GetMethodFromHandle(method.MethodHandle, type.TypeHandle);
        }

        public static NativeCode GetNativeCode(MethodBase method) => RuntimeMethodCache.GetNativeCode(method);

        public static void PrepareMethod(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);
        }
    }
}
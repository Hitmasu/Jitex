using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Jitex.Utils
{
    internal static class MethodHelper
    {
        private static readonly ConstructorInfo CtorHandle;
        private static readonly MethodInfo GetMethodBase;
        private static readonly MethodInfo GetMethodDescriptorInfo;

        private static readonly ConcurrentDictionary<IntPtr, MethodBase?> Cache = new ConcurrentDictionary<IntPtr, MethodBase?>();

        static MethodHelper()
        {
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

        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            MethodBase? method = GetFromCache(methodHandle);

            if (method == null)
            {
                object? handle = GetMethodHandleFromPointer(methodHandle);
                method = GetMethodBase.Invoke(null, new[] { null, handle }) as MethodBase;
                Cache.TryAdd(methodHandle, method);
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

        public static IntPtr GetMethodAddress(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);

            IntPtr methodPointer = handle.GetFunctionPointer();

            byte jmp = Marshal.ReadByte(methodPointer);

            if (jmp == 0xE9)
            {
                int jmpSize = Marshal.ReadInt32(methodPointer + 1);
                methodPointer = new IntPtr(methodPointer.ToInt64() + jmpSize + 5);
            }

            return methodPointer;
        }

        public static RuntimeMethodHandle GetMethodHandle(MethodBase method)
        {
            if (method is DynamicMethod)
            {
                return (RuntimeMethodHandle)GetMethodDescriptorInfo.Invoke(method, null);
            }
            else
            {
                return method.MethodHandle;
            }
        }
    }
}

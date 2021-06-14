using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Jitex.Runtime;
using Jitex.Utils.Extension;

namespace Jitex.Utils
{
    public static class MethodHelper
    {
        private static readonly IntPtr PrecodeFixupThunkAddress;
        private static readonly ConstructorInfo CtorHandle;
        private static readonly MethodInfo GetMethodBase;
        private static readonly Type CanonType;
        private static readonly MethodInfo? GetMethodDescriptorInfo;

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

            PrecodeFixupThunkAddress = GetPrecodeFixupThunkAddress();
        }

        private static IntPtr GetPrecodeFixupThunkAddress()
        {
            MethodInfo methodStub = typeof(MethodHelper).GetMethod("MethodToNeverBeCalled", BindingFlags.Static | BindingFlags.NonPublic)!;

            IntPtr functionalPointer = methodStub.MethodHandle.GetFunctionPointer();
            int jmpSize = Marshal.ReadInt32(functionalPointer, 1);

            return functionalPointer + jmpSize + 5;
        }

        private static object? GetRuntimeMethodHandle(IntPtr methodHandle)
        {
            return CtorHandle!.Invoke(new object?[] { methodHandle });
        }

        internal static MethodBase GetBaseMethodGeneric(MethodBase method)
        {
            bool hasCanon = false;

            Type originalType = method.DeclaringType;

            if (method.DeclaringType is { IsGenericType: true })
            {
                Type[] genericTypeArguments = method.DeclaringType.GetGenericArguments();

                if (ReadGenericTypes(ref genericTypeArguments))
                    hasCanon = true;

                originalType = originalType.GetGenericTypeDefinition().MakeGenericType(genericTypeArguments);
            }

            if (method.IsGenericMethod)
            {
                Type[]? genericMethodArguments = method.GetGenericArguments();
                if (ReadGenericTypes(ref genericMethodArguments))
                    hasCanon = true;

                method = ((MethodInfo)method).GetGenericMethodDefinition().MakeGenericMethod(genericMethodArguments);
            }

            if (!hasCanon)
                return method;

            return MethodBase.GetMethodFromHandle(method.MethodHandle, originalType.TypeHandle);

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

        internal static MethodBase GetOriginalMethod(MethodBase method)
        {
            if (!HasCannon(method))
                return method;

            return GetBaseMethodGeneric(method);
        }

        internal static bool HasCannon(MethodBase method)
        {
            if (method is MethodInfo { IsGenericMethod: true } methodInfo)
                return methodInfo.GetGenericArguments().Any(w => w.IsCanon());

            if (method.DeclaringType != null)
                return TypeHelper.HasCanon(method.DeclaringType);

            return false;
        }

        /// <summary>
        /// Get handle from a method.
        /// </summary>
        /// <param name="method">Method to get handle.</param>
        /// <returns>Handle from method.</returns>
        public static RuntimeMethodHandle GetMethodHandle(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            if (method is DynamicMethod)
                return (RuntimeMethodHandle)GetMethodDescriptorInfo.Invoke(method, null);

            return method.MethodHandle;
        }

        /// <summary>
        /// Get method from a handle.
        /// </summary>
        /// <param name="methodHandle">Handle of method.</param>
        /// <returns>Method from handle.</returns>
        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            object? handle = GetRuntimeMethodHandle(methodHandle);
            MethodBase? method = GetMethodBase.Invoke(null, new[] { null, handle }) as MethodBase;
            return method;
        }

        /// <summary>
        /// Get method from handle and type.
        /// </summary>
        /// <param name="methodHandle">Handle of method.</param>
        /// <param name="typeHandle">Handle of type.</param>
        /// <returns>Method from handle and type.</returns>
        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle, IntPtr typeHandle)
        {
            MethodBase? method = GetMethodFromHandle(methodHandle);

            if (method == null)
                return null;

            Type type = TypeHelper.GetTypeFromHandle(typeHandle);

            RuntimeMethodHandle handle = GetMethodHandle(method);
            return MethodBase.GetMethodFromHandle(handle, type.TypeHandle);
        }

        internal static NativeCode GetNativeCode(MethodBase method) => GetNativeCodeAsync(method).GetAwaiter().GetResult();

        /// <summary>
        /// Get native code info from a method.
        /// </summary>
        /// <param name="method"></param>
        /// <returns>Native code info from method.</returns>
        public static Task<NativeCode> GetNativeCodeAsync(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            return RuntimeMethodCache.GetNativeCodeAsync(method);
        }

        /// <summary>
        /// Returns if method is already compiled.
        /// </summary>
        /// <param name="method">Method to get status.</param>
        /// <returns>Returns true if method is compiled, otherwise returns false.</returns>
        public static bool IsCompiled(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            method = GetOriginalMethod(method);

            IntPtr methodHandle = GetMethodHandle(method).Value;

            //Maybe we can replace by FunctionalPointer == 0xE9?
            return Marshal.ReadIntPtr(methodHandle, IntPtr.Size * 2) != IntPtr.Zero;
        }

        public static bool IsHookable(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            method = GetOriginalMethod(method);

            if (!IsCompiled(method))
                return true;

            return RuntimeMethodCache.GetMethodCompiledInfo(method) != null;
        }

        public static void ForceRecompile(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            if (!IsCompiled(method))
                return;

            method = GetOriginalMethod(method);

            SetMethodToPrecodeFixup(method);
        }

        internal static void SetMethodToPrecodeFixup(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);

            IntPtr methodHandle = handle.Value;
            IntPtr functionalPointer = handle.GetFunctionPointer();

            int jmpSize = (int)(PrecodeFixupThunkAddress.ToInt64() - functionalPointer.ToInt64() - 5);

            //Write PrecodeFixupThunk
            Marshal.WriteByte(functionalPointer, 0xE8); //call instruction
            Marshal.WriteByte(functionalPointer, 5, 0x5E); //pop instruction
            Marshal.WriteInt32(functionalPointer, 1, jmpSize);

            Marshal.WriteIntPtr(methodHandle, IntPtr.Size * 2, IntPtr.Zero);
        }

        internal static void PrepareMethod(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        [Obsolete("This method shouldn't be called!")]
        private static void MethodToNeverBeCalled()
        {
            throw new NotImplementedException("This method shouldn't be called!");
        }
    }
}
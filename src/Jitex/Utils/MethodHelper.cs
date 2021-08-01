using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Exceptions;
using Jitex.Framework;
using Jitex.Runtime;
using Jitex.Utils.Extension;

namespace Jitex.Utils
{
    public static class MethodHelper
    {
        private static readonly bool CanRecompileMethod;

        private static readonly Type CanonType;
        private static readonly IntPtr PrecodeFixupThunkAddress;
        private static readonly ConstructorInfo CtorRuntimeMethodHandeInternal;
        private static readonly MethodInfo GetMethodBase;
        private static readonly MethodInfo GetMethodDescriptorInfo;
        private static readonly MethodInfo GetFunctionPointerInternal;

        static MethodHelper()
        {
            Type runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal")!;
            Type runtimeType = Type.GetType("System.RuntimeType")!;

            CanonType = Type.GetType("System.__Canon")!;

            CtorRuntimeMethodHandeInternal = runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IntPtr) }, null)!;

            GetMethodBase = runtimeType.GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { runtimeType, runtimeMethodHandleInternalType }, null)!;

            GetMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance)!;

            GetFunctionPointerInternal = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Static | BindingFlags.NonPublic)!;

            PrecodeFixupThunkAddress = GetPrecodeFixupThunkAddress();

            CanRecompileMethod = RuntimeFramework.Framework >= new Version(5, 0, 0);
        }

        private static IntPtr GetPrecodeFixupThunkAddress()
        {
            MethodInfo methodStub = typeof(MethodHelper).GetMethod("MethodToNeverBeCalled", BindingFlags.Static | BindingFlags.NonPublic)!;
            IntPtr functionPointer = methodStub.MethodHandle.GetFunctionPointer();
            int jmpSize = Marshal.ReadInt32(functionPointer, 1);

            return functionPointer + jmpSize + 5;
        }

        private static object? GetRuntimeMethodHandleInternal(IntPtr methodHandle)
        {
            return CtorRuntimeMethodHandeInternal!.Invoke(new object?[] { methodHandle });
        }

        internal static MethodBase GetBaseMethodGeneric(MethodBase method)
        {
            MethodBase originalMethod = method;
            bool methodHasCanon = HasCanon(method, false);
            bool typeHasCanon = TypeHelper.HasCanon(method.DeclaringType);

            if (methodHasCanon || (typeHasCanon && (method.IsGenericMethod || method.IsStatic)))
            {
                IntPtr methodHandle = GetDirectMethodHandle(method);
                originalMethod = GetMethodFromHandle(methodHandle)!;
            }
            else if (typeHasCanon)
            {
                Type typeCanon = TypeHelper.GetBaseTypeGeneric(method.DeclaringType!);
                originalMethod = MethodBase.GetMethodFromHandle(method.MethodHandle, typeCanon.TypeHandle);
            }

            return originalMethod;
        }

        public static MethodBase GetOriginalMethod(MethodBase method)
        {
            return GetBaseMethodGeneric(method);
        }

        internal static bool IsGenericInitialized(MethodBase method)
        {
            if (method.DeclaringType is { IsGenericType: true })
            {
                foreach (Type type in method.DeclaringType.GetGenericArguments())
                {
                    if (type.IsGenericParameter)
                        return false;
                }
            }

            if (method.IsGenericMethod)
            {
                MethodInfo methodInfo = (MethodInfo)method;

                foreach (Type type in methodInfo.GetGenericArguments())
                {
                    if (type.IsGenericParameter)
                        return false;
                }
            }

            return true;
        }

        internal static bool HasCanon(MethodBase method, bool checkDeclaredType = true, bool ignoreCanonType = false)
        {
            bool hasCanon = false;

            if (method is MethodInfo { IsGenericMethod: true } methodInfo)
            {
                Type[] types = method.GetGenericArguments();

                foreach (Type type in types)
                {
                    if (ignoreCanonType && type == CanonType)
                        continue;

                    if (type.IsCanon())
                    {
                        hasCanon = true;
                        break;
                    }
                }

            }

            if (!hasCanon && checkDeclaredType && method.DeclaringType != null)
                hasCanon = TypeHelper.HasCanon(method.DeclaringType);

            return hasCanon;
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

        public static IntPtr GetFunctionPointer(IntPtr methodHandle)
        {
            object handle = GetRuntimeMethodHandleInternal(methodHandle);
            return (IntPtr)GetFunctionPointerInternal.Invoke(null, new[] { handle });
        }

        private static IntPtr GetDirectMethodHandle(MethodBase method)
        {
            bool methodHasCanon = HasCanon(method, false, true);
            bool typeHasCanon = TypeHelper.HasCanon(method.DeclaringType, true);

            IntPtr methodHandle = method.MethodHandle.Value;

            if (CanRecompileMethod && (methodHasCanon || typeHasCanon && (method.IsGenericMethod || method.IsStatic)))
                methodHandle = Marshal.ReadIntPtr(methodHandle, IntPtr.Size);
            else
                methodHandle = GetMethodHandle(method).Value;

            return methodHandle;
        }

        /// <summary>
        /// Get method from a handle.
        /// </summary>
        /// <param name="methodHandle">Handle of method.</param>
        /// <returns>Method from handle.</returns>
        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle)
        {
            object? handle = GetRuntimeMethodHandleInternal(methodHandle);
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

        public static IntPtr GetNativeAddress(MethodBase method)
        {
            RuntimeMethodHandle handle = GetMethodHandle(method);
            RuntimeHelpers.PrepareMethod(handle);

            IntPtr functionPointer = handle.GetFunctionPointer();

            byte opCode = MemoryHelper.Read<byte>(functionPointer, 0);

            if (opCode == 0xE9)
            {
                int jmpSize = MemoryHelper.Read<int>(functionPointer, 1);
                return functionPointer + jmpSize + 5;
            }

            return functionPointer;
        }

        /// <summary>
        /// Returns if method was compiled as ReadyToRun (R2R).
        /// </summary>
        /// <param name="method">Method to check ReadyToRun.</param>
        /// <returns>Returns true is was compiled as ReadyToRun otherwise false.</returns>
        public static bool IsReadyToRun(MethodBase method) => ReadyToRunHelper.MethodIsReadyToRun(method);

        /// <summary>
        /// Disable ReadyToRun on method, forcing method to be compiled by jit.
        /// </summary>
        /// <param name="method">Method to disable ReadyToRun.</param>
        /// <returns>Returns false if method is not ReadyToRun otherwise true.</returns>
        public static bool DisableReadyToRun(MethodBase method) => ReadyToRunHelper.DisableReadyToRun(method);

        //internal static NativeCode GetNativeCode(MethodBase method, CancellationToken cancellationToken) => GetNativeCodeAsync(method, cancellationToken).GetAwaiter().GetResult();

        ///// <summary>
        ///// Get native code from a method.
        ///// </summary>
        ///// <param name="method">Method to get native code.</param>
        ///// <returns>Native code info from method.</returns>
        //public static Task<NativeCode> GetNativeCodeAsync(MethodBase method, CancellationToken cancellationToken)
        //{
        //    if (method == null) throw new ArgumentNullException(nameof(method));

        //    return RuntimeMethodCache.GetNativeCodeAsync(method, cancellationToken);
        //}

        /// <summary>
        /// Returns if method is already compiled.
        /// </summary>
        /// <param name="method">Method to get status.</param>
        /// <returns>Returns true if method is compiled, otherwise returns false.</returns>
        public static bool IsCompiled(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            method = GetOriginalMethod(method);
            IntPtr methodHandle = GetDirectMethodHandle(method);
            int offset = GetHandleOffset(method);
            return Marshal.ReadIntPtr(methodHandle, IntPtr.Size * offset) != IntPtr.Zero;
        }

        /// <summary>
        /// Return is method can be resolved
        /// </summary>
        /// <param name="method">Method to check</param>
        /// <returns>Returns true if method is resolvable, otherwise returns false.</returns>
        public static bool IsResolvable(MethodBase method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));

            CheckIfGenericIsInitialized(method);

            method = GetOriginalMethod(method);

            if (!IsCompiled(method))
                return true;

            return RuntimeMethodCache.GetMethodCompiledInfo(method) != null;
        }

        /// <summary>
        /// Set state method to be compiled
        /// </summary>
        /// <param name="method">Method to be compiled.</param>
        public static void ForceRecompile(MethodBase method)
        {
            if (!CanRecompileMethod) throw new UnsupportedFrameworkVersion("Recompile method is only supported on .NET 5 or above.");
            if (method == null) throw new ArgumentNullException(nameof(method));

            CheckIfGenericIsInitialized(method);

            SetMethodToPrecodeFixup(method);
        }

        internal static void SetMethodToPrecodeFixup(MethodBase method)
        {
            IntPtr methodHandle = GetDirectMethodHandle(method);
            IntPtr functionPointer = GetFunctionPointer(methodHandle);
            int jmpSize = (int)(PrecodeFixupThunkAddress.ToInt64() - functionPointer.ToInt64() - 5);
            int offset = GetHandleOffset(method);

            Marshal.WriteIntPtr(methodHandle, IntPtr.Size * offset, IntPtr.Zero);

            Marshal.WriteByte(functionPointer, 0xE8); //call instruction
            Marshal.WriteByte(functionPointer, 5, 0x5E); //pop instruction
            Marshal.WriteInt32(functionPointer, 1, jmpSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHandleOffset(MethodBase method)
        {
            if (TypeHelper.IsGeneric(method.DeclaringType) && !method.IsGenericMethod)
                return 1;

            if (method.IsGenericMethod)
                return 5;

            return 2;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckIfGenericIsInitialized(MethodBase method)
        {
            if (!IsGenericInitialized(method))
            {
                MethodInfo methodInfo = (MethodInfo)method;

                if (method == methodInfo.GetGenericMethodDefinition())
                    throw new ArgumentException("Generic methods cannot be recompiled by generic method definition.\n" +
                                                "It's necessary substitute generic parameters types from generic definition: "
                                                + method);
            }
        }

        /// <summary>
        /// Get RID from a method.
        /// </summary>
        /// <param name="method">Method to get RID.</param>
        /// <returns>RID from method.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetRID(MethodBase method) => method.MetadataToken & 0x00FFFFFF;
    }
}

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Jitex.Framework;
using Jitex.Runtime;
using Jitex.Utils.Extension;

namespace Jitex.Utils
{
    public static class MethodHelper
    {
        private static readonly RuntimeFramework Framework = RuntimeFramework.GetFramework();
        private static readonly Type CanonType;
        private static readonly IntPtr PrecodeFixupThunkAddress;
        private static readonly ConstructorInfo CtorRuntimeMethodHandeInternal;
        private static readonly MethodInfo GetMethodBase;
        private static readonly MethodInfo GetMethodDescriptorInfo;
        private static readonly MethodInfo GetFunctionPointerInternal;

        static MethodHelper()
        {
            Type runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal");
            Type runtimeType = Type.GetType("System.RuntimeType");

            CanonType = Type.GetType("System.__Canon");

            CtorRuntimeMethodHandeInternal = runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, new[] { typeof(IntPtr) }, null);

            GetMethodBase = runtimeType
                                .GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null, new[] { runtimeType, runtimeMethodHandleInternalType }, null);

            GetMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);

            GetFunctionPointerInternal = typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer", BindingFlags.Static | BindingFlags.NonPublic);

            PrecodeFixupThunkAddress = GetPrecodeFixupThunkAddress();
        }

        private static IntPtr GetPrecodeFixupThunkAddress()
        {
            MethodInfo methodStub = typeof(MethodHelper).GetMethod("MethodToNeverBeCalled", BindingFlags.Static | BindingFlags.NonPublic)!;

            IntPtr functionalPointer = methodStub.MethodHandle.GetFunctionPointer();
            int jmpSize = Marshal.ReadInt32(functionalPointer, 1);

            return functionalPointer + jmpSize + 5;
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

        private static IntPtr GetFunctionPointer(IntPtr methodHandle)
        {
            object handle = GetRuntimeMethodHandleInternal(methodHandle);
            return (IntPtr)GetFunctionPointerInternal.Invoke(null, new[] { handle });
        }

        public static IntPtr GetDirectMethodHandle(MethodBase method)
        {
            bool methodHasCanon = HasCanon(method, false, true);
            bool typeHasCanon = TypeHelper.HasCanon(method.DeclaringType, true);

            IntPtr methodHandle = method.MethodHandle.Value;

            if (methodHasCanon || (typeHasCanon && (method.IsGenericMethod || method.IsStatic)))
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
            IntPtr methodHandle = GetDirectMethodHandle(method);
            int offset = GetHandleOffset(method);
            return Marshal.ReadIntPtr(methodHandle, IntPtr.Size * offset) != IntPtr.Zero;
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

            if (!IsGenericInitialized(method))
            {
                MethodInfo methodInfo = (MethodInfo)method;

                if (method == methodInfo.GetGenericMethodDefinition())
                    throw new ArgumentException("Generic methods cannot be recompiled by generic method definition.\n" +
                                                "It's necessary substitute generic parameters types from generic definition: "
                                                + method.ToString());
            }

            if (!IsCompiled(method))
                return;

            SetMethodToPrecodeFixup(method);
        }

        internal static bool IsDynamicMethod(MethodBase method) => method is DynamicMethod;

        internal static void SetMethodToPrecodeFixup(MethodBase method)
        {
            IntPtr methodHandle = GetDirectMethodHandle(method);
            IntPtr functionPointer = GetFunctionPointer(methodHandle);

            int jmpSize = (int)(PrecodeFixupThunkAddress.ToInt64() - functionPointer.ToInt64() - 5);
            int offset = GetHandleOffset(method);

            if (Framework.FrameworkVersion >= new Version(5, 0, 0))
            {
                //Write PrecodeFixupThunk
                Marshal.WriteByte(functionPointer, 0xE8); //call instruction
                Marshal.WriteByte(functionPointer, 5, 0x5E); //pop instruction
                Marshal.WriteInt32(functionPointer, 1, jmpSize);

                Marshal.WriteIntPtr(methodHandle, IntPtr.Size * offset, IntPtr.Zero);
            }
            else
            {
                Marshal.WriteIntPtr(methodHandle, IntPtr.Size * offset, functionPointer);
                Marshal.WriteIntPtr(methodHandle, IntPtr.Size * offset - 1, IntPtr.Zero);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHandleOffset(MethodBase method)
        {
            int offset = 0;

            if (TypeHelper.IsGeneric(method.DeclaringType) && !method.IsGenericMethod)
            {
                offset = 1;
            }
            else if (method.IsGenericMethod)
            {
                offset = 5;
            }
            else
            {
                offset = 2;
            }

            return offset;
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
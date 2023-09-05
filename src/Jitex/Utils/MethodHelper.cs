using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Jitex.Exceptions;
using Jitex.Framework;
using Jitex.PE;
using Jitex.Runtime;
using Jitex.Utils.Extension;

namespace Jitex.Utils
{
    public static class MethodHelper
    {
        private static readonly bool CanRecompileMethod;

        private static readonly Type CanonType;
        private static IntPtr PreCodeFixupThunkAddress;
        private static readonly ConstructorInfo CtorRuntimeMethodHandeInternal;
        private static readonly MethodInfo GetMethodBase;
        private static readonly MethodInfo GetMethodDescriptorInfo;
        private static readonly MethodInfo GetFunctionPointerInternal;
        private static readonly MethodInfo GetSlot;

        static MethodHelper()
        {
            Type runtimeMethodHandleInternalType = Type.GetType("System.RuntimeMethodHandleInternal")!;
            Type runtimeType = Type.GetType("System.RuntimeType")!;
            Type iRuntimeMethodInfo = Type.GetType("System.IRuntimeMethodInfo")!;

            CanonType = Type.GetType("System.__Canon")!;

            CtorRuntimeMethodHandeInternal =
                runtimeMethodHandleInternalType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] { typeof(IntPtr) }, null)!;

            GetMethodBase = runtimeType.GetMethod("GetMethodBase", BindingFlags.NonPublic | BindingFlags.Static, null,
                new[] { runtimeType, runtimeMethodHandleInternalType }, null)!;

            GetMethodDescriptorInfo =
                typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.Instance | BindingFlags.NonPublic)!;

            GetFunctionPointerInternal =
                typeof(RuntimeMethodHandle).GetMethod("GetFunctionPointer",
                    BindingFlags.Static | BindingFlags.NonPublic)!;

            GetSlot = typeof(RuntimeMethodHandle).GetMethod("GetSlot", BindingFlags.Static | BindingFlags.NonPublic,
                null, new[] { iRuntimeMethodInfo }, null)!;

            PreCodeFixupThunkAddress = GetPrecodeFixupThunkAddress();

            CanRecompileMethod = RuntimeFramework.Framework >= new Version(5, 0, 0);
        }

        private static IntPtr GetPrecodeFixupThunkAddress()
        {
            var methodStub =
                typeof(MethodHelper).GetMethod("MethodToNeverBeCalled", BindingFlags.Static | BindingFlags.NonPublic)!;
            var functionPointer = methodStub.MethodHandle.GetFunctionPointer();
            var jmpSize = Marshal.ReadInt32(functionPointer, 1);

            return functionPointer + jmpSize + 5;
        }

        private static object GetRuntimeMethodHandleInternal(IntPtr methodHandle)
        {
            return CtorRuntimeMethodHandeInternal!.Invoke(new object?[] { methodHandle });
        }

        private static int GetMethodSlot(MethodInfo method)
        {
            return (int)GetSlot.Invoke(null, new object[] { method });
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

        private static bool IsGenericInitialized(MethodBase method)
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

            if (method is MethodInfo { IsGenericMethod: true })
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

            if (DynamicHelpers.IsRTDynamicMethod(method))
            {
                method = DynamicHelpers.GetOwner(method);
                return GetMethodHandle(method);
            }

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
        public static MethodBase? GetMethodFromHandle(IntPtr methodHandle, RuntimeTypeHandle typeHandle)
        {
            MethodBase? method = GetMethodFromHandle(methodHandle);

            if (method == null)
                return null;

            RuntimeMethodHandle handle = GetMethodHandle(method);
            return MethodBase.GetMethodFromHandle(handle, typeHandle);
        }

        public static IntPtr GetNativeAddress(MethodBase method, bool prepareMethod = true)
        {
            var handle = GetMethodHandle(method);

            if (prepareMethod)
                RuntimeHelpers.PrepareMethod(handle);

            var functionPointer = handle.GetFunctionPointer();

            var offset = 0;

            if (OSHelper.IsArm64 && RuntimeFramework.Framework.FrameworkVersion < new Version(7, 0))
                offset = 4;

            var opCode = MemoryHelper.Read<byte>(functionPointer, offset);

            if (OSHelper.IsArm64)
            {
                //LDR OpCode
                if (opCode is 0x0B or 0x6B)
                {
                    var midAddress = GetMidAddress(functionPointer);
                    return MemoryHelper.Read<IntPtr>(midAddress);
                }
            }
            else
            {
                //MOV OpCode
                if (opCode == 0xE9)
                {
                    var jmpSize = MemoryHelper.Read<int>(functionPointer, 1);
                    return functionPointer + jmpSize + 5;
                }
            }


            return functionPointer;
        }

        /// <summary>
        /// Returns if method was compiled as ReadyToRun (R2R).
        /// </summary>
        /// <param name="method">Method to check ReadyToRun.</param>
        /// <returns>Returns true is was compiled as ReadyToRun otherwise false.</returns>
        public static bool IsReadyToRun(MethodBase method)
        {
            NativeReader reader = new NativeReader(method.Module);
            return reader.IsReadyToRun(method);
        }

        /// <summary>
        /// Disable ReadyToRun on method, forcing method to be compiled by jit.
        /// </summary>
        /// <param name="method">Method to disable ReadyToRun.</param>
        /// <returns>Returns false if method is not ReadyToRun otherwise true.</returns>
        public static bool DisableReadyToRun(MethodBase method)
        {
            if (!IsReadyToRun(method))
                return false;

            NativeReader reader = new(method.Module);
            return reader.DisableReadyToRun(method);
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
            int offset = GetFunctionPointerOffset(method);
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
        /// <returns>Returns if state was set sucessfully.</returns>
        public static bool ForceRecompile(MethodBase method)
        {
            if (!CanRecompileMethod)
                throw new UnsupportedFrameworkVersion("Recompile method is only supported on .NET 5 or above.");

            if (method == null)
                throw new ArgumentNullException(nameof(method));

            CheckIfGenericIsInitialized(method);

            return SetMethodPreCode(method);
        }

        private static bool SetMethodPreCode(MethodBase method)
        {
            var methodHandle = GetDirectMethodHandle(method);
            var functionPointer = GetFunctionPointer(methodHandle);

            var offset = GetFunctionPointerOffset(method);

            //Remove functionPointer on MethodDesc
            MemoryHelper.Write(methodHandle, IntPtr.Size * offset, IntPtr.Zero);

            //Set call instruction to call PreCodeFixup
            if (OSHelper.IsArm64)
            {
                var midAddress = GetMidAddress(functionPointer);
                var compileAddress = functionPointer + IntPtr.Size;

                MemoryHelper.Write(midAddress, compileAddress);
            }
            else
            {
                var jmpSize = (int)(PreCodeFixupThunkAddress.ToInt64() - functionPointer.ToInt64() - 5);

                MemoryHelper.Write<byte>(functionPointer, 0xE8);
                MemoryHelper.Write<byte>(functionPointer, 5, 0x5E);
                MemoryHelper.Write(functionPointer, 1, jmpSize);
            }

            if (!method.IsVirtual)
                return false;

            IntPtr typeHandle = method.DeclaringType!.TypeHandle.Value;

            //To find start of vtable, currently we need search by some specific value on TypeDesc.
            //That's a dumb way to get address of vtable, but sadly, i can't find a better way.
            //TODO: Find a better way to get vtable from type.
            IntPtr startVTable = typeHandle + IntPtr.Size * 2;
            IntPtr endVTable = startVTable + IntPtr.Size * 100;

            Type[] interfaces = method.DeclaringType.GetInterfaces();
            Type? lastInterface = interfaces.LastOrDefault();

            bool addressFound = false;

            if (lastInterface != null)
            {
                //When classes implements interfaces, we can find the start of vtable searching by TypeHandle from interfaces implemented.
                //All TypeHandles from all interfaces, will stored on TypeDesc. 
                //VTable starts after last TypeHandle from last interface implemented.
                //
                //Eg.:
                // ------
                // public class MyType : IInterface1, IInterface2, IInterface3
                // ------
                //<address>:    (Number of Interfaces + 1) 00 00 00 00 00 00 00
                //<address+8>:  IInterface1TypeHandle
                //<address+16>: IInterface2TypeHandle
                //<address+24>: IInterface3TypeHandle
                //<address+32>: VTable

                do
                {
                    if (MemoryHelper.Read<IntPtr>(startVTable) == lastInterface.TypeHandle.Value)
                    {
                        startVTable += IntPtr.Size;
                        addressFound = true;
                        break;
                    }

                    startVTable += IntPtr.Size;
                } while (startVTable.ToInt64() < endVTable.ToInt64());
            }
            else
            {
                //Normal classes have a weird pointer to vtable:
                //---
                //<address>:    00 00 00 00 00 00 00 00
                //<address+8>:  [pointer to <address+16>]
                //<address+16>: VTable
                //---
                //So, we basically need find an address value which pointer to the next address.
                do
                {
                    IntPtr value = MemoryHelper.Read<IntPtr>(startVTable);
                    startVTable += IntPtr.Size;

                    if (value == startVTable)
                    {
                        addressFound = true;
                        break;
                    }
                } while (startVTable.ToInt64() < endVTable.ToInt64());
            }

            if (!addressFound)
                return false;

            int originalSlot = GetMethodSlot((MethodInfo)method);

            //If method is virtual, we need get the "virtual" function pointer, which sadly, it's not same from MethodHandle.
            //I can't find a way to get that pointer, but we can assume his allocated after/before function pointer from last constructor:
            //Eg.:
            //--
            //<address>:    Constructor[0] Function Pointer
            //<address+8>:  Constructor[1] Function Pointer
            //<address+16>: Constructor[2] Function Pointer
            //....
            //<address+..>: Virtual Methods Function Pointer
            //--

            ConstructorInfo lastCtor = method.DeclaringType!.GetConstructors((BindingFlags)(-1)).Last();

            int vTableIndex = method.MetadataToken - lastCtor.MetadataToken;

            IntPtr ctorPointer = lastCtor.MethodHandle.GetFunctionPointer();
            IntPtr virtualFunctionPointer = ctorPointer + IntPtr.Size * vTableIndex;

            if (virtualFunctionPointer == ctorPointer)
            {
                if (lastCtor.MetadataToken > method.MetadataToken)
                    virtualFunctionPointer -= IntPtr.Size;
                else
                    virtualFunctionPointer += IntPtr.Size;
            }

            MemoryHelper.Write(startVTable, IntPtr.Size * originalSlot, virtualFunctionPointer);

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetFunctionPointerOffset(MethodBase method)
        {
            if (method.IsFinal)
                return 3;

            if (method.DeclaringType is { IsGenericType: true } && !method.IsGenericMethod || method.IsVirtual)
                return 1;

            if (method.IsGenericMethod)
                return 5;

            return 2;
        }

        /// <summary>
        /// Initialize generic method if method is generic.
        /// </summary>
        /// <param name="method">Method to initialize.</param>
        /// <param name="typeGenericArguments">Generic arguments from declared type.</param>
        /// <param name="methodGenericArguments">Generic arguments from method.</param>
        /// <returns>If method or declared type is generic, returns a MethodInfo with arguements typed, otherwise return parameter method.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        internal static MethodBase TryInitializeGenericMethod(MethodBase method, Type[]? typeGenericArguments,
            Type[]? methodGenericArguments)
        {
            if (!method.IsGenericMethod && method.DeclaringType is not { IsGenericType: true })
                return method;

            Type declaringType = method.DeclaringType;

            if (TypeHelper.HasCanon(declaringType))
            {
                if (typeGenericArguments == null)
                    throw new ArgumentNullException(nameof(typeGenericArguments));

                declaringType = declaringType.MakeGenericType(typeGenericArguments);
            }

            if (HasCanon(method, false))
            {
                if (methodGenericArguments == null)
                    throw new ArgumentNullException(nameof(methodGenericArguments));

                MethodInfo methodInfo = (MethodInfo)method;
                method = methodInfo.GetGenericMethodDefinition().MakeGenericMethod(methodGenericArguments);
            }

            return GetMethodFromHandle(method.MethodHandle.Value, declaringType.TypeHandle)!;
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
        internal static int GetRID(MethodBase method) => method.MetadataToken & 0x00FFFFFF;

        private static IntPtr GetMidAddress(IntPtr functionPointer)
        {
            var jmpSize = MemoryHelper.Read<ushort>(functionPointer, 1);
            var size = (jmpSize << 4) * 2;
            return functionPointer + size;
        }
    }
}